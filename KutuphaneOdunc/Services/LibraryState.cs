using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Services;

// Uygulama genelindeki kitap ve ödünç durumu. Metot gövdeleri #8'de yazılacak.
public class LibraryState
{
    public const string LoansKey = "kutuphane.loans";
    public const string AvailabilityKey = "kutuphane.availability";

    private readonly IBookService _bookService;
    private readonly ILocalStorageService _storage;
    private readonly TimeProvider _time;

    public LibraryState(IBookService bookService, ILocalStorageService storage, TimeProvider time)
    {
        _bookService = bookService;
        _storage = storage;
        _time = time;
    }

    public IReadOnlyList<Book> Books { get; private set; } = Array.Empty<Book>();
    public IReadOnlyList<Loan> Loans { get; private set; } = Array.Empty<Loan>();

    public event Action? OnChange;

    public Task InitializeAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult> BorrowAsync(string bookId, BorrowFormModel form)
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult> ExtendAsync(string loanId)
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult> ReturnAsync(string loanId)
    {
        throw new NotImplementedException();
    }

    public bool HasActiveLoan(string bookId)
    {
        throw new NotImplementedException();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
