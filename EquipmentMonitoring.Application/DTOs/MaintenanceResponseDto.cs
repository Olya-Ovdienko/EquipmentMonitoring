using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.DTOs;

/// <summary>
/// DTO відповіді з повною інформацією про технічне обслуговування.
/// </summary>
public class MaintenanceResponseDto
{
    /// <summary>
    /// Унікальний ідентифікатор запису обслуговування.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Ідентифікатор пов'язаного обладнання.
    /// </summary>
    /// <example>1</example>
    public int EquipmentId { get; set; }

    /// <summary>
    /// Запланована дата проведення обслуговування.
    /// </summary>
    /// <example>2026-11-15T10:00:00Z</example>
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Фактична дата проведення робіт.
    /// </summary>
    /// <example>2026-11-15T12:30:00Z</example>
    public DateTime? PerformedDate { get; set; }

    /// <summary>
    /// Опис робіт.
    /// </summary>
    /// <example>Заміна мастила та планова діагностика підшипників</example>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Поточний статус обслуговування.
    /// </summary>
    /// <example>Scheduled</example>
    public MaintenanceStatus Status { get; set; }
}