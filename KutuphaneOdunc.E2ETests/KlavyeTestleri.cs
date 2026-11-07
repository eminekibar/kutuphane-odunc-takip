using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// #50: bonus özelliklerin akışları sadece klavyeyle (Tab / Shift+Tab / Enter / Boşluk).
// Fare kullanılmıyor; tıklama yerine odak Tab ile elemana getirilip tuşa basılıyor.
public class KlavyeTestleri : E2ETest
{
    public KlavyeTestleri(AppFixture app) : base(app) { }

    private IKeyboard Klavye => Page.Keyboard;

    // Odak hedefe gelene kadar Tab'a basar. Hedefe ulaşılamazsa elemana klavyeyle gidilemiyor demektir.
    private async Task TabIleGitAsync(ILocator hedef, int enFazla = 60)
    {
        for (var i = 0; i < enFazla; i++)
        {
            if (await hedef.EvaluateAsync<bool>("e => e === document.activeElement"))
                return;
            await Klavye.PressAsync("Tab");
        }
        Assert.Fail($"{hedef} elemanına {enFazla} Tab basışında ulaşılamadı.");
    }

    private Task ProfilVeKayitlariHazirlaAsync() => DepolamayiDegistirAsync("""
        const gun = n => { const d = new Date(); d.setDate(d.getDate() + n);
            const p = x => String(x).padStart(2, '0');
            return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T00:00:00`; };
        const yil = new Date().getFullYear();
        localStorage.setItem('kutuphane.profile', JSON.stringify({ FullName: 'Ayşe Yılmaz', Email: 'ayse@ornek.com' }));
        localStorage.setItem('kutuphane.loans', JSON.stringify([
            { Id: `LIB-${yil}-101`, BookId: 'B001', BorrowerName: 'Ayşe Yılmaz', Email: 'ayse@ornek.com',
              BorrowDate: gun(-2), DueDate: gun(12), ReturnedAt: null, IsExtended: false },
            { Id: `LIB-${yil}-102`, BookId: 'B003', BorrowerName: 'Ali Kaya', Email: 'ali@ornek.com',
              BorrowDate: gun(-2), DueDate: gun(12), ReturnedAt: null, IsExtended: false }
        ]));
        """);

    [Fact]
    public async Task Profil_klavyeyle_olusturulmali()
    {
        await GitAsync("/profile");

        await TabIleGitAsync(Page.Locator("#profile-fullname"));
        await Klavye.TypeAsync(Ad);
        await Klavye.PressAsync("Tab");
        await Expect(Page.Locator("#profile-email")).ToBeFocusedAsync();
        await Klavye.TypeAsync(Eposta);
        await Klavye.PressAsync("Enter");

        // Kaydedince odak mesaja taşınıyor (#39).
        await Expect(Page.Locator(".feedback")).ToBeFocusedAsync();
        await Expect(Page.Locator(".feedback")).ToHaveTextAsync("Profiliniz kaydedildi.");
        await Expect(Page.Locator(".profile-badge .name")).ToHaveTextAsync(Ad);
    }

    [Fact]
    public async Task Profil_silme_sorusu_klavyeyle_iptal_edilip_onaylanmali()
    {
        await GitAsync("/books");
        await ProfilVeKayitlariHazirlaAsync();
        await GitAsync("/profile");

        // Sil → odak soruya taşınıyor (#39).
        await TabIleGitAsync(Page.Locator(".delete-button"));
        await Klavye.PressAsync("Enter");
        await Expect(Page.Locator(".confirm")).ToBeFocusedAsync();

        // Vazgeç (Boşluk ile) → soru kapanıyor, odak "Profili sil"e dönüyor.
        await TabIleGitAsync(Page.Locator(".confirm-no"));
        await Klavye.PressAsync("Space");
        await Expect(Page.Locator(".confirm")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".delete-button")).ToBeFocusedAsync();

        // Tekrar sil → Tab ile Evet → Enter.
        await Klavye.PressAsync("Enter");
        await Expect(Page.Locator(".confirm")).ToBeFocusedAsync();
        await Klavye.PressAsync("Tab");
        await Expect(Page.Locator(".confirm-yes")).ToBeFocusedAsync();
        await Klavye.PressAsync("Enter");

        await Expect(Page.Locator(".feedback")).ToBeFocusedAsync();
        await Expect(Page.Locator(".feedback")).ToHaveTextAsync("Profiliniz silindi.");
        await Expect(Page.Locator(".profile-badge .name")).ToHaveTextAsync("Profil oluştur");
    }

    [Fact]
    public async Task Kitap_klavyeyle_profilden_dolu_formla_odunc_alinmali()
    {
        await GitAsync("/books");
        await DepolamayiDegistirAsync(
            "localStorage.setItem('kutuphane.profile', JSON.stringify({ FullName: 'Ayşe Yılmaz', Email: 'ayse@ornek.com' }));");

        await TabIleGitAsync(KitapKarti("Yazılım Tasarımına Giriş"));
        await Klavye.PressAsync("Enter");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/books/B001$"));

        await TabIleGitAsync(Page.Locator(".borrow-button"));
        await Klavye.PressAsync("Enter");

        // Form profilden dolu geliyor (#41); doğrudan gönderilebiliyor.
        await TabIleGitAsync(Page.Locator("form button[type=submit]"));
        await Expect(Page.Locator("#borrow-fullname")).ToHaveValueAsync(Ad);
        await Klavye.PressAsync("Enter");

        // Gönderince odak sonuca, bir Tab sonra "Ödünç kayıtlarıma git" linkine (#17).
        await Expect(Page.Locator(".result")).ToBeFocusedAsync();
        await Klavye.PressAsync("Tab");
        await Expect(Page.Locator(".result a[href='my-loans']")).ToBeFocusedAsync();
        await Klavye.PressAsync("Enter");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/my-loans$"));
    }

    [Fact]
    public async Task Sure_klavyeyle_uzatilip_kitap_iade_edilmeli()
    {
        await GitAsync("/books");
        await ProfilVeKayitlariHazirlaAsync();
        await GitAsync("/my-loans");
        var kart = KayitKarti("Yazılım Tasarımına Giriş");

        await TabIleGitAsync(kart.Locator(".extend-button"));
        await Klavye.PressAsync("Enter");
        await Expect(Page.Locator(".feedback")).ToBeFocusedAsync();
        await Expect(kart.Locator(".badge-extended")).ToBeVisibleAsync();

        await TabIleGitAsync(kart.Locator(".return-button"));
        await Klavye.PressAsync("Enter");
        await Expect(Page.Locator(".feedback")).ToBeFocusedAsync();
        await Expect(Page.Locator(".history .loan-status")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Sadece_benim_kayitlarim_klavyeyle_secilmeli()
    {
        await GitAsync("/books");
        await ProfilVeKayitlariHazirlaAsync();
        await GitAsync("/my-loans");
        await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(2);

        await TabIleGitAsync(Page.Locator(".filter-mine"));
        await Klavye.PressAsync("Space");
        await Expect(Page.Locator(".filter-mine")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(1);

        // Shift+Tab ile geri dönüp "Tüm kayıtlar".
        await Klavye.PressAsync("Shift+Tab");
        await Expect(Page.Locator(".filter-all")).ToBeFocusedAsync();
        await Klavye.PressAsync("Enter");
        await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task Siralama_ve_stok_filtresi_klavyeyle_degistirilmeli()
    {
        await GitAsync("/books");
        var ilkKart = Page.Locator(".book-card .title").First;
        var oncekiIlk = await ilkKart.TextContentAsync();

        // Açılır liste kapalıyken ok tuşu bir sonraki seçeneği seçiyor.
        await TabIleGitAsync(Page.Locator("#book-sort"));
        await Klavye.PressAsync("ArrowDown");
        await Expect(Page.Locator("#book-sort")).Not.ToHaveValueAsync(await Page.Locator("#book-sort option").First.GetAttributeAsync("value") ?? "");
        await Expect(ilkKart).Not.ToHaveTextAsync(oncekiIlk!);

        // Onay kutusu Boşluk ile işaretleniyor; stokta olmayan kitaplar gizleniyor.
        await Expect(Page.Locator(".out-of-stock")).Not.ToHaveCountAsync(0);
        await TabIleGitAsync(Page.Locator("#book-available-only"));
        await Klavye.PressAsync("Space");
        await Expect(Page.Locator("#book-available-only")).ToBeCheckedAsync();
        await Expect(Page.Locator(".out-of-stock")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Benzer_kitaba_klavyeyle_gecince_odak_yeni_basliga_gitmeli()
    {
        await GitAsync("/books/B001");

        await TabIleGitAsync(Page.Locator(".similar-books .book-card").First);
        await Klavye.PressAsync("Enter");

        // Sayfa değişmeden kitap değişiyor; odak yeni kitabın başlığına taşınıyor (#48).
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/books/B003$"));
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Veri Yapıları ve Algoritmalar");
        await Expect(Page.Locator("h1")).ToBeFocusedAsync();
        await Klavye.PressAsync("Tab");
        await Expect(Page.Locator(".borrow-button")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Takvime_ekle_klavyeyle_dosya_indirmeli()
    {
        await GitAsync("/books");
        await ProfilVeKayitlariHazirlaAsync();
        await GitAsync("/my-loans");

        await TabIleGitAsync(KayitKarti("Yazılım Tasarımına Giriş").Locator(".calendar-button"));
        var indirme = await Page.RunAndWaitForDownloadAsync(() => Klavye.PressAsync("Enter"));

        Assert.EndsWith(".ics", indirme.SuggestedFilename);
    }
}
