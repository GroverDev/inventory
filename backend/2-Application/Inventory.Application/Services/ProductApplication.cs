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
            // Solo se verifica que exista cuando el producto declara uno: es
            // opcional, y `GetLaboratory` lanza si no lo encuentra.
            if (!string.IsNullOrWhiteSpace(productRequest.LaboratoryId))
                await _laboratoryRepository.GetLaboratory(Guid.Parse(productRequest.LaboratoryId));
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
            // Solo se verifica que exista cuando el producto declara uno: es
            // opcional, y `GetLaboratory` lanza si no lo encuentra.
            if (!string.IsNullOrWhiteSpace(productRequest.LaboratoryId))
                await _laboratoryRepository.GetLaboratory(Guid.Parse(productRequest.LaboratoryId));
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

    public async Task<PagedResponse<List<ProductResponse>>> GetProductsStock(string productName, int page, int pageSize)
    {
        var resp = new PagedResponse<List<ProductResponse>> { Data = [], Page = page, PageSize = pageSize };
        try
        {
            var (items, total) = await _productRepository.GetProductsStock(productName, page, pageSize);
            resp.Data = items;
            resp.TotalCount = total;
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
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

    public async Task<Response<int>> BulkUpdateProducts(List<ProductBulkUpdateRequest> items, int modifiedBy)
    {
        Response<int> respuesta = new();
        try
        {
            foreach (var item in items)
            {
                item.ProductName = item.ProductName.Trim().ToUpper();
            }

            respuesta.Data = await _productRepository.BulkUpdateProducts(items, modifiedBy);
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    /// <summary>
    /// Activa el seguimiento de un producto, por lotes o por números de serie.
    /// No tiene vuelta atrás por diseño: una vez que hay existencias
    /// identificadas, volver a 'none' dejaría stock que nadie podría consumir.
    /// </summary>
    public async Task<Response<bool>> ActivateTracking(string id, string modo)
    {
        Response<bool> respuesta = new();
        try
        {
            if (!Guid.TryParse(id, out var productId) || productId == Guid.Empty)
                throw new CustomException("El identificador del producto no es válido.", MessageTypes.Warning);

            // Se valida contra la lista cerrada antes de tocar la base: 'none' no
            // se activa (es el estado inicial) y cualquier otro valor es un error
            // del cliente, no algo que deba llegar a PostgreSQL.
            if (modo != "lot" && modo != "serial")
                throw new CustomException(
                    "El seguimiento solo puede activarse por lotes o por números de serie.",
                    MessageTypes.Warning);

            await _productRepository.ActivateTracking(productId, modo);
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(ex.messageType == MessageTypes.Nothing ? MessageTypes.Warning : ex.messageType, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
}
