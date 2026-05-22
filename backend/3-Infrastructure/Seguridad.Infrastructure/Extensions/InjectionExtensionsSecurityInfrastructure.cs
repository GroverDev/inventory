using Microsoft.Extensions.DependencyInjection;

namespace Seguridad.Infrastructure.Extensions;

public static class InjectionExtensionsSecurityInfrastructure
{
    public static IServiceCollection AddInjectionSecurityInfraestructure(this IServiceCollection services)
    {
        services.AddSingleton<SeguridadDbContext>();

        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
        services.AddScoped<IMfaRepository, MfaRepository>();
        services.AddScoped<IFormsRepository, FormsRepository>();
        services.AddScoped<IRolesRepository, RolesRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IModulesRepository, ModulesRepository>();
        return services;
    }
}
