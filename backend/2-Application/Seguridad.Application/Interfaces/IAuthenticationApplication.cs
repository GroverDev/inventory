using Common.Utilities;
using Seguridad.Domain;

namespace Seguridad.Application;

public interface IAuthenticationApplication
{
    Task<Response<LoginResponse>> Login(LoginRequest login);

    /// <summary>
    /// Emite y persiste un refresh token nuevo. Devuelve el valor en claro,
    /// que solo se entrega al cliente en esta llamada.
    /// </summary>
    Task<string> IssueRefreshToken(int userId, string device, string loginFrom, int days);

    /// <summary>
    /// Canjea un refresh token por datos de sesión frescos y rota el token.
    /// El <see cref="LoginResponse.Token"/> (JWT) lo completa el controlador.
    /// </summary>
    Task<Response<LoginResponse>> Refresh(RefreshTokenRequest request, int days);

    /// <summary>Revoca un refresh token (cierre de sesión explícito).</summary>
    Task<Response<bool>> RevokeRefreshToken(string refreshToken);
}
