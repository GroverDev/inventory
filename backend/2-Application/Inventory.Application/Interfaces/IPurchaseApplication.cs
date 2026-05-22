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

    public Task<Response<List<PurchaseProductResponse>>> GetPurchases(string purchaseDateInitial, string purchaseDateEnd, Domain.Enums.PurchaseStatusEnum purchaseStatus);

    public Task<Response<PurchaseRequest>> GetPurchase(string id);
    public Task<Response<bool>> DeletePurchase(string id, int modifiedBy);
}
