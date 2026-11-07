using KutuphaneOdunc.Models;

namespace KutuphaneOdunc.Services;

public enum ToastKind
{
    Success,
    Error,
    Info
}

public sealed record ToastMessage(Guid Id, string Text, ToastKind Kind);

// Ekranın köşesinde kısa süre görünen bildirimleri tutar. Bildirimleri ToastHost bileşeni gösterir ve süresi dolunca kapatır.
public class ToastService
{
    // Aynı anda en fazla bu kadar bildirim görünür; yenisi gelince en eskisi kalkar.
    public const int MaxMessages = 3;

    private readonly List<ToastMessage> _messages = new();

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public event Action? OnChange;

    public ToastMessage Show(string message, ToastKind kind)
    {
        var toast = new ToastMessage(Guid.NewGuid(), message, kind);
        _messages.Add(toast);

        if (_messages.Count > MaxMessages)
            _messages.RemoveAt(0);

        OnChange?.Invoke();
        return toast;
    }

    // LibraryState işlemlerinin sonucunu doğrudan bildirime çevirir: başarı yeşil, kural ihlali kırmızı.
    public ToastMessage Show(ActionResult result) =>
        Show(result.Message, result.Success ? ToastKind.Success : ToastKind.Error);

    public void Dismiss(Guid id)
    {
        if (_messages.RemoveAll(m => m.Id == id) > 0)
            OnChange?.Invoke();
    }
}
