using System.Text.RegularExpressions;
using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// K1, K2 ve sayfalar arası gezinme
public class KatalogTestleri : E2ETest
{
    public KatalogTestleri(AppFixture app) : base(app) { }

    [Fact]
    public async Task Butun_kitaplar_listelenmeli()
    {
        await GitAsync("/books");

        await Expect(Page.Locator(".book-card")).ToHaveCountAsync(18);
        await Expect(Page.Locator(".result-count")).ToHaveTextAsync("18 kitap listeleniyor");
    }

    [Fact]
    public async Task K1_kartta_kitap_bilgileri_gorunmeli()
    {
        await GitAsync("/books");
        var kart = KitapKarti("Yazılım Tasarımına Giriş");

        await Expect(kart.Locator(".author")).ToHaveTextAsync("Ayşe Yılmaz");
        await Expect(kart.Locator(".category")).ToHaveTextAsync("Bilgisayar Bilimleri");
        await Expect(kart.Locator(".stock")).ToContainTextAsync("Mevcut kopya: 3");
        await Expect(kart.Locator("img")).ToHaveAttributeAsync("src", "images/B001.svg");

        // Görselin gerçekten yüklendiğini kontrol et (dosya yoksa genişlik 0 olur).
        var genislik = await kart.Locator("img").EvaluateAsync<int>("img => img.naturalWidth");
        Assert.True(genislik > 0);
    }

    [Fact]
    public async Task K1_stokta_olmayan_kitapta_etiket_gorunmeli()
    {
        await GitAsync("/books");
        var kart = KitapKarti("Araştırma Yöntemlerine Giriş");

        await Expect(kart.Locator(".stock")).ToContainTextAsync("Mevcut kopya: 0");
        await Expect(kart.Locator(".out-of-stock")).ToHaveTextAsync("Stokta yok");
    }

    [Theory]
    [InlineData("vapur", "", 1)]
    [InlineData("kayacan", "", 2)]
    [InlineData("istanbul", "", 1)]   // küçük i ile "İstanbul"
    [InlineData("AYŞE", "", 2)]       // büyük harfle yazar
    [InlineData("", "Edebiyat", 4)]
    [InlineData("kasaba", "Edebiyat", 1)]
    [InlineData("vapur", "Bilim", 0)]
    [InlineData("zzz", "", 0)]
    public async Task K2_arama_ve_kategori_filtresi_calismali(string arama, string kategori, int beklenen)
    {
        await GitAsync("/books");

        if (arama != "")
            await Page.Locator("#book-search").FillAsync(arama);
        if (kategori != "")
            await Page.Locator("#book-category").SelectOptionAsync(kategori);

        await Expect(Page.Locator(".book-card")).ToHaveCountAsync(beklenen);
        await Expect(Page.Locator(".result-count")).ToHaveTextAsync($"{beklenen} kitap listeleniyor");
        if (beklenen == 0)
            await Expect(Page.Locator(".empty")).ToContainTextAsync("Kitap bulunamadı");
    }

    [Fact]
    public async Task Karta_tiklayinca_detay_sayfasi_acilmali()
    {
        await GitAsync("/books");

        await KitapKarti("Yazılım Tasarımına Giriş").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/books/B001$"));
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Yazılım Tasarımına Giriş");
    }

    [Fact]
    public async Task Ana_adres_kitaplar_sayfasina_yonlenmeli()
    {
        await GitAsync("/");

        await Expect(Page).ToHaveURLAsync(new Regex("/books$"));
    }

    [Fact]
    public async Task Olmayan_sayfada_bulunamadi_mesaji_gorunmeli()
    {
        await GitAsync("/olmayan-sayfa");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Sayfa bulunamadı");
    }
}
