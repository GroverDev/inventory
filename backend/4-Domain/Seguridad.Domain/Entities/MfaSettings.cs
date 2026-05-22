namespace Seguridad.Domain;

public class MfaSettings
{
    public string EncryptionKeyHex { get; set; } = "";
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public string Issuer { get; set; } = "PuntoDeVenta";
}
