using Bunit;
using KutuphaneOdunc.Models;
using KutuphaneOdunc.Pages;
using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOdunc.Tests.Pages;

public class BookDetailPageTests : BunitContext
{
    private static List<Book> OrnekKitaplar() => new()
    {
        new() { Id = "B001", Title = "Yazılım Tasarımına Giriş", Author = "Ayşe Yılmaz", Category = "Bilgisayar Bilimleri", TotalCopies = 5, AvailableCopies = 3 },
        new() { Id = "B002", Title = "Araştırma Yöntemlerine Giriş", Author = "Murat Demir", Category = "Bilim", TotalCopies = 2, AvailableCopies = 0 }
    };

    private readonly FakeLocalStorage _storage = new();

    private void ServisleriKaydet(IBookService? bookService = null)
    {
        Services.AddSingleton(bookService ?? new FakeBookService(OrnekKitaplar()));
        Services.AddSingleton<ILocalStorageService>(_storage);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTime(2026, 11, 10)));
        Services.AddScoped<LibraryState>();
        Services.AddScoped<ProfileService>();
        Services.AddScoped<ToastService>();
    }

    // Sayfa açılmadan önce localStorage'a profil yazar; sayfa açılınca ProfileService bunu yükler.
    private void ProfilYaz(string adSoyad, string eposta) =>
        _storage.SetAsync(ProfileService.StorageKey, new UserProfile { FullName = adSoyad, Email = eposta }).Wait();

    private IRenderedComponent<BookDetail> SayfayiAc(string id)
    {
        var cut = Render<BookDetail>(p => p.Add(x => x.Id, id));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".loading")));
        return cut;
    }

    private static void FormuDoldurVeGonder(IRenderedComponent<BookDetail> cut)
    {
        cut.Find("#borrow-fullname").Change("Ayşe Yılmaz");
        cut.Find("#borrow-email").Change("ayse@ornek.com");
        cut.Find("form").Submit();
    }

    private static string MevcutKopya(IRenderedComponent<BookDetail> cut) =>
        cut.Find(".available").TextContent.Trim();

    [Fact]
    public void Kitap_bilgileri_gosterilmeli_ve_form_kapali_olmali()
    {
        ServisleriKaydet();

        var cut = SayfayiAc("B001");

        Assert.Equal("Yazılım Tasarımına Giriş", cut.Find("h1").TextContent);
        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public void Odunc_Al_a_tiklaninca_form_acilmali()
    {
        ServisleriKaydet();
        var cut = SayfayiAc("B001");

        cut.Find(".borrow-button").Click();

        Assert.NotNull(cut.Find(".borrow-form form"));
    }

    [Fact]
    public void Vazgec_formu_kapatmali()
    {
        ServisleriKaydet();
        var cut = SayfayiAc("B001");
        cut.Find(".borrow-button").Click();

        cut.Find("button.btn-outline-secondary").Click();

        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public void Odunc_alma_sonrasi_kopya_sayisi_ekranda_1_azalmali()
    {
        ServisleriKaydet();
        var cut = SayfayiAc("B001");
        Assert.Equal("3", MevcutKopya(cut));

        cut.Find(".borrow-button").Click();
        FormuDoldurVeGonder(cut);

        cut.WaitForAssertion(() => Assert.Equal("2", MevcutKopya(cut)));
    }

    [Fact]
    public void Basarili_odunc_sonrasi_kod_teslim_tarihi_ve_link_gorunmeli()
    {
        ServisleriKaydet();
        var cut = SayfayiAc("B001");

        cut.Find(".borrow-button").Click();
        FormuDoldurVeGonder(cut);

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".alert-success")));
        var bilgiler = cut.FindAll(".result-facts dd").Select(d => d.TextContent).ToList();
        Assert.Equal(new[] { "LIB-2026-101", "24.11.2026" }, bilgiler);
        Assert.Equal("my-loans", cut.Find(".alert-success a").GetAttribute("href"));
        Assert.Equal("Ödünç kayıtlarıma git", cut.Find(".alert-success a").TextContent);
        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public void Odunc_sonrasi_buton_pasif_olmali_ve_sebebi_yazmali()
    {
        ServisleriKaydet();
        var cut = SayfayiAc("B001");

        cut.Find(".borrow-button").Click();
        FormuDoldurVeGonder(cut);

        cut.WaitForAssertion(() => Assert.True(cut.Find(".borrow-button").HasAttribute("disabled")));
        Assert.Equal("Bu kitabı zaten ödünç aldınız.", cut.Find(".borrow-reason").TextContent);
    }

    [Fact]
    public void Odunc_reddedilirse_donen_mesaj_gorunmeli()
    {
        ServisleriKaydet();
        var cut = SayfayiAc("B001");
        cut.Find(".borrow-button").Click();

        // Form açıkken aynı kitap başka bir yerden ödünç alındı.
        var library = Services.GetRequiredService<LibraryState>();
        cut.InvokeAsync(() => library.BorrowAsync("B001", new BorrowFormModel { FullName = "Ali Kaya", Email = "ali@ornek.com" }));
        FormuDoldurVeGonder(cut);

        cut.WaitForAssertion(() =>
            Assert.Equal("Bu kitap için iade edilmemiş bir ödünç kaydınız var.", cut.Find(".alert-danger").TextContent.Trim()));
        Assert.Empty(cut.FindAll(".alert-success"));
        Assert.Single(library.Loans);
    }

    [Fact]
    public void Gecersiz_id_de_Kitap_bulunamadi_ve_liste_linki_gorunmeli()
    {
        ServisleriKaydet();

        var cut = SayfayiAc("B999");

        Assert.Equal("Kitap bulunamadı", cut.Find("h1").TextContent);
        Assert.Equal("books", cut.Find(".not-found a").GetAttribute("href"));
        Assert.Empty(cut.FindAll(".book-details"));
    }

    [Fact]
    public void Id_buyuk_kucuk_harf_farkiyla_da_bulunmali()
    {
        ServisleriKaydet();

        var cut = SayfayiAc("b001");

        Assert.Equal("Yazılım Tasarımına Giriş", cut.Find("h1").TextContent);
    }

    [Fact]
    public void Kitaplar_okunamazsa_hata_mesaji_gorunmeli()
    {
        ServisleriKaydet(new HataVerenBookService());

        var cut = SayfayiAc("B001");

        Assert.Contains("Kitap bilgileri yüklenemedi", cut.Find(".alert-danger").TextContent);
        Assert.Empty(cut.FindAll(".book-details"));
    }

    [Fact]
    public void Odunc_sonrasi_odak_sonuc_kutusuna_tasinmali()
    {
        ServisleriKaydet();
        var cut = SayfayiAc("B001");

        cut.Find(".borrow-button").Click();
        FormuDoldurVeGonder(cut);

        // FocusAsync'in hangi elemana çağrıldığı, elemanın bUnit'in verdiği referans kimliğiyle karşılaştırılıyor.
        cut.WaitForAssertion(() =>
        {
            var focus = JSInterop.VerifyFocusAsyncInvoke();
            var reference = (Microsoft.AspNetCore.Components.ElementReference)focus.Arguments[0]!;
            Assert.Equal(reference.Id, cut.Find(".result").GetAttribute("blazor:elementreference"));
        });
        Assert.Equal("-1", cut.Find(".result").GetAttribute("tabindex"));
    }

    // #41: profilden doldurma

    [Fact]
    public void Profil_yokken_not_ve_profil_linki_gorunmeli()
    {
        ServisleriKaydet();
        var cut = SayfayiAc("B001");

        cut.Find(".borrow-button").Click();

        Assert.Contains("Bilgilerinizin otomatik dolması için", cut.Find(".profile-hint").TextContent);
        Assert.Equal("profile", cut.Find(".profile-hint a").GetAttribute("href"));
        Assert.Equal("", cut.Find("#borrow-fullname").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Profil_varken_not_gorunmemeli_ve_form_profille_dolu_acilmali()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");
        ServisleriKaydet();
        var cut = SayfayiAc("B001");

        cut.Find(".borrow-button").Click();

        Assert.Empty(cut.FindAll(".profile-hint"));
        Assert.Equal("Ayşe Yılmaz", cut.Find("#borrow-fullname").GetAttribute("value"));
        Assert.Equal("ayse@ornek.com", cut.Find("#borrow-email").GetAttribute("value"));
    }

    [Fact]
    public void Profille_dolu_form_dogrudan_gonderilince_kayit_profil_bilgileriyle_olusmali()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");
        ServisleriKaydet();
        var cut = SayfayiAc("B001");

        cut.Find(".borrow-button").Click();
        cut.Find("form").Submit();

        var library = Services.GetRequiredService<LibraryState>();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".alert-success")));
        Assert.Equal("Ayşe Yılmaz", library.Loans.Single().BorrowerName);
        Assert.Equal("ayse@ornek.com", library.Loans.Single().Email);
    }

    [Fact]
    public void Odunc_alininca_basari_bildirimi_gosterilmeli()
    {
        ServisleriKaydet();
        var cut = SayfayiAc("B001");

        cut.Find(".borrow-button").Click();
        FormuDoldurVeGonder(cut);

        var toasts = Services.GetRequiredService<ToastService>();
        cut.WaitForAssertion(() => Assert.Single(toasts.Messages));
        Assert.Equal(ToastKind.Success, toasts.Messages[0].Kind);
        Assert.StartsWith("Kitap ödünç alındı. Ödünç kodunuz: LIB-2026-", toasts.Messages[0].Text);
        // Sayfa içindeki sonuç kutusu da yerinde duruyor.
        Assert.NotNull(cut.Find(".result .code"));
    }

    // #48: benzer kitaplar

    private static FakeBookService AyniKategoridenKitaplar() => new(new List<Book>
    {
        new() { Id = "B001", Title = "Yazılım Tasarımına Giriş", Category = "Bilgisayar Bilimleri", TotalCopies = 5, AvailableCopies = 3 },
        new() { Id = "B003", Title = "Veri Yapıları ve Algoritmalar", Category = "Bilgisayar Bilimleri", TotalCopies = 4, AvailableCopies = 4 },
        new() { Id = "B002", Title = "Araştırma Yöntemlerine Giriş", Category = "Bilim", TotalCopies = 2, AvailableCopies = 0 }
    });

    [Fact]
    public void Benzer_kitaplar_bolumunde_ayni_kategoriden_kitap_gorunmeli()
    {
        ServisleriKaydet(AyniKategoridenKitaplar());

        var cut = SayfayiAc("B001");

        var basliklar = cut.FindAll(".similar-books .book-card .title").Select(t => t.TextContent).ToList();
        Assert.Equal(new[] { "Veri Yapıları ve Algoritmalar" }, basliklar);
    }

    [Fact]
    public void Benzer_kitaba_tiklaninca_adres_ve_gosterilen_kitap_degismeli()
    {
        ServisleriKaydet(AyniKategoridenKitaplar());
        JSInterop.SetupVoid("scrollTo", _ => true).SetVoidResult();
        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var cut = SayfayiAc("B001");

        cut.Find(".similar-books .book-card").Click();

        Assert.EndsWith("/books/B003", navigation.Uri);

        // Gerçek uygulamada Router bu adres için aynı sayfa bileşenine yeni Id'yi veriyor; testte bunu biz yapıyoruz.
        cut.Render(p => p.Add(x => x.Id, "B003"));

        Assert.Equal("Veri Yapıları ve Algoritmalar", cut.Find("h1").TextContent);
        Assert.Equal("4", MevcutKopya(cut));
        Assert.Equal(new[] { "Yazılım Tasarımına Giriş" },
            cut.FindAll(".similar-books .book-card .title").Select(t => t.TextContent));
    }

    [Fact]
    public void Baska_kitaba_gecince_acik_form_ve_sonuc_sifirlanmali()
    {
        ServisleriKaydet(AyniKategoridenKitaplar());
        JSInterop.SetupVoid("scrollTo", _ => true).SetVoidResult();
        var cut = SayfayiAc("B001");
        cut.Find(".borrow-button").Click();
        Assert.NotEmpty(cut.FindAll("form"));

        cut.Render(p => p.Add(x => x.Id, "B003"));

        Assert.Empty(cut.FindAll("form"));
        Assert.Empty(cut.FindAll(".result"));
    }

    [Fact]
    public void Baska_kitaba_gecince_sayfa_en_uste_kaymali_ve_odak_basliga_gitmeli()
    {
        ServisleriKaydet(AyniKategoridenKitaplar());
        JSInterop.SetupVoid("scrollTo", _ => true).SetVoidResult();
        var cut = SayfayiAc("B001");

        cut.Render(p => p.Add(x => x.Id, "B003"));

        cut.WaitForAssertion(() =>
        {
            var kaydirma = JSInterop.VerifyInvoke("scrollTo");
            Assert.Equal(new object?[] { 0, 0 }, kaydirma.Arguments);
            // Başlık BookDetails'in içinde; bUnit alt bileşendeki elemanın kimliğini HTML'e yazmadığı için
            // odak çağrısının yapıldığı ve başlığın odaklanabilir olduğu kontrol ediliyor (tarayıcıda da denendi).
            JSInterop.VerifyFocusAsyncInvoke();
        });
        Assert.Equal("-1", cut.Find("h1").GetAttribute("tabindex"));
        Assert.Equal("Veri Yapıları ve Algoritmalar", cut.Find("h1").TextContent);
    }

    [Fact]
    public void Ilk_acilista_sayfa_kaydirilmamali()
    {
        ServisleriKaydet(AyniKategoridenKitaplar());

        // scrollTo ayarlanmadı: bUnit'in katı modunda çağrılsaydı test hata verirdi.
        var cut = SayfayiAc("B001");

        Assert.Empty(JSInterop.Invocations["scrollTo"]);
        Assert.Equal("Yazılım Tasarımına Giriş", cut.Find("h1").TextContent);
    }

    private sealed class HataVerenBookService : IBookService
    {
        public Task<List<Book>> GetBooksAsync() =>
            Task.FromException<List<Book>>(new HttpRequestException("books.json okunamadı"));
    }
}
