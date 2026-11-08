namespace EquipmentMonitoring.Application.DTOs;

public class MeasurementResponseDto
{
    /// <summary>Unique identifier of the measurement.</summary>
    public Guid Id { get; set; }

    /// <summary>Identifier of the equipment associated with this measurement.</summary>
    public Guid EquipmentId { get; set; }

    /// <summary>Text name of the measurement kind, such as Temperature, Voltage, or Load.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Numeric measurement value.</summary>
    public double Value { get; set; }

    /// <summary>Measurement unit: °C for temperature, V for voltage, or % for load.</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Measurement time in UTC.</summary>
    public DateTime Timestamp { get; set; }
}
