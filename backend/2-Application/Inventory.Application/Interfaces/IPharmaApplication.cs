using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface IPharmaApplication
{
    Task<Response<List<PharmaCatalogItem>>> GetForms();
    Task<Response<List<PharmaCatalogItem>>> GetRoutes();
    Task<Response<List<PharmaSubstance>>> SearchSubstances(string nombre);
    Task<Response<ProductPharma>> GetByProduct(string productId);
    Task<Response<bool>> Save(string productId, ProductPharmaRequest request, int userId);
    Task<Response<string>> GetLeaflet(string productId);
    Task<Response<bool>> SaveLeaflet(string productId, string? contenido, int userId);
    Task<Response<List<ProductEquivalentResponse>>> GetEquivalents(string productId);
    Task<Response<bool>> AddAlternative(string productId, string alternativeId, string? motivo, int userId);
    Task<Response<bool>> RemoveAlternative(string productId, string alternativeId);
}
