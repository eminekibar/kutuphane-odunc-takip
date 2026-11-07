using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Components;

public class BookDetailsTests : BunitContext
{
    private static Book OrnekKitap(int mevcutKopya = 3) => new()
    {
        Id = "B001",
        Title = "Yazılım Tasarımına Giriş",
        Author = "Ayşe Yılmaz",
        Category = "Bilgisayar Bilimleri",
        Publisher = "Örnek Yayınları",
        PublicationYear = 2022,
        CoverUrl = "images/B001.svg",
        TotalCopies = 5,
        AvailableCopies = mevcutKopya
    };

    private static List<string> Bilgiler(IRenderedComponent<BookDetails> cut) =>
        cut.FindAll(".facts dd").Select(d => d.TextContent.Trim()).ToList();

    [Fact]
    public void Kitap_bilgileri_gosterilmeli()
    {
        var cut = Render<BookDetails>(p => p.Add(x => x.Book, OrnekKitap()));

        Assert.Equal("Yazılım Tasarımına Giriş", cut.Find("h1").TextContent);
        Assert.Equal("Ayşe Yılmaz", cut.Find(".author").TextContent);
        Assert.Equal(new[] { "Örnek Yayınları", "2022", "5", "3" }, Bilgiler(cut));
    }

    [Fact]
    public void Normal_durumda_buton_aktif_olmali_ve_mesaj_olmamali()
    {
        var cut = Render<BookDetails>(p => p.Add(x => x.Book, OrnekKitap()));

        Assert.False(cut.Find("button").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".borrow-reason"));
    }

    [Fact]
    public void Kopya_sifirsa_buton_pasif_olmali_ve_sebebi_yazmali()
    {
        var cut = Render<BookDetails>(p => p.Add(x => x.Book, OrnekKitap(mevcutKopya: 0)));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
        Assert.Equal("Şu an ödünç alınabilir kopya yok.", cut.Find(".borrow-reason").TextContent);
    }

    [Fact]
    public void Aktif_kayit_varsa_buton_pasif_olmali_ve_sebebi_yazmali()
    {
        var cut = Render<BookDetails>(p => p
            .Add(x => x.Book, OrnekKitap())
            .Add(x => x.HasActiveLoan, true));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
        Assert.Equal("Bu kitabı zaten ödünç aldınız.", cut.Find(".borrow-reason").TextContent);
    }

    [Fact]
    public void Hem_kopya_yoksa_hem_aktif_kayit_varsa_aktif_kayit_mesaji_gorunmeli()
    {
        var cut = Render<BookDetails>(p => p
            .Add(x => x.Book, OrnekKitap(mevcutKopya: 0))
            .Add(x => x.HasActiveLoan, true));

        Assert.Equal("Bu kitabı zaten ödünç aldınız.", cut.Find(".borrow-reason").TextContent);
    }

    [Fact]
    public void Pasif_butonun_sebebi_ekran_okuyucuya_baglanmali()
    {
        var cut = Render<BookDetails>(p => p.Add(x => x.Book, OrnekKitap(mevcutKopya: 0)));

        Assert.Equal("borrow-reason", cut.Find("button").GetAttribute("aria-describedby"));
        Assert.NotNull(cut.Find("#borrow-reason"));
    }

    [Fact]
    public void Butona_tiklayinca_OnBorrowClick_cagrilmali()
    {
        var cagrildi = false;
        var cut = Render<BookDetails>(p => p
            .Add(x => x.Book, OrnekKitap())
            .Add(x => x.OnBorrowClick, () => cagrildi = true));

        cut.Find("button").Click();

        Assert.True(cagrildi);
    }

    [Fact]
    public void Gorsel_yuklenemezse_varsayilan_kapak_gosterilmeli()
    {
        var cut = Render<BookDetails>(p => p.Add(x => x.Book, OrnekKitap()));

        cut.Find("img").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        Assert.Equal("images/default.svg", cut.Find("img").GetAttribute("src"));
    }
}
