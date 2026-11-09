using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IAlertService
{
    Task<IReadOnlyList<AlertResponseDto>> GetAsync(
        AlertSeverity? severity,
        bool? isAcknowledged,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AlertResponseDto>> GetByEquipmentIdAsync(
        Guid equipmentId,
        AlertSeverity? severity,
        bool? isAcknowledged,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken);

    Task<AlertResponseDto?> AcknowledgeAsync(
        Guid alertId,
        CancellationToken cancellationToken);
}
