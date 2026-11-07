using KutuphaneOdunc.Models;
using KutuphaneOdunc.Services;

namespace KutuphaneOdunc.Tests.Services;

public class ToastServiceTests
{
    [Fact]
    public void Show_mesaj_eklemeli_ve_OnChange_tetiklemeli()
    {
        var servis = new ToastService();
        var tetiklendi = 0;
        servis.OnChange += () => tetiklendi++;

        var mesaj = servis.Show("Kitap iade edildi.", ToastKind.Success);

        Assert.Equal(1, tetiklendi);
        var eklenen = Assert.Single(servis.Messages);
        Assert.Equal(mesaj, eklenen);
        Assert.Equal("Kitap iade edildi.", eklenen.Text);
        Assert.Equal(ToastKind.Success, eklenen.Kind);
    }

    [Fact]
    public void Dismiss_dogru_mesaji_silmeli()
    {
        var servis = new ToastService();
        var birinci = servis.Show("Birinci", ToastKind.Info);
        var ikinci = servis.Show("İkinci", ToastKind.Error);
        var tetiklendi = 0;
        servis.OnChange += () => tetiklendi++;

        servis.Dismiss(birinci.Id);

        Assert.Equal(1, tetiklendi);
        Assert.Equal(ikinci, Assert.Single(servis.Messages));
    }

    [Fact]
    public void Olmayan_mesaj_icin_Dismiss_OnChange_tetiklememeli()
    {
        var servis = new ToastService();
        servis.Show("Birinci", ToastKind.Info);
        var tetiklendi = 0;
        servis.OnChange += () => tetiklendi++;

        servis.Dismiss(Guid.NewGuid());

        Assert.Equal(0, tetiklendi);
        Assert.Single(servis.Messages);
    }

    [Fact]
    public void Ayni_anda_en_fazla_uc_mesaj_olmali_ve_en_eskisi_kalkmali()
    {
        var servis = new ToastService();

        foreach (var metin in new[] { "1", "2", "3", "4" })
            servis.Show(metin, ToastKind.Info);

        Assert.Equal(new[] { "2", "3", "4" }, servis.Messages.Select(m => m.Text));
    }

    [Theory]
    [InlineData(true, ToastKind.Success)]
    [InlineData(false, ToastKind.Error)]
    public void Islem_sonucu_basariya_gore_dogru_turle_gosterilmeli(bool basarili, ToastKind beklenen)
    {
        var servis = new ToastService();
        var sonuc = basarili ? ActionResult.Ok("Tamam") : ActionResult.Fail("Olmadı");

        servis.Show(sonuc);

        var mesaj = Assert.Single(servis.Messages);
        Assert.Equal(beklenen, mesaj.Kind);
        Assert.Equal(sonuc.Message, mesaj.Text);
    }
}
