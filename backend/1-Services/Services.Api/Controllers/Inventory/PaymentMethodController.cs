using Common.Utilities;
using Inventory.Application.Interfaces;
using Inventory.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[ApiController]
public class PaymentMethodController(IPaymentMethodApplication _paymentMethodApplication) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Response<List<PaymentMethod>>>> GetPaymentMethods()
    {
        return await _paymentMethodApplication.GetPaymentMethods();
    }
}
