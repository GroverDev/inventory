using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface IBranchApplication
{
    public Task<Response<string>> CreateBranch(BranchRequest branchRequest, int createdBy);
    public Task<Response<bool>> UpdateBranch(BranchRequest branchRequest, int modifiedBy);
    public Task<Response<bool>> DeleteBranch(Guid id, int modifiedBy);
    public Task<Response<List<BranchRequest>>> GetBranches(string name, bool includeInactive);
    public Task<Response<BranchRequest>> GetBranch(Guid id);

    /// <summary>Sucursales activas en las que el usuario está habilitado.</summary>
    public Task<List<Guid>> GetUserBranchIds(int userId);
}
