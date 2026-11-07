using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Services;

public interface IBookService
{
    Task<List<Book>> GetBooksAsync();
}
