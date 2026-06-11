using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;
using Microsoft.Extensions.Options;

namespace Inventory.Application;

public class SalesApplication(
    ISalesRepository _salesRepository,
    IProductRepository _productRepository,
    ICustomersRepository _customersRepository,
    IDiscountRepository _discountRepository,
    IOptions<PosSettings> _posSettings) : ISalesApplication
{
    public async Task<Response<string>> CreateSale(SaleRequest saleRequest, int createdBy, string userRole, bool supervisorApproved = false)
    {
        Response<string> respuesta = new();
        try
        {
            if (saleRequest.Detail.Count > 0)
            {
                // Recalculate line discounts from predefined discount catalog when DiscountId is provided
                foreach (var line in saleRequest.Detail)
                {
                    if (!string.IsNullOrEmpty(line.DiscountId) && Guid.TryParse(line.DiscountId, out var discountGuid))
                    {
                        var discount = await _discountRepository.GetDiscount(discountGuid)
                            ?? throw new CustomException($"El descuento indicado en el producto no existe o está inactivo.");

                        line.LineTotalDiscounts = discount.Type == "Percentage"
                            ? Math.Round(line.LineSubtotal * discount.Value / 100, 2)
                            : Math.Min(discount.Value, line.LineSubtotal);
                    }
                    line.LineTotal = line.LineSubtotal - line.LineTotalDiscounts;
                }

                // Recalculate header discount
                if (!string.IsNullOrEmpty(saleRequest.HeaderDiscountId) && Guid.TryParse(saleRequest.HeaderDiscountId, out var headerDiscGuid))
                {
                    var headerDiscount = await _discountRepository.GetDiscount(headerDiscGuid)
                        ?? throw new CustomException("El descuento de cabecera no existe o está inactivo.");

                    decimal subtotalAfterLineDiscounts = saleRequest.Detail.Sum(x => x.LineTotal);
                    saleRequest.HeaderDiscountAmount = headerDiscount.Type == "Percentage"
                        ? Math.Round(subtotalAfterLineDiscounts * headerDiscount.Value / 100, 2)
                        : Math.Min(headerDiscount.Value, subtotalAfterLineDiscounts);
                }

                // Enforce manual discount limits for cashiers (omitir si un supervisor ya autorizó)
                if (userRole == "Cajero" && !supervisorApproved)
                {
                    var cfg = _posSettings.Value;
                    foreach (var line in saleRequest.Detail)
                    {
                        // Solo descuentos manuales (sin DiscountId de catálogo)
                        if (!string.IsNullOrEmpty(line.DiscountId) || line.LineTotalDiscounts == 0) continue;

                        if (line.DiscountType == "Percentage" && line.DiscountValue > cfg.MaxCashierDiscountPct)
                            throw new CustomException(
                                $"Descuento manual por porcentaje supera el límite del {cfg.MaxCashierDiscountPct}% permitido para cajeros.");

                        if (line.DiscountType == "FixedAmount" && line.DiscountValue > cfg.MaxCashierDiscountAmount)
                            throw new CustomException(
                                $"Descuento manual por monto supera el límite de Bs. {cfg.MaxCashierDiscountAmount} permitido para cajeros.");
                    }

                    // Descuento global manual
                    if (string.IsNullOrEmpty(saleRequest.HeaderDiscountId) && saleRequest.HeaderDiscountAmount > 0)
                    {
                        if (saleRequest.HeaderDiscountType == "Percentage" && saleRequest.HeaderDiscountValue > cfg.MaxCashierDiscountPct)
                            throw new CustomException(
                                $"Descuento global por porcentaje supera el límite del {cfg.MaxCashierDiscountPct}% permitido para cajeros.");

                        if (saleRequest.HeaderDiscountType == "FixedAmount" && saleRequest.HeaderDiscountValue > cfg.MaxCashierDiscountAmount)
                            throw new CustomException(
                                $"Descuento global por monto supera el límite de Bs. {cfg.MaxCashierDiscountAmount} permitido para cajeros.");
                    }
                }

                // Recompute sale totals on the server — never trust client-side totals
                saleRequest.Subtotal              = saleRequest.Detail.Sum(x => x.LineSubtotal);
                var totalLineDiscounts             = saleRequest.Detail.Sum(x => x.LineTotalDiscounts);
                saleRequest.TotalDiscounts         = totalLineDiscounts + saleRequest.HeaderDiscountAmount;
                saleRequest.Total                  = saleRequest.Subtotal - saleRequest.TotalDiscounts;

                if (saleRequest.Total <= 0)
                    throw new CustomException("El total de la venta no puede ser cero o negativo.");

                if (saleRequest.Payments.Count == 0)
                    throw new CustomException("Debe registrar al menos un método de pago.");

                var totalPaid = saleRequest.Payments.Sum(p => p.AmountGiven);
                if (totalPaid < saleRequest.Total)
                    throw new CustomException("El monto total pagado no puede ser menor al total de la venta.");

                var respCustomer = await _customersRepository.GetCustomer(Guid.Parse(saleRequest.CustomerId));

                saleRequest.SaleDate = saleRequest.SaleDate == "" ? DateTime.Now.ToString() : saleRequest.SaleDate;
                saleRequest.Id = Guid.Empty.ToString();
                if (string.IsNullOrEmpty(saleRequest.HeaderDiscountId))
                    saleRequest.HeaderDiscountId = Guid.Empty.ToString();
                saleRequest.Detail.ForEach(x =>
                {
                    x.SaleId = saleRequest.Id;
                    x.Id = saleRequest.Id;
                    if (string.IsNullOrEmpty(x.DiscountId))
                        x.DiscountId = Guid.Empty.ToString();
                });

                var sale = saleRequest.Adapt<Sale>();

                // Convertir Guid.Empty → null en campos opcionales de descuento
                if (sale.HeaderDiscountId == Guid.Empty) sale.HeaderDiscountId = null;
                foreach (var det in sale.Detail)
                    if (det.DiscountId == Guid.Empty) det.DiscountId = null;

                AuditHelper.SetCreated(sale, createdBy);
                foreach (var det in sale.Detail)
                {
                    var respProduct = await _productRepository.GetProduct(det.ProductId);
                    AuditHelper.SetCreated(det, createdBy);
                }

                respuesta.Data = await _salesRepository.CreateSale(sale);
                respuesta.ok = true;
            }
            else
            {
                throw new CustomException("El detalle de la venta no puede estar vacio.");
            }
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> UpdateSale(SaleRequest saleRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            if (saleRequest.Detail.Count > 0)
            {
                var sale = saleRequest.Adapt<Sale>();
                AuditHelper.SetModified(sale, modifiedBy);
                foreach (var det in sale.Detail)
                {
                    AuditHelper.SetModified(det, modifiedBy);
                }
                var rowsAffected = await _salesRepository.UpdateSale(sale);
                if (rowsAffected <= 0)
                    throw new CustomException("No se pudo modificar la venta");
                respuesta.Data = respuesta.ok = true;
            }
            else
            {
                throw new CustomException("El detalle de la venta no puede estar vacio.");
            }
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> DeleteSale(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            Guid saleId = Guid.Parse(id);

            var rowsAffected = await _salesRepository.DeleteSale(saleId, modifiedBy);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo eliminar la venta");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
    public async Task<Response<SalesPagedResponse>> GetSales(string saleDateInitial, string saleDateEnd, int userId, string rol, int page = 1, int pageSize = 50, string? sellerName = null)
    {
        Response<SalesPagedResponse> response = new() { Data = new() };
        try
        {
            saleDateInitial += " 00:00:01";
            saleDateEnd     += " 23:59:59";

            if (!DateTime.TryParse(saleDateInitial, out _))
                throw new CustomException("Fecha desde, es incorrecto.", MessageTypes.Warning);
            if (Convert.ToDateTime(saleDateInitial).Year > DateTime.Now.Year + 1)
                throw new CustomException($"Fecha desde, el año no puede ser mayor al año {DateTime.Now.Year + 1}.", MessageTypes.Warning);
            if (Convert.ToDateTime(saleDateInitial).Year < 1900)
                throw new CustomException("Fecha desde, el año no puede ser menor al año 1900.", MessageTypes.Warning);

            if (!DateTime.TryParse(saleDateEnd, out _))
                throw new CustomException("Fecha hasta, es incorrecto.", MessageTypes.Warning);
            if (Convert.ToDateTime(saleDateEnd).Year > DateTime.Now.Year + 1)
                throw new CustomException($"Fecha hasta, el año no puede ser mayor al año {DateTime.Now.Year + 1}.", MessageTypes.Warning);
            if (Convert.ToDateTime(saleDateEnd).Year < 1900)
                throw new CustomException("Fecha hasta, el año no puede ser menor al año 1900.", MessageTypes.Warning);

            if (Convert.ToDateTime(saleDateInitial) > Convert.ToDateTime(saleDateEnd))
                throw new CustomException("Fecha desde, no puede ser mayor a la Fecha hasta.", MessageTypes.Warning);

            int? filterUserId   = rol == "Cajero" ? userId : null;
            string? filterSeller = rol == "Cajero" ? null  : sellerName;

            response.Data = await _salesRepository.GetSales(
                Convert.ToDateTime(saleDateInitial),
                Convert.ToDateTime(saleDateEnd),
                filterUserId,
                page,
                pageSize,
                filterSeller);

            response.ok = true;
        }
        catch (CustomException ex) { response.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { response.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return response;
    }
    public async Task<Response<SaleProductResponse>> GetSale(string id)
    {
        Response<SaleProductResponse> respSales = new() { Data = new() };
        try
        {
            Guid saleId = Guid.Parse(id);
            respSales.Data = await _salesRepository.GetSale(saleId);
            respSales.ok = true;
        }
        catch (CustomException ex) { respSales.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respSales.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respSales;
    }
}
