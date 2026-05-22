using System.Data;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IPurchaseDetailRepository
{
    public Task<bool> CreatePurchaseDetail(PurchaseDetail detail, IDbConnection db, IDbTransaction transaction);
    public Task<bool> UpdatePurchaseDetail(PurchaseDetail detail, IDbConnection db, IDbTransaction transaction);
    public Task<bool> ReceiveOrdersDetail(PurchaseDeliveryDetail detail, IDbConnection db, IDbTransaction transaction);
}
