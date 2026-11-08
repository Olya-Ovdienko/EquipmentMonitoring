using System.ComponentModel.DataAnnotations;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.DTOs;

public class CreateMeasurementDto
{
    /// <summary>Identifier of the registered equipment to which the measurement belongs.</summary>
    /// <remarks>This field is required and must identify existing equipment.</remarks>
    [Required]
    public Guid? EquipmentId { get; set; }

    /// <summary>Required measurement kind: Temperature (0), Voltage (1), or Load (2).</summary>
    /// <remarks>The API serializes this enum as a numeric JSON value.</remarks>
    [Required]
    public MeasurementType? Type { get; set; }

    /// <summary>Finite measurement value. Negative temperature is allowed; negative voltage and load are not.</summary>
    public double Value { get; set; }

    /// <summary>Optional UTC measurement time. It cannot be in the future; when omitted, current UTC time is used.</summary>
    public DateTime? Timestamp { get; set; }
}
