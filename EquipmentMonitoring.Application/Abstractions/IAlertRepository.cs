using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IAlertRepository
{
    Task AddRangeAsync(
        IReadOnlyCollection<Alert> alerts,
        CancellationToken cancellationToken);
}
