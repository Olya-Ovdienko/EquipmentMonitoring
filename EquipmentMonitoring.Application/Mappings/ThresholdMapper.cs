using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Mappings;

public static class ThresholdMapper
{
    public static Threshold ToDomain(CreateThresholdDto dto)
    {
        return new Threshold(
            dto.EquipmentId,
            dto.MeasurementType!.Value,
            dto.MinValue,
            dto.MaxValue,
            dto.Severity!.Value);
    }

    public static ThresholdResponseDto ToResponse(Threshold threshold)
    {
        return new ThresholdResponseDto
        {
            Id = threshold.Id,
            EquipmentId = threshold.EquipmentId,
            MeasurementType = threshold.MeasurementType,
            MinValue = threshold.MinValue,
            MaxValue = threshold.MaxValue,
            Severity = threshold.Severity
        };
    }
}
