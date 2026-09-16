using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ClickYa.Api.Security;

public sealed class WebLoginTicketService
{
    private sealed record Ticket(string AccessToken, DateTimeOffset ExpiresAt);
    private readonly ConcurrentDictionary<string, Ticket> _tickets = new();

    public string Issue(string accessToken)
    {
        RemoveExpired();
        var value = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _tickets[value] = new Ticket(accessToken, DateTimeOffset.UtcNow.AddMinutes(3));
        return value;
    }

    public string? Redeem(string value)
    {
        if (!_tickets.TryRemove(value, out var ticket) || ticket.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;
        return ticket.AccessToken;
    }

    private void RemoveExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _tickets.Where(x => x.Value.ExpiresAt <= now))
            _tickets.TryRemove(item.Key, out _);
    }
}
