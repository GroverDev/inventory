using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IPurchaseRepository
{
    public Task<bool> CreatePurchase(Purchase purchase);
    public Task<int> UpdatePurchase(Purchase purchase);
    public Task<int> DeletePurchase(Guid id, int idUserModified);
    public Task<int> ReceiveOrders(PurchaseDelivery purchaseDelivery);
    /// <summary>Cierra con faltante una orden parcialmente recibida.</summary>
    public Task<int> ClosePurchase(Guid id, int idUserModified);
    /// <summary>Anula una orden que aún no recibió mercadería.</summary>
    public Task<int> CancelPurchase(Guid id, int idUserModified);
    public Task<List<PurchaseProductResponse>> GetPurchases(DateOnly purchaseDateInitial, DateOnly purchaseDateEnd, Domain.Enums.PurchaseStatusEnum purchaseStatus);
    public Task<List<Purchase>> GetPurchases(string PurchaseDate);
    public Task<PurchaseProductResponse> GetPurchase(Guid Id);

}
