using System.Security.Cryptography;
using System.Text;
namespace Common.Utilities.Cryptography;

public class Hash
{

    public static string SHA512Hash(string inputString)
    {
        SHA512 sha512 = SHA512.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(inputString);
        byte[] hash = sha512.ComputeHash(bytes);
        return GetStringFromHash(hash);
    }
    private static string GetStringFromHash(byte[] hash)
    {
        StringBuilder result = new();

        for (int i = 0; i < hash.Length; i++)
        {
            result.Append(hash[i].ToString("X2"));
        }
        return result.ToString();
    }

    #region Nueva forma de encripytar el password
    public static string HashPassword(string password)
    {
        // Parámetros PBKDF2
        int iterations = 60000; // Número de iteraciones
        int saltSize = 16;      // Tamaño del salt en bytes
        int hashSize = 64;      // Tamaño del hash en bytes para SHA-512

        // Generar un salt aleatorio
        byte[] salt = new byte[saltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Derivar la clave usando PBKDF2 con SHA-512
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA512))
        {
            byte[] hash = pbkdf2.GetBytes(hashSize);

            // Combinar salt y hash
            byte[] hashBytes = new byte[saltSize + hashSize];
            Array.Copy(salt, 0, hashBytes, 0, saltSize);
            Array.Copy(hash, 0, hashBytes, saltSize, hashSize);

            // Convertir a cadena Base64 para almacenamiento
            string base64Hash = Convert.ToBase64String(hashBytes);

            // Incluir el prefijo $pbkdf2-sha512$ y las iteraciones en el formato final
            string hashedPassword = $"$pbkdf2-sha512${iterations}${base64Hash}";

            return hashedPassword;
        }
    }

    public static bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        // Extraer el prefijo, el número de iteraciones y el hash Base64
        var parts = hashedPassword.Split('$');
        if (parts.Length != 4 || parts[1] != "pbkdf2-sha512")
        {
            throw new FormatException("Formato de hash de contraseña no válido.");
        }

        int iterations = int.Parse(parts[2]);
        byte[] hashBytes = Convert.FromBase64String(parts[3]);

        // Extraer el salt y el hash
        int saltSize = 16;
        int hashSize = 64;
        byte[] salt = new byte[saltSize];
        byte[] hash = new byte[hashSize];
        Array.Copy(hashBytes, 0, salt, 0, saltSize);
        Array.Copy(hashBytes, saltSize, hash, 0, hashSize);

        // Derivar la clave usando PBKDF2 con SHA-512 y el mismo salt e iteraciones
        using (var pbkdf2 = new Rfc2898DeriveBytes(providedPassword, salt, iterations, HashAlgorithmName.SHA512))
        {
            byte[] providedHash = pbkdf2.GetBytes(hashSize);

            // Comparar el hash derivado con el hash almacenado
            return providedHash.SequenceEqual(hash);
        }
    }
    #endregion
}
