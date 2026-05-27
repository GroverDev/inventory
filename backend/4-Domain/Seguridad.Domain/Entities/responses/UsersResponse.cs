namespace Seguridad.Domain;

public class UsersResponse
{
    public string Uuid { get; set; } = "";
    public string UserName { get; set; } = "";
    public bool ChangePassword { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastAccess { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool MfaEnabled { get; set; }
    public bool MfaRequired { get; set; }
}
