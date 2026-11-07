using Bunit;
using KutuphaneOdunc.Layout;
using KutuphaneOdunc.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOdunc.Tests.Layout;

public class LayoutTests : BunitContext
{
    [Fact]
    public void Menude_kitaplar_ve_odunc_kayitlarim_linkleri_olmali()
    {
        var cut = Render<NavMenu>();

        var linkler = cut.FindAll("a.nav-link").Select(a => a.GetAttribute("href")).ToList();

        Assert.Contains("books", linkler);
        Assert.Contains("my-loans", linkler);
    }

    [Fact]
    public void Menude_profilim_linki_olmali()
    {
        var cut = Render<NavMenu>();

        var link = cut.FindAll("a.nav-link").Single(a => a.GetAttribute("href") == "profile");

        Assert.Equal("Profilim", link.TextContent.Trim());
    }

    [Fact]
    public void Ana_sayfa_kitaplar_sayfasina_yonlendirmeli()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();

        Render<Home>();

        Assert.EndsWith("/books", navigation.Uri);
    }
}
