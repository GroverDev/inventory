using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class BranchApplication(IBranchRepository _branchRepository) : IBranchApplication
{
    public async Task<Response<string>> CreateBranch(BranchRequest branchRequest, int createdBy)
    {
        Response<string> respuesta = new();
        try
        {
            branchRequest.Id = Guid.Empty.ToString();

            var branch = branchRequest.Adapt<Branch>();
            branch.CreatedBy = branch.ModifiedBy = createdBy;
            branch.Created = branch.Modified = DateTime.UtcNow;

            respuesta.Data = (await _branchRepository.CreateBranch(branch)).ToString();
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> UpdateBranch(BranchRequest branchRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            var branch = branchRequest.Adapt<Branch>();
            branch.ModifiedBy = modifiedBy;
            branch.Modified = DateTime.UtcNow;

            if (await _branchRepository.UpdateBranch(branch) <= 0)
                throw new CustomException("No se pudo modificar la sucursal.");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> DeleteBranch(Guid id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            if (await _branchRepository.DeleteBranch(id, modifiedBy) <= 0)
                throw new CustomException("No se pudo eliminar la sucursal.");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<List<BranchRequest>>> GetBranches(string name, bool includeInactive)
    {
        Response<List<BranchRequest>> respuesta = new() { Data = [] };
        try
        {
            var branches = await _branchRepository.GetBranches(name, includeInactive);
            respuesta.Data = branches.Adapt<List<BranchRequest>>();
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public Task<List<Guid>> GetUserBranchIds(int userId) => _branchRepository.GetUserBranchIds(userId);

    public async Task<Response<BranchRequest>> GetBranch(Guid id)
    {
        Response<BranchRequest> respuesta = new() { Data = new() };
        try
        {
            respuesta.Data = (await _branchRepository.GetBranch(id)).Adapt<BranchRequest>();
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(ex.messageType, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
}
