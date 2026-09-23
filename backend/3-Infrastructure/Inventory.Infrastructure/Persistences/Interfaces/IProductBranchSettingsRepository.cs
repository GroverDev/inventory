using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IProductBranchSettingsRepository
{
    /// <summary>Todas las sucursales activas, con la excepción del producto si la tienen.</summary>
    public Task<List<ProductBranchSettingResponse>> GetSettings(Guid productId);

    /// <summary>Guarda las excepciones indicadas. Una fila con los dos valores nulos se borra.</summary>
    public Task SaveSettings(Guid productId, List<ProductBranchSettingRequest> settings, int userId);
}
