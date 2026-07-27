using System;

namespace Services.Api.jwt;

public class JwtSettings
{

    public string Secret { get; set; }="";
    public string Issuer { get; set; }="";
    public string Audience { get; set; }="";
    public string TimeToken { get; set; }="";

    /// <summary>
    /// Minutos de vida del access token para clientes con refresh token
    /// (móvil). Es corto a propósito: la sesión larga la sostiene el refresh
    /// token, que sí se puede revocar.
    /// </summary>
    public string TimeTokenRefreshable { get; set; } = "60";

    /// <summary>Días de vida del refresh token.</summary>
    public int RefreshTokenDays { get; set; } = 30;
}
