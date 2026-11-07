using KutuphaneOdunc.Models;
using KutuphaneOdunc.Services;

namespace KutuphaneOdunc.Tests.Fakes;

// books.json yerine verilen kitapları döndürür.
// Her çağrıda yeni kopyalar döner: gerçek servis de her seferinde dosyayı baştan okur.
// Böylece iki LibraryState aynı Book nesnelerini paylaşmaz.
public class FakeBookService : IBookService
{
    private readonly List<Book> _books;

    public FakeBookService(IEnumerable<Book> books)
    {
        _books = books.ToList();
    }

    public Task<List<Book>> GetBooksAsync()
    {
        var copies = _books.Select(b => new Book
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            Category = b.Category,
            Publisher = b.Publisher,
            PublicationYear = b.PublicationYear,
            CoverUrl = b.CoverUrl,
            TotalCopies = b.TotalCopies,
            AvailableCopies = b.AvailableCopies
        }).ToList();

        return Task.FromResult(copies);
    }
}
