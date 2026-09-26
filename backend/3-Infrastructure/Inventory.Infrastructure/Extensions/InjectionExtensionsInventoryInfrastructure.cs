using Inventory.Infrastructure.Persistences.Interfaces;
using Inventory.Infrastructure.Persistences.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Extensions;

public static class InjectionExtensionsInventoryInfraestructure
{
    public static IServiceCollection AddInjectionInventoryInfraestructure(this IServiceCollection services)
    {
        // Scoped, no Singleton: el contexto depende del tenant del request, que
        // cambia en cada llamada. Como Singleton quedaría fijado al primer tenant
        // que entre y todos los demás leerían sus datos.
        services.AddScoped<InventoryDbContext>();
        // Scoped por la misma razón: la carpeta de destino depende del tenant del request.
        services.AddScoped<ITenantFolderResolver, TenantFolderResolver>();
        services.AddScoped<IImageStorage, DiskImageStorage>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICustomersRepository, CustomersRepository>();
        services.AddScoped<ILaboratoryRepository, LaboratoryRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IStockTransferRepository, StockTransferRepository>();
        services.AddScoped<IProductBranchSettingsRepository, ProductBranchSettingsRepository>();
        services.AddScoped<IDiscountLimitsRepository, DiscountLimitsRepository>();
        services.AddScoped<IHeldSaleRepository, HeldSaleRepository>();
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
        services.AddScoped<IPharmaRepository, PharmaRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<ISalePaymentRepository, SalePaymentRepository>();
        services.AddScoped<ISaleReturnRepository, SaleReturnRepository>();
        services.AddScoped<ICashMovementRepository, CashMovementRepository>();
        services.AddScoped<ICashSessionRepository, CashSessionRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        return services;
    }
}


