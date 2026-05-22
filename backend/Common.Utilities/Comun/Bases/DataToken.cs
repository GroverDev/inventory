using System;

namespace Common.Utilities.Comun.Bases;

public class DataToken
{
    public int UserId { get; set; } = 0;
    public string Uuid { get; set; } = "";
    public int SessionId { get; set; }
    public string Email { get; set; } = "";
    public string Rol { get; set; } = "";
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
}