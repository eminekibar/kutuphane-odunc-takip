using System.Text;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// #50: sayfaların axe-core ile otomatik erişilebilirlik taraması.
// Chrome'daki Lighthouse'un Accessibility bölümü de axe-core kurallarını kullanıyor; bu testler aynı kontrolü her PR'da yapıyor.
public class ErisilebilirlikTestleri : E2ETest
{
    public ErisilebilirlikTestleri(AppFixture app) : base(app) { }

    // Sadece WCAG 2.0 ve 2.1'in A ve AA kuralları. axe'nin "best practice" kuralları WCAG şartı değil.
    private static readonly AxeRunOptions WcagKurallari = new()
    {
        RunOnly = new RunOnlyOptions
        {
            Type = "tag",
            Values = new List<string> { "wcag2a", "wcag2aa", "wcag21a", "wcag21aa" }
        }
    };

    private async Task TaraAsync()
    {
        // Bildirim 0,2 sn'lik bir geçişle (opacity 0 → 1) ekrana giriyor. Geçişin ortasında taranırsa yazı yarı saydam
        // olduğu için kontrast düşük ölçülüyor. Kullanıcının gördüğü durağan hal taransın diye animasyonların bitmesi bekleniyor.
        await Page.WaitForFunctionAsync("() => document.getAnimations().every(a => a.playState !== 'running')");

        var sonuc = await Page.RunAxe(WcagKurallari);

        if (sonuc.Violations.Length == 0)
            return;

        // Test başarısız olursa hangi kuralın, nerede bozulduğu mesajda görünsün.
        var mesaj = new StringBuilder($"{Page.Url} sayfasında {sonuc.Violations.Length} erişilebilirlik ihlali:");
        foreach (var ihlal in sonuc.Violations)
        {
            mesaj.AppendLine().Append($"- {ihlal.Id} ({ihlal.Impact}): {ihlal.Help}");
            foreach (var eleman in ihlal.Nodes)
                mesaj.AppendLine().Append($"    {eleman.Target}");
        }
        Assert.Fail(mesaj.ToString());
    }

    private async Task ProfilKaydetAsync()
    {
        await GitAsync("/profile");
        await Page.Locator("#profile-fullname").FillAsync(Ad);
        await Page.Locator("#profile-email").FillAsync(Eposta);
        await Page.Locator(".save-button").ClickAsync();
        await Expect(Page.Locator(".feedback")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Kitaplar_sayfasi_WCAG_ihlali_icermemeli()
    {
        await GitAsync("/books");

        await TaraAsync();
    }

    [Fact]
    public async Task Kitap_detayi_form_acikken_WCAG_ihlali_icermemeli()
    {
        await FormuAcAsync("B001");
        await Expect(Page.Locator("form.borrow-form, .borrow-form form")).ToHaveCountAsync(1);

        await TaraAsync();
    }

    [Fact]
    public async Task Kitap_detayi_hatali_form_ve_sonuc_kutusuyla_WCAG_ihlali_icermemeli()
    {
        // Hata mesajları ve başarı kutusu da renk ve etiket açısından taransın.
        await FormuAcAsync("B001");
        await FormuGonderAsync();
        await Expect(Page.Locator(".validation-message")).ToHaveCountAsync(2);
        await TaraAsync();

        await FormuDoldurAsync(Ad, Eposta);
        await FormuGonderAsync();
        await Expect(Page.Locator(".result")).ToBeVisibleAsync();
        await TaraAsync();
    }

    [Fact]
    public async Task Odunc_kayitlari_kayit_profil_ve_filtre_varken_WCAG_ihlali_icermemeli()
    {
        await ProfilKaydetAsync();
        await OduncAlAsync("B001");
        await OduncAlAsync("B003", "Ali Kaya", "ali@ornek.com");
        await GitAsync("/my-loans");
        await KayitKarti("Veri Yapıları ve Algoritmalar").Locator(".return-button").ClickAsync();

        // Özet, filtre, aktif ve iade edilmiş kayıtlar, menüdeki rozet ve profil alanı aynı anda ekranda.
        await Expect(Page.Locator(".filter")).ToBeVisibleAsync();
        await Expect(Page.Locator(".loan-summary")).ToBeVisibleAsync();
        await Expect(Page.Locator(".history .loan-status")).ToHaveCountAsync(1);
        await TaraAsync();

        await Page.Locator(".filter-mine").ClickAsync();
        await TaraAsync();
    }

    [Fact]
    public async Task Profil_sayfasi_bos_dolu_ve_silme_sorusuyla_WCAG_ihlali_icermemeli()
    {
        await GitAsync("/profile");
        await TaraAsync();

        await ProfilKaydetAsync();
        await TaraAsync();

        await Page.Locator(".delete-button").ClickAsync();
        await Expect(Page.Locator(".confirm")).ToBeVisibleAsync();
        await TaraAsync();
    }

    [Fact]
    public async Task Bildirim_gorunurken_WCAG_ihlali_icermemeli()
    {
        await OduncAlAsync("B001");
        await Expect(Page.Locator(".toast-item")).ToBeVisibleAsync();

        await TaraAsync();
    }

    [Fact]
    public async Task Bulunamayan_sayfa_WCAG_ihlali_icermemeli()
    {
        await GitAsync("/bu-sayfa-yok");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Sayfa bulunamadı");

        await TaraAsync();
    }

    [Fact]
    public async Task Bulunamayan_kitap_WCAG_ihlali_icermemeli()
    {
        await GitAsync("/books/B999");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Kitap bulunamadı");

        await TaraAsync();
    }
}
