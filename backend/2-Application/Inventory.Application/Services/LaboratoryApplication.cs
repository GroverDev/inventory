using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class LaboratoryApplication(ILaboratoryRepository _laboratoryRepository) : ILaboratoryApplication
{
    public async Task<Response<bool>> CreateLaboratory(LaboratoryRequest laboratoryRequest, int createdBy)
    {
        Response<bool> respuesta = new();
        try
        {
            laboratoryRequest.Id = Guid.Empty.ToString();

            var laboratory = laboratoryRequest.Adapt<Laboratory>();
            laboratory.CreatedBy = laboratory.ModifiedBy = createdBy;
            laboratory.Created = laboratory.Modified = DateTime.UtcNow;
            laboratory.State = true;

            respuesta.Data = await _laboratoryRepository.CreateLaboratory(laboratory);
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> UpdateLaboratory(LaboratoryRequest laboratoryRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            var laboratory = laboratoryRequest.Adapt<Laboratory>();
            laboratory.ModifiedBy = modifiedBy;
            laboratory.Modified = DateTime.UtcNow;

            var rowsAffected = await _laboratoryRepository.UpdateLaboratory(laboratory);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo modificar el laboratorio");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> DeleteLaboratory(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            Guid laboratoryId = Guid.Parse(id);

            var rowsAffected = await _laboratoryRepository.DeleteLaboratory(laboratoryId, modifiedBy);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo eliminar al fabricante");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<List<LaboratoryRequest>>> GetLaboratories(string laboratoryName)
    {
        Response<List<LaboratoryRequest>> laboratories = new() { Data = [] };
        try
        {
            var resp = await _laboratoryRepository.GetLaboratories(laboratoryName);
            foreach (var laboratoryItem in resp)
            {
                var laboratoryNew = laboratoryItem.Adapt<LaboratoryRequest>();
                laboratories.Data.Add(laboratoryNew);
            }

            laboratories.ok = true;
        }
        catch (CustomException ex) { laboratories.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { laboratories.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return laboratories;
    }

    public async Task<Response<LaboratoryRequest>> GetLaboratory(string id)
    {
        Response<LaboratoryRequest> respLaboratory = new() { Data = new() };
        try
        {
            Guid laboratoryId = Guid.Parse(id);
            var laboratory = await _laboratoryRepository.GetLaboratory(laboratoryId);

            var clientNew = laboratory.Adapt<LaboratoryRequest>();
            respLaboratory.Data = clientNew;
            respLaboratory.ok = true;
        }
        catch (CustomException ex) { respLaboratory.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respLaboratory.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respLaboratory;
    }

  
}
