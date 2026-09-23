using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IBranchRepository
{
    /// <summary>Crea la sucursal y habilita en ella a quien la crea. Devuelve su id.</summary>
    public Task<Guid> CreateBranch(Branch branch);
    public Task<int> UpdateBranch(Branch branch);
    public Task<List<Branch>> GetBranches(string name, bool includeInactive);
    public Task<Branch> GetBranch(Guid id);
    public Task<int> DeleteBranch(Guid id, int modifiedBy);

    /// <summary>Sucursales activas en las que el usuario está habilitado.</summary>
    public Task<List<Guid>> GetUserBranchIds(int userId);
}
