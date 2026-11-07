using System.Reflection;
using Bunit;
using KutuphaneOdunc.Models;
using KutuphaneOdunc.Pages;
using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOdunc.Tests.Pages;

public class MyLoansPageTests : BunitContext
{
    private static readonly DateTime Bugun = new(2026, 11, 10);

    private readonly FakeLocalStorage _storage = new();
    private readonly FixedTimeProvider _saat = new(Bugun);

    private static List<Book> OrnekKitaplar() => new()
    {
        new() { Id = "B001", Title = "Yazılım Tasarımına Giriş", TotalCopies = 5, AvailableCopies = 3 },
        new() { Id = "B002", Title = "Araştırma Yöntemlerine Giriş", TotalCopies = 2, AvailableCopies = 1 },
        new() { Id = "B003", Title = "Veri Yapıları ve Algoritmalar", TotalCopies = 1, AvailableCopies = 1 }
    };

    public MyLoansPageTests()
    {
        Services.AddSingleton<IBookService>(new FakeBookService(OrnekKitaplar()));
        Services.AddSingleton<ILocalStorageService>(_storage);
        Services.AddSingleton<TimeProvider>(_saat);
        Services.AddScoped<LibraryState>();
        Services.AddScoped<ProfileService>();
        Services.AddScoped<ToastService>();
    }

    private LibraryState Library => Services.GetRequiredService<LibraryState>();

    // Sayfa açılmadan önce localStorage'a kayıt yazar; sayfa bunları yükleyecek.
    private void KayitlariYaz(params Loan[] kayitlar) =>
        _storage.SetAsync(LibraryState.LoansKey, kayitlar.ToList()).Wait();

    private void ProfilYaz(string adSoyad, string eposta) =>
        _storage.SetAsync(ProfileService.StorageKey, new UserProfile { FullName = adSoyad, Email = eposta }).Wait();

    private static Loan Kayit(string kod, string kitapId, DateTime teslim, DateTime? iade = null,
        string eposta = "ayse@ornek.com") => new()
    {
        Id = kod,
        BookId = kitapId,
        BorrowerName = "Ayşe Yılmaz",
        Email = eposta,
        BorrowDate = teslim.AddDays(-14),
        DueDate = teslim,
        ReturnedAt = iade
    };

    private IRenderedComponent<MyLoans> SayfayiAc()
    {
        var cut = Render<MyLoans>();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".loading")));
        return cut;
    }

    private static List<string> Basliklar(IRenderedComponent<MyLoans> cut, string bolum) =>
        cut.FindAll($".{bolum} .loan-status h3").Select(h => h.TextContent).ToList();

    [Fact]
    public void Kayit_yoksa_bos_mesaj_ve_kitaplar_linki_gorunmeli()
    {
        var cut = SayfayiAc();

        Assert.Contains("Henüz ödünç kaydınız yok.", cut.Find(".empty").TextContent);
        Assert.Equal("books", cut.Find(".empty a").GetAttribute("href"));
        Assert.Empty(cut.FindAll(".loan-status"));
    }

    [Fact]
    public void Aktif_ve_gecikmis_kayitlar_ustte_iade_edilenler_Gecmiste_listelenmeli()
    {
        KayitlariYaz(
            Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)),
            Kayit("LIB-2026-102", "B002", teslim: Bugun.AddDays(-2)),
            Kayit("LIB-2026-103", "B003", teslim: Bugun.AddDays(-4), iade: Bugun.AddDays(-6)));

        var cut = SayfayiAc();

        // Güncel kayıtlar teslim tarihine göre: gecikmiş olan önce.
        Assert.Equal(new[] { "Araştırma Yöntemlerine Giriş", "Yazılım Tasarımına Giriş" }, Basliklar(cut, "current"));
        Assert.Equal(new[] { "Veri Yapıları ve Algoritmalar" }, Basliklar(cut, "history"));
        Assert.Equal("Geçmiş", cut.Find(".history h2").TextContent);
    }

    [Fact]
    public void Iade_edilmis_kayit_yoksa_Gecmis_bolumu_gorunmemeli()
    {
        KayitlariYaz(Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)));

        var cut = SayfayiAc();

        Assert.Empty(cut.FindAll(".history"));
    }

    // Özet kutuları

    private static (string Aktif, string Gecikmis, string Iade) Ozet(IRenderedComponent<MyLoans> cut) =>
        (cut.Find(".loan-summary .active .count").TextContent,
         cut.Find(".loan-summary .overdue .count").TextContent,
         cut.Find(".loan-summary .returned .count").TextContent);

    [Fact]
    public void Kayit_yokken_ozet_gorunmemeli()
    {
        var cut = SayfayiAc();

        Assert.Empty(cut.FindAll(".loan-summary"));
    }

    [Fact]
    public void Ozet_kayit_sayilarini_gostermeli_ve_iadeden_sonra_guncellenmeli()
    {
        KayitlariYaz(
            Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)),
            Kayit("LIB-2026-102", "B002", teslim: Bugun.AddDays(-2)));
        var cut = SayfayiAc();

        Assert.Equal(("1", "1", "0"), Ozet(cut));
        Assert.Contains("highlight", cut.Find(".loan-summary .overdue").ClassList);

        // Güncel kayıtların ilki gecikmiş olan B002.
        cut.Find(".current .return-button").Click();

        cut.WaitForAssertion(() => Assert.Equal(("1", "0", "1"), Ozet(cut)));
        Assert.DoesNotContain("highlight", cut.Find(".loan-summary .overdue").ClassList);
    }

    [Fact]
    public void Ozet_filtre_uygulanmis_listeyi_saymali()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");
        KayitlariYaz(
            Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)),
            Kayit("LIB-2026-102", "B002", teslim: Bugun.AddDays(-2), eposta: "mehmet@ornek.com"));
        var cut = SayfayiAc();
        Assert.Equal(("1", "1", "0"), Ozet(cut));

        cut.Find(".filter-mine").Click();

        Assert.Equal(("1", "0", "0"), Ozet(cut));
    }

    [Fact]
    public void Iade_sonrasi_kayit_Gecmis_bolumune_gecmeli()
    {
        KayitlariYaz(Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)));
        var cut = SayfayiAc();

        cut.Find(".current .return-button").Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(new[] { "Yazılım Tasarımına Giriş" }, Basliklar(cut, "history")));
        Assert.Empty(Basliklar(cut, "current"));
        Assert.Equal("İade edildi", cut.Find(".history .badge-state").TextContent);
        Assert.Equal("Kitap iade edildi.", cut.Find(".feedback").TextContent.Trim());
        Assert.Contains("alert-success", cut.Find(".feedback").ClassList);
    }

    [Fact]
    public void Uzatma_sonrasi_basari_mesaji_ve_Uzatildi_etiketi_gorunmeli()
    {
        KayitlariYaz(Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)));
        var cut = SayfayiAc();

        cut.Find(".extend-button").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".badge-extended")));
        Assert.Contains("22.11.2026", cut.Find(".feedback").TextContent);
        Assert.True(cut.Find(".extend-button").HasAttribute("disabled"));
    }

    [Fact]
    public void Islem_reddedilirse_hata_mesaji_gorunmeli()
    {
        // Sayfa açıkken gün değişti ve kaydın süresi geçti; buton henüz yeniden çizilmedi.
        KayitlariYaz(Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)));
        var cut = SayfayiAc();
        _saat.Advance(TimeSpan.FromDays(6));

        cut.Find(".extend-button").Click();

        cut.WaitForAssertion(() => Assert.Contains("alert-danger", cut.Find(".feedback").ClassList));
        Assert.Equal("Teslim tarihi geçmiş bir kaydın süresi uzatılamaz.", cut.Find(".feedback").TextContent.Trim());
        Assert.Equal("alert", cut.Find(".feedback").GetAttribute("role"));
    }

    [Fact]
    public void Baska_yerde_yapilan_degisiklik_sayfaya_yansimali()
    {
        var cut = SayfayiAc();
        Assert.NotNull(cut.Find(".empty"));

        // Örneğin detay sayfasında ödünç alındı: OnChange tetiklenir, sayfa kendini yeniden çizer.
        cut.InvokeAsync(() => Library.BorrowAsync("B001", new BorrowFormModel { FullName = "Ayşe Yılmaz", Email = "ayse@ornek.com" }));

        cut.WaitForAssertion(() =>
            Assert.Equal(new[] { "Yazılım Tasarımına Giriş" }, Basliklar(cut, "current")));
    }

    [Fact]
    public void Iade_sonrasi_odak_mesaj_kutusuna_tasinmali()
    {
        KayitlariYaz(Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)));
        var cut = SayfayiAc();

        cut.Find(".current .return-button").Click();

        cut.WaitForAssertion(() =>
        {
            var focus = JSInterop.VerifyFocusAsyncInvoke();
            var reference = (Microsoft.AspNetCore.Components.ElementReference)focus.Arguments[0]!;
            Assert.Equal(reference.Id, cut.Find(".feedback").GetAttribute("blazor:elementreference"));
        });
    }

    // "Sadece benim kayıtlarım" filtresi

    [Fact]
    public void Profil_yoksa_filtre_gorunmemeli()
    {
        KayitlariYaz(Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)));

        var cut = SayfayiAc();

        Assert.Empty(cut.FindAll(".filter"));
    }

    [Fact]
    public void Profil_varsa_filtre_gorunmeli_ve_Tum_kayitlar_secili_olmali()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");
        KayitlariYaz(
            Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)),
            Kayit("LIB-2026-102", "B002", teslim: Bugun.AddDays(3), eposta: "mehmet@ornek.com"));

        var cut = SayfayiAc();

        Assert.Equal("true", cut.Find(".filter-all").GetAttribute("aria-pressed"));
        Assert.Equal("false", cut.Find(".filter-mine").GetAttribute("aria-pressed"));
        Assert.Equal(2, cut.FindAll(".loan-status").Count);
    }

    [Fact]
    public void Sadece_benim_kayitlarim_profil_e_postasina_gore_suzmeli()
    {
        // Profildeki e-posta büyük harfle yazılmış; kayıtlardakiyle yine eşleşmeli.
        ProfilYaz("Ayşe Yılmaz", "AYSE@ornek.com");
        KayitlariYaz(
            Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)),
            Kayit("LIB-2026-102", "B002", teslim: Bugun.AddDays(3), eposta: "mehmet@ornek.com"),
            Kayit("LIB-2026-103", "B003", teslim: Bugun.AddDays(-4), iade: Bugun.AddDays(-6), eposta: "Ayse@Ornek.com"));
        var cut = SayfayiAc();

        cut.Find(".filter-mine").Click();

        Assert.Equal(new[] { "Yazılım Tasarımına Giriş" }, Basliklar(cut, "current"));
        Assert.Equal(new[] { "Veri Yapıları ve Algoritmalar" }, Basliklar(cut, "history"));
        Assert.Equal("true", cut.Find(".filter-mine").GetAttribute("aria-pressed"));

        cut.Find(".filter-all").Click();

        Assert.Equal(new[] { "Araştırma Yöntemlerine Giriş", "Yazılım Tasarımına Giriş" }, Basliklar(cut, "current"));
    }

    [Fact]
    public void Eslesen_kayit_yoksa_mesaj_gorunmeli()
    {
        ProfilYaz("Zeynep Kaya", "zeynep@ornek.com");
        KayitlariYaz(Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)));
        var cut = SayfayiAc();

        cut.Find(".filter-mine").Click();

        Assert.Equal("Bu e-postayla alınmış kayıt yok.", cut.Find(".filter-empty").TextContent);
        Assert.Empty(cut.FindAll(".loan-status"));
        Assert.NotNull(cut.Find(".filter"));
    }

    // Bildirimler

    private ToastService Toasts => Services.GetRequiredService<ToastService>();

    [Fact]
    public void Basarili_iadeden_sonra_basari_bildirimi_gosterilmeli()
    {
        KayitlariYaz(Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)));
        var cut = SayfayiAc();

        cut.Find(".current .return-button").Click();

        cut.WaitForAssertion(() => Assert.Single(Toasts.Messages));
        Assert.Equal(ToastKind.Success, Toasts.Messages[0].Kind);
        Assert.Equal("Kitap iade edildi.", Toasts.Messages[0].Text);
    }

    [Fact]
    public void Reddedilen_islemde_hata_bildirimi_gosterilmeli()
    {
        KayitlariYaz(Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)));
        var cut = SayfayiAc();
        _saat.Advance(TimeSpan.FromDays(6));

        cut.Find(".extend-button").Click();

        cut.WaitForAssertion(() => Assert.Single(Toasts.Messages));
        Assert.Equal(ToastKind.Error, Toasts.Messages[0].Kind);
        Assert.Equal("Teslim tarihi geçmiş bir kaydın süresi uzatılamaz.", Toasts.Messages[0].Text);
    }

    [Fact]
    public async Task Sayfa_kapaninca_OnChange_aboneligi_birakilmali()
    {
        var cut = SayfayiAc();
        Assert.NotNull(AboneListesi());

        await DisposeComponentsAsync();

        Assert.Null(AboneListesi());
    }

    // OnChange event'inin arkasındaki alan: abone yoksa null.
    private Delegate? AboneListesi() =>
        (Delegate?)typeof(LibraryState)
            .GetField(nameof(LibraryState.OnChange), BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(Library);
}
