using System.Collections.Concurrent;

namespace ClickYa.Api.Security;

public sealed class LoginThrottle
{
    private sealed record AttemptState(int Count, DateTimeOffset WindowStart, DateTimeOffset BlockedUntil);
    private readonly ConcurrentDictionary<string, AttemptState> _attempts = new();

    public bool IsBlocked(string key)
    {
        if (!_attempts.TryGetValue(key, out var state)) return false;
        if (state.BlockedUntil > DateTimeOffset.UtcNow) return true;
        if (DateTimeOffset.UtcNow - state.WindowStart > TimeSpan.FromMinutes(15))
            _attempts.TryRemove(key, out _);
        return false;
    }

    public void RegisterFailure(string key)
    {
        var now = DateTimeOffset.UtcNow;
        _attempts.AddOrUpdate(
            key,
            _ => new AttemptState(1, now, DateTimeOffset.MinValue),
            (_, current) =>
            {
                var state = now - current.WindowStart > TimeSpan.FromMinutes(15)
                    ? new AttemptState(0, now, DateTimeOffset.MinValue)
                    : current;
                var count = state.Count + 1;
                return state with
                {
                    Count = count,
                    BlockedUntil = count >= 5 ? now.AddMinutes(15) : DateTimeOffset.MinValue
                };
            });
    }

    public void RegisterSuccess(string key) => _attempts.TryRemove(key, out _);
}
