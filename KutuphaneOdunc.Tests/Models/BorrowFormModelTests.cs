using System.ComponentModel.DataAnnotations;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Models;

// Blazor'daki DataAnnotationsValidator da aynı öznitelikleri kullanır.
// Burada formu arayüz olmadan Validator.TryValidateObject ile doğruluyoruz.
public class BorrowFormModelTests
{
    private static List<ValidationResult> Validate(BorrowFormModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static List<string> ErrorsFor(List<ValidationResult> results, string property) =>
        results.Where(r => r.MemberNames.Contains(property)).Select(r => r.ErrorMessage!).ToList();

    [Fact]
    public void Gecerli_form_hatasiz_olmali()
    {
        var model = new BorrowFormModel { FullName = "Ayşe Yılmaz", Email = "ayse@ornek.com" };

        Assert.Empty(Validate(model));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_ad_soyad_kabul_edilmemeli(string fullName)
    {
        var model = new BorrowFormModel { FullName = fullName, Email = "ayse@ornek.com" };

        var errors = ErrorsFor(Validate(model), nameof(BorrowFormModel.FullName));

        Assert.Contains("Ad soyad zorunludur.", errors);
    }

    [Fact]
    public void Uc_karakterden_kisa_ad_soyad_kabul_edilmemeli()
    {
        var model = new BorrowFormModel { FullName = "Al", Email = "ayse@ornek.com" };

        var errors = ErrorsFor(Validate(model), nameof(BorrowFormModel.FullName));

        Assert.Contains("Ad soyad 3 ile 60 karakter arasında olmalıdır.", errors);
    }

    [Fact]
    public void Altmis_karakterden_uzun_ad_soyad_kabul_edilmemeli()
    {
        var model = new BorrowFormModel { FullName = new string('a', 61), Email = "ayse@ornek.com" };

        var errors = ErrorsFor(Validate(model), nameof(BorrowFormModel.FullName));

        Assert.Contains("Ad soyad 3 ile 60 karakter arasında olmalıdır.", errors);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(60)]
    public void Sinir_uzunluktaki_ad_soyad_kabul_edilmeli(int length)
    {
        var model = new BorrowFormModel { FullName = new string('a', length), Email = "ayse@ornek.com" };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void Bos_e_posta_kabul_edilmemeli()
    {
        var model = new BorrowFormModel { FullName = "Ayşe Yılmaz", Email = "" };

        var errors = ErrorsFor(Validate(model), nameof(BorrowFormModel.Email));

        Assert.Contains("E-posta zorunludur.", errors);
    }

    [Theory]
    [InlineData("ayseornek.com")]
    [InlineData("ayse@")]
    [InlineData("@ornek.com")]
    public void Hatali_e_posta_kabul_edilmemeli(string email)
    {
        var model = new BorrowFormModel { FullName = "Ayşe Yılmaz", Email = email };

        var errors = ErrorsFor(Validate(model), nameof(BorrowFormModel.Email));

        Assert.Contains("Geçerli bir e-posta adresi girin.", errors);
    }
}
