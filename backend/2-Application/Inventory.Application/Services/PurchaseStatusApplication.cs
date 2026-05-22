using System;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistences.Interfaces;

namespace Inventory.Application.Services;

public class PurchaseStatusApplication(IPurchaseStatusRepository _purchaseStatusRepository): IPurchaseStatusApplication
{
    public async Task<Response<List<PurchaseStatusResponse>>> GetPurchaseStatus()
    {
        Response<List<PurchaseStatusResponse>> purchasesStatus = new() { Data = [] };
        try
        {
            var respList = await _purchaseStatusRepository.GetPurchaseStatus();
            purchasesStatus.Data = respList;
            purchasesStatus.ok = true;

        }
        catch (CustomException ex) { purchasesStatus.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { purchasesStatus.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return purchasesStatus;
    }
}
