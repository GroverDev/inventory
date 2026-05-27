using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class SalesApplication(
    ISalesRepository _salesRepository, 
    IProductRepository _productRepository,
    ICustomersRepository _customersRepository): ISalesApplication
{
    public async Task<Response<string>> CreateSale(SaleRequest saleRequest, int createdBy)
    {
        Response<string> respuesta = new();
        try
        {
            if (saleRequest.Detail.Count > 0)
            {
                var suma = saleRequest.Detail.Sum(x => x.LineTotal);
                if (suma != saleRequest.Total)
                {
                    throw new CustomException("El total general no es igual a la suma del detalle de venta.");
                }

                // if ((saleRequest.TotalPaidClient - saleRequest.TotalReturnedClient) < saleRequest.TotalPrice)
                // {
                //     throw new CustomException("El total pagado, no puede ser menor al monto total de la venta.");
                // }

                if (saleRequest.Payments.Count == 0)
                    throw new CustomException("Debe registrar al menos un método de pago.");

                var totalPaid = saleRequest.Payments.Sum(p => p.AmountGiven);
                if (totalPaid < saleRequest.Total)
                    throw new CustomException("El monto total pagado no puede ser menor al total de la venta.");

                var respCustomer = await _customersRepository.GetCustomer(Guid.Parse(saleRequest.CustomerId));

                saleRequest.SaleDate = saleRequest.SaleDate == "" ? DateTime.Now.ToString() : saleRequest.SaleDate;
                saleRequest.Id = Guid.Empty.ToString();
                saleRequest.Detail.ForEach(x => { x.SaleId = saleRequest.Id; x.Id = saleRequest.Id; });

                var sale = saleRequest.Adapt<Sale>();

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
    public async Task<Response<List<SaleProductResponse>>> GetSales(string saleDateInitial, string saleDateEnd, int userId, string rol)
    {
        Response<List<SaleProductResponse>> sales = new() { Data = [] };
        try
        {
            saleDateInitial += " 00:00:01";
            saleDateEnd += " 23:59:59";
            #region Valida Fechas
            if (!DateTime.TryParse(saleDateInitial, out _))
                throw new CustomException("Fecha desde, es incorrecto.", MessageTypes.Warning);

            if (Convert.ToDateTime(saleDateInitial).Year > DateTime.Now.Year + 1)
                throw new CustomException($"Fecha desde, el año  no puede ser mayor al año {(DateTime.Now.Year + 1).ToString()}.", MessageTypes.Warning);

            if (Convert.ToDateTime(saleDateInitial).Year < 1900)
                throw new CustomException("Fecha desde, el año no puede ser menor al año 1900.", MessageTypes.Warning);

            if (!DateTime.TryParse(saleDateEnd, out _))
                throw new CustomException("Fecha hasta, es incorrecto.", MessageTypes.Warning);

            if (Convert.ToDateTime(saleDateEnd).Year > DateTime.Now.Year + 1)
                throw new CustomException($"Fecha hasta, el año  no puede ser mayor al año {(DateTime.Now.Year + 1).ToString()}.", MessageTypes.Warning);

            if (Convert.ToDateTime(saleDateEnd).Year < 1900)
                throw new CustomException("Fecha hasta, el año no puede ser menor al año 1900.", MessageTypes.Warning);

            if (Convert.ToDateTime(saleDateInitial) > Convert.ToDateTime(saleDateEnd))
                throw new CustomException("Fecha desde, no puede ser mayor a la Fecha hasta.", MessageTypes.Warning);
            #endregion

            int? filterUserId = rol == "Cajero" ? userId : null;
            var respList = await _salesRepository.GetSales(Convert.ToDateTime(saleDateInitial), Convert.ToDateTime(saleDateEnd), filterUserId);

            foreach (var saleItem in respList)
            {
                var sale = saleItem.Adapt<SaleProductResponse>();
                sales.Data.Add(sale);
            }
            sales.ok = true;

        }
        catch (CustomException ex) { sales.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { sales.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return sales;
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
