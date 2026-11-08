namespace EquipmentMonitoring.Application.DTOs;

public class MeasurementResponseDto
{
    public Guid Id { get; set; }

    public Guid EquipmentId { get; set; }

    public string Type { get; set; } = string.Empty;

    public double Value { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
