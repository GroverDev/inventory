using Inventory.Domain;
using Inventory.Domain.Entities;
using Inventory.Domain.Entities.Requests;
using Inventory.Domain.Entities.Responses;
using Mapster;

namespace Inventory.Application.Mappers;

public class InventoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Unit of Measurement
        config.NewConfig<UnitOfMeasurement, UnitOfMeasurementResponse>().TwoWays();
        config.NewConfig<UnitOfMeasurement, UnitOfMeasurementRequest>()
            .Map(dest => dest.Name, src => src.UnitName)
            .TwoWays();
        config.NewConfig<UnitOfMeasurementRequest, UnitOfMeasurement>()
            .Map(dest => dest.UnitName, src => src.Name);

        // Product
        config.NewConfig<Product, ProductRequest>().TwoWays();
        config.NewConfig<ProductRequest, Product>()
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id));

        // Laboratory
        config.NewConfig<Laboratory, LaboratoryRequest>().TwoWays();

        // Customer
        config.NewConfig<Customer, CustomerRequest>().TwoWays();

        // Provider
        config.NewConfig<Provider, ProviderRequest>().TwoWays();

        // Sale
        config.NewConfig<SaleRequest, Sale>()
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id))
            .Map(dest => dest.CustomerId, src => string.IsNullOrEmpty(src.CustomerId) ? Guid.Empty : Guid.Parse(src.CustomerId))
            .Map(dest => dest.SaleDate, src => Convert.ToDateTime(src.SaleDate));
        
        config.NewConfig<SaleDetailRequest, SaleDetail>()
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id))
            .Map(dest => dest.SaleId, src => string.IsNullOrEmpty(src.SaleId) ? Guid.Empty : Guid.Parse(src.SaleId))
            .Map(dest => dest.ProductId, src => string.IsNullOrEmpty(src.ProductId) ? Guid.Empty : Guid.Parse(src.ProductId));
    }
}
