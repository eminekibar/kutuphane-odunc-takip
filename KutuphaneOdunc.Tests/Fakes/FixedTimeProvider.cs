namespace KutuphaneOdunc.Tests.Fakes;

// Her zaman verilen tarihi döndüren saat. Advance ile ileri alınabilir.
// Saat dilimi UTC tutulur ki GetLocalNow() da tam olarak verilen tarihi versin.
public class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTime now)
    {
        _now = new DateTimeOffset(DateTime.SpecifyKind(now, DateTimeKind.Unspecified), TimeSpan.Zero);
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan time) => _now = _now.Add(time);
}
