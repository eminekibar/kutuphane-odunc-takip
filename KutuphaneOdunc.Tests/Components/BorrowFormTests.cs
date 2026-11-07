using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Components;

public class BorrowFormTests : BunitContext
{
    private static List<string> HataMesajlari(IRenderedComponent<BorrowForm> cut) =>
        cut.FindAll(".validation-message").Select(e => e.TextContent).ToList();

    [Fact]
    public void Bos_form_gonderilince_hata_mesajlari_gorunmeli_ve_OnSubmit_cagrilmamali()
    {
        var cagrildi = false;
        var cut = Render<BorrowForm>(p => p.Add(x => x.OnSubmit, (BorrowFormModel _) => cagrildi = true));

        cut.Find("form").Submit();

        Assert.False(cagrildi);
        Assert.Contains("Ad soyad zorunludur.", HataMesajlari(cut));
        Assert.Contains("E-posta zorunludur.", HataMesajlari(cut));
    }

    [Fact]
    public void Hatali_e_posta_icin_mesaj_gorunmeli()
    {
        var cagrildi = false;
        var cut = Render<BorrowForm>(p => p.Add(x => x.OnSubmit, (BorrowFormModel _) => cagrildi = true));

        cut.Find("#borrow-fullname").Change("Ayşe Yılmaz");
        cut.Find("#borrow-email").Change("ayse");
        cut.Find("form").Submit();

        Assert.False(cagrildi);
        Assert.Equal(new[] { "Geçerli bir e-posta adresi girin." }, HataMesajlari(cut));
    }

    [Fact]
    public void Kisa_ad_icin_mesaj_gorunmeli()
    {
        var cut = Render<BorrowForm>();

        cut.Find("#borrow-fullname").Change("Al");
        cut.Find("#borrow-email").Change("ali@ornek.com");
        cut.Find("form").Submit();

        Assert.Equal(new[] { "Ad soyad 3 ile 60 karakter arasında olmalıdır." }, HataMesajlari(cut));
    }

    [Fact]
    public void Hatali_alan_invalid_olarak_isaretlenmeli()
    {
        var cut = Render<BorrowForm>();

        cut.Find("form").Submit();

        Assert.Contains("invalid", cut.Find("#borrow-fullname").ClassList);
        Assert.Equal("true", cut.Find("#borrow-fullname").GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Gecerli_form_OnSubmit_i_girilen_bilgilerle_cagirmali()
    {
        BorrowFormModel? gelen = null;
        var cut = Render<BorrowForm>(p => p.Add(x => x.OnSubmit, (BorrowFormModel m) => gelen = m));

        cut.Find("#borrow-fullname").Change("Ayşe Yılmaz");
        cut.Find("#borrow-email").Change("ayse@ornek.com");
        cut.Find("form").Submit();

        Assert.NotNull(gelen);
        Assert.Equal("Ayşe Yılmaz", gelen!.FullName);
        Assert.Equal("ayse@ornek.com", gelen.Email);
        Assert.Empty(HataMesajlari(cut));
    }

    [Fact]
    public void Vazgec_butonu_OnCancel_i_cagirmali()
    {
        var cagrildi = false;
        var cut = Render<BorrowForm>(p => p.Add(x => x.OnCancel, () => cagrildi = true));

        cut.Find("button.btn-outline-secondary").Click();

        Assert.True(cagrildi);
    }

    [Theory]
    [InlineData("borrow-fullname", "Ad soyad")]
    [InlineData("borrow-email", "E-posta")]
    public void Her_alanin_etiketi_olmali(string alanId, string etiket)
    {
        var cut = Render<BorrowForm>();

        Assert.Equal(etiket, cut.Find($"label[for='{alanId}']").TextContent);
        Assert.NotNull(cut.Find($"#{alanId}"));
    }
}
