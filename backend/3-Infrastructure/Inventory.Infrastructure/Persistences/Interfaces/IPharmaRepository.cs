using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IPharmaRepository
{
    Task<List<PharmaCatalogItem>> GetForms();
    Task<List<PharmaCatalogItem>> GetRoutes();
    Task<List<PharmaSubstance>> SearchSubstances(string nombre);
    Task<ProductPharma?> GetByProduct(Guid productId);
    Task Save(Guid productId, ProductPharma datos,
              List<(Guid? SubstanceId, string SubstanceName, decimal? Value, string? Unit, bool EsActivo)> componentes,
              int userId);
    Task<string?> GetLeaflet(Guid productId);
    Task SaveLeaflet(Guid productId, string? contenido, int userId);
    Task<List<ProductEquivalentResponse>> GetEquivalents(Guid productId);
    Task<List<ProductEquivalentResponse>> GetManualAlternatives(Guid productId);
    /// <summary>Los productos que sugieren a este: la relación al revés.</summary>
    Task<List<ProductEquivalentResponse>> GetSuggestedIn(Guid productId);

    Task AddAlternative(Guid productId, Guid alternativeId, string? motivo, int userId);
    Task RemoveAlternative(Guid productId, Guid alternativeId);
}
