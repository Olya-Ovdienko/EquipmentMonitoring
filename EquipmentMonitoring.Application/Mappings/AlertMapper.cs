using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Mappings;

public static class AlertMapper
{
    public static AlertResponseDto ToResponse(Alert alert)
    {
        return new AlertResponseDto
        {
            Id = alert.Id,
            EquipmentId = alert.EquipmentId,
            MeasurementId = alert.MeasurementId,
            Message = alert.Message,
            Severity = alert.Severity.ToString(),
            Timestamp = alert.Timestamp,
            IsAcknowledged = alert.IsAcknowledged
        };
    }
}
