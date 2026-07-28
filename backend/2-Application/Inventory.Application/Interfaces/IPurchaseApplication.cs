using System;
using Common.Utilities;
using Inventory.Domain;
using Inventory.Domain.Entities.Requests;

namespace Inventory.Application.Interfaces;

public interface IPurchaseApplication
{
    public Task<Response<bool>> CreatePurchase(PurchaseRequest purchaseRequest, int createdBy);

    public Task<Response<bool>> UpdatePurchase(PurchaseRequest purchaseRequest, int modifiedBy);

    public Task<Response<bool>> ReceiveOrders(PurchaseDeliveryRequest purchaseDeliveryRequest, int modifiedBy);

    /// <summary>Cierra con faltante una orden parcialmente recibida.</summary>
    public Task<Response<bool>> ClosePurchase(string id, int modifiedBy);

    /// <summary>Anula una orden que aún no recibió mercadería.</summary>
    public Task<Response<bool>> CancelPurchase(string id, int modifiedBy);

    public Task<Response<List<PurchaseProductResponse>>> GetPurchases(string purchaseDateInitial, string purchaseDateEnd, Domain.Enums.PurchaseStatusEnum purchaseStatus);

    public Task<Response<PurchaseRequest>> GetPurchase(string id);
    public Task<Response<bool>> DeletePurchase(string id, int modifiedBy);
}
