namespace KutuphaneOdunc.Models;

public class Book
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public string Publisher { get; set; } = "";
    public int PublicationYear { get; set; }
    public string? CoverUrl { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
