using System.Security.Cryptography;

namespace ClickYa.Api.Security;

public static class PasswordSecurity
{
    private const string Prefix = "PBKDF2-SHA256";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            value,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool IsHash(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith(Prefix + "$", StringComparison.Ordinal);

    public static bool Verify(string suppliedValue, string? storedValue, out bool needsUpgrade)
    {
        needsUpgrade = false;
        if (string.IsNullOrEmpty(suppliedValue) || string.IsNullOrEmpty(storedValue))
            return false;

        if (!IsHash(storedValue))
        {
            needsUpgrade = true;
            return FixedTimeEquals(suppliedValue, storedValue);
        }

        var parts = storedValue.Split('$');
        if (parts.Length != 4 ||
            !int.TryParse(parts[1], out var iterations) ||
            iterations < 100_000 || iterations > 1_000_000)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length != SaltSize || expected.Length != HashSize)
                return false;
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                suppliedValue,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            needsUpgrade = iterations < Iterations;
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = System.Text.Encoding.UTF8.GetBytes(left);
        var rightBytes = System.Text.Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
