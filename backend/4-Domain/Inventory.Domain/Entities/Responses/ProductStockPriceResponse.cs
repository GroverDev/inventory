using System;

namespace Inventory.Domain.Entities.Responses;

public class ProductStockPriceResponse
{
    public Guid Id { get; set; } = Guid.Empty;
    public decimal SalePrice { get; set; }

    public int CurrentStock { get; set; }
    
}
