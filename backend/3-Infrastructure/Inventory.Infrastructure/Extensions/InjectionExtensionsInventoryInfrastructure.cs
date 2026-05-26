using Inventory.Infrastructure.Persistences.Interfaces;
using Inventory.Infrastructure.Persistences.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Extensions;

public static class InjectionExtensionsInventoryInfraestructure
{
    public static IServiceCollection AddInjectionInventoryInfraestructure(this IServiceCollection services)
    {
        services.AddSingleton<InventoryDbContext>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
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
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<ISalePaymentRepository, SalePaymentRepository>();
        services.AddScoped<ISaleReturnRepository, SaleReturnRepository>();
        return services;
    }
}


