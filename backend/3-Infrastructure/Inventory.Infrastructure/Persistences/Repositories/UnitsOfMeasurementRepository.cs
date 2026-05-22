using System;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistences.Interfaces;

namespace Inventory.Infrastructure;

public class UnitsOfMeasurementRepository(InventoryDbContext _DbContext) : IUnitsOfMeasurementRepository
{
    public async Task<string> CreateUnitOfMeasurement(UnitOfMeasurement unit)
{
    using var db = _DbContext.CreateConnection;
    try
    {
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            unit.Id = Guid.NewGuid();
            string sqlQuery = @"
                SELECT CASE WHEN EXISTS(SELECT 1 
                                          FROM unit_of_measurement
                                         WHERE unit_name = @UnitName 
                                           AND state = true)
                            THEN CAST(1 as BIT) 
                            ELSE CAST(0 as BIT) 
                       END ";
            bool existeUnit = await db.QuerySingleAsync<bool>(sqlQuery, new { UnitName = unit.UnitName });

            if (existeUnit)
            {
                throw new CustomException("El nombre de la unidad de medida ya existe, por favor verifique", MessageTypes.Warning);
            }

            sqlQuery = @"
                        INSERT INTO unit_of_measurement
                              (id, unit_name, proportion, precision_rounding, is_large_than_default, is_default, 
                               is_active, state, created_by, created, modified_by, modified)
                       VALUES(@Id, @UnitName, @Proportion, @PrecisionRounding, @IsLargeThanDefault, @IsDefault,
                               @IsActive, @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
                    ";

            var result = await db.ExecuteAsync(sqlQuery, unit);
            transaction.Commit();
        }
        catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex); }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception(ex.Message, ex);
        }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
    finally { db.Close(); }
    return unit.Id.ToString();
}

public async Task<int> UpdateUnitOfMeasurement(UnitOfMeasurement unit)
{
    using var db = _DbContext.CreateConnection;
    int numberRows = 0;
    try
    {
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            string sqlQuery = @"
                        UPDATE unit_of_measurement
                           SET unit_name = @UnitName, 
                               proportion = @Proportion, 
                               precision_rounding = @PrecisionRounding, 
                               is_large_than_default = @IsLargeThanDefault, 
                               is_default = @IsDefault,
                               is_active = @IsActive,
                               modified_by = @ModifiedBy, 
                               modified = @Modified
                         WHERE id = @Id;
                    ";

            numberRows = await db.ExecuteAsync(sqlQuery, unit);
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception(ex.Message, ex);
        }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
    finally { db.Close(); }

    return numberRows;
}

public async Task<int> DeleteUnitOfMeasurement(Guid id, int idUserModified)
{
    using var db = _DbContext.CreateConnection;
    int numberRows = 0;
    try
    {
        DateTime fechaActual = DateTime.Now;
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            string sqlQuery = @"
                        UPDATE unit_of_measurement
                           SET state = false,
                               modified_by = @ModifiedBy, 
                               modified = @Modified
                         WHERE id = @Id;
                    ";
            numberRows = await db.ExecuteAsync(sqlQuery, new { Id = id, ModifiedBy = idUserModified, Modified = fechaActual });
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception(ex.Message, ex);
        }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw new Exception(ex.Message, ex); }
    finally { db.Close(); }

    return numberRows;
}

public async Task<UnitOfMeasurement> GetUnitOfMeasurement(Guid Id)
{
    UnitOfMeasurement unit = new();
    using var db = _DbContext.CreateConnection;
    try
    {
        db.Open();
        string sqlQuery = @"
                    SELECT id, unit_name, proportion, precision_rounding, is_large_than_default,is_default, is_active
                        FROM unit_of_measurement
                        WHERE state
                        AND id = @Id;
                ";
        var result = await db.QueryAsync<UnitOfMeasurement>(sqlQuery, new { Id });
        if (result!.ToList().Count > 0)
        {
            unit = result!.ToList().First();
        }
        else
        {
            throw new CustomException("No existe la Unidad de medida, de acuerdo a los parametros ingresados", Common.Utilities.MessageTypes.Info);
        }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
    catch (Exception ex) { throw new Exception(ex.Message, ex); }
    finally { db.Close(); }
    return unit;
}

public async Task<List<UnitOfMeasurement>> GetUnitsOfMeasurement(string unitOfMeasurementName)
{
    List<UnitOfMeasurement> listUnits = [];
    using var db = _DbContext.CreateConnection;
    try
    {
        unitOfMeasurementName = "%" + unitOfMeasurementName + "%";
        db.Open();

        string sqlQuery = @"
                        SELECT id, unit_name, proportion, precision_rounding, is_large_than_default,is_default, is_active
                        FROM unit_of_measurement
                        WHERE state
                          AND unit_name ILIKE @unitOfMeasurementName;
                ";
        var result = await db.QueryAsync<UnitOfMeasurement>(sqlQuery, new { unitOfMeasurementName });
        listUnits = result!.ToList();
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw new Exception(ex.Message, ex); }
    finally { db.Close(); }

    return listUnits;
}
}
