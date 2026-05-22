using System;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Persistences.Interfaces;

public interface IUnitsOfMeasurementRepository
{
    public Task<UnitOfMeasurement> GetUnitOfMeasurement(Guid Id);
    public Task<List<UnitOfMeasurement>> GetUnitsOfMeasurement(string unitOfMeasurementName);
    public Task<string> CreateUnitOfMeasurement(UnitOfMeasurement unitOfMeasurement);
    public Task<int> UpdateUnitOfMeasurement(UnitOfMeasurement unitOfMeasurement);
    public Task<int> DeleteUnitOfMeasurement(Guid id, int idUserModified);
}
