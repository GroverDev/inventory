using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface IProductBranchSettingsApplication
{
    public Task<Response<List<ProductBranchSettingResponse>>> GetSettings(Guid productId);
    public Task<Response<bool>> SaveSettings(Guid productId, List<ProductBranchSettingRequest> settings, int userId);
}
