using OtpNet;

namespace Common.Utilities.CustomCryptography;

public class TotpHelper
{
    public static string GenerateSecret()
    {
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(secretKey);
    }

    public static bool VerifyCode(string secret, string code)
    {
        try
        {
            var bytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(bytes);
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }

    public static string GetQrCodeUri(string secret, string email, string issuer = "PuntoDeVenta")
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }
}
