using Bunit;
using KutuphaneOdunc.Components;
using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Tests.Components;

public class LoanSummaryTests : BunitContext
{
    private static readonly DateTime Bugun = new(2026, 11, 10);

    private static Loan Kayit(string kod, DateTime teslim, DateTime? iade = null) => new()
    {
        Id = kod,
        BookId = "B001",
        BorrowDate = teslim.AddDays(-14),
        DueDate = teslim,
        ReturnedAt = iade
    };

    private IRenderedComponent<LoanSummary> Ciz(params Loan[] kayitlar) =>
        Render<LoanSummary>(p => p
            .Add(x => x.Loans, kayitlar)
            .Add(x => x.Today, Bugun));

    private static string Sayi(IRenderedComponent<LoanSummary> cut, string kutu) =>
        cut.Find($".summary-box.{kutu} .count").TextContent;

    [Fact]
    public void Karisik_kayitlarla_uc_sayi_dogru_olmali()
    {
        var cut = Ciz(
            Kayit("LIB-2026-101", teslim: Bugun.AddDays(5)),
            Kayit("LIB-2026-102", teslim: Bugun),                    // son gün: hâlâ aktif
            Kayit("LIB-2026-103", teslim: Bugun.AddDays(-2)),        // gecikmiş
            Kayit("LIB-2026-104", teslim: Bugun.AddDays(-4), iade: Bugun.AddDays(-6)),
            Kayit("LIB-2026-105", teslim: Bugun.AddDays(-1), iade: Bugun)); // süresi geçmiş ama iade edilmiş

        Assert.Equal("2", Sayi(cut, "active"));
        Assert.Equal("1", Sayi(cut, "overdue"));
        Assert.Equal("2", Sayi(cut, "returned"));
    }

    [Fact]
    public void Kayit_yokken_uc_sayi_da_sifir_olmali()
    {
        var cut = Ciz();

        Assert.Equal("0", Sayi(cut, "active"));
        Assert.Equal("0", Sayi(cut, "overdue"));
        Assert.Equal("0", Sayi(cut, "returned"));
    }

    [Fact]
    public void Gecikmis_kayit_varsa_kutu_vurgulu_olmali()
    {
        var cut = Ciz(Kayit("LIB-2026-101", teslim: Bugun.AddDays(-1)));

        Assert.Contains("highlight", cut.Find(".summary-box.overdue").ClassList);
    }

    [Fact]
    public void Gecikmis_kayit_yoksa_kutu_vurgusuz_olmali()
    {
        var cut = Ciz(Kayit("LIB-2026-101", teslim: Bugun.AddDays(5)));

        Assert.DoesNotContain("highlight", cut.Find(".summary-box.overdue").ClassList);
    }

    [Fact]
    public void Kutu_etiketleri_dogru_olmali()
    {
        var cut = Ciz();

        var etiketler = cut.FindAll(".summary-box .label").Select(e => e.TextContent).ToList();
        Assert.Equal(new[] { "Aktif", "Gecikmiş", "İade edilen" }, etiketler);
        Assert.Equal("Kayıt özeti", cut.Find(".loan-summary").GetAttribute("aria-label"));
    }
}
