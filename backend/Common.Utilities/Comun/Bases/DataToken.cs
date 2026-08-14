using System;

namespace Common.Utilities.Comun.Bases;

public class DataToken
{
    public int UserId { get; set; } = 0;
    public int TenantId { get; set; } = 0;
    public string Uuid { get; set; } = "";
    public int SessionId { get; set; }
    public string Email { get; set; } = "";
    /// <summary>Rol efectivo. Se conserva por compatibilidad.</summary>
    public string Rol { get; set; } = "";

    /// <summary>Todos los roles activos, separados por coma. Fuente de verdad.</summary>
    public string Roles { get; set; } = "";
    public string UserName { get; set; } = "";

    public string RouteApi { get; set; } = "";
    public string Method { get; set; } = "";
    public string Ip { get; set; } = "";

    public bool ok { get; set; }

}
public class TokenDataConst
{
    public const string SESSION_ID = "SessionId";
    public const string USER_ID = "Id";
    public const string UUID = "Uuid";
    public const string ROL = "Rol";
    public const string EMAIL = "Email";
    public const string TENANT_ID = "TenantId";
    public const string ROLES = "Roles";
}