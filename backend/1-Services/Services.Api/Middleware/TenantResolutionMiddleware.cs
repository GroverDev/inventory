using Common.Utilities.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using CONST = Common.Utilities.Comun.Bases.TokenDataConst;

namespace Services.Api.Middleware;

/// <summary>
/// Lee el tenant y la sucursal del JWT y los deja disponibles para el resto del request.
/// </summary>
/// <remarks>
/// Debe registrarse <b>después</b> de <c>UseAuthentication()</c>: antes de eso
/// los claims todavía no están resueltos y el tenant saldría siempre vacío.
/// <para>
/// En endpoints anónimos (login, health check) no hay claim y el tenant queda sin
/// resolver, que es lo correcto. Quien intente tocar datos de negocio desde ahí se
/// encuentra con la excepción de <see cref="TenantConnectionFactory"/>.
/// </para>
/// </remarks>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenant)
    {
        var claim = context.User?.FindFirst(CONST.TENANT_ID);

        if (claim is not null && int.TryParse(claim.Value, out int tenantId) && tenantId > 0)
        {
            tenant.SetTenant(tenantId);

            // Todo token con tenant debe traer también la sucursal. Solo los
            // emitidos antes de que existieran las sucursales no la traen: se
            // responde 401 y la web y el móvil los renuevan solos con el
            // refresh token, que ya emite el JWT con sucursal. Dejarlos pasar
            // haría fallar cualquier escritura con un error de columna nula.
            //
            // Los endpoints anónimos quedan fuera: el cliente puede mandar un
            // token viejo en la cabecera justamente al llamar al refresh, y
            // cortarlo ahí dejaría sin salida al único camino que lo arregla.
            var branchClaim = context.User?.FindFirst(CONST.BRANCH_ID);
            if (branchClaim is not null && Guid.TryParse(branchClaim.Value, out Guid branchId) && branchId != Guid.Empty)
            {
                tenant.SetBranch(branchId);
            }
            else if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }
}

public static class TenantResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
        => app.UseMiddleware<TenantResolutionMiddleware>();
}
