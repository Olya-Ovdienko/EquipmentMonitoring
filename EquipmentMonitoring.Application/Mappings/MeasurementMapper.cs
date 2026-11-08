using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Mappings;

public static class MeasurementMapper
{
    public static Measurement ToDomain(CreateMeasurementDto dto)
    {
        return new Measurement(
            dto.EquipmentId!.Value,
            dto.Type!.Value,
            dto.Value,
            dto.Timestamp);
    }

    public static MeasurementResponseDto ToResponse(Measurement measurement)
    {
        return new MeasurementResponseDto
        {
            Id = measurement.Id,
            EquipmentId = measurement.EquipmentId,
            Type = measurement.Type.ToString(),
            Value = measurement.Value,
            Unit = measurement.Unit,
            Timestamp = measurement.Timestamp
        };
    }
}
