namespace KutuphaneOdunc.Models;

// Tarihler gün bazında karşılaştırılır, saat dikkate alınmaz.
// Son teslim günü hâlâ Aktif sayılır; gecikme ertesi gün başlar.
public static class LoanRules
{
    public static LoanState GetState(Loan loan, DateTime today)
    {
        if (loan.ReturnedAt is not null)
            return LoanState.Returned;

        return today.Date > loan.DueDate.Date ? LoanState.Overdue : LoanState.Active;
    }

    // Gecikmiş kayıtta negatif döner (ör. -3 = 3 gün gecikti). İade edilmiş kayıtta 0.
    public static int RemainingDays(Loan loan, DateTime today)
    {
        if (loan.ReturnedAt is not null)
            return 0;

        return (loan.DueDate.Date - today.Date).Days;
    }

    // "Teslimi yaklaşıyor" için kalan gün sınırı.
    public const int DueSoonDays = 3;

    // Aktif kayıtta teslime 3 gün ya da daha az kaldıysa true. Son teslim günü de dahil (0 gün kaldı).
    // Gecikmiş ve iade edilmiş kayıtlar yaklaşan sayılmaz; LoanState'e yeni bir durum eklenmez.
    public static bool IsDueSoon(Loan loan, DateTime today)
    {
        return GetState(loan, today) == LoanState.Active && RemainingDays(loan, today) <= DueSoonDays;
    }

    // K7 ve K9: yalnızca aktif (iade edilmemiş, süresi geçmemiş) ve daha önce uzatılmamış kayıt.
    public static bool CanExtend(Loan loan, DateTime today)
    {
        return GetState(loan, today) == LoanState.Active && !loan.IsExtended;
    }
}
