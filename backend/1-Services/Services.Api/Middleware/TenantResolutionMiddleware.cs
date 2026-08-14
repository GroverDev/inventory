using Common.Utilities.MultiTenancy;
using CONST = Common.Utilities.Comun.Bases.TokenDataConst;

namespace Services.Api.Middleware;

/// <summary>
/// Lee el tenant del JWT y lo deja disponible para el resto del request.
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
        }

        await next(context);
    }
}

public static class TenantResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
        => app.UseMiddleware<TenantResolutionMiddleware>();
}
