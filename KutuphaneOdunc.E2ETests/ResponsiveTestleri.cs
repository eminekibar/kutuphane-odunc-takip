using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// #50: bonus özelliklerin ekranları 375 (telefon), 768 (tablet) ve 1280 (dizüstü) piksel genişlikte.
// Her ekranda sayfanın yana taşmadığı kontrol ediliyor.
//
// Ekran görüntüleri sadece E2E_EKRAN_KLASORU ortam değişkeni verilirse alınır (ör. wiki'nin images klasörü).
// CI'da bu değişken yok, orada görüntü alınmaz.
public class ResponsiveTestleri : E2ETest
{
    public ResponsiveTestleri(AppFixture app) : base(app) { }

    private static readonly string? EkranKlasoru = Environment.GetEnvironmentVariable("E2E_EKRAN_KLASORU");

    // Her genişlik ayrı bir test olarak çalışıyor; her testin kendi tarayıcı bağlamı ve boş localStorage'ı var (E2ETest).
    private async Task GenislikAyarlaAsync(int genislik) =>
        await Page.SetViewportSizeAsync(genislik, 900);

    private async Task TasmaYokAsync(string ekran, int genislik)
    {
        var olcu = await Page.EvaluateAsync<int[]>(
            "() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");

        Assert.True(olcu[0] <= olcu[1],
            $"{ekran} ekranı {genislik}px genişlikte yana taşıyor: içerik {olcu[0]}px, görünen alan {olcu[1]}px.");
    }

    // tamSayfa: false ise sadece görünen alan çekilir (uzun kitap listesinin tamamı gerekmeyen ekranlar için).
    private async Task EkranGoruntusuAsync(string ekran, int genislik, bool tamSayfa)
    {
        if (string.IsNullOrWhiteSpace(EkranKlasoru))
            return;

        Directory.CreateDirectory(EkranKlasoru);
        await Page.ScreenshotAsync(new()
        {
            Path = Path.Combine(EkranKlasoru, $"50-{ekran}-{genislik}.png"),
            FullPage = tamSayfa,
            // Bildirim animasyonu bitmiş halde çekilsin.
            Animations = ScreenshotAnimations.Disabled
        });
    }

    private async Task KontrolEtAsync(string ekran, int genislik, bool tamSayfa = true)
    {
        await TasmaYokAsync(ekran, genislik);
        await EkranGoruntusuAsync(ekran, genislik, tamSayfa);
    }

    // Profil ve üç farklı durumda kayıt: biri aktif (bu profilin), biri gecikmiş (başkasının), biri iade edilmiş.
    private Task VeriHazirlaAsync() => DepolamayiDegistirAsync("""
        const gun = n => { const d = new Date(); d.setDate(d.getDate() + n);
            const p = x => String(x).padStart(2, '0');
            return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T00:00:00`; };
        const yil = new Date().getFullYear();
        localStorage.setItem('kutuphane.profile', JSON.stringify({ FullName: 'Ayşe Yılmaz', Email: 'ayse@ornek.com' }));
        localStorage.setItem('kutuphane.loans', JSON.stringify([
            { Id: `LIB-${yil}-101`, BookId: 'B001', BorrowerName: 'Ayşe Yılmaz', Email: 'ayse@ornek.com',
              BorrowDate: gun(-12), DueDate: gun(2), ReturnedAt: null, IsExtended: false },
            { Id: `LIB-${yil}-102`, BookId: 'B011', BorrowerName: 'Ali Kaya', Email: 'ali@ornek.com',
              BorrowDate: gun(-20), DueDate: gun(-3), ReturnedAt: null, IsExtended: true },
            { Id: `LIB-${yil}-103`, BookId: 'B004', BorrowerName: 'Ayşe Yılmaz', Email: 'ayse@ornek.com',
              BorrowDate: gun(-30), DueDate: gun(-16), ReturnedAt: gun(-18), IsExtended: false }
        ]));
        """);

    // 375'te (640px'ten dar ekranlarda) menü kapalı geliyor; menüdeki rozet ve profil alanı için önce açılmalı.
    private async Task MenuyuAcAsync(int genislik)
    {
        if (genislik > 640)
            return;

        await Page.Locator(".navbar-toggler").ClickAsync();
        await Expect(Page.Locator(".profile-badge")).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData(375)]
    [InlineData(768)]
    [InlineData(1280)]
    public async Task Kitaplar_sayfasi_siralama_alanlariyla_tasmamali(int genislik)
    {
        await GenislikAyarlaAsync(genislik);
        await GitAsync("/books");

        await Expect(Page.Locator("#book-sort")).ToBeVisibleAsync();
        await Expect(Page.Locator("#book-available-only")).ToBeVisibleAsync();
        await KontrolEtAsync("kitaplar-siralama", genislik, tamSayfa: false);
    }

    [Theory]
    [InlineData(375)]
    [InlineData(768)]
    [InlineData(1280)]
    public async Task Kitap_detayi_benzer_kitaplarla_tasmamali(int genislik)
    {
        await GenislikAyarlaAsync(genislik);
        await GitAsync("/books/B011"); // en uzun adlı kitap

        await Expect(Page.Locator(".similar-books .book-card")).Not.ToHaveCountAsync(0);
        await KontrolEtAsync("benzer-kitaplar", genislik);
    }

    [Theory]
    [InlineData(375)]
    [InlineData(768)]
    [InlineData(1280)]
    public async Task Odunc_kayitlari_ozet_ve_filtreyle_tasmamali(int genislik)
    {
        await GenislikAyarlaAsync(genislik);
        await GitAsync("/books");
        await VeriHazirlaAsync();
        await GitAsync("/my-loans");

        await Expect(Page.Locator(".loan-summary")).ToBeVisibleAsync();
        await Expect(Page.Locator(".filter")).ToBeVisibleAsync();
        await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(3);
        await KontrolEtAsync("odunc-kayitlari", genislik);
    }

    [Theory]
    [InlineData(375)]
    [InlineData(768)]
    [InlineData(1280)]
    public async Task Menu_rozet_ve_profil_alaniyla_tasmamali(int genislik)
    {
        await GenislikAyarlaAsync(genislik);
        await GitAsync("/books");
        await VeriHazirlaAsync();

        await MenuyuAcAsync(genislik);
        await Expect(Page.Locator("nav a[href='my-loans'] .loan-badge")).ToHaveTextAsync("2");
        await Expect(Page.Locator(".profile-badge .name")).ToHaveTextAsync("Ayşe Yılmaz");
        await KontrolEtAsync("menu", genislik, tamSayfa: false);
    }

    [Theory]
    [InlineData(375)]
    [InlineData(768)]
    [InlineData(1280)]
    public async Task Profil_sayfasi_ve_silme_sorusu_tasmamali(int genislik)
    {
        await GenislikAyarlaAsync(genislik);
        await GitAsync("/books");
        await VeriHazirlaAsync();
        await GitAsync("/profile");

        await Expect(Page.Locator(".summary .avatar")).ToHaveTextAsync("AY");
        await KontrolEtAsync("profil", genislik);

        await Page.Locator(".delete-button").ClickAsync();
        await Expect(Page.Locator(".confirm")).ToBeVisibleAsync();
        await KontrolEtAsync("profil-silme", genislik);
    }

    [Theory]
    [InlineData(375)]
    [InlineData(768)]
    [InlineData(1280)]
    public async Task Bildirim_gorunurken_tasmamali(int genislik)
    {
        await GenislikAyarlaAsync(genislik);
        await OduncAlAsync("B011");

        await Expect(Page.Locator(".toast-item")).ToBeVisibleAsync();
        await KontrolEtAsync("bildirim", genislik);

        // Bildirim görünen alanın içinde olmalı, kenardan dışarı taşmamalı.
        var kutu = await Page.Locator(".toast-item").BoundingBoxAsync();
        Assert.NotNull(kutu);
        Assert.True(kutu!.X >= 0 && kutu.X + kutu.Width <= genislik,
            $"Bildirim {genislik}px genişlikte ekranın dışına taşıyor: x={kutu.X}, genişlik={kutu.Width}.");
    }
}
