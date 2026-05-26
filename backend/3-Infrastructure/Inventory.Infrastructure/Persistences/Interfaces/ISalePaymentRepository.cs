using System.Data;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ISalePaymentRepository
{
    Task CreateSalePayments(List<SalePayment> payments, IDbConnection db, IDbTransaction transaction);
    Task<List<SalePaymentResponse>> GetSalePayments(Guid saleId, IDbConnection db);
}
