using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Services.Api.Controllers.Security;


[ApiExplorerSettings(GroupName = "SECURITY")]
[Route("api/[controller]")]
[ApiController]

public class TestController : ControllerBase
{
    [HttpGet]
    public ActionResult OkTest()
    {
        return Ok(":)"); 
    }
    [Authorize]
    [HttpGet("private")]
    public ActionResult OkTestPrivate()
    {
        return Ok(); 
    }
}

