using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KutuphaneOdunc.Models;

// Bu tarayıcıya ait profil. Doğrulama kuralları BorrowFormModel ile aynı.
public class UserProfile
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "Ad soyad 3 ile 60 karakter arasında olmalıdır.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = "";

    // Avatar için baş harfler: ilk ve son kelimenin ilk harfi. Ad'dan hesaplandığı için kaydedilmez.
    [JsonIgnore]
    public string Initials
    {
        get
        {
            var words = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (words.Length == 0)
                return "";

            var first = ToUpperTurkish(words[0][0]);
            return words.Length == 1 ? $"{first}" : $"{first}{ToUpperTurkish(words[^1][0])}";
        }
    }

    // char.ToUpper o anki dil ayarına göre çalışır; tarayıcı İngilizceyse "i" → "I" olur.
    // Sonuç tarayıcıdan bağımsız olsun diye Türkçe i/ı elle çevrilir.
    private static char ToUpperTurkish(char c) => c switch
    {
        'i' => 'İ',
        'ı' => 'I',
        _ => char.ToUpperInvariant(c)
    };
}
