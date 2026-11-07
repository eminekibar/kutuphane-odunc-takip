using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Models;
using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOdunc.Tests.Components;

public class ProfileBadgeTests : BunitContext
{
    private readonly FakeLocalStorage _storage = new();

    public ProfileBadgeTests()
    {
        Services.AddSingleton<ILocalStorageService>(_storage);
        Services.AddScoped<ProfileService>();
    }

    private ProfileService Profiles => Services.GetRequiredService<ProfileService>();

    private void ProfilYaz(string adSoyad, string eposta) =>
        _storage.SetAsync(ProfileService.StorageKey, new UserProfile { FullName = adSoyad, Email = eposta }).Wait();

    // Ekran okuyucunun okuyacağı ad: linkin içindeki yazı, aria-hidden olan kısımlar (avatar) hariç (#67).
    private static string OkunanAd(IRenderedComponent<ProfileBadge> cut)
    {
        var link = cut.Find("a");
        var parcalar = link.Descendants().OfType<AngleSharp.Dom.IText>()
            .Where(t => t.ParentElement?.Closest("[aria-hidden='true']") is null)
            .Select(t => t.Text);
        return System.Text.RegularExpressions.Regex.Replace(string.Concat(parcalar), @"\s+", " ").Trim();
    }

    [Fact]
    public void Profil_yokken_Profil_olustur_ve_kisi_ikonu_gorunmeli()
    {
        var cut = Render<ProfileBadge>();

        Assert.Equal("Profil oluştur", cut.Find(".name").TextContent);
        Assert.NotNull(cut.Find(".avatar.no-profile .person-icon"));
        Assert.Equal("Profil oluştur", OkunanAd(cut));
    }

    [Fact]
    public void Profil_varken_bas_harfler_ve_ad_gorunmeli()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");

        var cut = Render<ProfileBadge>();

        Assert.Equal("AY", cut.Find(".avatar").TextContent.Trim());
        Assert.Empty(cut.FindAll(".person-icon"));
        Assert.Equal("Ayşe Yılmaz", cut.Find(".name").TextContent);
        Assert.Equal("Profilim: Ayşe Yılmaz", OkunanAd(cut));
    }

    [Fact]
    public void Linkte_aria_label_olmamali_ve_gorunen_ad_okunan_adin_icinde_gecmeli()
    {
        // WCAG 2.5.3 (Label in Name): aria-label, ekrandaki "AY" harflerini içermediği için kuralı bozuyordu (#67).
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");

        var cut = Render<ProfileBadge>();

        Assert.False(cut.Find("a").HasAttribute("aria-label"));
        Assert.Contains(cut.Find(".name").TextContent, OkunanAd(cut));
        Assert.Equal("Profilim: ", cut.Find("a .visually-hidden").TextContent);
    }

    [Fact]
    public void Avatar_ekran_okuyucudan_gizlenmeli()
    {
        var cut = Render<ProfileBadge>();

        Assert.Equal("true", cut.Find(".avatar").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Link_profile_adresine_gitmeli()
    {
        var cut = Render<ProfileBadge>();

        Assert.Equal("profile", cut.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void Profil_kaydedilince_bilesen_yeniden_cizilmeli()
    {
        var cut = Render<ProfileBadge>();
        Assert.Equal("Profil oluştur", cut.Find(".name").TextContent);

        // Profil sayfasında kaydedilmiş gibi: bileşenin dışından servis üzerinden.
        cut.InvokeAsync(() => Profiles.SaveAsync(new UserProfile { FullName = "ilker özgür", Email = "ilker@ornek.com" }));

        cut.WaitForAssertion(() => Assert.Equal("İÖ", cut.Find(".avatar").TextContent.Trim()));
        Assert.Equal("ilker özgür", cut.Find(".name").TextContent);
        Assert.Equal("Profilim: ilker özgür", OkunanAd(cut));
    }

    [Fact]
    public void Profil_silinince_Profil_olustur_a_donmeli()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");
        var cut = Render<ProfileBadge>();

        cut.InvokeAsync(() => Profiles.ClearAsync());

        cut.WaitForAssertion(() => Assert.Equal("Profil oluştur", cut.Find(".name").TextContent));
        Assert.NotNull(cut.Find(".person-icon"));
    }

    [Fact]
    public async Task Bilesen_kaldirilinca_OnChange_aboneligi_birakilmali()
    {
        Render<ProfileBadge>();
        Assert.NotNull(AboneListesi());

        await DisposeComponentsAsync();

        Assert.Null(AboneListesi());
    }

    // OnChange event'inin arkasındaki alan: abone yoksa null (#16'daki yöntem).
    private Delegate? AboneListesi() =>
        (Delegate?)typeof(ProfileService)
            .GetField(nameof(ProfileService.OnChange), BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(Profiles);
}
