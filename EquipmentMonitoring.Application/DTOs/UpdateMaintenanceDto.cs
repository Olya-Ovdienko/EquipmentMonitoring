using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.DTOs;

/// <summary>
/// DTO для оновлення інформації або зміни стану технічного обслуговування.
/// </summary>
public class UpdateMaintenanceDto
{
    /// <summary>
    /// Оновлений опис проведених робіт або приміток.
    /// </summary>
    /// <example>Заміну мастила виконано повністю, підшипники в нормі</example>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Новий статус технічного обслуговування (Scheduled, InProgress, Completed, Cancelled).
    /// </summary>
    /// <example>2</example>
    public MaintenanceStatus Status { get; set; }

    /// <summary>
    /// Фактична дата та час проведення робіт.
    /// </summary>
    /// <example>2026-11-15T12:30:00Z</example>
    public DateTime? PerformedDate { get; set; }
}