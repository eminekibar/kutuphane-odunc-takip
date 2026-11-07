using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// K11: sayfa yenilense de veriler kalmalı
public class KalicilikTestleri : E2ETest
{
    public KalicilikTestleri(AppFixture app) : base(app) { }

    [Fact]
    public async Task K11_yenileme_sonrasi_kayit_ve_kopya_sayisi_kalmali()
    {
        var kod = await OduncAlAsync("B001");

        await GitAsync("/my-loans");
        await YenileAsync();
        await Expect(Page.Locator(".loan-status .code")).ToHaveTextAsync(kod);

        await GitAsync("/books/B001");
        await YenileAsync();
        await Expect(Page.Locator(".available")).ToHaveTextAsync("2");
        await Expect(Page.Locator(".borrow-button")).ToBeDisabledAsync();
    }

    [Fact]
    public async Task K11_yeni_sekmede_de_ayni_kayitlar_gorunmeli()
    {
        var kod = await OduncAlAsync("B001");

        var yeniSekme = await Page.Context.NewPageAsync();
        await yeniSekme.GotoAsync("/my-loans");

        await Expect(yeniSekme.Locator(".loan-status .code")).ToHaveTextAsync(kod);
    }

    [Fact]
    public async Task Bozuk_veride_uygulama_cokmemeli()
    {
        await GitAsync("/books");
        await DepolamayiDegistirAsync("localStorage.setItem('kutuphane.loans', 'bozuk veri');");

        await GitAsync("/my-loans");

        await Expect(Page.Locator(".empty")).ToContainTextAsync("Henüz ödünç kaydınız yok.");
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
