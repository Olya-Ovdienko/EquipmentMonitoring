namespace EquipmentMonitoring.Application.DTOs;

public class MaintenanceResponseDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? PerformedDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}