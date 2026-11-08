using EquipmentMonitoring.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public class EquipmentSerialNumberChecker : IEquipmentSerialNumberChecker
{
    private readonly EquipmentDbContext _dbContext;

    public EquipmentSerialNumberChecker(EquipmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsSerialNumberUniqueAsync(
        string serialNumber,
        Guid? excludedEquipmentId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Equipments
            .Where(equipment => equipment.SerialNumber == serialNumber);

        if (excludedEquipmentId.HasValue)
        {
            query = query.Where(equipment => equipment.Id != excludedEquipmentId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }
}
