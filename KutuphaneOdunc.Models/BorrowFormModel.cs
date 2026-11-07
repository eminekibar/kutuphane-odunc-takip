using System.ComponentModel.DataAnnotations;

namespace KutuphaneOdunc.Models;

public class BorrowFormModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "Ad soyad 3 ile 60 karakter arasında olmalıdır.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = "";
}
