using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistences.Interfaces;

namespace Inventory.Application;

public class ProductApplication(
    IProductRepository _productRepository,
    ILaboratoryRepository _laboratoryRepository,
    IUnitsOfMeasurementRepository _unitsOfMeasurementRepository
    ) : IProductApplication
{
    public async Task<Response<string>> CreateProduct(ProductRequest productRequest, int createdBy)
    {
        Response<string> respuesta = new() { Data = "" };
        try
        {
            var respLab = await _laboratoryRepository.GetLaboratory(Guid.Parse(productRequest.LaboratoryId));
            var respUnit = await _unitsOfMeasurementRepository.GetUnitOfMeasurement(Guid.Parse(productRequest.UomId));

            productRequest.ProductName = productRequest.ProductName.Trim().ToUpper();
            productRequest.Description = productRequest.Description.Trim().ToUpper();

            var product = productRequest.Adapt<Product>();
            product.IsActive = true;
            

            AuditHelper.SetCreated(product, createdBy);
            respuesta.Data = await _productRepository.CreateProduct(product);
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> UpdateProduct(ProductRequest productRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            var respLab = await _laboratoryRepository.GetLaboratory(Guid.Parse(productRequest.LaboratoryId));
            var respUnit = await _unitsOfMeasurementRepository.GetUnitOfMeasurement(Guid.Parse(productRequest.UomId));

            var product = productRequest.Adapt<Product>();
            AuditHelper.SetModified(product, modifiedBy);
            
            var rowsAffected = await _productRepository.UpdateProduct(product);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo modificar el producto");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> DeleteProduct(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            Guid productId = Guid.Parse(id);

            var rowsAffected = await _productRepository.DeleteProduct(productId, modifiedBy);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo eliminar el producto");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<List<ProductResponse>>> GetProducts(string productName)
    {
        Response<List<ProductResponse>> products = new() { Data = new() };
        try
        {
            products.Data = await _productRepository.GetProducts(productName);
            products.ok = true;
        }
        catch (CustomException ex) { products.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { products.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return products;
    }

    public async Task<Response<ProductResponse>> GetProduct(string id)
    {
        Response<ProductResponse> product = new() { Data = new() };
        try
        {
            Guid productId = Guid.Parse(id);
            product.Data = await _productRepository.GetProduct(productId);
            product.ok = true;
        }
        catch (CustomException ex) { product.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { product.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return product;
    }

    public async Task<Response<ProductStockPriceResponse>> GetProductStockPrice(string id)
    {
        Response<ProductStockPriceResponse> product = new() { Data = new() };
        try
        {
            Guid productId = Guid.Parse(id);
            product.Data = await _productRepository.GetProductStockPrice(productId);
            product.ok = true;
        }
        catch (CustomException ex) { product.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { product.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return product;
    }
}
