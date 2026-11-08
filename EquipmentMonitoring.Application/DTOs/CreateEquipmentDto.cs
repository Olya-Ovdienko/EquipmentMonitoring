using System.ComponentModel.DataAnnotations;

namespace EquipmentMonitoring.Application.DTOs;

public class CreateEquipmentDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    [Required]
    public string SerialNumber { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
}
