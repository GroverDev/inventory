using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ILaboratoryRepository
{
    public Task<bool> CreateLaboratory(Laboratory laboratory);
    public Task<int> UpdateLaboratory(Laboratory laboratory);
    public Task<List<Laboratory>> GetLaboratories(string laboratoryName);
    public Task<Laboratory> GetLaboratory(Guid Id);
    public Task<int> DeleteLaboratory(Guid id, int idUserModified);
}
