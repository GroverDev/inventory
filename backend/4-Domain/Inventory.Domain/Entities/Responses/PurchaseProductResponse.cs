namespace Inventory.Domain;

public class PurchaseProductResponse
{
    public Guid Id { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public decimal Total { get; set; }
    public bool IsActive { get; set; }

    public Guid ProviderId { get; set; }
    public DateOnly EstimatedDeliveryDate { get; set; }
    public int PurchaseStatusId { get; set; }

    public List<PurchaseProductDetailResponse> Detail { get; set; } = [];

    public string ProviderName { get; set; } = "";
    public string PurchaseStatusName { get; set; } = "";
}
