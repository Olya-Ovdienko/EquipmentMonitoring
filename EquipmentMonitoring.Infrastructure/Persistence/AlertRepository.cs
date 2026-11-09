using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public sealed class AlertRepository(EquipmentDbContext dbContext) : IAlertRepository
{
    public async Task<IReadOnlyList<Alert>> GetAsync(
        Guid? equipmentId,
        AlertSeverity? severity,
        bool? isAcknowledged,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        IQueryable<Alert> query = dbContext.Alerts
            .AsNoTracking();

        if (equipmentId.HasValue)
        {
            query = query.Where(alert => alert.EquipmentId == equipmentId.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(alert => alert.Severity == severity.Value);
        }

        if (isAcknowledged.HasValue)
        {
            query = query.Where(alert => alert.IsAcknowledged == isAcknowledged.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(alert => alert.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(alert => alert.Timestamp <= toDate.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Alerts.SingleOrDefaultAsync(alert => alert.Id == id, cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<Alert> alerts,
        CancellationToken cancellationToken)
    {
        dbContext.Alerts.AddRange(alerts);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
