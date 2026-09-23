namespace Bacon.Scheduler.Tests.Fakes;

internal sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTime utcNow) => _utcNow = new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

    public void Advance(TimeSpan timeSpan) => _utcNow = _utcNow.Add(timeSpan);
}
