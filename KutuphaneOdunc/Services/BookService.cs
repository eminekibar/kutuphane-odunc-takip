using System.Net.Http.Json;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Services;

public class BookService : IBookService
{
    private readonly HttpClient _http;

    public BookService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Book>> GetBooksAsync()
    {
        var books = await _http.GetFromJsonAsync<List<Book>>("data/books.json");
        return books ?? new List<Book>();
    }
}
