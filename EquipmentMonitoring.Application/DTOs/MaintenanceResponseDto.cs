using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.DTOs;

/// <summary>
/// DTO response containing full maintenance details.
/// </summary>
public class MaintenanceResponseDto
{
    /// <summary>
    /// Unique identifier of the maintenance record.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the associated equipment.
    /// </summary>
    /// <example>1</example>
    public int EquipmentId { get; set; }

    /// <summary>
    /// Scheduled date for maintenance.
    /// </summary>
    /// <example>2026-11-15T10:00:00Z</example>
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Actual date when work was performed (null if not completed).
    /// </summary>
    /// <example>2026-11-15T12:30:00Z</example>
    public DateTime? PerformedDate { get; set; }

    /// <summary>
    /// Work description.
    /// </summary>
    /// <example>Oil change and scheduled bearing diagnostics</example>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current maintenance status.
    /// </summary>
    /// <example>Scheduled</example>
    public MaintenanceStatus Status { get; set; }
}