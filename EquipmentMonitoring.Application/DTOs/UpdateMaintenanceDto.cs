using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.DTOs;

public class UpdateMaintenanceDto
{
    public DateTime? PerformedDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public MaintenanceStatus Status { get; set; }
}