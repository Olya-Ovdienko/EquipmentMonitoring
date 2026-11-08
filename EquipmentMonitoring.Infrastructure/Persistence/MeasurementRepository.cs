using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public sealed class MeasurementRepository(EquipmentDbContext dbContext) : IMeasurementRepository
{
    public async Task AddAsync(Measurement measurement, CancellationToken cancellationToken)
    {
        dbContext.Measurements.Add(measurement);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Measurement>> GetByEquipmentIdAsync(
        Guid equipmentId,
        MeasurementType? type,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        IQueryable<Measurement> query = dbContext.Measurements
            .AsNoTracking()
            .Where(measurement => measurement.EquipmentId == equipmentId);

        if (type.HasValue)
        {
            query = query.Where(measurement => measurement.Type == type.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(measurement => measurement.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(measurement => measurement.Timestamp <= endDate.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
