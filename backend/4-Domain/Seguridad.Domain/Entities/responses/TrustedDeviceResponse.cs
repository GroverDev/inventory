namespace Seguridad.Domain;

/// <summary>Dispositivo de confianza activo, para que el propio usuario vea y gestione los suyos.</summary>
public class TrustedDeviceResponse
{
    public long Id { get; set; }
    public string DeviceLabel { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
