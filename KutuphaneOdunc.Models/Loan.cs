namespace KutuphaneOdunc.Models;

public class Loan
{
    public string Id { get; set; } = "";
    public string BookId { get; set; } = "";
    public string BorrowerName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public bool IsExtended { get; set; }
}
