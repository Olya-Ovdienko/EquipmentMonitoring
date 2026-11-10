using System.ComponentModel.DataAnnotations;

namespace EquipmentMonitoring.Application.DTOs;

/// <summary>
/// DTO для створення нового запису технічного обслуговування.
/// </summary>
public class CreateMaintenanceDto
{
    /// <summary>
    /// Ідентифікатор обладнання, для якого планується обслуговування.
    /// </summary>
    /// <example>1</example>
    [Required]
    public int EquipmentId { get; set; }

    /// <summary>
    /// Запланована дата проведення технічного обслуговування (має бути в майбутньому).
    /// </summary>
    /// <example>2026-11-15T10:00:00Z</example>
    [Required]
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Детальний опис запланованих робіт або причини обслуговування.
    /// </summary>
    /// <example>Заміна мастила та планова діагностика підшипників</example>
    [Required]
    public string Description { get; set; } = string.Empty;
}