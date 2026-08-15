using System;
using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain;
using Inventory.Domain.Entities.Requests;
using Inventory.Infrastructure;

namespace Inventory.Application.Services;

public class PurchaseApplication(IPurchaseRepository _purchaseRepository): IPurchaseApplication
{
    /// <summary>
    /// Un producto solo puede aparecer una vez por orden. Con dos líneas del mismo
    /// producto el pendiente deja de ser atribuible a una línea concreta y la
    /// recepción no puede saber contra cuál descontar.
    /// </summary>
    private static void EnsureNoDuplicateProducts(List<PurchaseDetailRequest> detail)
    {
        var duplicated = detail
            .GroupBy(d => d.ProductId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicated is not null)
            throw new CustomException(
                $"El producto '{duplicated.First().ProductName}' está repetido en el detalle: únalo en una sola línea.",
                MessageTypes.Warning);
    }

    public async Task<Response<bool>> CreatePurchase(PurchaseRequest purchaseRequest, int createdBy)
    {
        Response<bool> respuesta = new();
        try
        {
            if (purchaseRequest.Detail.Count > 0)
            {
                var suma = purchaseRequest.Detail.Sum(x => x.OrderFinalPrice);
                if (suma != purchaseRequest.Total)
                {
                    throw new CustomException("El total general no es igual a la suma del detalle del pedido.");
                }

                EnsureNoDuplicateProducts(purchaseRequest.Detail);

                purchaseRequest.Id = Guid.Empty.ToString();
                purchaseRequest.Detail.ForEach(x => { x.PurchaseId = purchaseRequest.Id; x.Id = purchaseRequest.Id; x.DeliveryUnitPrice = 0; x.DeliveredQuantity = 0; x.DeliveryFinalPrice = 0; });

                var purchase = purchaseRequest.Adapt<Purchase>();

                purchase.CreatedBy = purchase.ModifiedBy = createdBy;
                purchase.Created = purchase.Modified = DateTime.Now;
                purchase.State = true;
                purchase.Detail.ForEach(d => { d.CreatedBy = purchase.CreatedBy; d.Created = purchase.Created; d.Modified = purchase.Created; d.ModifiedBy = purchase.CreatedBy; d.State = purchase.State; });

                respuesta.Data = await _purchaseRepository.CreatePurchase(purchase);
                respuesta.ok = true;
            }
            else
            {
                throw new CustomException("El detalle de la compra no puede estar vacio.");
            }
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> UpdatePurchase(PurchaseRequest purchaseRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            if (purchaseRequest.Detail.Count > 0)
            {
                var suma = purchaseRequest.Detail.Sum(x => x.OrderFinalPrice);
                if (suma != purchaseRequest.Total)
                    throw new CustomException("El total general no es igual a la suma del detalle del pedido.");

                EnsureNoDuplicateProducts(purchaseRequest.Detail);

                // Las líneas agregadas durante la edición llegan sin Id: normalizarlas
                // a Guid.Empty evita que Mapster falle al parsear una cadena vacía y
                // le indica al repositorio que debe insertarlas.
                purchaseRequest.Detail.ForEach(x =>
                {
                    if (!Guid.TryParse(x.Id, out var detailId) || detailId == Guid.Empty)
                        x.Id = Guid.Empty.ToString();
                    x.PurchaseId = purchaseRequest.Id;
                });

                var purchase = purchaseRequest.Adapt<Purchase>();
                purchase.ModifiedBy = modifiedBy;
                purchase.Modified = DateTime.Now;

                var rowsAffected = await _purchaseRepository.UpdatePurchase(purchase);
                if (rowsAffected <= 0)
                    throw new CustomException("No se pudo modificar la venta");
                respuesta.Data = respuesta.ok = true;
            }
            else
            {
                throw new CustomException("El detalle de la compra no puede estar vacio.");
            }
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
    public async Task<Response<bool>> ReceiveOrders(PurchaseDeliveryRequest purchaseDeliveryRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            if (!Guid.TryParse(purchaseDeliveryRequest.PurchaseId, out var purchaseId) || purchaseId == Guid.Empty)
                throw new CustomException("La orden de compra indicada no es válida.", MessageTypes.Warning);

            if (!DateTime.TryParse(purchaseDeliveryRequest.DeliveryDate, out var deliveryDate))
                throw new CustomException("La fecha de recepción no es válida.", MessageTypes.Warning);

            if (deliveryDate.Date > DateTime.Now.Date)
                throw new CustomException("La fecha de recepción no puede ser futura.", MessageTypes.Warning);

            // Solo viajan al dominio las líneas con mercadería efectivamente recibida.
            var lines = purchaseDeliveryRequest.Detail.Where(d => d.DeliveryQuantity > 0).ToList();
            if (lines.Count == 0)
                throw new CustomException("Debe indicar al menos un producto con cantidad recibida.", MessageTypes.Warning);

            var now = DateTime.Now;
            var purchaseDelivery = new PurchaseDelivery
            {
                PurchaseId = purchaseId,
                DeliveryDate = deliveryDate,
                // Sin uid del cliente se genera uno: la operación deja de ser
                // idempotente, pero nunca se bloquea una recepción legítima.
                OperationUid = Guid.TryParse(purchaseDeliveryRequest.OperationUid, out var uid) && uid != Guid.Empty
                    ? uid
                    : Guid.NewGuid(),
                IsActive = true,
                State = true,
                CreatedBy = modifiedBy,
                ModifiedBy = modifiedBy,
                Created = now,
                Modified = now
            };

            foreach (var line in lines)
            {
                if (!Guid.TryParse(line.ProductId, out var productId) || productId == Guid.Empty)
                    throw new CustomException("Uno de los productos de la recepción no es válido.", MessageTypes.Warning);

                DateTime? vencimiento = null;
                if (!string.IsNullOrWhiteSpace(line.ExpiryDate))
                {
                    if (!DateTime.TryParse(line.ExpiryDate, out var parsed))
                        throw new CustomException(
                            $"La fecha de vencimiento «{line.ExpiryDate}» no es válida.", MessageTypes.Warning);
                    vencimiento = parsed.Date;
                }

                purchaseDelivery.Detail.Add(new PurchaseDeliveryDetail
                {
                    ProductId = productId,
                    DeliveryQuantity = line.DeliveryQuantity,
                    UnitPrice = line.UnitPrice,
                    LotCode = string.IsNullOrWhiteSpace(line.LotCode) ? null : line.LotCode.Trim(),
                    ExpiryDate = vencimiento,
                    DeliveryDate = deliveryDate,
                    State = true,
                    CreatedBy = modifiedBy,
                    ModifiedBy = modifiedBy,
                    Created = now,
                    Modified = now
                });
            }

            // El estado resultante lo deriva el repositorio de los saldos reales;
            // lo que el cliente haya enviado en PurchaseStatusId se ignora.
            var rowsAffected = await _purchaseRepository.ReceiveOrders(purchaseDelivery);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo registrar la recepción.");

            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(ex.messageType == MessageTypes.Nothing ? MessageTypes.Warning : ex.messageType, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    /// <summary>Cierra con faltante una orden que el proveedor no completará.</summary>
    public async Task<Response<bool>> ClosePurchase(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            if (!Guid.TryParse(id, out var purchaseId) || purchaseId == Guid.Empty)
                throw new CustomException("La orden de compra indicada no es válida.", MessageTypes.Warning);

            var rowsAffected = await _purchaseRepository.ClosePurchase(purchaseId, modifiedBy);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo cerrar la orden de compra.");

            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(ex.messageType == MessageTypes.Nothing ? MessageTypes.Warning : ex.messageType, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    /// <summary>Anula una orden que todavía no recibió mercadería.</summary>
    public async Task<Response<bool>> CancelPurchase(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            if (!Guid.TryParse(id, out var purchaseId) || purchaseId == Guid.Empty)
                throw new CustomException("La orden de compra indicada no es válida.", MessageTypes.Warning);

            var rowsAffected = await _purchaseRepository.CancelPurchase(purchaseId, modifiedBy);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo cancelar la orden de compra.");

            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(ex.messageType == MessageTypes.Nothing ? MessageTypes.Warning : ex.messageType, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<List<PurchaseProductResponse>>> GetPurchases(string purchaseDateInitial, string purchaseDateEnd, Domain.Enums.PurchaseStatusEnum purchaseStatus)
    {
        Response<List<PurchaseProductResponse>> purchases = new() { Data = [] };
        try
        {
            purchaseDateInitial += " 00:00:01";
            purchaseDateEnd += " 23:59:59";
            #region Valida Fechas
            if (!DateTime.TryParse(purchaseDateInitial, out _))
                throw new CustomException("Fecha desde, es incorrecto.", MessageTypes.Warning);

            if (Convert.ToDateTime(purchaseDateInitial).Year > DateTime.Now.Year + 1)
                throw new CustomException($"Fecha desde, el año  no puede ser mayor al año {(DateTime.Now.Year + 1).ToString()}.", MessageTypes.Warning);

            if (Convert.ToDateTime(purchaseDateInitial).Year < 1900)
                throw new CustomException("Fecha desde, el año no puede ser menor al año 1900.", MessageTypes.Warning);

            if (!DateTime.TryParse(purchaseDateEnd, out _))
                throw new CustomException("Fecha hasta, es incorrecto.", MessageTypes.Warning);

            if (Convert.ToDateTime(purchaseDateEnd).Year > DateTime.Now.Year + 1)
                throw new CustomException($"Fecha hasta, el año  no puede ser mayor al año {(DateTime.Now.Year + 1).ToString()}.", MessageTypes.Warning);

            if (Convert.ToDateTime(purchaseDateEnd).Year < 1900)
                throw new CustomException("Fecha hasta, el año no puede ser menor al año 1900.", MessageTypes.Warning);

            if (Convert.ToDateTime(purchaseDateInitial) > Convert.ToDateTime(purchaseDateEnd))
                throw new CustomException("Fecha desde, no puede ser mayor a la Fecha hasta.", MessageTypes.Warning);
            #endregion

            var respList = await _purchaseRepository.GetPurchases(Convert.ToDateTime(purchaseDateInitial), Convert.ToDateTime(purchaseDateEnd), purchaseStatus);

            foreach (var item in respList)
            {
                var purchase = item.Adapt<PurchaseProductResponse>();
                purchases.Data.Add(purchase);
            }
            purchases.ok = true;

        }
        catch (CustomException ex) { purchases.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { purchases.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return purchases;
    }

    public async Task<Response<PurchaseRequest>> GetPurchase(string id)
    {
        Response<PurchaseRequest> respPurchases = new() { Data = new() };
        try
        {
            Guid purchaseId = Guid.Parse(id);
            var purchase = await _purchaseRepository.GetPurchase(purchaseId);

            var purchaseRequest = purchase.Adapt<PurchaseRequest>();

            respPurchases.Data = purchaseRequest;
            respPurchases.ok = true;
        }
        catch (CustomException ex) { respPurchases.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respPurchases.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respPurchases;
    }

    public async Task<Response<bool>> DeletePurchase(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            Guid purchaseId = Guid.Parse(id);

            var rowsAffected = await _purchaseRepository.DeletePurchase(purchaseId, modifiedBy);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo eliminar la compra");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
}
