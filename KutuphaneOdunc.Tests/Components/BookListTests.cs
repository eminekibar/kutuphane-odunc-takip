using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Components;

public class BookListTests : BunitContext
{
    private static List<Book> OrnekKitaplar() => new()
    {
        new() { Id = "B001", Title = "Yazılım Tasarımına Giriş", Author = "Ayşe Yılmaz", Category = "Bilgisayar Bilimleri", PublicationYear = 2022, TotalCopies = 5, AvailableCopies = 3 },
        new() { Id = "B004", Title = "Web Uygulamalarında Bileşen Mimarisi", Author = "Ayşe Yılmaz", Category = "Bilgisayar Bilimleri", PublicationYear = 2024, TotalCopies = 3, AvailableCopies = 2 },
        new() { Id = "B010", Title = "İstanbul'da Son Vapur", Author = "Oğuz Kayacan", Category = "Edebiyat", PublicationYear = 2021, TotalCopies = 3, AvailableCopies = 3 },
        new() { Id = "B012", Title = "Sessiz Kasaba", Author = "Oğuz Kayacan", Category = "Edebiyat", PublicationYear = 2016, TotalCopies = 1, AvailableCopies = 0 }
    };

    private IRenderedComponent<BookList> RenderList(Action<Book>? onSelected = null) =>
        Render<BookList>(p => p
            .Add(x => x.Books, OrnekKitaplar())
            .Add(x => x.OnBookSelected, onSelected ?? new Action<Book>(_ => { })));

    private static List<string> KartBasliklari(IRenderedComponent<BookList> cut) =>
        cut.FindAll(".book-card .title").Select(e => e.TextContent).ToList();

    [Fact]
    public void Baslangicta_butun_kitaplar_listelenmeli()
    {
        var cut = RenderList();

        Assert.Equal(4, cut.FindAll(".book-card").Count);
        Assert.Contains("4 kitap listeleniyor", cut.Find(".result-count").TextContent);
    }

    [Fact]
    public void Kategori_listesi_kitaplardaki_kategorilerden_olusmali()
    {
        var cut = RenderList();

        var secenekler = cut.FindAll("#book-category option").Select(o => o.TextContent).ToList();

        Assert.Equal(new[] { "Tüm kategoriler", "Bilgisayar Bilimleri", "Edebiyat" }, secenekler);
    }

    [Fact]
    public void Kitap_adina_gore_arama_yapilabilmeli()
    {
        var cut = RenderList();

        cut.Find("#book-search").Input("vapur");

        Assert.Equal(new[] { "İstanbul'da Son Vapur" }, KartBasliklari(cut));
    }

    [Fact]
    public void Yazara_gore_arama_yapilabilmeli()
    {
        var cut = RenderList();

        cut.Find("#book-search").Input("kayacan");

        Assert.Equal(new[] { "İstanbul'da Son Vapur", "Sessiz Kasaba" }, KartBasliklari(cut));
    }

    [Fact]
    public void Aramada_turkce_buyuk_kucuk_harf_dikkate_alinmamali()
    {
        var cut = RenderList();

        cut.Find("#book-search").Input("istanbul");

        Assert.Equal(new[] { "İstanbul'da Son Vapur" }, KartBasliklari(cut));
    }

    [Fact]
    public void Kategoriye_gore_filtrelenebilmeli()
    {
        var cut = RenderList();

        cut.Find("#book-category").Change("Edebiyat");

        Assert.Equal(new[] { "İstanbul'da Son Vapur", "Sessiz Kasaba" }, KartBasliklari(cut));
    }

    [Fact]
    public void Arama_ve_kategori_birlikte_calismali()
    {
        var cut = RenderList();

        cut.Find("#book-category").Change("Bilgisayar Bilimleri");
        cut.Find("#book-search").Input("web");

        Assert.Equal(new[] { "Web Uygulamalarında Bileşen Mimarisi" }, KartBasliklari(cut));
    }

    [Fact]
    public void Sonuc_yoksa_kitap_bulunamadi_yazmali()
    {
        var cut = RenderList();

        cut.Find("#book-search").Input("olmayan kitap");

        Assert.Empty(cut.FindAll(".book-card"));
        Assert.Contains("Kitap bulunamadı", cut.Find(".empty").TextContent);
        Assert.Contains("0 kitap listeleniyor", cut.Find(".result-count").TextContent);
    }

    [Fact]
    public void Secilen_kitap_OnBookSelected_ile_bildirilmeli()
    {
        Book? secilen = null;
        var cut = RenderList(b => secilen = b);

        // Liste ada göre sıralı geldiği için kart sırasıyla değil adıyla bulunuyor (#49).
        cut.FindAll(".book-card").Single(k => k.QuerySelector(".title")!.TextContent == "İstanbul'da Son Vapur").Click();

        Assert.Equal("B010", secilen?.Id);
    }

    // Sıralama ve "Sadece ödünç alınabilir olanlar" (#49)

    private IRenderedComponent<BookList> RenderList(List<Book> kitaplar) =>
        Render<BookList>(p => p.Add(x => x.Books, kitaplar));

    private static Book Kitap(string baslik) => new() { Id = baslik, Title = baslik, Author = "Yazar", Category = "Kategori", TotalCopies = 1, AvailableCopies = 1 };

    [Fact]
    public void Varsayilan_siralama_ada_gore_olmali()
    {
        var cut = RenderList();

        Assert.Equal(new[] { "İstanbul'da Son Vapur", "Sessiz Kasaba", "Web Uygulamalarında Bileşen Mimarisi", "Yazılım Tasarımına Giriş" },
            KartBasliklari(cut));
    }

    [Theory]
    [InlineData("title", "İstanbul'da Son Vapur", "Sessiz Kasaba", "Web Uygulamalarında Bileşen Mimarisi", "Yazılım Tasarımına Giriş")]
    // Aynı yazarın kitapları kendi aralarında ada göre.
    [InlineData("author", "Web Uygulamalarında Bileşen Mimarisi", "Yazılım Tasarımına Giriş", "İstanbul'da Son Vapur", "Sessiz Kasaba")]
    [InlineData("year", "Web Uygulamalarında Bileşen Mimarisi", "Yazılım Tasarımına Giriş", "İstanbul'da Son Vapur", "Sessiz Kasaba")]
    // İstanbul'da ve Yazılım'ın ikisinde de 3 kopya var: eşitlikte ada göre.
    [InlineData("available", "İstanbul'da Son Vapur", "Yazılım Tasarımına Giriş", "Web Uygulamalarında Bileşen Mimarisi", "Sessiz Kasaba")]
    public void Her_siralama_secenegi_dogru_sirayi_vermeli(string secenek, params string[] beklenen)
    {
        var cut = RenderList();

        cut.Find("#book-sort").Change(secenek);

        Assert.Equal(beklenen, KartBasliklari(cut));
    }

    [Fact]
    public void Turkce_alfabe_sirasi_uygulanmali()
    {
        var cut = RenderList(new List<Book> { Kitap("Çiçek"), Kitap("Cam"), Kitap("Şeker"), Kitap("Su") });

        Assert.Equal(new[] { "Cam", "Çiçek", "Su", "Şeker" }, KartBasliklari(cut));
    }

    [Fact]
    public void Noktasiz_I_noktali_I_dan_once_gelmeli()
    {
        var cut = RenderList(new List<Book> { Kitap("İnci"), Kitap("Irmak"), Kitap("Ilık") });

        Assert.Equal(new[] { "Ilık", "Irmak", "İnci" }, KartBasliklari(cut));
    }

    [Fact]
    public void Sadece_odunc_alinabilir_olanlar_stokta_olmayanlari_gizlemeli()
    {
        var cut = RenderList();

        cut.Find("#book-available-only").Change(true);

        Assert.DoesNotContain("Sessiz Kasaba", KartBasliklari(cut));
        Assert.Equal(3, cut.FindAll(".book-card").Count);
        Assert.Contains("3 kitap listeleniyor", cut.Find(".result-count").TextContent);

        cut.Find("#book-available-only").Change(false);

        Assert.Equal(4, cut.FindAll(".book-card").Count);
    }

    [Fact]
    public void Arama_kategori_siralama_ve_onay_kutusu_birlikte_calismali()
    {
        var cut = RenderList();

        cut.Find("#book-category").Change("Edebiyat");
        cut.Find("#book-sort").Change("year");
        Assert.Equal(new[] { "İstanbul'da Son Vapur", "Sessiz Kasaba" }, KartBasliklari(cut));

        cut.Find("#book-available-only").Change(true);
        Assert.Equal(new[] { "İstanbul'da Son Vapur" }, KartBasliklari(cut));
        Assert.Contains("1 kitap listeleniyor", cut.Find(".result-count").TextContent);

        cut.Find("#book-category").Change("");
        cut.Find("#book-search").Input("ayşe");
        Assert.Equal(new[] { "Web Uygulamalarında Bileşen Mimarisi", "Yazılım Tasarımına Giriş" }, KartBasliklari(cut));
    }

    [Theory]
    [InlineData("book-sort", "Sıralama")]
    [InlineData("book-available-only", "Sadece ödünç alınabilir olanlar")]
    public void Yeni_alanlarin_etiketi_olmali(string id, string etiket)
    {
        var cut = RenderList();

        Assert.Equal(etiket, cut.Find($"label[for='{id}']").TextContent);
    }
}
