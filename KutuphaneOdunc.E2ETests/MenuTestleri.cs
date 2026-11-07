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

    // #69: rozet gelince "Ödünç Kayıtlarım" iki satıra bölünüyordu (masaüstünde menü 240 piksel).
    [Fact]
    public async Task Rozetli_menu_linki_tek_satirda_kalmali_ve_rozet_linkin_icinde_olmali()
    {
        await OduncAlAsync("B001");
        var link = Page.Locator("nav a[href='my-loans']");
        await Expect(Rozet).ToHaveTextAsync("1");

        // Yazının kaç satıra yayıldığı: metin düğümünün tarayıcıdaki satır kutularının sayısı.
        var satir = await link.EvaluateAsync<int>("""
            a => {
                const metin = [...a.childNodes].find(n => n.nodeType === Node.TEXT_NODE && n.textContent.includes('Ödünç'));
                const aralik = document.createRange();
                aralik.selectNodeContents(metin);
                return [...aralik.getClientRects()].filter(r => r.width > 0).length;
            }
            """);
        Assert.Equal(1, satir);

        var kutu = (await link.BoundingBoxAsync())!;
        var rozet = (await Rozet.BoundingBoxAsync())!;
        Assert.True(rozet.X + rozet.Width <= kutu.X + kutu.Width, "Rozet linkin dışına taşıyor.");
    }
}
