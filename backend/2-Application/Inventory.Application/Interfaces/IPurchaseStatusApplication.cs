using System;
using Common.Utilities;
using Inventory.Domain.Entities;

namespace Inventory.Application.Interfaces;

public interface IPurchaseStatusApplication
{
    public Task<Response<List<PurchaseStatusResponse>>> GetPurchaseStatus();
}
