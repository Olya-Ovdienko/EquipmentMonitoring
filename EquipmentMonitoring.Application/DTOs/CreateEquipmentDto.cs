using System.ComponentModel.DataAnnotations;

namespace EquipmentMonitoring.Application.DTOs;

/// <summary>Contains the values required to register a new equipment item.</summary>
/// <remarks>Validation rules are enforced by <c>CreateEquipmentDtoValidator</c>.</remarks>
public class CreateEquipmentDto
{
    /// <summary>Gets or sets the equipment name; required and 3–100 characters long.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the equipment type; required and one of Датчик, Контролер, or Сервер.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the serial number; required and unique across registered equipment.</summary>
    [Required]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the equipment location; required.</summary>
    public string Location { get; set; } = string.Empty;
}
