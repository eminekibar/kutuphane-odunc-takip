using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Components;

public class LoanStatusTests : BunitContext
{
    // Ödünç 1 Kasım, son teslim 15 Kasım.
    private static readonly DateTime BorrowDate = new(2026, 11, 1);
    private static readonly DateTime DueDate = new(2026, 11, 15);

    private static readonly DateTime AktifGun = new(2026, 11, 10);
    private static readonly DateTime GecikmisGun = new(2026, 11, 18);

    private static Loan OrnekKayit(DateTime? iadeTarihi = null, bool uzatildi = false) => new()
    {
        Id = "LIB-2026-101",
        BookId = "B001",
        BorrowerName = "Ayşe Yılmaz",
        Email = "ayse@ornek.com",
        BorrowDate = BorrowDate,
        DueDate = DueDate,
        ReturnedAt = iadeTarihi,
        IsExtended = uzatildi
    };

    private IRenderedComponent<LoanStatus> Ciz(Loan kayit, DateTime bugun,
        Action<string>? onExtend = null, Action<string>? onReturn = null) =>
        Render<LoanStatus>(p => p
            .Add(x => x.Loan, kayit)
            .Add(x => x.BookTitle, "Yazılım Tasarımına Giriş")
            .Add(x => x.Today, bugun)
            .Add(x => x.OnExtend, onExtend ?? (_ => { }))
            .Add(x => x.OnReturn, onReturn ?? (_ => { })));

    private static bool Pasif(IRenderedComponent<LoanStatus> cut, string selector) =>
        cut.Find(selector).HasAttribute("disabled");

    [Fact]
    public void Kitap_adi_kod_ve_tarihler_gosterilmeli()
    {
        var cut = Ciz(OrnekKayit(), AktifGun);

        var bilgiler = cut.FindAll(".facts dd").Select(d => d.TextContent.Trim()).ToList();
        Assert.Equal("Yazılım Tasarımına Giriş", cut.Find("h3").TextContent);
        Assert.Equal(new[] { "LIB-2026-101", "01.11.2026", "15.11.2026", "5 gün" }, bilgiler);
    }

    // Etiketler

    [Fact]
    public void Aktif_kayitta_Aktif_etiketi_gorunmeli()
    {
        var cut = Ciz(OrnekKayit(), AktifGun);

        Assert.Equal("Aktif", cut.Find(".badge-state").TextContent);
        Assert.Contains("active", cut.Find(".loan-status").ClassList);
    }

    [Fact]
    public void Gecikmis_kayitta_Gecikmis_etiketi_ve_gecikme_gunu_gorunmeli()
    {
        var cut = Ciz(OrnekKayit(), GecikmisGun);

        Assert.Equal("Gecikmiş", cut.Find(".badge-state").TextContent);
        Assert.Contains("overdue", cut.Find(".loan-status").ClassList);
        Assert.Equal("3 gün gecikti", cut.Find(".remaining").TextContent);
    }

    [Fact]
    public void Iade_edilmis_kayitta_Iade_edildi_etiketi_ve_iade_tarihi_gorunmeli()
    {
        var cut = Ciz(OrnekKayit(iadeTarihi: new DateTime(2026, 11, 8)), AktifGun);

        Assert.Equal("İade edildi", cut.Find(".badge-state").TextContent);
        Assert.Contains("returned", cut.Find(".loan-status").ClassList);
        Assert.Contains("08.11.2026", cut.Find(".facts").TextContent);
        Assert.Empty(cut.FindAll(".remaining"));
    }

    [Fact]
    public void Son_teslim_gunu_Bugun_son_gun_yazmali()
    {
        var cut = Ciz(OrnekKayit(), DueDate);

        Assert.Equal("Aktif", cut.Find(".badge-state").TextContent);
        Assert.Equal("Bugün son gün", cut.Find(".remaining").TextContent);
    }

    [Fact]
    public void Uzatilmis_kayitta_Uzatildi_etiketi_gorunmeli()
    {
        var cut = Ciz(OrnekKayit(uzatildi: true), AktifGun);

        Assert.Equal("Uzatıldı", cut.Find(".badge-extended").TextContent);
    }

    [Fact]
    public void Uzatilmamis_kayitta_Uzatildi_etiketi_gorunmemeli()
    {
        var cut = Ciz(OrnekKayit(), AktifGun);

        Assert.Empty(cut.FindAll(".badge-extended"));
    }

    // Butonlar (K7, K9)

    [Fact]
    public void Aktif_ve_uzatilmamis_kayitta_iki_buton_da_aktif_olmali()
    {
        var cut = Ciz(OrnekKayit(), AktifGun);

        Assert.False(Pasif(cut, ".extend-button"));
        Assert.False(Pasif(cut, ".return-button"));
    }

    [Fact]
    public void Uzatilmis_kayitta_uzat_butonu_pasif_olmali()
    {
        var cut = Ciz(OrnekKayit(uzatildi: true), AktifGun);

        Assert.True(Pasif(cut, ".extend-button"));
        Assert.False(Pasif(cut, ".return-button"));
    }

    [Fact]
    public void Gecikmis_kayitta_uzat_butonu_pasif_iade_butonu_aktif_olmali()
    {
        var cut = Ciz(OrnekKayit(), GecikmisGun);

        Assert.True(Pasif(cut, ".extend-button"));
        Assert.False(Pasif(cut, ".return-button"));
    }

    [Fact]
    public void Iade_edilmis_kayitta_iki_buton_da_pasif_olmali()
    {
        var cut = Ciz(OrnekKayit(iadeTarihi: new DateTime(2026, 11, 8)), AktifGun);

        Assert.True(Pasif(cut, ".extend-button"));
        Assert.True(Pasif(cut, ".return-button"));
    }

    // Callback'ler

    [Fact]
    public void Uzat_butonu_OnExtend_i_kayit_koduyla_cagirmali()
    {
        string? gelenKod = null;
        var cut = Ciz(OrnekKayit(), AktifGun, onExtend: kod => gelenKod = kod);

        cut.Find(".extend-button").Click();

        Assert.Equal("LIB-2026-101", gelenKod);
    }

    [Fact]
    public void Iade_butonu_OnReturn_u_kayit_koduyla_cagirmali()
    {
        string? gelenKod = null;
        var cut = Ciz(OrnekKayit(), AktifGun, onReturn: kod => gelenKod = kod);

        cut.Find(".return-button").Click();

        Assert.Equal("LIB-2026-101", gelenKod);
    }

    [Fact]
    public void Butonlar_kitap_basligina_baglanmali()
    {
        var cut = Ciz(OrnekKayit(), AktifGun);

        Assert.Equal("loan-LIB-2026-101", cut.Find("h3").Id);
        Assert.Equal("loan-LIB-2026-101", cut.Find(".extend-button").GetAttribute("aria-describedby"));
        Assert.Equal("loan-LIB-2026-101", cut.Find(".return-button").GetAttribute("aria-describedby"));
    }
}
