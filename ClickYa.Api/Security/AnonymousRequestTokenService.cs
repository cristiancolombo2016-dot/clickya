using System.Security.Cryptography;

namespace ClickYa.Api.Security;

public sealed class AnonymousRequestTokenService
{
    public string Create() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public string Hash(string token) => Convert.ToHexString(
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    public bool Matches(string token, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expectedHash)) return false;
        var actual = Convert.FromHexString(Hash(token));
        var expected = Convert.FromHexString(expectedHash);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
