using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Services;

// Uygulama genelindeki kitap ve ödünç durumu. Her değişiklik localStorage'a yazılır.
public class LibraryState
{
    public const string LoansKey = "kutuphane.loans";
    public const string AvailabilityKey = "kutuphane.availability";

    private const int LoanDays = 14;
    private const int ExtensionDays = 7;
    private const int FirstSequence = 101;

    private readonly IBookService _bookService;
    private readonly ILocalStorageService _storage;
    private readonly TimeProvider _time;

    private List<Book> _books = new();
    private List<Loan> _loans = new();
    private Task? _initializeTask;

    public LibraryState(IBookService bookService, ILocalStorageService storage, TimeProvider time)
    {
        _bookService = bookService;
        _storage = storage;
        _time = time;
    }

    public IReadOnlyList<Book> Books => _books;
    public IReadOnlyList<Loan> Loans => _loans;

    public event Action? OnChange;

    private DateTime Today => _time.GetLocalNow().Date;

    // Birden fazla sayfa aynı anda çağırsa da yükleme bir kez yapılır; sonraki çağrılar aynı Task'ı bekler.
    public Task InitializeAsync()
    {
        _initializeTask ??= LoadAsync();
        return _initializeTask;
    }

    private async Task LoadAsync()
    {
        _books = await _bookService.GetBooksAsync();

        var availability = await _storage.GetAsync<Dictionary<string, int>>(AvailabilityKey);
        if (availability is not null)
        {
            foreach (var book in _books)
            {
                if (availability.TryGetValue(book.Id, out var copies))
                    book.AvailableCopies = Math.Clamp(copies, 0, book.TotalCopies);
            }
        }

        _loans = await _storage.GetAsync<List<Loan>>(LoansKey) ?? new List<Loan>();
    }

    public async Task<ActionResult> BorrowAsync(string bookId, BorrowFormModel form)
    {
        var book = _books.FirstOrDefault(b => b.Id == bookId);
        if (book is null)
            return ActionResult.Fail("Kitap bulunamadı.");

        if (HasActiveLoan(bookId))
            return ActionResult.Fail("Bu kitap için iade edilmemiş bir ödünç kaydınız var.");

        if (book.AvailableCopies <= 0)
            return ActionResult.Fail("Bu kitabın şu anda ödünç alınabilir kopyası yok.");

        var today = Today;
        var loan = new Loan
        {
            Id = $"LIB-{today.Year}-{NextSequence()}",
            BookId = book.Id,
            BorrowerName = form.FullName.Trim(),
            Email = form.Email.Trim(),
            BorrowDate = today,
            DueDate = today.AddDays(LoanDays)
        };

        _loans.Add(loan);
        book.AvailableCopies = Math.Max(0, book.AvailableCopies - 1);

        await SaveAsync();
        return ActionResult.Ok($"Kitap ödünç alındı. Ödünç kodunuz: {loan.Id}", loan);
    }

    public async Task<ActionResult> ExtendAsync(string loanId)
    {
        var loan = _loans.FirstOrDefault(l => l.Id == loanId);
        if (loan is null)
            return ActionResult.Fail("Ödünç kaydı bulunamadı.");

        var today = Today;
        if (!LoanRules.CanExtend(loan, today))
        {
            var message = LoanRules.GetState(loan, today) switch
            {
                LoanState.Returned => "İade edilmiş bir kaydın süresi uzatılamaz.",
                LoanState.Overdue => "Teslim tarihi geçmiş bir kaydın süresi uzatılamaz.",
                _ => "Bu kaydın süresi daha önce uzatıldı. Süre yalnızca bir kez uzatılabilir."
            };
            return ActionResult.Fail(message);
        }

        loan.DueDate = loan.DueDate.AddDays(ExtensionDays);
        loan.IsExtended = true;

        await SaveAsync();
        return ActionResult.Ok($"Süre 7 gün uzatıldı. Yeni teslim tarihi: {loan.DueDate:dd.MM.yyyy}", loan);
    }

    public async Task<ActionResult> ReturnAsync(string loanId)
    {
        var loan = _loans.FirstOrDefault(l => l.Id == loanId);
        if (loan is null)
            return ActionResult.Fail("Ödünç kaydı bulunamadı.");

        if (loan.ReturnedAt is not null)
            return ActionResult.Fail("Bu kayıt zaten iade edildi.");

        loan.ReturnedAt = Today;

        var book = _books.FirstOrDefault(b => b.Id == loan.BookId);
        if (book is not null)
            book.AvailableCopies = Math.Min(book.TotalCopies, book.AvailableCopies + 1);

        await SaveAsync();
        return ActionResult.Ok("Kitap iade edildi.", loan);
    }

    public bool HasActiveLoan(string bookId)
    {
        return _loans.Any(l => l.BookId == bookId && l.ReturnedAt is null);
    }

    // Kod LIB-{yıl}-{sıra} biçiminde. Sıra yıldan bağımsız olarak en büyük sıranın bir fazlası.
    private int NextSequence()
    {
        var max = FirstSequence - 1;
        foreach (var loan in _loans)
        {
            var parts = loan.Id.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var sequence) && sequence > max)
                max = sequence;
        }
        return max + 1;
    }

    private async Task SaveAsync()
    {
        await _storage.SetAsync(LoansKey, _loans);
        await _storage.SetAsync(AvailabilityKey, _books.ToDictionary(b => b.Id, b => b.AvailableCopies));
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
