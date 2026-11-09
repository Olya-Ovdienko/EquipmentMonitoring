using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public sealed class AlertRepository(EquipmentDbContext dbContext) : IAlertRepository
{
    public async Task AddRangeAsync(
        IReadOnlyCollection<Alert> alerts,
        CancellationToken cancellationToken)
    {
        dbContext.Alerts.AddRange(alerts);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
