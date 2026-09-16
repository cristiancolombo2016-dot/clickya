using System.Security.Cryptography;
using System.Text;

namespace ClickYa.Api.Security;

public sealed class SensitiveDataProtector
{
    private readonly byte[] _key;

    public SensitiveDataProtector(IConfiguration configuration)
    {
        var configured = configuration["Security:SensitiveDataKey"];
        try
        {
            _key = Convert.FromBase64String(configured ?? "");
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Security__SensitiveDataKey debe ser Base64 válido.");
        }

        if (_key.Length != 32)
            throw new InvalidOperationException("Security__SensitiveDataKey debe contener exactamente 32 bytes.");
    }

    public string Protect(string value)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var plain = Encoding.UTF8.GetBytes(value);
        var cipher = new byte[plain.Length];
        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, plain, cipher, tag);
        return $"v1.{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(tag)}.{Convert.ToBase64String(cipher)}";
    }

    public string Unprotect(string protectedValue)
    {
        var parts = protectedValue.Split('.', 4);
        if (parts.Length != 4 || parts[0] != "v1")
            throw new CryptographicException("Dato protegido inválido.");

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var cipher = Convert.FromBase64String(parts[3]);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, tag.Length);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
