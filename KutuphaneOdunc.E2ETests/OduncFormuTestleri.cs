using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// K5: ödünç alma formu ve doğrulama
public class OduncFormuTestleri : E2ETest
{
    public OduncFormuTestleri(AppFixture app) : base(app) { }

    [Fact]
    public async Task K5_bos_form_gonderilemez()
    {
        await FormuAcAsync("B001");

        await FormuGonderAsync();

        await Expect(Page.Locator(".validation-message"))
            .ToHaveTextAsync(new[] { "Ad soyad zorunludur.", "E-posta zorunludur." });
        await Expect(Page.Locator(".result")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task K5_kisa_ad_ve_hatali_e_posta_reddedilmeli()
    {
        await FormuAcAsync("B001");

        await FormuDoldurAsync("Al", "ayse");
        await FormuGonderAsync();

        await Expect(Page.Locator(".validation-message")).ToHaveTextAsync(new[]
        {
            "Ad soyad 3 ile 60 karakter arasında olmalıdır.",
            "Geçerli bir e-posta adresi girin."
        });
        await Expect(Page.Locator(".result")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Vazgec_formu_kapatmali()
    {
        await FormuAcAsync("B001");

        await Page.Locator("form .btn-outline-secondary").ClickAsync();

        await Expect(Page.Locator("form")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Form_beyaz_kart_icinde_ve_dar_gorunmeli()
    {
        await FormuAcAsync("B001");

        var kart = Page.Locator(".borrow-form");
        var kutu = await kart.BoundingBoxAsync();
        var zemin = await kart.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");

        Assert.NotNull(kutu);
        Assert.True(kutu!.Width <= 560, $"Form genişliği {kutu.Width}px, en fazla 520px olmalı.");
        Assert.Equal("rgb(255, 255, 255)", zemin);
    }
}
