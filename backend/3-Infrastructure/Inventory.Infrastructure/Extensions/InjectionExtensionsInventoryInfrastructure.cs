using Inventory.Infrastructure.Persistences.Interfaces;
using Inventory.Infrastructure.Persistences.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Extensions;

public static class InjectionExtensionsInventoryInfraestructure
{
    public static IServiceCollection AddInjectionInventoryInfraestructure(this IServiceCollection services)
    {
        services.AddSingleton<InventoryDbContext>();
        services.AddScoped<ICustomersRepository, CustomersRepository>();
        services.AddScoped<ILaboratoryRepository, LaboratoryRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<ISalesDetailRepository, SalesDetailRepository>();
        services.AddScoped<ISalesRepository, SalesRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPurchaseDetailRepository, PurchaseDetailRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IPurchaseStatusRepository, PurchaseStatusRepository>();
        services.AddScoped<IUnitsOfMeasurementRepository,UnitsOfMeasurementRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        return services;
    }
}


