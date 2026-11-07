// Verilen ad ve içerikle tarayıcıda bir dosya indirir. Blazor'dan IJSRuntime ile çağrılır:
// kutuphane.downloadFile("LIB-2026-104.ics", "BEGIN:VCALENDAR...", "text/calendar;charset=utf-8")
window.kutuphane = window.kutuphane || {};

window.kutuphane.downloadFile = function (fileName, content, mimeType) {
    // Blob, metni UTF-8 olarak saklar; Türkçe karakterler bozulmaz.
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);

    // Görünmeyen bir link oluşturulup tıklanır; "download" niteliği tarayıcıya dosyayı açmak yerine indirmesini söyler.
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();

    // Geçici adres bellekte kalmasın.
    setTimeout(() => URL.revokeObjectURL(url), 0);
};
