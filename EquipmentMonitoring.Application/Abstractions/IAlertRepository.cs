using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IAlertRepository
{
    Task<IReadOnlyList<Alert>> GetAsync(
        Guid? equipmentId,
        AlertSeverity? severity,
        bool? isAcknowledged,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken);

    Task<Alert?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddRangeAsync(
        IReadOnlyCollection<Alert> alerts,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
