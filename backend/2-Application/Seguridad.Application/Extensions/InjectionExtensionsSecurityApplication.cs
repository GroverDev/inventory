using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mapster;

namespace Seguridad.Application.Extensions;

public static class InjectionExtensionsSecurityApplication
{
    public static IServiceCollection AddInjectionSecurityApplication(this IServiceCollection services)
    {
        //services.AddSingleton(configuration);
        // services.AddFluentValidation(options => {
        //     options.RegisterValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies().Where(p=>p.IsDynamic));
        // });

        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        
        services.AddScoped<IAuthenticationApplication, AuthenticationApplication>();
        services.AddScoped<IMfaApplication, MfaApplication>();
        services.AddScoped<IFormsApplication, FormsApplication>();
        services.AddScoped<IRolesApplication, RolesApplication>();
        services.AddScoped<IAccessMenuApplication, AccessMenuApplication>();
        services.AddScoped<IUsersApplication, UsersApplication>();
        services.AddScoped<IModulesApplication, ModulesApplication>();
        return services;
    }
}
