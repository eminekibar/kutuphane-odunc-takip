using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// K4 ve K6: ödünç alma
public class OduncAlmaTestleri : E2ETest
{
    public OduncAlmaTestleri(AppFixture app) : base(app) { }

    [Fact]
    public async Task K6_ilk_odunc_kodu_tarihi_ve_kopya_sayisi_dogru_olmali()
    {
        var kod = await OduncAlAsync("B001");

        Assert.Equal(Kod(101), kod);
        await Expect(Page.Locator(".result-facts dd").Nth(1)).ToHaveTextAsync(Tarih(Bugun.AddDays(14)));
        await Expect(Page.Locator(".available")).ToHaveTextAsync("2");
    }

    [Fact]
    public async Task K4_ayni_kitap_ikinci_kez_odunc_alinamaz()
    {
        await OduncAlAsync("B001");

        await Expect(Page.Locator(".borrow-button")).ToBeDisabledAsync();
        await Expect(Page.Locator(".borrow-reason")).ToHaveTextAsync("Bu kitabı zaten ödünç aldınız.");
    }

    [Fact]
    public async Task K6_listede_kopya_sayisi_azalmali()
    {
        await OduncAlAsync("B001");

        await GitAsync("/books");

        await Expect(KitapKarti("Yazılım Tasarımına Giriş").Locator(".stock")).ToContainTextAsync("Mevcut kopya: 2");
    }

    [Fact]
    public async Task K6_kodlar_sirayla_artmali()
    {
        Assert.Equal(Kod(101), await OduncAlAsync("B001"));
        Assert.Equal(Kod(102), await OduncAlAsync("B003"));
    }

    [Fact]
    public async Task K6_yeni_kod_en_buyuk_siranin_bir_fazlasi_olmali()
    {
        await OduncAlAsync("B001");
        await DepolamayiDegistirAsync($$"""
            const loans = JSON.parse(localStorage.getItem('kutuphane.loans'));
            loans[0].Id = '{{Kod(150)}}';
            localStorage.setItem('kutuphane.loans', JSON.stringify(loans));
            """);

        Assert.Equal(Kod(151), await OduncAlAsync("B003"));
    }

    [Fact]
    public async Task K6_son_kopya_alininca_stokta_yok_olmali()
    {
        await OduncAlAsync("B005");

        await Expect(Page.Locator(".available")).ToHaveTextAsync("0");
        await GitAsync("/books");
        await Expect(KitapKarti("Ağ Güvenliğinin Temelleri").Locator(".out-of-stock")).ToHaveTextAsync("Stokta yok");
    }
}
