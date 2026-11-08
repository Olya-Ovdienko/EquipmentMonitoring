using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IMeasurementService
{
    Task<MeasurementResponseDto> CreateAsync(
        CreateMeasurementDto dto,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MeasurementResponseDto>> GetByEquipmentIdAsync(
        Guid equipmentId,
        MeasurementType? type,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);
}
