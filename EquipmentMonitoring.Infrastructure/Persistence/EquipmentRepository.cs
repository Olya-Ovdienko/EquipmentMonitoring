using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public sealed class EquipmentRepository(EquipmentDbContext dbContext) : IEquipmentRepository
{
    public async Task AddAsync(Equipment equipment, CancellationToken cancellationToken)
    {
        dbContext.Equipments.Add(equipment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsSerialNumberUniqueConstraintViolation(exception))
        {
            throw new DuplicateEquipmentSerialNumberException(exception);
        }
    }

    public Task<Equipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Equipments
            .AsNoTracking()
            .SingleOrDefaultAsync(equipment => equipment.Id == id, cancellationToken);
    }

    private static bool IsSerialNumberUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException
        {
            SqliteExtendedErrorCode: 2067
        } sqliteException
        && sqliteException.Message.Contains(
            "UNIQUE constraint failed: Equipments.SerialNumber",
            StringComparison.Ordinal);
    }
}
