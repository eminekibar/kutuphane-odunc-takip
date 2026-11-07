using KutuphaneOdunc.Models;
using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;

namespace KutuphaneOdunc.Tests.Services;

public class ProfileServiceTests
{
    private static readonly UserProfile Ornek = new() { FullName = "Ayşe Yılmaz", Email = "ayse@ornek.com" };

    private static async Task<ProfileService> ServisOlustur(FakeLocalStorage storage)
    {
        var servis = new ProfileService(storage);
        await servis.InitializeAsync();
        return servis;
    }

    [Fact]
    public async Task Bos_storage_ile_profil_null_olmali()
    {
        var servis = await ServisOlustur(new FakeLocalStorage());

        Assert.Null(servis.Profile);
    }

    [Fact]
    public async Task Kaydedilen_profil_okunabilmeli()
    {
        var servis = await ServisOlustur(new FakeLocalStorage());

        await servis.SaveAsync(Ornek);

        Assert.Equal("Ayşe Yılmaz", servis.Profile!.FullName);
        Assert.Equal("ayse@ornek.com", servis.Profile.Email);
    }

    [Fact]
    public async Task Kaydedilen_profil_localStorage_a_yazilmali()
    {
        var storage = new FakeLocalStorage();
        var servis = await ServisOlustur(storage);

        await servis.SaveAsync(Ornek);

        Assert.True(storage.Items.ContainsKey(ProfileService.StorageKey));
        Assert.DoesNotContain("Initials", storage.Items[ProfileService.StorageKey]);
    }

    [Fact]
    public async Task Yeni_servis_ayni_storage_ile_profili_gormeli()
    {
        var storage = new FakeLocalStorage();
        var ilk = await ServisOlustur(storage);
        await ilk.SaveAsync(Ornek);

        // Sayfa yenilenmiş gibi: aynı storage ile sıfırdan başlayan yeni bir servis.
        var ikinci = await ServisOlustur(storage);

        Assert.Equal("Ayşe Yılmaz", ikinci.Profile!.FullName);
        Assert.Equal("ayse@ornek.com", ikinci.Profile.Email);
    }

    [Fact]
    public async Task Silinen_profil_null_olmali_ve_yeni_serviste_de_gorunmemeli()
    {
        var storage = new FakeLocalStorage();
        var servis = await ServisOlustur(storage);
        await servis.SaveAsync(Ornek);

        await servis.ClearAsync();

        Assert.Null(servis.Profile);
        Assert.Null((await ServisOlustur(storage)).Profile);
    }

    [Fact]
    public async Task Kaydetme_ve_silmede_OnChange_tetiklenmeli()
    {
        var servis = await ServisOlustur(new FakeLocalStorage());
        var sayac = 0;
        servis.OnChange += () => sayac++;

        await servis.SaveAsync(Ornek);
        Assert.Equal(1, sayac);

        await servis.ClearAsync();
        Assert.Equal(2, sayac);
    }

    [Fact]
    public async Task Bastaki_ve_sondaki_bosluklar_silinmeli()
    {
        var servis = await ServisOlustur(new FakeLocalStorage());

        await servis.SaveAsync(new UserProfile { FullName = "  Ayşe Yılmaz ", Email = " ayse@ornek.com " });

        Assert.Equal("Ayşe Yılmaz", servis.Profile!.FullName);
        Assert.Equal("ayse@ornek.com", servis.Profile.Email);
    }

    [Fact]
    public async Task Kaydedilen_profil_formun_nesnesinden_bagimsiz_olmali()
    {
        var servis = await ServisOlustur(new FakeLocalStorage());
        var form = new UserProfile { FullName = "Ayşe Yılmaz", Email = "ayse@ornek.com" };

        await servis.SaveAsync(form);
        form.FullName = "Başka Biri";

        Assert.Equal("Ayşe Yılmaz", servis.Profile!.FullName);
    }
}
