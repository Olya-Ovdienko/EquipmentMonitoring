using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.DTOs;

public class ThresholdResponseDto
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public MeasurementType MeasurementType { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public AlertSeverity Severity { get; set; }
}
