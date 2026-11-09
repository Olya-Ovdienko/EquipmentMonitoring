using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public sealed class ThresholdRepository(EquipmentDbContext dbContext) : IThresholdRepository
{
    public async Task<IReadOnlyList<Threshold>> GetByEquipmentAndMeasurementTypeAsync(
        Guid equipmentId,
        MeasurementType measurementType,
        CancellationToken cancellationToken)
    {
        return await dbContext.Thresholds
            .AsNoTracking()
            .Where(threshold => threshold.EquipmentId == equipmentId
                && threshold.MeasurementType == measurementType)
            .ToListAsync(cancellationToken);
    }
}
