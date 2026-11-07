using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// Bütün uçtan uca test sınıflarının ortak tabanı.
// Her test kendi tarayıcı bağlamıyla (context) başlar; localStorage boştur, testler birbirini etkilemez.
[Collection(AppCollection.Name)]
public abstract class E2ETest : IAsyncLifetime
{
    protected const string Ad = "Ayşe Yılmaz";
    protected const string Eposta = "ayse@ornek.com";

    private readonly AppFixture _app;
    private IBrowserContext _context = default!;

    protected E2ETest(AppFixture app)
    {
        _app = app;
    }

    protected IPage Page { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        _context = await _app.Browser.NewContextAsync(new()
        {
            BaseURL = _app.BaseUrl,
            Locale = "tr-TR",
            ViewportSize = new() { Width = 1280, Height = 900 }
        });
        Page = await _context.NewPageAsync();
    }

    public Task DisposeAsync() => _context.CloseAsync();

    protected static DateTime Bugun => DateTime.Today;

    protected static string Tarih(DateTime tarih) => tarih.ToString("dd.MM.yyyy");

    protected static string Kod(int sira) => $"LIB-{Bugun.Year}-{sira}";

    // Sayfaya gider ve Blazor'un açılıp verinin yüklenmesini bekler.
    protected async Task GitAsync(string adres)
    {
        await Page.GotoAsync(adres);
        await YuklenmesiniBekleAsync();
    }

    protected async Task YenileAsync()
    {
        await Page.ReloadAsync();
        await YuklenmesiniBekleAsync();
    }

    private async Task YuklenmesiniBekleAsync()
    {
        await Expect(Page.Locator(".loading-progress")).ToHaveCountAsync(0); // Blazor açılış dairesi
        await Expect(Page.Locator(".loading")).ToHaveCountAsync(0);          // sayfaların "Yükleniyor..." yazısı
    }

    // localStorage'ı değiştiren bir JavaScript çalıştırır ve sayfayı yeniler (uygulama veriyi açılışta okuyor).
    protected async Task DepolamayiDegistirAsync(string script)
    {
        await Page.EvaluateAsync(script);
        await YenileAsync();
    }

    protected ILocator KitapKarti(string baslik) =>
        Page.Locator(".book-card").Filter(new() { HasText = baslik });

    protected ILocator KayitKarti(string baslik) =>
        Page.Locator(".loan-status").Filter(new() { HasText = baslik });

    protected async Task FormuAcAsync(string kitapId)
    {
        await GitAsync($"/books/{kitapId}");
        await Page.Locator(".borrow-button").ClickAsync();
    }

    protected async Task FormuDoldurAsync(string ad, string eposta)
    {
        await Page.Locator("#borrow-fullname").FillAsync(ad);
        await Page.Locator("#borrow-email").FillAsync(eposta);
        // InputText değeri "change" olayında alıyor; alandan çıkınca tetikleniyor.
        await Page.Locator("#borrow-email").BlurAsync();
    }

    protected Task FormuGonderAsync() => Page.Locator("form button[type=submit]").ClickAsync();

    // Kitabı ödünç alır ve oluşan ödünç kodunu döndürür.
    protected async Task<string> OduncAlAsync(string kitapId, string ad = Ad, string eposta = Eposta)
    {
        await FormuAcAsync(kitapId);
        await FormuDoldurAsync(ad, eposta);
        await FormuGonderAsync();

        var kod = Page.Locator(".result .code");
        await Expect(kod).ToBeVisibleAsync();
        return (await kod.TextContentAsync())!.Trim();
    }
}
