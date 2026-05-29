using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain;
using Inventory.Infrastructure;
using Mapster;

namespace Inventory.Application.Services;

public class DiscountApplication(IDiscountRepository _discountRepository) : IDiscountApplication
{
    public async Task<Response<List<DiscountResponse>>> GetDiscounts()
    {
        Response<List<DiscountResponse>> respuesta = new() { Data = [] };
        try
        {
            respuesta.Data = await _discountRepository.GetDiscounts();
            respuesta.ok = true;
        }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<DiscountResponse>> GetDiscount(string id)
    {
        Response<DiscountResponse> respuesta = new() { Data = new() };
        try
        {
            var discount = await _discountRepository.GetDiscount(Guid.Parse(id));
            if (discount is null)
                throw new CustomException("Descuento no encontrado.");
            respuesta.Data = discount;
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<string>> CreateDiscount(DiscountRequest request, int createdBy)
    {
        Response<string> respuesta = new();
        try
        {
            ValidateRequest(request);

            request.Id = Guid.Empty.ToString();
            var discount = request.Adapt<Discount>();
            AuditHelper.SetCreated(discount, createdBy);

            respuesta.Data = await _discountRepository.CreateDiscount(discount);
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> UpdateDiscount(string id, DiscountRequest request, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            ValidateRequest(request);

            request.Id = id;
            var discount = request.Adapt<Discount>();
            discount.Id = Guid.Parse(id);
            AuditHelper.SetModified(discount, modifiedBy);

            var rows = await _discountRepository.UpdateDiscount(discount);
            if (rows <= 0) throw new CustomException("No se pudo modificar el descuento.");

            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> DeleteDiscount(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            var rows = await _discountRepository.DeleteDiscount(Guid.Parse(id), modifiedBy);
            if (rows <= 0) throw new CustomException("No se pudo eliminar el descuento.");

            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    private static void ValidateRequest(DiscountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new CustomException("El nombre del descuento es requerido.");

        if (request.Type != "Percentage" && request.Type != "FixedAmount")
            throw new CustomException("El tipo de descuento debe ser 'Percentage' o 'FixedAmount'.");

        if (request.Value <= 0)
            throw new CustomException("El valor del descuento debe ser mayor a cero.");

        if (request.Type == "Percentage" && request.Value > 100)
            throw new CustomException("Un descuento por porcentaje no puede superar el 100%.");
    }
}
