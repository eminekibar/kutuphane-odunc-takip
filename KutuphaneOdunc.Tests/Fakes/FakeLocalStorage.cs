using System.Text.Json;
using KutuphaneOdunc.Services;

namespace KutuphaneOdunc.Tests.Fakes;

// Tarayıcının localStorage'ı yerine bellekte bir Dictionary tutar.
// Gerçek localStorage gibi değerleri JSON metni olarak saklar; okunan her değer yeni bir nesnedir.
public class FakeLocalStorage : ILocalStorageService
{
    public Dictionary<string, string> Items { get; } = new();

    public Task<T?> GetAsync<T>(string key)
    {
        if (!Items.TryGetValue(key, out var json))
            return Task.FromResult<T?>(default);

        return Task.FromResult(JsonSerializer.Deserialize<T>(json));
    }

    public Task SetAsync<T>(string key, T value)
    {
        Items[key] = JsonSerializer.Serialize(value);
        return Task.CompletedTask;
    }
}
