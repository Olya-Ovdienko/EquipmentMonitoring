namespace EquipmentMonitoring.Application.DTOs;

public class CreateMaintenanceDto
{
    public int EquipmentId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string Description { get; set; } = string.Empty;
}