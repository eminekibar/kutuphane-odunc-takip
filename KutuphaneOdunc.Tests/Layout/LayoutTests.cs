using Bunit;
using KutuphaneOdunc.Layout;
using KutuphaneOdunc.Pages;
using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOdunc.Tests.Layout;

public class LayoutTests : BunitContext
{
    // Menüdeki profil alanı (ProfileBadge, #40) ProfileService kullanıyor.
    public LayoutTests()
    {
        Services.AddSingleton<ILocalStorageService>(new FakeLocalStorage());
        Services.AddScoped<ProfileService>();
    }

    [Fact]
    public void Menude_kitaplar_ve_odunc_kayitlarim_linkleri_olmali()
    {
        var cut = Render<NavMenu>();

        var linkler = cut.FindAll("a.nav-link").Select(a => a.GetAttribute("href")).ToList();

        Assert.Contains("books", linkler);
        Assert.Contains("my-loans", linkler);
    }

    [Fact]
    public void Menude_profil_alani_olmali_ve_ayri_Profilim_linki_olmamali()
    {
        var cut = Render<NavMenu>();

        // #39'daki "Profilim" linkinin yerini menünün altındaki profil alanı aldı; aynı sayfaya iki link olmasın.
        Assert.DoesNotContain(cut.FindAll("a.nav-link"), a => a.GetAttribute("href") == "profile");
        Assert.Equal("profile", cut.Find(".profile-badge a").GetAttribute("href"));
    }

    [Fact]
    public void Ana_sayfa_kitaplar_sayfasina_yonlendirmeli()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();

        Render<Home>();

        Assert.EndsWith("/books", navigation.Uri);
    }
}
