using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;

namespace KutuphaneOdunc.Tests.Services;

public class BookServiceTests
{
    private const string SampleJson = """
        [
          {
            "id": "B001",
            "title": "Yazılım Tasarımına Giriş",
            "author": "Ayşe Yılmaz",
            "category": "Bilgisayar Bilimleri",
            "publisher": "Örnek Yayınları",
            "publicationYear": 2022,
            "coverUrl": "images/B001.svg",
            "totalCopies": 5,
            "availableCopies": 3
          },
          {
            "id": "B002",
            "title": "Araştırma Yöntemlerine Giriş",
            "author": "Murat Demir",
            "category": "Bilim",
            "publisher": "Akademi Kitap",
            "publicationYear": 2020,
            "coverUrl": "images/B002.svg",
            "totalCopies": 2,
            "availableCopies": 0
          }
        ]
        """;

    private static (BookService Service, FakeHttpMessageHandler Handler) CreateService(string json)
    {
        var handler = new FakeHttpMessageHandler(json);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return (new BookService(http), handler);
    }

    [Fact]
    public async Task Json_dosyasindaki_kitap_sayisi_kadar_kitap_donmeli()
    {
        var (service, _) = CreateService(SampleJson);

        var books = await service.GetBooksAsync();

        Assert.Equal(2, books.Count);
    }

    [Fact]
    public async Task Kitabin_butun_alanlari_dolu_gelmeli()
    {
        var (service, _) = CreateService(SampleJson);

        var book = (await service.GetBooksAsync())[0];

        Assert.Equal("B001", book.Id);
        Assert.Equal("Yazılım Tasarımına Giriş", book.Title);
        Assert.Equal("Ayşe Yılmaz", book.Author);
        Assert.Equal("Bilgisayar Bilimleri", book.Category);
        Assert.Equal("Örnek Yayınları", book.Publisher);
        Assert.Equal(2022, book.PublicationYear);
        Assert.Equal("images/B001.svg", book.CoverUrl);
        Assert.Equal(5, book.TotalCopies);
        Assert.Equal(3, book.AvailableCopies);
    }

    [Fact]
    public async Task Istek_data_books_json_adresine_gitmeli()
    {
        var (service, handler) = CreateService(SampleJson);

        await service.GetBooksAsync();

        Assert.Equal("http://localhost/data/books.json", handler.LastRequestUri?.ToString());
    }

    [Fact]
    public async Task Gercek_books_json_dosyasi_okunabilmeli()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "books.json"));
        var (service, _) = CreateService(json);

        var books = await service.GetBooksAsync();

        Assert.Equal(18, books.Count);
        Assert.All(books, b =>
        {
            Assert.False(string.IsNullOrWhiteSpace(b.Id));
            Assert.False(string.IsNullOrWhiteSpace(b.Title));
            Assert.True(b.TotalCopies >= 1);
        });
    }
}
