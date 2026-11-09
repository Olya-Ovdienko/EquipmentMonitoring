namespace EquipmentMonitoring.Application.DTOs;

public class AlertResponseDto
{
    /// <summary>Unique alert identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Identifier of the equipment that produced the alert.</summary>
    public Guid EquipmentId { get; set; }

    /// <summary>Identifier of the measurement that caused the alert.</summary>
    public Guid MeasurementId { get; set; }

    /// <summary>Human-readable alert message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Alert severity, such as Warning or Critical.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the measurement that caused the alert.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Indicates whether the alert has been acknowledged.</summary>
    public bool IsAcknowledged { get; set; }
}
