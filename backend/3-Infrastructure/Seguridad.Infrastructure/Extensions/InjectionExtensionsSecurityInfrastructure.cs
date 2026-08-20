using Microsoft.Extensions.DependencyInjection;

namespace Seguridad.Infrastructure.Extensions;

public static class InjectionExtensionsSecurityInfrastructure
{
    public static IServiceCollection AddInjectionSecurityInfraestructure(this IServiceCollection services)
    {
        // Scoped, no Singleton: ver la nota en el equivalente de Inventory.
        services.AddScoped<SeguridadDbContext>();

        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITrustedDeviceRepository, TrustedDeviceRepository>();
        services.AddScoped<IMfaRepository, MfaRepository>();
        services.AddScoped<IFormsRepository, FormsRepository>();
        services.AddScoped<IRolesRepository, RolesRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IModulesRepository, ModulesRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        return services;
    }
}
