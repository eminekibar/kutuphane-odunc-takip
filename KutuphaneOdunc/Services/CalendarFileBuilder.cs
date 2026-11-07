using System.Globalization;
using System.Text;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Services;

// Teslim tarihi için takvim dosyası (.ics, iCalendar - RFC 5545) üretir.
// Dosya Google Takvim, Outlook ve telefon takvimlerine aktarılabiliyor.
public static class CalendarFileBuilder
{
    private const string LineBreak = "\r\n";

    public static string FileName(Loan loan) => $"{loan.Id}.ics";

    // now: dosyanın oluşturulma anı (DTSTAMP). UTC değilse UTC'ye çevrilir.
    public static string Build(Loan loan, string bookTitle, DateTime now)
    {
        var stamp = now.Kind == DateTimeKind.Local ? now.ToUniversalTime() : now;

        var lines = new[]
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Kutuphane Odunc Takip//TR",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:{loan.Id}@kutuphane-odunc-takip",
            $"DTSTAMP:{stamp.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture)}",
            // Tüm gün etkinliği: başlangıç teslim günü, bitiş ertesi gün (bitiş günü etkinliğe dahil değil).
            $"DTSTART;VALUE=DATE:{Date(loan.DueDate)}",
            $"DTEND;VALUE=DATE:{Date(loan.DueDate.AddDays(1))}",
            $"SUMMARY:{Escape($"Kitap iadesi: {bookTitle}")}",
            $"DESCRIPTION:{Escape($"Ödünç kodu: {loan.Id}")}",
            "END:VEVENT",
            "END:VCALENDAR"
        };

        var builder = new StringBuilder();
        foreach (var line in lines)
            builder.Append(Fold(line)).Append(LineBreak);
        return builder.ToString();
    }

    private static string Date(DateTime date) => date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    // Metin değerlerinde \ ; , ve satır sonu özel anlam taşıyor, önlerine \ konur.
    private static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n");

    // Standart bir satırın en fazla 75 bayt olmasını istiyor; uzun satır bölünür ve devam satırı
    // bir boşlukla başlar. Türkçe harfler UTF-8'de 2 bayt olduğu için karakter değil bayt sayılır.
    private static string Fold(string line)
    {
        const int maxBytes = 75;
        var result = new StringBuilder();
        var bytes = 0;

        foreach (var c in line)
        {
            var size = Encoding.UTF8.GetByteCount(c.ToString());
            if (bytes + size > maxBytes)
            {
                result.Append(LineBreak).Append(' ');
                bytes = 1; // devam satırının başındaki boşluk
            }
            result.Append(c);
            bytes += size;
        }
        return result.ToString();
    }
}
