using System;
using System.Security.Cryptography;
using System.Text;

namespace Common.Utilities.CustomCryptography;


public class HashUserId
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("Clave-CRYP-secreta-UserID-palabr");
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("mi-CRYP-Id-User-");

    public static string EncodeId(int id)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;

            byte[] idBytes = BitConverter.GetBytes(id);

            using (var encryptor = aes.CreateEncryptor())
            {
                byte[] encrypted = encryptor.TransformFinalBlock(idBytes, 0, idBytes.Length);

                // Convertir a Base64 URL-safe
                return Convert.ToBase64String(encrypted)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');
            }
        }
    }

    public static int DecodeId(string encryptedId)
    {
        try
        {
            // Restaurar Base64 normal
            string base64 = encryptedId.Replace('-', '+').Replace('_', '/');

            // Agregar padding si es necesario
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            byte[] encrypted = Convert.FromBase64String(base64);

            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                    return BitConverter.ToInt32(decrypted, 0);
                }
            }
        }
        catch
        {
            throw new ArgumentException("ID encriptado inválido");
        }
    }
}

