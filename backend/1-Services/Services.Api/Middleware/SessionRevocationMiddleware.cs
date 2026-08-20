using Common.Utilities.Security;
using CONST = Common.Utilities.Comun.Bases.TokenDataConst;

namespace Services.Api.Middleware;

/// <summary>
/// Corta en seco cualquier request cuyo SessionId haya sido cerrado desde el
/// panel de administración ("cerrar sesión" de otro usuario). Sin esto, cerrar
/// una sesión solo impediría renovarla: el access token ya emitido —un JWT
/// autocontenido— seguiría sirviendo hasta vencer solo.
/// </summary>
/// <remarks>
/// Debe registrarse <b>después</b> de <c>UseAuthentication()</c>, por la misma
/// razón que TenantResolutionMiddleware: antes de eso los claims todavía no
/// están resueltos.
/// </remarks>
public sealed class SessionRevocationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SessionRevocationRegistry registry)
    {
        var claim = context.User?.FindFirst(CONST.SESSION_ID);

        if (claim is not null && int.TryParse(claim.Value, out int sessionId) && registry.IsRevoked(sessionId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                ok = false,
                Message = new { MessageType = "warning", Description = "La sesión fue cerrada por un administrador." }
            });
            return;
        }

        await next(context);
    }
}

public static class SessionRevocationMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionRevocation(this IApplicationBuilder app)
        => app.UseMiddleware<SessionRevocationMiddleware>();
}
