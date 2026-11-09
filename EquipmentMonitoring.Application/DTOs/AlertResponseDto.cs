namespace EquipmentMonitoring.Application.DTOs;

public class AlertResponseDto
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid MeasurementId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsAcknowledged { get; set; }
}
