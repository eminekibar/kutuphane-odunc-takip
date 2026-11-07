using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Components;

public class BookListTests : BunitContext
{
    private static List<Book> OrnekKitaplar() => new()
    {
        new() { Id = "B001", Title = "Yazılım Tasarımına Giriş", Author = "Ayşe Yılmaz", Category = "Bilgisayar Bilimleri", TotalCopies = 5, AvailableCopies = 3 },
        new() { Id = "B004", Title = "Web Uygulamalarında Bileşen Mimarisi", Author = "Ayşe Yılmaz", Category = "Bilgisayar Bilimleri", TotalCopies = 3, AvailableCopies = 2 },
        new() { Id = "B010", Title = "İstanbul'da Son Vapur", Author = "Oğuz Kayacan", Category = "Edebiyat", TotalCopies = 3, AvailableCopies = 3 },
        new() { Id = "B012", Title = "Sessiz Kasaba", Author = "Oğuz Kayacan", Category = "Edebiyat", TotalCopies = 1, AvailableCopies = 0 }
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

        cut.FindAll(".book-card")[2].Click();

        Assert.Equal("B010", secilen?.Id);
    }
}
