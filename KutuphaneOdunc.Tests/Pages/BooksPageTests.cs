using Bunit;
using KutuphaneOdunc.Models;
using KutuphaneOdunc.Pages;
using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOdunc.Tests.Pages;

public class BooksPageTests : BunitContext
{
    private static List<Book> OrnekKitaplar() => new()
    {
        new() { Id = "B001", Title = "Yazılım Tasarımına Giriş", Author = "Ayşe Yılmaz", Category = "Bilgisayar Bilimleri", TotalCopies = 5, AvailableCopies = 3 },
        new() { Id = "B002", Title = "Araştırma Yöntemlerine Giriş", Author = "Murat Demir", Category = "Bilim", TotalCopies = 2, AvailableCopies = 0 }
    };

    // Sayfanın kullandığı LibraryState'i gerçek servisler yerine sahte servislerle kurar.
    private void ServisleriKaydet(IBookService bookService)
    {
        Services.AddSingleton(bookService);
        Services.AddSingleton<ILocalStorageService>(new FakeLocalStorage());
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTime(2026, 11, 10)));
        Services.AddScoped<LibraryState>();
    }

    [Fact]
    public void Sayfa_acilinca_kitaplar_listelenmeli()
    {
        ServisleriKaydet(new FakeBookService(OrnekKitaplar()));

        var cut = Render<Books>();

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".book-card").Count));
        Assert.Empty(cut.FindAll(".loading"));
    }

    [Fact]
    public void Kitaplar_yuklenirken_yukleniyor_yazmali()
    {
        var bekleyen = new BekleyenBookService();
        ServisleriKaydet(bekleyen);

        var cut = Render<Books>();

        Assert.Equal("Yükleniyor...", cut.Find(".loading").TextContent);
        Assert.Empty(cut.FindAll(".book-card"));

        bekleyen.Tamamla(OrnekKitaplar());

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".book-card").Count));
    }

    [Fact]
    public void Kitaplar_okunamazsa_hata_mesaji_gorunmeli()
    {
        ServisleriKaydet(new HataVerenBookService());

        var cut = Render<Books>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Kitap listesi yüklenemedi", cut.Find(".alert-danger").TextContent));
        Assert.Empty(cut.FindAll(".book-card"));
    }

    [Fact]
    public void Kitap_secilince_detay_sayfasina_gidilmeli()
    {
        ServisleriKaydet(new FakeBookService(OrnekKitaplar()));
        var navigation = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Books>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".book-card").Count));

        // Liste ada göre sıralı geldiği için kart sırasıyla değil adıyla bulunuyor (#49).
        cut.FindAll(".book-card").Single(k => k.QuerySelector(".title")!.TextContent == "Yazılım Tasarımına Giriş").Click();

        Assert.EndsWith("/books/B001", navigation.Uri);
    }

    // Sonucu test istediğinde veren servis: "Yükleniyor..." durumunu yakalamak için.
    private sealed class BekleyenBookService : IBookService
    {
        private readonly TaskCompletionSource<List<Book>> _sonuc = new();

        public Task<List<Book>> GetBooksAsync() => _sonuc.Task;

        public void Tamamla(List<Book> kitaplar) => _sonuc.SetResult(kitaplar);
    }

    // books.json okunamamış gibi hata fırlatan servis.
    private sealed class HataVerenBookService : IBookService
    {
        public Task<List<Book>> GetBooksAsync() =>
            Task.FromException<List<Book>>(new HttpRequestException("books.json okunamadı"));
    }
}
