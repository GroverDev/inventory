using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Seguridad.Domain;
using Sqids;

namespace Services.Api.Utils;

public class TokenJwt
{
    public static string GetToken(LoginResponse login, string secret, string timeToken)
    {
        string userId = Common.Utilities.CustomCryptography.EncondeUserId.EncodeId(login.UserId);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                [
                    new Claim("Id", userId),
                    new Claim("Uuid", login.Uuid),
                    new Claim("Email", login.Email.ToString()),
                    new Claim("SessionId", login.SesionId.ToString()),
                    new Claim("Rol", login.RolName),
                    new Claim("Roles", login.Roles),
                    new Claim("TenantId", login.TenantId.ToString())
                ]
                ),
            Expires = DateTime.UtcNow.AddMinutes(Convert.ToInt32(timeToken)),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string GetTotpPendingToken(int userId, string secret)
    {
        string encodedUserId = Common.Utilities.CustomCryptography.EncondeUserId.EncodeId(userId);
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                [
                    new Claim("Id", encodedUserId),
                    new Claim("TotpPending", "true")
                ]
                ),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static int? ValidateTotpPendingToken(string token, string secret)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secret);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var totpPendingClaim = principal.Claims.FirstOrDefault(c => c.Type == "TotpPending");
            if (totpPendingClaim?.Value != "true") return null;

            var idClaim = principal.Claims.FirstOrDefault(c => c.Type == "Id");
            if (idClaim == null) return null;

            int userId = Common.Utilities.CustomCryptography.EncondeUserId.DecodeId(idClaim.Value);
            return userId > 0 ? userId : null;
        }
        catch
        {
            return null;
        }
    }
}
