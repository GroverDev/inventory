using System;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistences.Interfaces;

namespace Inventory.Infrastructure.Persistences.Repositories;

public class PurchaseStatusRepository(InventoryDbContext _DbContext) : IPurchaseStatusRepository
{
    public async Task<List<PurchaseStatusResponse>>GetPurchaseStatus()
    {
        List<PurchaseStatusResponse> listPurchasesStatus = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                        SELECT id, description
                        FROM purchases_status ps              ";

            var result = await db.QueryAsync<PurchaseStatusResponse>(sqlQuery);
            listPurchasesStatus = result!.ToList();

        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listPurchasesStatus;
    }
}
