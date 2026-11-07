using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// #45: menüdeki ödünç rozeti
public class MenuTestleri : E2ETest
{
    public MenuTestleri(AppFixture app) : base(app) { }

    private ILocator Rozet => Page.Locator("nav a[href='my-loans'] .loan-badge");

    [Fact]
    public async Task Rozet_odunc_alma_ve_iadeyle_guncellenmeli()
    {
        await GitAsync("/books");
        await Expect(Rozet).ToHaveCountAsync(0);

        // Ödünç alındıktan sonra sayfa yenilenmeden rozet güncelleniyor.
        await OduncAlAsync("B001");
        await Expect(Rozet).ToHaveTextAsync("1");

        await OduncAlAsync("B005");
        await Expect(Rozet).ToHaveTextAsync("2");

        await GitAsync("/my-loans");
        await KayitKarti("Ağ Güvenliğinin Temelleri").Locator(".return-button").ClickAsync();
        await Expect(Rozet).ToHaveTextAsync("1");
    }
}
