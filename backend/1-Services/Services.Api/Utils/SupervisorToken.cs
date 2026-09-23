using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Services.Api.Utils;

/// <summary>
/// Token de un supervisor que autoriza algo en el punto de venta: un descuento
/// por encima del tope del cajero, o un cierre de caja con diferencia.
/// </summary>
/// <remarks>
/// Es el JWT de una sesión del supervisor (el POS lo obtiene iniciando sesión con
/// sus credenciales). Vale si está firmado y vigente, es de un rol distinto de
/// Cajero, y de la misma empresa que quien opera: si no, el supervisor de otra
/// farmacia serviría para autorizar acá.
/// </remarks>
public static class SupervisorToken
{
    /// <returns>El id del supervisor, o null si el token no autoriza.</returns>
    public static int? Validate(string? token, string secret, int tenantId)
    {
        if (string.IsNullOrEmpty(token)) return null;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);

            var jwt = handler.ReadJwtToken(token);
            var role = jwt.Claims.FirstOrDefault(c => c.Type == "Rol")?.Value;
            var tenant = jwt.Claims.FirstOrDefault(c => c.Type == "TenantId")?.Value;
            var id = jwt.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            if (string.IsNullOrEmpty(role) || role == "Cajero" || tenant != tenantId.ToString() || id is null)
                return null;

            int userId = Common.Utilities.CustomCryptography.EncondeUserId.DecodeId(id);
            return userId > 0 ? userId : null;
        }
        catch { return null; }
    }
}
