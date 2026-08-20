namespace Seguridad.Domain;

/// <summary>
/// Fila de sec.trusted_devices. El valor en claro solo existe en el cliente:
/// aquí se guarda su hash.
/// </summary>
public class TrustedDevice
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public string TokenHash { get; set; } = "";
    public string DeviceLabel { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;
}
