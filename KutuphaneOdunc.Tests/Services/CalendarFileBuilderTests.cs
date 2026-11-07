using System.Text;
using KutuphaneOdunc.Models;
using KutuphaneOdunc.Services;

namespace KutuphaneOdunc.Tests.Services;

public class CalendarFileBuilderTests
{
    private static readonly DateTime Simdi = new(2026, 11, 10, 9, 30, 0, DateTimeKind.Utc);

    private static Loan OrnekKayit(DateTime? teslim = null) => new()
    {
        Id = "LIB-2026-104",
        BookId = "B001",
        BorrowDate = new DateTime(2026, 11, 10),
        DueDate = teslim ?? new DateTime(2026, 11, 24)
    };

    private static string Uret(string kitapAdi = "Yazılım Tasarımına Giriş", DateTime? teslim = null) =>
        CalendarFileBuilder.Build(OrnekKayit(teslim), kitapAdi, Simdi);

    // Uzun satırlar bölünmüş olabilir; okurken devam satırları birleştirilir (standarttaki "unfold").
    private static List<string> Satirlar(string ics) =>
        ics.Replace("\r\n ", "").Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();

    [Fact]
    public void Takvim_basi_ve_sonu_olmali()
    {
        var satirlar = Satirlar(Uret());

        Assert.Equal("BEGIN:VCALENDAR", satirlar.First());
        Assert.Equal("END:VCALENDAR", satirlar.Last());
        Assert.Contains("BEGIN:VEVENT", satirlar);
        Assert.Contains("END:VEVENT", satirlar);
    }

    [Fact]
    public void Teslim_tarihi_tum_gun_etkinligi_olmali()
    {
        var satirlar = Satirlar(Uret());

        Assert.Contains("DTSTART;VALUE=DATE:20261124", satirlar);
        Assert.Contains("DTEND;VALUE=DATE:20261125", satirlar);
    }

    [Fact]
    public void Ay_sonundaki_teslimde_bitis_ertesi_ayin_ilk_gunu_olmali()
    {
        var satirlar = Satirlar(Uret(teslim: new DateTime(2026, 11, 30)));

        Assert.Contains("DTSTART;VALUE=DATE:20261130", satirlar);
        Assert.Contains("DTEND;VALUE=DATE:20261201", satirlar);
    }

    [Fact]
    public void Baslikta_kitap_adi_aciklamada_odunc_kodu_olmali()
    {
        var satirlar = Satirlar(Uret());

        Assert.Contains("SUMMARY:Kitap iadesi: Yazılım Tasarımına Giriş", satirlar);
        Assert.Contains("DESCRIPTION:Ödünç kodu: LIB-2026-104", satirlar);
    }

    [Fact]
    public void UID_odunc_koduna_gore_DTSTAMP_verilen_zamandan_olmali()
    {
        var satirlar = Satirlar(Uret());

        Assert.Contains("UID:LIB-2026-104@kutuphane-odunc-takip", satirlar);
        Assert.Contains("DTSTAMP:20261110T093000Z", satirlar);
    }

    [Fact]
    public void Satirlar_CRLF_ile_ayrilmali()
    {
        var ics = Uret();

        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        // Her \n'in önünde \r olmalı; tek başına \n olmamalı.
        Assert.Equal(ics.Split("\n").Length - 1, ics.Split("\r\n").Length - 1);
    }

    [Fact]
    public void Ozel_karakterler_kacirilmali()
    {
        var satirlar = Satirlar(Uret(kitapAdi: "Bilim, Felsefe; ve Sanat"));

        Assert.Contains("SUMMARY:Kitap iadesi: Bilim\\, Felsefe\\; ve Sanat", satirlar);
    }

    [Fact]
    public void Uzun_satirlar_75_bayttan_uzun_olmamali()
    {
        var ics = Uret(kitapAdi: "Kış Bahçesinde Unutulan Şarkılar ve Uzun Bir Yolculuğun Hikâyesi");

        foreach (var satir in ics.Split("\r\n"))
            Assert.True(Encoding.UTF8.GetByteCount(satir) <= 75, $"{Encoding.UTF8.GetByteCount(satir)} bayt: {satir}");

        // Birleştirilince başlık bozulmadan geri gelmeli.
        Assert.Contains("SUMMARY:Kitap iadesi: Kış Bahçesinde Unutulan Şarkılar ve Uzun Bir Yolculuğun Hikâyesi", Satirlar(ics));
    }

    [Fact]
    public void Tarih_dil_ayarindan_bagimsiz_yazilmali()
    {
        var onceki = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            // Bazı takvimlerde (ör. Arapça, Tayca) rakamlar ve yıllar farklı yazılıyor.
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("th-TH");

            Assert.Contains("DTSTART;VALUE=DATE:20261124", Satirlar(Uret()));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = onceki;
        }
    }

    [Fact]
    public void Dosya_adi_odunc_kodu_olmali()
    {
        Assert.Equal("LIB-2026-104.ics", CalendarFileBuilder.FileName(OrnekKayit()));
    }
}
