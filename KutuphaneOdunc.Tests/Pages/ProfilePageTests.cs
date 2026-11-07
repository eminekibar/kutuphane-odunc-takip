using Bunit;
using KutuphaneOdunc.Models;
using KutuphaneOdunc.Pages;
using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOdunc.Tests.Pages;

public class ProfilePageTests : BunitContext
{
    private readonly FakeLocalStorage _storage = new();

    public ProfilePageTests()
    {
        Services.AddSingleton<ILocalStorageService>(_storage);
        Services.AddScoped<ProfileService>();
    }

    private ProfileService Profiles => Services.GetRequiredService<ProfileService>();

    private void ProfilYaz(string adSoyad, string eposta) =>
        _storage.SetAsync(ProfileService.StorageKey, new UserProfile { FullName = adSoyad, Email = eposta }).Wait();

    private IRenderedComponent<Profile> SayfayiAc()
    {
        var cut = Render<Profile>();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".loading")));
        return cut;
    }

    private static void Doldur(IRenderedComponent<Profile> cut, string adSoyad, string eposta)
    {
        cut.Find("#profile-fullname").Change(adSoyad);
        cut.Find("#profile-email").Change(eposta);
    }

    [Fact]
    public void Bilgi_notu_gorunmeli()
    {
        var cut = SayfayiAc();

        Assert.Contains("Profil bilgileriniz sadece bu tarayıcıda saklanır.", cut.Find(".info-note").TextContent);
    }

    [Fact]
    public void Profil_yoksa_form_bos_ve_sil_butonu_gizli_olmali()
    {
        var cut = SayfayiAc();

        Assert.Equal("", cut.Find("#profile-fullname").GetAttribute("value") ?? "");
        Assert.Empty(cut.FindAll(".delete-button"));
        Assert.Empty(cut.FindAll(".avatar"));
    }

    [Fact]
    public void Kayitli_profil_formda_ve_avatarda_gorunmeli()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");

        var cut = SayfayiAc();

        Assert.Equal("Ayşe Yılmaz", cut.Find("#profile-fullname").GetAttribute("value"));
        Assert.Equal("ayse@ornek.com", cut.Find("#profile-email").GetAttribute("value"));
        Assert.Equal("AY", cut.Find(".avatar").TextContent);
    }

    [Fact]
    public void Hatali_e_postada_mesaj_gorunmeli_ve_kaydedilmemeli()
    {
        var cut = SayfayiAc();

        Doldur(cut, "Ayşe Yılmaz", "ayse");
        cut.Find("form").Submit();

        Assert.Contains("Geçerli bir e-posta adresi girin.", cut.FindAll(".validation-message").Select(m => m.TextContent));
        Assert.Null(Profiles.Profile);
        Assert.Empty(cut.FindAll(".feedback"));
    }

    [Fact]
    public void Gecerli_formda_profil_kaydedilmeli()
    {
        var cut = SayfayiAc();

        Doldur(cut, "Ayşe Yılmaz", "ayse@ornek.com");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal("Profiliniz kaydedildi.", cut.Find(".feedback").TextContent));
        Assert.Equal("Ayşe Yılmaz", Profiles.Profile!.FullName);
        Assert.Equal("AY", cut.Find(".avatar").TextContent);
        Assert.True(_storage.Items.ContainsKey(ProfileService.StorageKey));
    }

    [Fact]
    public void Sil_e_basinca_sayfanin_icinde_onay_sorusu_cikmali()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");
        var cut = SayfayiAc();

        cut.Find(".delete-button").Click();

        Assert.Contains("Emin misiniz?", cut.Find(".confirm").TextContent);
        Assert.Empty(cut.FindAll(".delete-button"));
        Assert.NotNull(Profiles.Profile);
    }

    [Fact]
    public void Silme_onayi_Vazgec_ile_iptal_edilmeli()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");
        var cut = SayfayiAc();
        cut.Find(".delete-button").Click();

        cut.Find(".confirm-no").Click();

        Assert.Empty(cut.FindAll(".confirm"));
        Assert.NotNull(cut.Find(".delete-button"));
        Assert.Equal("Ayşe Yılmaz", Profiles.Profile!.FullName);
    }

    [Fact]
    public void Silme_onayi_Evet_ile_profili_silmeli()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");
        var cut = SayfayiAc();
        cut.Find(".delete-button").Click();

        cut.Find(".confirm-yes").Click();

        cut.WaitForAssertion(() => Assert.Equal("Profiliniz silindi.", cut.Find(".feedback").TextContent));
        Assert.Null(Profiles.Profile);
        Assert.Empty(cut.FindAll(".avatar"));
        Assert.Empty(cut.FindAll(".confirm"));
        Assert.Equal("", cut.Find("#profile-fullname").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Onay_sorusu_acilinca_odak_soruya_tasinmali()
    {
        ProfilYaz("Ayşe Yılmaz", "ayse@ornek.com");
        var cut = SayfayiAc();

        cut.Find(".delete-button").Click();

        var focus = JSInterop.VerifyFocusAsyncInvoke();
        var reference = (Microsoft.AspNetCore.Components.ElementReference)focus.Arguments[0]!;
        Assert.Equal(reference.Id, cut.Find(".confirm").GetAttribute("blazor:elementreference"));
    }
}
