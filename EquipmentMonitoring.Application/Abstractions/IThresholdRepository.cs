using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IThresholdRepository
{
    Task<IReadOnlyList<Threshold>> GetByEquipmentAndMeasurementTypeAsync(
        Guid equipmentId,
        MeasurementType measurementType,
        CancellationToken cancellationToken);
}
