using System.Security.Cryptography;
using System.Text;

namespace Common.Utilities.CustomCryptography;

public static class EncryptionHelper
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static string Encrypt(string plaintext, byte[] key)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, NonceSize);
        tag.CopyTo(result, NonceSize + ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string encryptedBase64, byte[] key)
    {
        var combined = Convert.FromBase64String(encryptedBase64);
        var nonce = combined[..NonceSize];
        var tag = combined[^TagSize..];
        var ciphertext = combined[NonceSize..^TagSize];

        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    public static byte[] KeyFromHex(string hex)
    {
        if (hex.Length != 64)
            throw new ArgumentException("La clave AES-256 debe tener 64 caracteres hexadecimales (32 bytes).");

        var bytes = new byte[32];
        for (int i = 0; i < 32; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }
}
