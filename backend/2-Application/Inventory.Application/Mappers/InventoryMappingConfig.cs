using System.Globalization;
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
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id))
            // Sin categoría va NULL, no Guid.Empty: con Guid.Empty el INSERT
            // choca contra products_category_id_fkey. Lo cubre
            // ProductoSinClasificacionTests.
            .Map(dest => dest.CategoryId,
                 src => string.IsNullOrWhiteSpace(src.CategoryId) ? (Guid?)null : Guid.Parse(src.CategoryId))
            // Sin laboratorio va NULL, no Guid.Empty: la columna es nullable y
            // tiene FK, así que un Guid vacío no referenciaría a nada y la
            // inserción fallaría.
            .Map(dest => dest.LaboratoryId,
                 src => string.IsNullOrWhiteSpace(src.LaboratoryId) ? (Guid?)null : Guid.Parse(src.LaboratoryId));

        // Category
        config.NewConfig<Category, CategoryRequest>().TwoWays();

        // Laboratory
        config.NewConfig<Laboratory, LaboratoryRequest>().TwoWays();

        // Customer
        config.NewConfig<Customer, CustomerRequest>().TwoWays();

        // Provider
        config.NewConfig<Provider, ProviderRequest>().TwoWays();

        // Purchase
        config.NewConfig<PurchaseRequest, Purchase>()
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id))
            .Map(dest => dest.ProviderId, src => string.IsNullOrEmpty(src.ProviderId) ? Guid.Empty : Guid.Parse(src.ProviderId))
            .Map(dest => dest.PurchaseDate, src => DateOnly.Parse(src.PurchaseDate, CultureInfo.InvariantCulture))
            .Map(dest => dest.EstimatedDeliveryDate, src => string.IsNullOrEmpty(src.EstimatedDeliveryDate)
                ? DateOnly.MinValue
                : DateOnly.Parse(src.EstimatedDeliveryDate, CultureInfo.InvariantCulture));

        config.NewConfig<PurchaseDetailRequest, PurchaseDetail>()
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id))
            .Map(dest => dest.PurchaseId, src => string.IsNullOrEmpty(src.PurchaseId) ? Guid.Empty : Guid.Parse(src.PurchaseId))
            .Map(dest => dest.ProductId, src => string.IsNullOrEmpty(src.ProductId) ? Guid.Empty : Guid.Parse(src.ProductId));

        // Purchase response → request (usado en GetPurchase para devolver al frontend)
        config.NewConfig<PurchaseProductResponse, PurchaseRequest>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.ProviderId, src => src.ProviderId.ToString())
            .Map(dest => dest.PurchaseDate, src => src.PurchaseDate.ToString("yyyy-MM-dd"))
            .Map(dest => dest.EstimatedDeliveryDate, src => src.EstimatedDeliveryDate == DateOnly.MinValue
                ? ""
                : src.EstimatedDeliveryDate.ToString("yyyy-MM-dd"));

        config.NewConfig<PurchaseProductDetailResponse, PurchaseDetailRequest>()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.PurchaseId, src => src.PurchaseId.ToString())
            .Map(dest => dest.ProductId, src => src.ProductId.ToString());

        // PurchaseDelivery
        config.NewConfig<PurchaseDeliveryRequest, PurchaseDelivery>()
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id))
            .Map(dest => dest.PurchaseId, src => string.IsNullOrEmpty(src.PurchaseId) ? Guid.Empty : Guid.Parse(src.PurchaseId))
            .Map(dest => dest.DeliveryDate, src => DateOnly.Parse(src.DeliveryDate, CultureInfo.InvariantCulture));

        config.NewConfig<PurchaseDeliveryDetailRequest, PurchaseDeliveryDetail>()
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id))
            .Map(dest => dest.PurchaseDeliveryId, src => string.IsNullOrEmpty(src.PurchaseDeliveryId) ? Guid.Empty : Guid.Parse(src.PurchaseDeliveryId))
            .Map(dest => dest.ProductId, src => string.IsNullOrEmpty(src.ProductId) ? Guid.Empty : Guid.Parse(src.ProductId));

        // Sale
        config.NewConfig<SaleRequest, Sale>()
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id))
            .Map(dest => dest.CustomerId, src => string.IsNullOrEmpty(src.CustomerId) ? Guid.Empty : Guid.Parse(src.CustomerId))
            .Map(dest => dest.SaleDate, src => Convert.ToDateTime(src.SaleDate))
            .Map(dest => dest.CashSessionId, src => string.IsNullOrEmpty(src.CashSessionId) ? (Guid?)null : Guid.Parse(src.CashSessionId));
        
        config.NewConfig<SaleDetailRequest, SaleDetail>()
            .Map(dest => dest.Id, src => string.IsNullOrEmpty(src.Id) ? Guid.Empty : Guid.Parse(src.Id))
            .Map(dest => dest.SaleId, src => string.IsNullOrEmpty(src.SaleId) ? Guid.Empty : Guid.Parse(src.SaleId))
            .Map(dest => dest.ProductId, src => string.IsNullOrEmpty(src.ProductId) ? Guid.Empty : Guid.Parse(src.ProductId));
    }
}
