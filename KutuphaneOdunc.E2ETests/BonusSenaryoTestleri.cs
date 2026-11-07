using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// #50: bonus özelliklerin (#39–#49) birlikte çalıştığını gösteren uçtan uca senaryo.
// Issue'daki 8 adım tek test; bir adım başarısız olursa hata mesajı hangi adım olduğunu söyler.
public class BonusSenaryoTestleri : E2ETest
{
    public BonusSenaryoTestleri(AppFixture app) : base(app) { }

    private const string Kitap1Id = "B001";
    private const string Kitap1 = "Yazılım Tasarımına Giriş";
    private const string Kitap2Id = "B003";
    private const string Kitap2 = "Veri Yapıları ve Algoritmalar";
    private const string BaskaAd = "Ali Kaya";
    private const string BaskaEposta = "ali@ornek.com";

    private ILocator ProfilAlani => Page.Locator(".profile-badge");
    private ILocator Rozet => Page.Locator("nav a[href='my-loans'] .loan-badge");
    private ILocator Ozet(string tur) => Page.Locator($".loan-summary .summary-box.{tur} .count");

    // Adım başarısız olursa hangi adımda kalındığı test çıktısında görünsün.
    private static async Task Adim(string ad, Func<Task> adim)
    {
        try
        {
            await adim();
        }
        catch (Exception ex)
        {
            throw new Exception($"Adım başarısız: {ad}{Environment.NewLine}{ex.Message}", ex);
        }
    }

    [Fact]
    public async Task Bonus_ozellikler_uctan_uca_birlikte_calismali()
    {
        string kod1 = "";

        await Adim("1. Profil oluştur → avatar ve ad menüde görünüyor", async () =>
        {
            await GitAsync("/profile");
            await Expect(ProfilAlani.Locator(".name")).ToHaveTextAsync("Profil oluştur");

            await Page.Locator("#profile-fullname").FillAsync(Ad);
            await Page.Locator("#profile-email").FillAsync(Eposta);
            await Page.Locator(".save-button").ClickAsync();

            await Expect(Page.Locator(".feedback")).ToHaveTextAsync("Profiliniz kaydedildi.");
            await Expect(ProfilAlani.Locator(".avatar")).ToHaveTextAsync("AY");
            await Expect(ProfilAlani.Locator(".name")).ToHaveTextAsync(Ad);
            // Okunan ad aria-label'dan değil linkin içeriğinden geliyor (#67); tarayıcının hesapladığı ad kontrol ediliyor.
            await Expect(ProfilAlani.Locator("a")).ToHaveAccessibleNameAsync($"Profilim: {Ad}");
        });

        await Adim("2. Kitap ödünç al → form profilden dolu, başarı bildirimi, rozet 1", async () =>
        {
            await FormuAcAsync(Kitap1Id);
            await Expect(Page.Locator(".profile-hint")).ToHaveCountAsync(0);
            await Expect(Page.Locator("#borrow-fullname")).ToHaveValueAsync(Ad);
            await Expect(Page.Locator("#borrow-email")).ToHaveValueAsync(Eposta);

            await FormuGonderAsync();

            var kod = Page.Locator(".result .code");
            await Expect(kod).ToBeVisibleAsync();
            kod1 = (await kod.TextContentAsync())!.Trim();
            await Expect(Page.Locator(".toast-item.success .toast-text")).ToContainTextAsync("Kitap ödünç alındı.");
            await Expect(Rozet).ToHaveTextAsync("1");
        });

        await Adim("3. Takvime ekle → .ics dosyası iniyor", async () =>
        {
            await GitAsync("/my-loans");

            var indirme = await Page.RunAndWaitForDownloadAsync(() =>
                KayitKarti(Kitap1).Locator(".calendar-button").ClickAsync());

            Assert.Equal($"{kod1}.ics", indirme.SuggestedFilename);
            var icerik = await File.ReadAllTextAsync((await indirme.PathAsync())!);
            Assert.StartsWith("BEGIN:VCALENDAR\r\n", icerik);
            Assert.EndsWith("END:VCALENDAR\r\n", icerik);
            Assert.Contains($"SUMMARY:Kitap iadesi: {Kitap1}", icerik);
            Assert.Contains($"DTSTART;VALUE=DATE:{Bugun.AddDays(14):yyyyMMdd}", icerik);
            Assert.Contains($"UID:{kod1}@kutuphane-odunc-takip", icerik);
        });

        await Adim("4. Başka bir adla başka kitap → 'Sadece benim kayıtlarım' ve özet doğru", async () =>
        {
            // Form profilden dolu geliyor; başkası adına almak için bilgiler değiştiriliyor.
            await FormuAcAsync(Kitap2Id);
            await FormuDoldurAsync(BaskaAd, BaskaEposta);
            await FormuGonderAsync();
            await Expect(Page.Locator(".result .code")).ToBeVisibleAsync();
            await Expect(Rozet).ToHaveTextAsync("2");

            await GitAsync("/my-loans");
            await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(2);
            await Expect(Ozet("active")).ToHaveTextAsync("2");

            await Page.Locator(".filter-mine").ClickAsync();
            await Expect(Page.Locator(".filter-mine")).ToHaveAttributeAsync("aria-pressed", "true");
            await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(1);
            await Expect(KayitKarti(Kitap1)).ToHaveCountAsync(1);
            await Expect(KayitKarti(Kitap2)).ToHaveCountAsync(0);
            await Expect(Ozet("active")).ToHaveTextAsync("1");

            await Page.Locator(".filter-all").ClickAsync();
            await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(2);
            await Expect(Ozet("active")).ToHaveTextAsync("2");
        });

        await Adim("5. Teslim tarihi 2 gün sonraya çekilince 'Teslimi yaklaşıyor' etiketi", async () =>
        {
            // DevTools'ta elle yapılan değişikliğin karşılığı. localStorage'daki alan adları PascalCase.
            // Testin çalıştığı gün gece yarısını geçerse "bugün" iki tarafta farklı olabilir; o zaman tekrar çalıştırın.
            await DepolamayiDegistirAsync($$"""
                const loans = JSON.parse(localStorage.getItem('kutuphane.loans'));
                const d = new Date(); d.setDate(d.getDate() + 2);
                const pad = n => String(n).padStart(2, '0');
                loans.find(l => l.Id === '{{kod1}}').DueDate = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T00:00:00`;
                localStorage.setItem('kutuphane.loans', JSON.stringify(loans));
                """);

            await GitAsync("/my-loans");
            var kart = KayitKarti(Kitap1);
            await Expect(kart.Locator(".badge-due-soon")).ToHaveTextAsync("Teslimi yaklaşıyor");
            await Expect(kart.Locator(".remaining")).ToHaveTextAsync("2 gün kaldı");
            await Expect(KayitKarti(Kitap2).Locator(".badge-due-soon")).ToHaveCountAsync(0);
        });

        await Adim("6. Kaydı iade et → rozet ve özet güncelleniyor", async () =>
        {
            await KayitKarti(Kitap1).Locator(".return-button").ClickAsync();

            await Expect(Page.Locator(".history .loan-status")).ToHaveCountAsync(1);
            await Expect(Rozet).ToHaveTextAsync("1");
            await Expect(Ozet("active")).ToHaveTextAsync("1");
            await Expect(Ozet("returned")).ToHaveTextAsync("1");
        });

        await Adim("7. Profili sil → menü 'Profil oluştur'a dönüyor, form boş geliyor", async () =>
        {
            await GitAsync("/profile");
            await Page.Locator(".delete-button").ClickAsync();
            await Page.Locator(".confirm-yes").ClickAsync();

            await Expect(Page.Locator(".feedback")).ToHaveTextAsync("Profiliniz silindi.");
            await Expect(ProfilAlani.Locator(".name")).ToHaveTextAsync("Profil oluştur");
            await Expect(ProfilAlani.Locator("a")).ToHaveAccessibleNameAsync("Profil oluştur");

            await FormuAcAsync("B004");
            await Expect(Page.Locator(".profile-hint")).ToBeVisibleAsync();
            await Expect(Page.Locator("#borrow-fullname")).ToHaveValueAsync("");
            await Expect(Page.Locator("#borrow-email")).ToHaveValueAsync("");
        });

        await Adim("8. F5 → kayıtlar duruyor, silinen profil geri gelmiyor (K11)", async () =>
        {
            await GitAsync("/my-loans");
            await YenileAsync();

            await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(2);
            await Expect(Page.Locator(".history .loan-status .code")).ToHaveTextAsync(kod1);
            await Expect(Rozet).ToHaveTextAsync("1");
            await Expect(ProfilAlani.Locator(".name")).ToHaveTextAsync("Profil oluştur");
            // Profil yokken filtre gösterilmiyor (#42).
            await Expect(Page.Locator(".filter")).ToHaveCountAsync(0);
        });
    }

    [Fact]
    public async Task Kaydedilen_profil_yenilemeden_sonra_menude_durmali()
    {
        // Senaryonun 8. adımı silinmiş profili kontrol ediyor; kayıtlı profilin kalıcılığı burada.
        await GitAsync("/profile");
        await Page.Locator("#profile-fullname").FillAsync(Ad);
        await Page.Locator("#profile-email").FillAsync(Eposta);
        await Page.Locator(".save-button").ClickAsync();
        await Expect(Page.Locator(".feedback")).ToBeVisibleAsync();

        await YenileAsync();

        await Expect(ProfilAlani.Locator(".name")).ToHaveTextAsync(Ad);
        await Expect(Page.Locator("#profile-fullname")).ToHaveValueAsync(Ad);
    }
}
