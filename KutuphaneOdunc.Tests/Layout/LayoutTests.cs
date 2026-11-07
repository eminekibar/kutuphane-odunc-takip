using Bunit;
using System.Reflection;
using KutuphaneOdunc.Layout;
using KutuphaneOdunc.Models;
using KutuphaneOdunc.Pages;
using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOdunc.Tests.Layout;

public class LayoutTests : BunitContext
{
    private static readonly DateTime Bugun = new(2026, 11, 10);

    private readonly FakeLocalStorage _storage = new();

    // Menüdeki profil alanı (ProfileBadge, #40) ProfileService, ödünç rozeti (#45) LibraryState kullanıyor.
    public LayoutTests()
    {
        Services.AddSingleton<ILocalStorageService>(_storage);
        Services.AddScoped<ProfileService>();
        Services.AddSingleton<IBookService>(new FakeBookService(new[]
        {
            new Book { Id = "B001", Title = "Yazılım Tasarımına Giriş", TotalCopies = 5, AvailableCopies = 3 },
            new Book { Id = "B002", Title = "Araştırma Yöntemlerine Giriş", TotalCopies = 2, AvailableCopies = 1 },
            new Book { Id = "B003", Title = "Veri Yapıları ve Algoritmalar", TotalCopies = 1, AvailableCopies = 1 }
        }));
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Bugun));
        Services.AddScoped<LibraryState>();
    }

    private LibraryState Library => Services.GetRequiredService<LibraryState>();

    // Menü açılmadan önce localStorage'a kayıt yazar; LibraryState açılışta bunları yükler.
    private void KayitlariYaz(params Loan[] kayitlar) =>
        _storage.SetAsync(LibraryState.LoansKey, kayitlar.ToList()).Wait();

    private static Loan Kayit(string kod, string kitapId, DateTime teslim, DateTime? iade = null) => new()
    {
        Id = kod,
        BookId = kitapId,
        BorrowerName = "Ayşe Yılmaz",
        Email = "ayse@ornek.com",
        BorrowDate = teslim.AddDays(-14),
        DueDate = teslim,
        ReturnedAt = iade
    };

    private IRenderedComponent<NavMenu> MenuyuAc()
    {
        var cut = Render<NavMenu>();
        cut.WaitForState(() => Library.Books.Count > 0);
        return cut;
    }

    [Fact]
    public void Menude_kitaplar_ve_odunc_kayitlarim_linkleri_olmali()
    {
        var cut = Render<NavMenu>();

        var linkler = cut.FindAll("a.nav-link").Select(a => a.GetAttribute("href")).ToList();

        Assert.Contains("books", linkler);
        Assert.Contains("my-loans", linkler);
    }

    [Fact]
    public void Menude_profil_alani_olmali_ve_ayri_Profilim_linki_olmamali()
    {
        var cut = Render<NavMenu>();

        // #39'daki "Profilim" linkinin yerini menünün altındaki profil alanı aldı; aynı sayfaya iki link olmasın.
        Assert.DoesNotContain(cut.FindAll("a.nav-link"), a => a.GetAttribute("href") == "profile");
        Assert.Equal("profile", cut.Find(".profile-badge a").GetAttribute("href"));
    }

    // Ödünç rozeti (#45)

    [Fact]
    public void Kayit_yokken_rozet_olmamali()
    {
        var cut = MenuyuAc();

        Assert.Empty(cut.FindAll(".loan-badge"));
    }

    [Fact]
    public void Iki_aktif_kayitta_rozet_2_olmali()
    {
        KayitlariYaz(
            Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)),
            Kayit("LIB-2026-102", "B002", teslim: Bugun.AddDays(10)));

        var cut = MenuyuAc();

        var rozet = cut.Find("a[href='my-loans'] .loan-badge");
        Assert.Equal("2", rozet.TextContent);
        Assert.DoesNotContain("overdue", rozet.ClassList);
        Assert.Equal("true", rozet.GetAttribute("aria-hidden"));
        Assert.Equal("(2 iade edilmemiş kayıt)", cut.Find("a[href='my-loans'] .visually-hidden").TextContent);
    }

    [Fact]
    public void Gecikmis_kayit_varken_rozet_kirmizi_olmali()
    {
        KayitlariYaz(
            Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)),
            Kayit("LIB-2026-102", "B002", teslim: Bugun.AddDays(-2)),
            Kayit("LIB-2026-103", "B003", teslim: Bugun.AddDays(-4), iade: Bugun.AddDays(-6)));

        var cut = MenuyuAc();

        // İade edilmiş kayıt sayılmaz: 1 aktif + 1 gecikmiş.
        var rozet = cut.Find(".loan-badge");
        Assert.Equal("2", rozet.TextContent);
        Assert.Contains("overdue", rozet.ClassList);
        Assert.Equal("(2 iade edilmemiş kayıt, 1 gecikmiş)", cut.Find("a[href='my-loans'] .visually-hidden").TextContent);
    }

    [Fact]
    public async Task Iade_yapilinca_rozet_azalmali_ve_son_iadede_kaybolmali()
    {
        KayitlariYaz(
            Kayit("LIB-2026-101", "B001", teslim: Bugun.AddDays(5)),
            Kayit("LIB-2026-102", "B002", teslim: Bugun.AddDays(10)));
        var cut = MenuyuAc();

        // İade sayfada yapılıyormuş gibi doğrudan LibraryState üzerinden; menü OnChange ile haber alır.
        await cut.InvokeAsync(() => Library.ReturnAsync("LIB-2026-101"));
        cut.WaitForAssertion(() => Assert.Equal("1", cut.Find(".loan-badge").TextContent));

        await cut.InvokeAsync(() => Library.ReturnAsync("LIB-2026-102"));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".loan-badge")));
    }

    [Fact]
    public async Task Odunc_alininca_rozet_gorunmeli()
    {
        var cut = MenuyuAc();

        await cut.InvokeAsync(() => Library.BorrowAsync("B001", new BorrowFormModel { FullName = "Ayşe Yılmaz", Email = "ayse@ornek.com" }));

        cut.WaitForAssertion(() => Assert.Equal("1", cut.Find(".loan-badge").TextContent));
    }

    [Fact]
    public async Task Menu_kapaninca_OnChange_aboneligi_birakilmali()
    {
        MenuyuAc();
        Assert.NotNull(AboneListesi());

        await DisposeComponentsAsync();

        Assert.Null(AboneListesi());
    }

    // OnChange event'inin arkasındaki alan: abone yoksa null.
    private Delegate? AboneListesi() =>
        (Delegate?)typeof(LibraryState)
            .GetField(nameof(LibraryState.OnChange), BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(Library);

    [Fact]
    public void Layoutta_bildirim_alani_olmali()
    {
        Services.AddScoped<KutuphaneOdunc.Services.ToastService>();

        var cut = Render<MainLayout>();

        Assert.NotNull(cut.Find(".toast-host"));
    }

    [Fact]
    public void Ana_sayfa_kitaplar_sayfasina_yonlendirmeli()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();

        Render<Home>();

        Assert.EndsWith("/books", navigation.Uri);
    }
}
