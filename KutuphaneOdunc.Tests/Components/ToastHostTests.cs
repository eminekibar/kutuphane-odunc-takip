using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KutuphaneOdunc.Tests.Components;

public class ToastHostTests : BunitContext
{
    public ToastHostTests()
    {
        Services.AddScoped<ToastService>();
    }

    private ToastService Toasts => Services.GetRequiredService<ToastService>();

    // Servise sayfadan gelmiş gibi bildirim ekler; Blazor'un çizim akışında çalışsın diye InvokeAsync ile.
    private static void Goster(IRenderedComponent<ToastHost> cut, ToastService toasts, string metin, ToastKind tur) =>
        cut.InvokeAsync(() => toasts.Show(metin, tur)).Wait();

    [Fact]
    public void Bos_kapsayici_canli_bolge_olarak_hazir_durmali()
    {
        var cut = Render<ToastHost>();

        var kapsayici = cut.Find(".toast-host");
        Assert.Equal("status", kapsayici.GetAttribute("role"));
        Assert.Equal("polite", kapsayici.GetAttribute("aria-live"));
        Assert.Empty(cut.FindAll(".toast-item"));
    }

    [Theory]
    [InlineData(ToastKind.Success, "success", "Başarılı")]
    [InlineData(ToastKind.Error, "error", "Hata")]
    [InlineData(ToastKind.Info, "info", "Bilgi")]
    public void Mesaj_turune_gore_sinif_ve_metinle_gorunmeli(ToastKind tur, string sinif, string etiket)
    {
        var cut = Render<ToastHost>();

        Goster(cut, Toasts, "Kitap iade edildi.", tur);

        var bildirim = cut.Find(".toast-item");
        Assert.Contains(sinif, bildirim.ClassList);
        Assert.Equal(etiket + ":", cut.Find(".toast-text .visually-hidden").TextContent.Trim());
        Assert.EndsWith("Kitap iade edildi.", cut.Find(".toast-text").TextContent);
        Assert.Equal("true", cut.Find(".toast-icon").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Kapat_butonu_mesaji_kaldirmali()
    {
        var cut = Render<ToastHost>();
        Goster(cut, Toasts, "Birinci", ToastKind.Info);
        Goster(cut, Toasts, "İkinci", ToastKind.Success);

        var kapat = cut.FindAll(".toast-close")[0];
        Assert.Equal("Bildirimi kapat", kapat.GetAttribute("aria-label"));
        kapat.Click();

        var kalan = Assert.Single(cut.FindAll(".toast-item"));
        Assert.EndsWith("İkinci", kalan.QuerySelector(".toast-text")!.TextContent);
        Assert.Single(Toasts.Messages);
    }

    [Fact]
    public void Mesaj_sure_dolunca_kendiliginden_kapanmali()
    {
        var cut = Render<ToastHost>(p => p.Add(x => x.Duration, TimeSpan.FromMilliseconds(50)));

        Goster(cut, Toasts, "Profiliniz kaydedildi.", ToastKind.Success);
        Assert.NotNull(cut.Find(".toast-item"));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast-item")));
        Assert.Empty(Toasts.Messages);
    }
}
