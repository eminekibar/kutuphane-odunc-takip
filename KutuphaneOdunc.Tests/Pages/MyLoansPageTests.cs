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
    }

    private LibraryState Library => Services.GetRequiredService<LibraryState>();

    // Sayfa açılmadan önce localStorage'a kayıt yazar; sayfa bunları yükleyecek.
    private void KayitlariYaz(params Loan[] kayitlar) =>
        _storage.SetAsync(LibraryState.LoansKey, kayitlar.ToList()).Wait();

    private static Loan Kayit(string kod, string kitapId, DateTime teslim, DateTime? iade = null) => new()
    {
        Id = kod,
        BookId = kitapId,
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
