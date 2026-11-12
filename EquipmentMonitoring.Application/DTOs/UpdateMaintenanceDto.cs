using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.DTOs;

/// <summary>
/// DTO for updating maintenance information or changing its status.
/// </summary>
public class UpdateMaintenanceDto
{
    /// <summary>
    /// Updated description of performed work or notes.
    /// </summary>
    /// <example>Oil change completed, bearings in good condition</example>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// New status of maintenance (Scheduled, InProgress, Completed, Cancelled).
    /// </summary>
    /// <example>2</example>
    public MaintenanceStatus Status { get; set; }

    /// <summary>
    /// Actual date and time when work was performed.
    /// </summary>
    /// <example>2026-11-15T12:30:00Z</example>
    public DateTime? PerformedDate { get; set; }
}