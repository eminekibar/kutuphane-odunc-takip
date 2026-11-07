namespace KutuphaneOdunc.Models;

// LibraryState işlemlerinin sonucu. Kural ihlalinde exception yerine Success = false ve bir mesaj döner.
public class ActionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public Loan? Loan { get; init; }

    public static ActionResult Ok(string message, Loan? loan = null) =>
        new() { Success = true, Message = message, Loan = loan };

    public static ActionResult Fail(string message) =>
        new() { Success = false, Message = message };
}
