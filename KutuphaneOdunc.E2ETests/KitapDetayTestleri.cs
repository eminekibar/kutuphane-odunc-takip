using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// Kitap detay sayfası ve K3
public class KitapDetayTestleri : E2ETest
{
    public KitapDetayTestleri(AppFixture app) : base(app) { }

    [Fact]
    public async Task Kitap_bilgileri_gorunmeli()
    {
        await GitAsync("/books/B001");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Yazılım Tasarımına Giriş");
        await Expect(Page.Locator(".facts dd")).ToHaveTextAsync(new[] { "Örnek Yayınları", "2022", "5", "3" });
        await Expect(Page).ToHaveTitleAsync("Yazılım Tasarımına Giriş - Kütüphane Ödünç Takip");
        await Expect(Page.Locator(".borrow-button")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Kitap_kodu_kucuk_harfle_yazilsa_da_acilmali()
    {
        await GitAsync("/books/b001");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Yazılım Tasarımına Giriş");
    }

    [Fact]
    public async Task Olmayan_kitapta_bulunamadi_mesaji_gorunmeli()
    {
        await GitAsync("/books/XYZ");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Kitap bulunamadı");
    }

    [Fact]
    public async Task K3_kopya_yoksa_odunc_al_pasif_olmali()
    {
        await GitAsync("/books/B002");

        await Expect(Page.Locator(".borrow-button")).ToBeDisabledAsync();
        await Expect(Page.Locator(".borrow-reason")).ToHaveTextAsync("Şu an ödünç alınabilir kopya yok.");
    }
}
