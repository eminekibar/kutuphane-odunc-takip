using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Components;

public class SimilarBooksTests : BunitContext
{
    private static Book Kitap(string id, string kategori, int mevcut = 1) => new()
    {
        Id = id,
        Title = $"Kitap {id}",
        Author = "Yazar",
        Category = kategori,
        CoverUrl = $"images/{id}.svg",
        TotalCopies = 2,
        AvailableCopies = mevcut
    };

    private static readonly List<Book> Kitaplar = new()
    {
        Kitap("B001", "Bilim"),
        Kitap("B002", "Bilim", mevcut: 0),
        Kitap("B003", "Tarih"),
        Kitap("B004", "Bilim"),
        Kitap("B005", "Bilim"),
        Kitap("B006", "Bilim"),
        Kitap("B007", "Bilim")
    };

    private IRenderedComponent<SimilarBooks> Ciz(Book gosterilen, IReadOnlyList<Book>? kitaplar = null, Action<Book>? onSelect = null) =>
        Render<SimilarBooks>(p => p
            .Add(x => x.Current, gosterilen)
            .Add(x => x.Books, kitaplar ?? Kitaplar)
            .Add(x => x.OnSelect, onSelect ?? (_ => { })));

    private static List<string> Basliklar(IRenderedComponent<SimilarBooks> cut) =>
        cut.FindAll(".book-card .title").Select(t => t.TextContent).ToList();

    [Fact]
    public void Sadece_ayni_kategoriden_gosterilen_kitap_haric_en_fazla_4_kitap_olmali()
    {
        var cut = Ciz(Kitaplar[0]);

        var basliklar = Basliklar(cut);
        Assert.Equal(4, basliklar.Count);
        Assert.DoesNotContain("Kitap B001", basliklar); // gösterilen kitap
        Assert.DoesNotContain("Kitap B003", basliklar); // başka kategori
    }

    [Fact]
    public void Odunc_alinabilir_kitaplar_once_gelmeli()
    {
        var cut = Ciz(Kitaplar[0]);

        // B002'nin kopyası yok: önce B004–B007 geliyor, 4 sınırı yüzünden B002 hiç görünmüyor.
        Assert.Equal(new[] { "Kitap B004", "Kitap B005", "Kitap B006", "Kitap B007" }, Basliklar(cut));
    }

    [Fact]
    public void Mevcut_olmayan_kitap_yer_kalirsa_en_sonda_gorunmeli()
    {
        var kitaplar = new List<Book> { Kitap("B001", "Bilim"), Kitap("B002", "Bilim", mevcut: 0), Kitap("B004", "Bilim") };

        var cut = Ciz(kitaplar[0], kitaplar);

        Assert.Equal(new[] { "Kitap B004", "Kitap B002" }, Basliklar(cut));
    }

    [Fact]
    public void Kategoride_baska_kitap_yoksa_bolum_olmamali()
    {
        var cut = Ciz(Kitaplar[2]); // Tarih kategorisinde tek kitap

        Assert.Empty(cut.FindAll(".similar-books"));
        Assert.Equal("", cut.Markup.Trim());
    }

    [Fact]
    public void Baslik_gorunmeli()
    {
        var cut = Ciz(Kitaplar[0]);

        Assert.Equal("Bu kategoriden diğer kitaplar", cut.Find("h2").TextContent);
    }

    [Fact]
    public void Karta_tiklaninca_OnSelect_o_kitapla_cagrilmali()
    {
        Book? secilen = null;
        var cut = Ciz(Kitaplar[0], onSelect: k => secilen = k);

        cut.FindAll(".book-card")[1].Click();

        Assert.Equal("B005", secilen?.Id);
    }
}
