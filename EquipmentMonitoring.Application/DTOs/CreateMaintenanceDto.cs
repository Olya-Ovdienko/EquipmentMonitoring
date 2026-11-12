using System.ComponentModel.DataAnnotations;

namespace EquipmentMonitoring.Application.DTOs;

/// <summary>
/// DTO for creating a new maintenance record.
/// </summary>
public class CreateMaintenanceDto
{
    /// <summary>
    /// Identifier of the equipment scheduled for maintenance.
    /// </summary>
    /// <example>1</example>
    [Required]
    public int EquipmentId { get; set; }

    /// <summary>
    /// Scheduled date for maintenance.
    /// </summary>
    /// <example>2026-11-15T10:00:00Z</example>
    [Required]
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Detailed description of scheduled maintenance work.
    /// </summary>
    /// <example>Oil change and scheduled bearing diagnostics</example>
    [Required]
    public string Description { get; set; } = string.Empty;
}