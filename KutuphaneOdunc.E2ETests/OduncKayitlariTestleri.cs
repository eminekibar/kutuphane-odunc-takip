using static Microsoft.Playwright.Assertions;

namespace KutuphaneOdunc.E2ETests;

// K7–K10: ödünç kayıtları, süre uzatma ve iade
public class OduncKayitlariTestleri : E2ETest
{
    public OduncKayitlariTestleri(AppFixture app) : base(app) { }

    private const string Kitap1 = "Yazılım Tasarımına Giriş";   // B001, 5 kopya, 3 mevcut
    private const string Kitap3 = "Veri Yapıları ve Algoritmalar"; // B003
    private const string Kitap5 = "Ağ Güvenliğinin Temelleri";   // B005, tek kopya

    [Fact]
    public async Task Kayit_yokken_bos_mesaj_gorunmeli()
    {
        await GitAsync("/my-loans");

        await Expect(Page.Locator(".empty")).ToContainTextAsync("Henüz ödünç kaydınız yok.");
    }

    [Fact]
    public async Task K10_kayit_bilgileri_ve_aktif_etiketi_gorunmeli()
    {
        var kod = await OduncAlAsync("B001");

        await GitAsync("/my-loans");
        var kart = KayitKarti(Kitap1);

        await Expect(kart.Locator(".facts dd")).ToHaveTextAsync(new[]
        {
            kod, Tarih(Bugun), Tarih(Bugun.AddDays(14)), "14 gün"
        });
        await Expect(kart.Locator(".badge-state")).ToHaveTextAsync("Aktif");
    }

    [Fact]
    public async Task K7_sure_bir_kez_7_gun_uzatilabilmeli()
    {
        await OduncAlAsync("B001");
        await GitAsync("/my-loans");
        var kart = KayitKarti(Kitap1);

        await kart.Locator(".extend-button").ClickAsync();

        await Expect(kart.Locator(".remaining")).ToHaveTextAsync("21 gün");
        await Expect(kart.Locator(".badge-extended")).ToHaveTextAsync("Uzatıldı");
        await Expect(kart.Locator(".extend-button")).ToBeDisabledAsync();
        await Expect(Page.Locator(".feedback")).ToContainTextAsync("7 gün uzatıldı");
    }

    [Fact]
    public async Task K7_K10_gecikmis_kayit_kirmizi_ve_uzatilamaz_olmali()
    {
        await OduncAlAsync("B003");
        await GitAsync("/my-loans");
        await DepolamayiDegistirAsync("""
            const loans = JSON.parse(localStorage.getItem('kutuphane.loans'));
            const d = new Date(); d.setDate(d.getDate() - 3);
            const pad = n => String(n).padStart(2, '0');
            loans.find(l => l.BookId === 'B003').DueDate =
              `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T00:00:00`;
            localStorage.setItem('kutuphane.loans', JSON.stringify(loans));
            """);
        var kart = KayitKarti(Kitap3);

        await Expect(kart.Locator(".badge-state")).ToHaveTextAsync("Gecikmiş");
        await Expect(kart.Locator(".remaining")).ToHaveTextAsync("3 gün gecikti");
        await Expect(kart.Locator(".extend-button")).ToBeDisabledAsync();
        await Expect(kart.Locator(".return-button")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task K8_K9_iade_edilen_kayit_gecmise_inmeli_ve_islem_yapilamamali()
    {
        await OduncAlAsync("B005");
        await GitAsync("/my-loans");

        await KayitKarti(Kitap5).Locator(".return-button").ClickAsync();

        var kart = Page.Locator(".loan-section.history .loan-status").Filter(new() { HasText = Kitap5 });
        await Expect(kart.Locator(".badge-state")).ToHaveTextAsync("İade edildi");
        await Expect(kart).ToContainTextAsync(Tarih(Bugun));
        await Expect(kart.Locator(".extend-button")).ToBeDisabledAsync();
        await Expect(kart.Locator(".return-button")).ToBeDisabledAsync();
    }

    [Fact]
    public async Task K8_K4_iadeden_sonra_kopya_artmali_ve_kitap_tekrar_alinabilmeli()
    {
        await OduncAlAsync("B005");
        await GitAsync("/my-loans");
        await KayitKarti(Kitap5).Locator(".return-button").ClickAsync();
        await Expect(Page.Locator(".loan-section.history")).ToBeVisibleAsync();

        await GitAsync("/books/B005");
        await Expect(Page.Locator(".available")).ToHaveTextAsync("1");
        await Expect(Page.Locator(".borrow-button")).ToBeEnabledAsync();

        Assert.Equal(Kod(102), await OduncAlAsync("B005"));
    }

    [Fact]
    public async Task Kayitta_odunc_alan_kisi_gorunmeli_ve_profil_e_postasiyla_suzulebilmeli()
    {
        await OduncAlAsync("B001");
        await OduncAlAsync("B003", "Mehmet Demir", "mehmet@ornek.com");

        await GitAsync("/my-loans");
        await Expect(KayitKarti(Kitap1).Locator(".borrower")).ToHaveTextAsync("Ödünç alan: Ayşe Yılmaz (ayse@ornek.com)");
        await Expect(KayitKarti(Kitap3).Locator(".borrower")).ToHaveTextAsync("Ödünç alan: Mehmet Demir (mehmet@ornek.com)");
        await Expect(Page.Locator(".filter")).ToHaveCountAsync(0);

        await DepolamayiDegistirAsync("""
            localStorage.setItem('kutuphane.profile', JSON.stringify({ FullName: 'Ayşe Yılmaz', Email: 'AYSE@ornek.com' }));
            """);

        await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(2);
        await Page.Locator(".filter-mine").ClickAsync();
        await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(1);
        await Expect(KayitKarti(Kitap1)).ToBeVisibleAsync();

        await Page.Locator(".filter-all").ClickAsync();
        await Expect(Page.Locator(".loan-status")).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task Teslimine_2_gun_kalan_kayitta_yaklasiyor_etiketi_gorunmeli()
    {
        await OduncAlAsync("B001");
        await DepolamayiDegistirAsync("""
            const loans = JSON.parse(localStorage.getItem('kutuphane.loans'));
            const d = new Date(); d.setDate(d.getDate() + 2);
            const pad = n => String(n).padStart(2, '0');
            loans[0].DueDate = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T00:00:00`;
            localStorage.setItem('kutuphane.loans', JSON.stringify(loans));
            """);

        await GitAsync("/my-loans");
        var kart = KayitKarti(Kitap1);

        await Expect(kart.Locator(".badge-state")).ToHaveTextAsync("Aktif");
        await Expect(kart.Locator(".badge-due-soon")).ToHaveTextAsync("Teslimi yaklaşıyor");
        await Expect(kart.Locator(".remaining")).ToHaveTextAsync("2 gün kaldı");
    }

    [Fact]
    public async Task K8_iade_toplam_kopyayi_asmamali()
    {
        await OduncAlAsync("B001");
        await DepolamayiDegistirAsync("""
            const a = JSON.parse(localStorage.getItem('kutuphane.availability'));
            a.B001 = 5;
            localStorage.setItem('kutuphane.availability', JSON.stringify(a));
            """);

        await GitAsync("/my-loans");
        await KayitKarti(Kitap1).Locator(".return-button").ClickAsync();
        await Expect(Page.Locator(".loan-section.history")).ToBeVisibleAsync();

        await GitAsync("/books/B001");
        await Expect(Page.Locator(".available")).ToHaveTextAsync("5");
    }
}
