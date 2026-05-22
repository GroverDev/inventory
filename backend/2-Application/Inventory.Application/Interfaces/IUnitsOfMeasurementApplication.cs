using System;
using Common.Utilities;
using Inventory.Domain.Entities.Responses;
using Inventory.Domain.Entities.Requests;

namespace Inventory.Application.Interfaces;

public interface IUnitsOfMeasurementApplication
{
    public Task<Response<UnitOfMeasurementResponse>> GetUnitOfMeasurement(string id);
    public Task<Response<List<UnitOfMeasurementResponse>>> GetUnitsOfMeasurement(string unitOfMeasurementName);
    public Task<Response<string>> CreateUnitOfMeasurement(UnitOfMeasurementRequest unitOfMeasurementRequest, int createdBy);
    public Task<Response<bool>> UpdateUnitOfMeasurement(UnitOfMeasurementRequest unitOfMeasurementRequest, int modifiedBy);
    public Task<Response<bool>> DeleteUnitOfMeasurement(string id, int modifiedBy);
}
