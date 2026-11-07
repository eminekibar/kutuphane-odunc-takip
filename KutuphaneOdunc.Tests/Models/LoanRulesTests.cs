using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Models;

public class LoanRulesTests
{
    // Bütün testler bu sabit tarihe göre. Ödünç tarihi 1 Kasım, son teslim 15 Kasım (14 gün).
    private static readonly DateTime BorrowDate = new(2026, 11, 1);
    private static readonly DateTime DueDate = new(2026, 11, 15);

    private static Loan NewLoan(DateTime? returnedAt = null, bool isExtended = false) => new()
    {
        Id = "LIB-2026-101",
        BookId = "B001",
        BorrowerName = "Ayşe Yılmaz",
        Email = "ayse@ornek.com",
        BorrowDate = BorrowDate,
        DueDate = DueDate,
        ReturnedAt = returnedAt,
        IsExtended = isExtended
    };

    // GetState

    [Fact]
    public void Suresi_gecmemis_kayit_aktif_olmali()
    {
        Assert.Equal(LoanState.Active, LoanRules.GetState(NewLoan(), new DateTime(2026, 11, 10)));
    }

    [Fact]
    public void Son_teslim_gunundeki_kayit_hala_aktif_olmali()
    {
        Assert.Equal(LoanState.Active, LoanRules.GetState(NewLoan(), DueDate));
    }

    [Fact]
    public void Son_teslim_gununun_aksami_da_aktif_olmali()
    {
        Assert.Equal(LoanState.Active, LoanRules.GetState(NewLoan(), DueDate.AddHours(23)));
    }

    [Fact]
    public void Son_teslim_gununden_sonra_kayit_gecikmis_olmali()
    {
        Assert.Equal(LoanState.Overdue, LoanRules.GetState(NewLoan(), DueDate.AddDays(1)));
    }

    [Fact]
    public void Iade_edilmis_kayit_suresi_gecse_de_iade_edildi_olmali()
    {
        var loan = NewLoan(returnedAt: new DateTime(2026, 11, 20));

        Assert.Equal(LoanState.Returned, LoanRules.GetState(loan, new DateTime(2026, 11, 25)));
    }

    // RemainingDays

    [Fact]
    public void Odunc_gunu_kalan_gun_14_olmali()
    {
        Assert.Equal(14, LoanRules.RemainingDays(NewLoan(), BorrowDate));
    }

    [Fact]
    public void Son_teslim_gunu_kalan_gun_0_olmali()
    {
        Assert.Equal(0, LoanRules.RemainingDays(NewLoan(), DueDate));
    }

    [Fact]
    public void Gecikmis_kayitta_kalan_gun_negatif_olmali()
    {
        Assert.Equal(-3, LoanRules.RemainingDays(NewLoan(), DueDate.AddDays(3)));
    }

    [Fact]
    public void Kalan_gun_hesabinda_saat_dikkate_alinmamali()
    {
        Assert.Equal(5, LoanRules.RemainingDays(NewLoan(), new DateTime(2026, 11, 10, 22, 30, 0)));
    }

    [Fact]
    public void Iade_edilmis_kayitta_kalan_gun_0_olmali()
    {
        var loan = NewLoan(returnedAt: new DateTime(2026, 11, 5));

        Assert.Equal(0, LoanRules.RemainingDays(loan, new DateTime(2026, 11, 8)));
    }

    // CanExtend (K7, K9)

    [Fact]
    public void Aktif_ve_uzatilmamis_kayit_uzatilabilmeli()
    {
        Assert.True(LoanRules.CanExtend(NewLoan(), new DateTime(2026, 11, 10)));
    }

    [Fact]
    public void Son_teslim_gunu_kayit_uzatilabilmeli()
    {
        Assert.True(LoanRules.CanExtend(NewLoan(), DueDate));
    }

    [Fact]
    public void Daha_once_uzatilmis_kayit_tekrar_uzatilamaz()
    {
        Assert.False(LoanRules.CanExtend(NewLoan(isExtended: true), new DateTime(2026, 11, 10)));
    }

    [Fact]
    public void Suresi_gecmis_kayit_uzatilamaz()
    {
        Assert.False(LoanRules.CanExtend(NewLoan(), DueDate.AddDays(1)));
    }

    [Fact]
    public void Iade_edilmis_kayit_uzatilamaz()
    {
        var loan = NewLoan(returnedAt: new DateTime(2026, 11, 5));

        Assert.False(LoanRules.CanExtend(loan, new DateTime(2026, 11, 8)));
    }
}
