using System.ComponentModel.DataAnnotations;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Models;

public class UserProfileTests
{
    [Theory]
    [InlineData("Ayşe", "A")]
    [InlineData("Ayşe Yılmaz", "AY")]
    [InlineData("Ali Can Demir", "AD")]
    [InlineData("ayşe yılmaz", "AY")]
    [InlineData("ilker özgür", "İÖ")]
    [InlineData("ırmak işık", "Iİ")]
    [InlineData("  Ayşe   Yılmaz  ", "AY")]
    [InlineData("", "")]
    public void Bas_harfler_dogru_hesaplanmali(string adSoyad, string beklenen)
    {
        var profil = new UserProfile { FullName = adSoyad };

        Assert.Equal(beklenen, profil.Initials);
    }

    [Fact]
    public void Bas_harfler_dil_ayarindan_bagimsiz_olmali()
    {
        // Tarayıcı İngilizce olsa bile "i" → "İ" olmalı.
        var onceki = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            Assert.Equal("İÖ", new UserProfile { FullName = "ilker özgür" }.Initials);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = onceki;
        }
    }

    [Fact]
    public void Dogrulama_kurallari_BorrowFormModel_ile_ayni_olmali()
    {
        var profil = new UserProfile { FullName = "Al", Email = "ayse" };
        var sonuclar = new List<ValidationResult>();

        Validator.TryValidateObject(profil, new ValidationContext(profil), sonuclar, validateAllProperties: true);

        var mesajlar = sonuclar.Select(r => r.ErrorMessage).ToList();
        Assert.Contains("Ad soyad 3 ile 60 karakter arasında olmalıdır.", mesajlar);
        Assert.Contains("Geçerli bir e-posta adresi girin.", mesajlar);
    }

    [Fact]
    public void Gecerli_profil_hatasiz_olmali()
    {
        var profil = new UserProfile { FullName = "Ayşe Yılmaz", Email = "ayse@ornek.com" };

        Assert.True(Validator.TryValidateObject(profil, new ValidationContext(profil), null, validateAllProperties: true));
    }
}
