using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Components;

public class BookCardTests : BunitContext
{
    private static Book OrnekKitap(int mevcutKopya = 3, string? kapak = "images/B001.svg") => new()
    {
        Id = "B001",
        Title = "Yazılım Tasarımına Giriş",
        Author = "Ayşe Yılmaz",
        Category = "Bilgisayar Bilimleri",
        Publisher = "Örnek Yayınları",
        PublicationYear = 2022,
        CoverUrl = kapak,
        TotalCopies = 5,
        AvailableCopies = mevcutKopya
    };

    [Fact]
    public void Kart_kitabin_bilgilerini_gostermeli()
    {
        var cut = Render<BookCard>(p => p.Add(x => x.Book, OrnekKitap()));

        Assert.Equal("Yazılım Tasarımına Giriş", cut.Find(".title").TextContent);
        Assert.Equal("Ayşe Yılmaz", cut.Find(".author").TextContent);
        Assert.Equal("Bilgisayar Bilimleri", cut.Find(".category").TextContent);
        Assert.Contains("Mevcut kopya: 3", cut.Find(".stock").TextContent);

        var img = cut.Find("img");
        Assert.Equal("images/B001.svg", img.GetAttribute("src"));
        Assert.Equal("Yazılım Tasarımına Giriş kitabının kapağı", img.GetAttribute("alt"));
    }

    [Fact]
    public void Kopya_sifirsa_stokta_yok_etiketi_gorunmeli()
    {
        var cut = Render<BookCard>(p => p.Add(x => x.Book, OrnekKitap(mevcutKopya: 0)));

        Assert.Equal("Stokta yok", cut.Find(".out-of-stock").TextContent);
    }

    [Fact]
    public void Kopya_varsa_stokta_yok_etiketi_gorunmemeli()
    {
        var cut = Render<BookCard>(p => p.Add(x => x.Book, OrnekKitap(mevcutKopya: 1)));

        Assert.Empty(cut.FindAll(".out-of-stock"));
    }

    [Fact]
    public void Karta_tiklayinca_OnSelect_kitapla_cagrilmali()
    {
        var kitap = OrnekKitap();
        Book? secilen = null;
        var cut = Render<BookCard>(p => p
            .Add(x => x.Book, kitap)
            .Add(x => x.OnSelect, (Book b) => secilen = b));

        cut.Find("button").Click();

        Assert.Same(kitap, secilen);
    }

    [Fact]
    public void Gorsel_yuklenemezse_varsayilan_kapak_gosterilmeli()
    {
        var cut = Render<BookCard>(p => p.Add(x => x.Book, OrnekKitap()));

        cut.Find("img").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        Assert.Equal("images/default.svg", cut.Find("img").GetAttribute("src"));
    }

    [Fact]
    public void Kapak_adresi_bossa_varsayilan_kapak_gosterilmeli()
    {
        var cut = Render<BookCard>(p => p.Add(x => x.Book, OrnekKitap(kapak: null)));

        Assert.Equal("images/default.svg", cut.Find("img").GetAttribute("src"));
    }
}
