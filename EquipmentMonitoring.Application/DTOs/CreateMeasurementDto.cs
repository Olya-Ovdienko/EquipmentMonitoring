using System.ComponentModel.DataAnnotations;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.DTOs;

public class CreateMeasurementDto
{
    [Required]
    public Guid? EquipmentId { get; set; }

    [Required]
    public MeasurementType? Type { get; set; }

    public double Value { get; set; }

    public DateTime? Timestamp { get; set; }
}
