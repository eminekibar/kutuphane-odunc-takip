using System.Text.Json;

namespace KutuphaneOdunc.Tests.Data;

// wwwroot/data/books.json dosyasının ödevdeki kurallara uyduğunu kontrol eder.
// books.json ve kapak görselleri test çıktısına TestData/ altına kopyalanır (bkz. KutuphaneOdunc.Tests.csproj).
public class BooksJsonTests
{
    private static readonly string BooksPath =
        Path.Combine(AppContext.BaseDirectory, "TestData", "books.json");

    private static List<JsonElement> LoadBooks()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(BooksPath));
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    [Fact]
    public void Dosya_bir_kitap_dizisi_olmali()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(BooksPath));
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public void En_az_15_kitap_olmali()
    {
        Assert.True(LoadBooks().Count >= 15);
    }

    [Fact]
    public void En_az_4_farkli_kategori_olmali()
    {
        var kategoriSayisi = LoadBooks()
            .Select(b => b.GetProperty("category").GetString())
            .Distinct()
            .Count();

        Assert.True(kategoriSayisi >= 4);
    }

    [Fact]
    public void Idler_benzersiz_olmali()
    {
        var idler = LoadBooks().Select(b => b.GetProperty("id").GetString()).ToList();

        Assert.Equal(idler.Count, idler.Distinct().Count());
    }

    [Theory]
    [InlineData("id")]
    [InlineData("title")]
    [InlineData("author")]
    [InlineData("category")]
    [InlineData("publisher")]
    public void Metin_alanlari_bos_olmamali(string alan)
    {
        foreach (var book in LoadBooks())
        {
            Assert.False(string.IsNullOrWhiteSpace(book.GetProperty(alan).GetString()),
                $"{book.GetProperty("id")} kitabında '{alan}' boş.");
        }
    }

    [Fact]
    public void Kopya_sayilari_gecerli_olmali()
    {
        foreach (var book in LoadBooks())
        {
            var id = book.GetProperty("id").GetString();
            var toplam = book.GetProperty("totalCopies").GetInt32();
            var mevcut = book.GetProperty("availableCopies").GetInt32();

            Assert.True(toplam >= 1, $"{id}: totalCopies en az 1 olmalı.");
            Assert.InRange(mevcut, 0, toplam);
        }
    }

    [Fact]
    public void Basim_yili_gecerli_olmali()
    {
        foreach (var book in LoadBooks())
        {
            Assert.InRange(book.GetProperty("publicationYear").GetInt32(), 1450, DateTime.Today.Year);
        }
    }

    [Fact]
    public void Stokta_olmayan_en_az_bir_kitap_olmali()
    {
        // K3'ün (pasif Ödünç Al butonu) arayüzde denenebilmesi için.
        Assert.Contains(LoadBooks(), b => b.GetProperty("availableCopies").GetInt32() == 0);
    }

    [Fact]
    public void Her_kitabin_kapak_dosyasi_olmali()
    {
        foreach (var book in LoadBooks())
        {
            var id = book.GetProperty("id").GetString();
            var coverUrl = book.GetProperty("coverUrl").GetString();

            Assert.False(string.IsNullOrWhiteSpace(coverUrl), $"{id}: coverUrl boş.");
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "TestData", coverUrl!)),
                $"{id}: {coverUrl} dosyası bulunamadı.");
        }
    }

    [Fact]
    public void Varsayilan_kapak_dosyasi_olmali()
    {
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "TestData", "images", "default.svg")));
    }
}
