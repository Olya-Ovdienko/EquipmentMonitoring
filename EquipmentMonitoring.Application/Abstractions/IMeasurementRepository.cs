using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IMeasurementRepository
{
    Task AddAsync(Measurement measurement, CancellationToken cancellationToken);

    Task<IReadOnlyList<Measurement>> GetByEquipmentIdAsync(
        Guid equipmentId,
        MeasurementType? type,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);
}
