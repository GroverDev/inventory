namespace Seguridad.Domain;

public class MfaInfo
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string MfaType { get; set; } = "totp";
    public string? SecretEncrypted { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsRequired { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string Email { get; set; } = "";
}
