using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface ILaboratoryApplication
{

    public Task<Response<bool>> CreateLaboratory(LaboratoryRequest laboratoryRequest, int createdBy);
    public Task<Response<bool>> UpdateLaboratory(LaboratoryRequest laboratoryRequest, int modifiedBy);
    public Task<Response<bool>> DeleteLaboratory(string id, int modifiedBy);
    public Task<Response<List<LaboratoryRequest>>> GetLaboratories(string laboratoryName);
    public Task<Response<LaboratoryRequest>> GetLaboratory(string id);
}

