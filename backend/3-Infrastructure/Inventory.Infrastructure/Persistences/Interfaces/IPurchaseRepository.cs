using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IPurchaseRepository
{
    public Task<bool> CreatePurchase(Purchase purchase);
    public Task<int> UpdatePurchase(Purchase purchase);
    public Task<int> DeletePurchase(Guid id, int idUserModified);
    public Task<int> ReceiveOrders(PurchaseDelivery purchaseDelivery);
    public Task<List<PurchaseProductResponse>> GetPurchases(DateTime purchaseDateInitial, DateTime purchaseDateEnd, Domain.Enums.PurchaseStatusEnum purchaseStatus);
    public Task<List<Purchase>> GetPurchases(string PurchaseDate);
    public Task<PurchaseProductResponse> GetPurchase(Guid Id);

}
