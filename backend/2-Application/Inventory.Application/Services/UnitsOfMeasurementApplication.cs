using System;
using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities.Responses;
using Inventory.Infrastructure.Persistences.Interfaces;

using Inventory.Domain.Entities.Requests;
using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public class UnitsOfMeasurementApplication(IUnitsOfMeasurementRepository _unitsOfMeasurementRepository) : IUnitsOfMeasurementApplication
{
    public async Task<Response<UnitOfMeasurementResponse>> GetUnitOfMeasurement(string id)
{
    Response<UnitOfMeasurementResponse> respLaboratory = new() { Data = new() };
    try
    {
        Guid unitId = Guid.Parse(id);
        var unit = await _unitsOfMeasurementRepository.GetUnitOfMeasurement(unitId);

        var unitNew = unit.Adapt<UnitOfMeasurementResponse>();
        respLaboratory.Data = unitNew;
        respLaboratory.ok = true;
    }
    catch (CustomException ex) { respLaboratory.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respLaboratory.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respLaboratory;
}

public async Task<Response<List<UnitOfMeasurementResponse>>> GetUnitsOfMeasurement(string unitOfMeasurementName)
{
    Response<List<UnitOfMeasurementResponse>> respUnits = new() { Data = new List<UnitOfMeasurementResponse>() };
    try
    {
        var resp = await _unitsOfMeasurementRepository.GetUnitsOfMeasurement(unitOfMeasurementName);
        foreach (var unit in resp)
        {
            var unitNew = unit.Adapt<UnitOfMeasurementResponse>();
            respUnits.Data.Add(unitNew);
        }
        respUnits.ok = true;
    }
    catch (CustomException ex) { respUnits.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respUnits.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respUnits!;
}


public async Task<Response<string>> CreateUnitOfMeasurement(UnitOfMeasurementRequest unitOfMeasurementRequest, int createdBy)
{
    Response<string> respuesta = new() { Data = "" };
    try
    {
        unitOfMeasurementRequest.Id = Guid.Empty.ToString();
        unitOfMeasurementRequest.Name = unitOfMeasurementRequest.Name.Trim().ToUpper();

        var unit = unitOfMeasurementRequest.Adapt<UnitOfMeasurement>();
        unit.UnitName = unitOfMeasurementRequest.Name; // El request expone 'Name' y la entidad 'UnitName', Mapster no los enlaza solo.

        unit.IsActive = true;
        unit.State = true;

        AuditHelper.SetCreated(unit, createdBy);
        respuesta.Data = await _unitsOfMeasurementRepository.CreateUnitOfMeasurement(unit);
        respuesta.ok = true;
    }
    catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respuesta;
}

public async Task<Response<bool>> UpdateUnitOfMeasurement(UnitOfMeasurementRequest unitOfMeasurementRequest, int modifiedBy)
{
    Response<bool> respuesta = new();
    try
    {
        var unit = unitOfMeasurementRequest.Adapt<UnitOfMeasurement>();
        unit.UnitName = unitOfMeasurementRequest.Name;

        AuditHelper.SetModified(unit, modifiedBy);

        var rowsAffected = await _unitsOfMeasurementRepository.UpdateUnitOfMeasurement(unit);
        if (rowsAffected <= 0)
            throw new CustomException("No se pudo modificar la unidad de medida");
        respuesta.Data = respuesta.ok = true;
    }
    catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respuesta;
}

public async Task<Response<bool>> DeleteUnitOfMeasurement(string id, int modifiedBy)
{
    Response<bool> respuesta = new();
    try
    {
        Guid unitId = Guid.Parse(id);

        var rowsAffected = await _unitsOfMeasurementRepository.DeleteUnitOfMeasurement(unitId, modifiedBy);
        if (rowsAffected <= 0)
            throw new CustomException("No se pudo eliminar la unidad de medida");
        respuesta.Data = respuesta.ok = true;
    }
    catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respuesta;
}
}
