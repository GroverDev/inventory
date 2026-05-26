using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class PaymentMethodApplication(IPaymentMethodRepository _paymentMethodRepository) : IPaymentMethodApplication
{
    public async Task<Response<List<PaymentMethod>>> GetPaymentMethods()
    {
        Response<List<PaymentMethod>> respuesta = new() { Data = [] };
        try
        {
            respuesta.Data = await _paymentMethodRepository.GetPaymentMethods();
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return respuesta;
    }
}
