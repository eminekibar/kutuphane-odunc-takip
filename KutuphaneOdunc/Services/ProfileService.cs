using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Services;

// Bu tarayıcıya ait profili localStorage'da tutar. Giriş sistemi değildir, şifre yoktur.
public class ProfileService
{
    public const string StorageKey = "kutuphane.profile";

    private readonly ILocalStorageService _storage;
    private Task? _initializeTask;

    public ProfileService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    public UserProfile? Profile { get; private set; }

    public event Action? OnChange;

    // Birden fazla sayfa çağırsa da yükleme bir kez yapılır (LibraryState ile aynı yöntem).
    public Task InitializeAsync()
    {
        _initializeTask ??= LoadAsync();
        return _initializeTask;
    }

    private async Task LoadAsync()
    {
        Profile = await _storage.GetAsync<UserProfile>(StorageKey);
    }

    public async Task SaveAsync(UserProfile profile)
    {
        // Formun modeli doğrudan saklanmaz; formda yazmaya devam edilirse kayıtlı profil değişmesin.
        var saved = new UserProfile
        {
            FullName = profile.FullName.Trim(),
            Email = profile.Email.Trim()
        };

        await _storage.SetAsync(StorageKey, saved);
        Profile = saved;
        OnChange?.Invoke();
    }

    public async Task ClearAsync()
    {
        // ILocalStorageService'te silme metodu yok; null yazılınca GetAsync de null döndürür.
        await _storage.SetAsync<UserProfile?>(StorageKey, null);
        Profile = null;
        OnChange?.Invoke();
    }
}
