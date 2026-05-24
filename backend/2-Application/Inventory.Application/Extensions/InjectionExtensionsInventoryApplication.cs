//using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Mapster;

namespace Inventory.Application;

public static class InjectionExtensionsInventoryApplication
{
    public static IServiceCollection AddInjectionInventoryApplication(this IServiceCollection services)
    {

        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        
        services.AddScoped<ICustomersApplication, CustomersApplication>();
        services.AddScoped<ILaboratoryApplication, LaboratoryApplication>();
        services.AddScoped<IProviderApplication, ProviderApplication>();
        services.AddScoped<ISalesApplication, SalesApplication>();
        services.AddScoped<IProductApplication, ProductApplication>();
        services.AddScoped<IPurchaseApplication, PurchaseApplication>();
        services.AddScoped<IPurchaseStatusApplication, PurchaseStatusApplication>();
        services.AddScoped<IUnitsOfMeasurementApplication, UnitsOfMeasurementApplication>();
        services.AddScoped<IDashboardApplication, DashboardApplication>();
        return services;
    }
}


