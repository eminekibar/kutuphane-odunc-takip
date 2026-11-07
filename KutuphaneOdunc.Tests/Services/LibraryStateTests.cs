using KutuphaneOdunc.Models;
using KutuphaneOdunc.Services;
using KutuphaneOdunc.Tests.Fakes;

namespace KutuphaneOdunc.Tests.Services;

// LibraryState'in kural testleri. Metotlar #8'de yazılacak, o zamana kadar testler kapalı.
public class LibraryStateTests
{
    private const string Skip = "#8 ile açılacak";

    private static readonly DateTime Today = new(2026, 11, 10, 10, 30, 0);

    private static readonly BorrowFormModel Form = new() { FullName = "Ayşe Yılmaz", Email = "ayse@ornek.com" };

    // B001: 2 kopya, ikisi de rafta. B002: stokta yok. B003: tek kopya.
    private static List<Book> SampleBooks() => new()
    {
        new() { Id = "B001", Title = "Yazılım Tasarımına Giriş", Author = "Ayşe Yılmaz", Category = "Bilgisayar Bilimleri", TotalCopies = 2, AvailableCopies = 2 },
        new() { Id = "B002", Title = "Araştırma Yöntemlerine Giriş", Author = "Murat Demir", Category = "Bilim", TotalCopies = 2, AvailableCopies = 0 },
        new() { Id = "B003", Title = "Veri Yapıları ve Algoritmalar", Author = "Kerem Aydın", Category = "Bilgisayar Bilimleri", TotalCopies = 1, AvailableCopies = 1 }
    };

    private static async Task<LibraryState> CreateStateAsync(FakeLocalStorage? storage = null, FixedTimeProvider? time = null)
    {
        var state = new LibraryState(
            new FakeBookService(SampleBooks()),
            storage ?? new FakeLocalStorage(),
            time ?? new FixedTimeProvider(Today));
        await state.InitializeAsync();
        return state;
    }

    private static Book BookById(LibraryState state, string id) => state.Books.Single(b => b.Id == id);

    // K4

    [Fact(Skip = Skip)]
    public async Task Aktif_kaydi_olan_kitap_tekrar_odunc_alinamaz()
    {
        var state = await CreateStateAsync();
        await state.BorrowAsync("B001", Form);

        var result = await state.BorrowAsync("B001", Form);

        Assert.False(result.Success);
        Assert.Single(state.Loans);
        Assert.Equal(1, BookById(state, "B001").AvailableCopies);
    }

    [Fact(Skip = Skip)]
    public async Task HasActiveLoan_iade_edilmemis_kayit_varsa_true_olmali()
    {
        var state = await CreateStateAsync();
        Assert.False(state.HasActiveLoan("B001"));

        var loan = (await state.BorrowAsync("B001", Form)).Loan!;
        Assert.True(state.HasActiveLoan("B001"));

        await state.ReturnAsync(loan.Id);
        Assert.False(state.HasActiveLoan("B001"));
    }

    // K6

    [Fact(Skip = Skip)]
    public async Task Ilk_odunc_kodu_LIB_yil_101_olmali()
    {
        var state = await CreateStateAsync();

        var result = await state.BorrowAsync("B001", Form);

        Assert.True(result.Success);
        Assert.Equal("LIB-2026-101", result.Loan!.Id);
    }

    [Fact(Skip = Skip)]
    public async Task Sonraki_kod_en_buyuk_siranin_bir_fazlasi_olmali()
    {
        var storage = new FakeLocalStorage();
        await storage.SetAsync(LibraryState.LoansKey, new List<Loan>
        {
            new() { Id = "LIB-2026-105", BookId = "B003", BorrowDate = Today.AddDays(-20), DueDate = Today.AddDays(-6), ReturnedAt = Today.AddDays(-8) },
            new() { Id = "LIB-2026-101", BookId = "B003", BorrowDate = Today.AddDays(-40), DueDate = Today.AddDays(-26), ReturnedAt = Today.AddDays(-30) }
        });
        var state = await CreateStateAsync(storage);

        var result = await state.BorrowAsync("B001", Form);

        Assert.Equal("LIB-2026-106", result.Loan!.Id);
    }

    [Fact(Skip = Skip)]
    public async Task Odunc_tarihi_bugun_teslim_tarihi_14_gun_sonra_olmali()
    {
        var state = await CreateStateAsync();

        var loan = (await state.BorrowAsync("B001", Form)).Loan!;

        Assert.Equal(Today.Date, loan.BorrowDate.Date);
        Assert.Equal(Today.Date.AddDays(14), loan.DueDate.Date);
        Assert.Null(loan.ReturnedAt);
        Assert.False(loan.IsExtended);
    }

    [Fact(Skip = Skip)]
    public async Task Kayit_formdaki_ad_ve_e_posta_ile_olusmali()
    {
        var state = await CreateStateAsync();

        var loan = (await state.BorrowAsync("B001", Form)).Loan!;

        Assert.Equal("B001", loan.BookId);
        Assert.Equal("Ayşe Yılmaz", loan.BorrowerName);
        Assert.Equal("ayse@ornek.com", loan.Email);
    }

    [Fact(Skip = Skip)]
    public async Task Odunc_alininca_mevcut_kopya_1_azalmali()
    {
        var state = await CreateStateAsync();

        await state.BorrowAsync("B001", Form);

        Assert.Equal(1, BookById(state, "B001").AvailableCopies);
    }

    [Fact(Skip = Skip)]
    public async Task Kopya_0_iken_odunc_alinamaz()
    {
        var state = await CreateStateAsync();

        var result = await state.BorrowAsync("B002", Form);

        Assert.False(result.Success);
        Assert.Empty(state.Loans);
        Assert.Equal(0, BookById(state, "B002").AvailableCopies);
    }

    // K7

    [Fact(Skip = Skip)]
    public async Task Ilk_uzatma_teslim_tarihini_7_gun_ileri_almali()
    {
        var state = await CreateStateAsync();
        var loan = (await state.BorrowAsync("B001", Form)).Loan!;

        var result = await state.ExtendAsync(loan.Id);

        var updated = state.Loans.Single(l => l.Id == loan.Id);
        Assert.True(result.Success);
        Assert.Equal(Today.Date.AddDays(21), updated.DueDate.Date);
        Assert.True(updated.IsExtended);
    }

    [Fact(Skip = Skip)]
    public async Task Ikinci_uzatma_reddedilmeli()
    {
        var state = await CreateStateAsync();
        var loan = (await state.BorrowAsync("B001", Form)).Loan!;
        await state.ExtendAsync(loan.Id);

        var result = await state.ExtendAsync(loan.Id);

        Assert.False(result.Success);
        Assert.Equal(Today.Date.AddDays(21), state.Loans.Single(l => l.Id == loan.Id).DueDate.Date);
    }

    [Fact(Skip = Skip)]
    public async Task Suresi_gecmis_kayit_uzatilamaz()
    {
        var time = new FixedTimeProvider(Today);
        var state = await CreateStateAsync(time: time);
        var loan = (await state.BorrowAsync("B001", Form)).Loan!;
        time.Advance(TimeSpan.FromDays(15));

        var result = await state.ExtendAsync(loan.Id);

        Assert.False(result.Success);
        Assert.False(state.Loans.Single(l => l.Id == loan.Id).IsExtended);
    }

    // K8

    [Fact(Skip = Skip)]
    public async Task Iade_kaydi_iade_edildi_olarak_isaretlemeli()
    {
        var state = await CreateStateAsync();
        var loan = (await state.BorrowAsync("B001", Form)).Loan!;

        var result = await state.ReturnAsync(loan.Id);

        var updated = state.Loans.Single(l => l.Id == loan.Id);
        Assert.True(result.Success);
        Assert.Equal(Today.Date, updated.ReturnedAt?.Date);
        Assert.Equal(LoanState.Returned, LoanRules.GetState(updated, Today));
    }

    [Fact(Skip = Skip)]
    public async Task Iade_mevcut_kopyayi_1_artirmali()
    {
        var state = await CreateStateAsync();
        var loan = (await state.BorrowAsync("B001", Form)).Loan!;

        await state.ReturnAsync(loan.Id);

        Assert.Equal(2, BookById(state, "B001").AvailableCopies);
    }

    [Fact(Skip = Skip)]
    public async Task Iade_toplam_kopyayi_asmamali()
    {
        // Kayıt aktif ama localStorage'daki kopya sayısı zaten toplam kopyaya eşit (tutarsız veri).
        var storage = new FakeLocalStorage();
        await storage.SetAsync(LibraryState.LoansKey, new List<Loan>
        {
            new() { Id = "LIB-2026-101", BookId = "B003", BorrowDate = Today.AddDays(-2), DueDate = Today.AddDays(12) }
        });
        await storage.SetAsync(LibraryState.AvailabilityKey, new Dictionary<string, int> { ["B003"] = 1 });
        var state = await CreateStateAsync(storage);

        await state.ReturnAsync("LIB-2026-101");

        Assert.Equal(1, BookById(state, "B003").AvailableCopies);
    }

    // K9

    [Fact(Skip = Skip)]
    public async Task Iade_edilmis_kayit_tekrar_iade_edilemez()
    {
        var state = await CreateStateAsync();
        var loan = (await state.BorrowAsync("B001", Form)).Loan!;
        await state.ReturnAsync(loan.Id);

        var result = await state.ReturnAsync(loan.Id);

        Assert.False(result.Success);
        Assert.Equal(2, BookById(state, "B001").AvailableCopies);
    }

    [Fact(Skip = Skip)]
    public async Task Iade_edilmis_kayit_uzatilamaz()
    {
        var state = await CreateStateAsync();
        var loan = (await state.BorrowAsync("B001", Form)).Loan!;
        await state.ReturnAsync(loan.Id);

        var result = await state.ExtendAsync(loan.Id);

        var updated = state.Loans.Single(l => l.Id == loan.Id);
        Assert.False(result.Success);
        Assert.False(updated.IsExtended);
        Assert.Equal(Today.Date.AddDays(14), updated.DueDate.Date);
    }

    // K11

    [Fact(Skip = Skip)]
    public async Task Odunc_sonrasi_localStorage_a_yazilmali()
    {
        var storage = new FakeLocalStorage();
        var state = await CreateStateAsync(storage);

        await state.BorrowAsync("B001", Form);

        var loans = await storage.GetAsync<List<Loan>>(LibraryState.LoansKey);
        var availability = await storage.GetAsync<Dictionary<string, int>>(LibraryState.AvailabilityKey);
        Assert.Single(loans!);
        Assert.Equal(1, availability!["B001"]);
    }

    [Fact(Skip = Skip)]
    public async Task Yeni_LibraryState_ayni_kayitlari_ve_kopya_sayilarini_gormeli()
    {
        var storage = new FakeLocalStorage();
        var first = await CreateStateAsync(storage);
        var borrowed = (await first.BorrowAsync("B001", Form)).Loan!;
        await first.BorrowAsync("B003", Form);
        await first.ExtendAsync(borrowed.Id);

        // Sayfa yenilenmiş gibi: aynı storage ile sıfırdan başlayan yeni bir LibraryState.
        var second = await CreateStateAsync(storage);

        Assert.Equal(2, second.Loans.Count);
        Assert.True(second.Loans.Single(l => l.Id == borrowed.Id).IsExtended);
        Assert.Equal(1, BookById(second, "B001").AvailableCopies);
        Assert.Equal(0, BookById(second, "B003").AvailableCopies);
    }

    // OnChange

    [Fact(Skip = Skip)]
    public async Task Basarili_islemden_sonra_OnChange_tetiklenmeli()
    {
        var state = await CreateStateAsync();
        var count = 0;
        state.OnChange += () => count++;

        await state.BorrowAsync("B001", Form);

        Assert.Equal(1, count);
    }
}
