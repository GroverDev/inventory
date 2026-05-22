using System;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Persistences.Interfaces;

public interface IPurchaseStatusRepository
{
    public Task<List<PurchaseStatusResponse>>GetPurchaseStatus();
}
