using Bunit;
using KutuphaneOdunc.Services;

namespace KutuphaneOdunc.Tests.Services;

// BunitJSInterop, IJSRuntime'ın sahte bir sürümü. Tarayıcı olmadan JS çağrılarını kaydeder
// ve önceden ayarlanan cevapları döndürür. Ayarlanmamış bir çağrı yapılırsa test hata verir.
public class LocalStorageServiceTests
{
    private readonly BunitJSInterop _jsInterop = new();

    private LocalStorageService CreateService() => new(_jsInterop.JSRuntime);

    [Fact]
    public async Task SetAsync_degeri_json_olarak_setItem_ile_yazmali()
    {
        _jsInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();

        await CreateService().SetAsync("kutuphane.availability", new Dictionary<string, int> { ["B001"] = 2 });

        var invocation = _jsInterop.VerifyInvoke("localStorage.setItem");
        Assert.Equal("kutuphane.availability", invocation.Arguments[0]);
        Assert.Equal("""{"B001":2}""", invocation.Arguments[1]);
    }

    [Fact]
    public async Task GetAsync_getItem_ile_okunan_jsonu_nesneye_cevirmeli()
    {
        _jsInterop.Setup<string?>("localStorage.getItem", "kutuphane.availability")
            .SetResult("""{"B001":2,"B002":0}""");

        var result = await CreateService().GetAsync<Dictionary<string, int>>("kutuphane.availability");

        Assert.NotNull(result);
        Assert.Equal(2, result["B001"]);
        Assert.Equal(0, result["B002"]);
    }

    [Fact]
    public async Task GetAsync_kayit_yoksa_null_donmeli()
    {
        _jsInterop.Setup<string?>("localStorage.getItem", "kutuphane.loans").SetResult(null);

        var result = await CreateService().GetAsync<List<string>>("kutuphane.loans");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_bozuk_jsonda_null_donmeli()
    {
        _jsInterop.Setup<string?>("localStorage.getItem", "kutuphane.loans").SetResult("{bozuk");

        var result = await CreateService().GetAsync<List<string>>("kutuphane.loans");

        Assert.Null(result);
    }
}
