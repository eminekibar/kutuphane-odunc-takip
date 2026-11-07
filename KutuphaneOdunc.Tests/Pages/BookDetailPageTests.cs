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

    private void ServisleriKaydet(IBookService? bookService = null)
    {
        Services.AddSingleton(bookService ?? new FakeBookService(OrnekKitaplar()));
        Services.AddSingleton<ILocalStorageService>(new FakeLocalStorage());
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTime(2026, 11, 10)));
        Services.AddScoped<LibraryState>();
    }

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

        Assert.NotNull(cut.Find("form.borrow-form"));
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

    private sealed class HataVerenBookService : IBookService
    {
        public Task<List<Book>> GetBooksAsync() =>
            Task.FromException<List<Book>>(new HttpRequestException("books.json okunamadı"));
    }
}
