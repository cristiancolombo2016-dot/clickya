using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClickYa.Api.Security;

public sealed class AccessTokenService
{
    private readonly byte[] _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _lifetime;

    public AccessTokenService(IConfiguration configuration)
    {
        var signingKey = configuration["Security:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
            throw new InvalidOperationException(
                "Falta Security__SigningKey. Debe ser un secreto aleatorio de al menos 32 caracteres.");

        _signingKey = Encoding.UTF8.GetBytes(signingKey);
        _issuer = configuration["Security:Issuer"] ?? "ClickYa.Api";
        _audience = configuration["Security:Audience"] ?? "ClickYa";
        var minutes = configuration.GetValue<int?>("Security:AccessTokenMinutes") ?? 60;
        _lifetime = TimeSpan.FromMinutes(Math.Clamp(minutes, 5, 480));
    }

    public string Create(string role, int subjectId, string displayName)
    {
        var now = DateTimeOffset.UtcNow;
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = _issuer,
            aud = _audience,
            sub = subjectId.ToString(),
            role,
            name = displayName,
            iat = now.ToUnixTimeSeconds(),
            exp = now.Add(_lifetime).ToUnixTimeSeconds(),
            jti = Guid.NewGuid().ToString("N")
        }));

        var unsigned = $"{header}.{payload}";
        return $"{unsigned}.{Sign(unsigned)}";
    }

    public ClaimsPrincipal? Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var parts = token.Split('.');
        if (parts.Length != 3) return null;

        var unsigned = $"{parts[0]}.{parts[1]}";
        var expected = Sign(unsigned);
        if (!FixedTimeEquals(parts[2], expected)) return null;

        try
        {
            using var document = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            var root = document.RootElement;
            if (root.GetProperty("iss").GetString() != _issuer ||
                root.GetProperty("aud").GetString() != _audience ||
                root.GetProperty("exp").GetInt64() <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return null;

            var subject = root.GetProperty("sub").GetString();
            var role = root.GetProperty("role").GetString();
            var name = root.GetProperty("name").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(role)) return null;

            var identity = new ClaimsIdentity(SecurityDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
            identity.AddClaim(new Claim(ClaimTypes.Name, name));
            return new ClaimsPrincipal(identity);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private string Sign(string value)
    {
        using var hmac = new HMACSHA256(_signingKey);
        return Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.ASCII.GetBytes(left);
        var rightBytes = Encoding.ASCII.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += padded.Length % 4 switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(padded);
    }
}
