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
            if (purchaseDeliveryRequest.Detail.Count > 0)
            {
                purchaseDeliveryRequest.Id = Guid.Empty.ToString();
                purchaseDeliveryRequest.Detail.ForEach(d => { d.Id = Guid.Empty.ToString(); d.PurchaseDeliveryId = Guid.Empty.ToString(); });

                var purchaseDelivery = purchaseDeliveryRequest.Adapt<PurchaseDelivery>();
                purchaseDelivery.CreatedBy = modifiedBy;
                purchaseDelivery.Created = DateTime.Now;
                purchaseDelivery.ModifiedBy = modifiedBy;
                purchaseDelivery.Modified = DateTime.Now;
                purchaseDelivery.State = true;

                purchaseDelivery.Detail.ForEach(d =>
                {
                    d.CreatedBy = purchaseDelivery.CreatedBy;
                    d.Created = purchaseDelivery.Created;
                    d.Modified = purchaseDelivery.Created;
                    d.ModifiedBy = purchaseDelivery.CreatedBy;
                    d.State = purchaseDelivery.State;
                });

                var rowsAffected = await _purchaseRepository.ReceiveOrders(purchaseDelivery);
                if (rowsAffected <= 0)
                    throw new CustomException("No se pudo modificar la compra");

                if ((Domain.Enums.PurchaseStatusEnum)purchaseDeliveryRequest.PurchaseStatusId == Domain.Enums.PurchaseStatusEnum.TOTALLY_RECEIVED)
                {
                    var purchaseProduct = await _purchaseRepository.GetPurchase(Guid.Parse(purchaseDeliveryRequest.PurchaseId));
                    var purchase = new Purchase()
                    {
                        Id = purchaseProduct.Id,
                        Total = purchaseProduct.Total,
                        PurchaseStatusId = purchaseProduct.PurchaseStatusId,
                        IsActive = purchaseProduct.IsActive,
                        ModifiedBy = modifiedBy,
                        Modified = purchaseDelivery.Modified
                    };
                    if (purchaseProduct.PurchaseStatusId != purchaseDeliveryRequest.PurchaseStatusId)
                    {
                        purchase.PurchaseStatusId = purchaseDeliveryRequest.PurchaseStatusId;
                        await _purchaseRepository.UpdatePurchase(purchase);
                    }
                }

                respuesta.Data = respuesta.ok = true;
            }
            else
            {
                throw new CustomException("El detalle del recepción no puede estar vacio.");
            }
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
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
