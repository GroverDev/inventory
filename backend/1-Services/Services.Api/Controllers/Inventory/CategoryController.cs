using Common.Utilities;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class CategoryController(ICategoryApplication _categoryApplication) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Response<bool>>> CreateCategory([FromBody] CategoryRequest categoryRequest)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);
        return await _categoryApplication.CreateCategory(categoryRequest, Convert.ToInt32(datos.UserId));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdateCategory(string id, [FromBody] CategoryRequest categoryRequest)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no válido" } });
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);
        return await _categoryApplication.UpdateCategory(categoryRequest, Convert.ToInt32(datos.UserId));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> DeleteCategory(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no válido" } });
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);
        return await _categoryApplication.DeleteCategory(id, Convert.ToInt32(datos.UserId));
    }

    [HttpGet]
    public async Task<ActionResult<Response<List<CategoryRequest>>>> GetCategories(string categoryName = "ALL")
    {
        return await _categoryApplication.GetCategories(categoryName == "ALL" ? "" : categoryName);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Response<CategoryRequest>>> GetCategory(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no válido" } });
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _categoryApplication.GetCategory(id);
    }
}
