namespace EquipmentMonitoring.Domain;

public class Maintenance
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? PerformedDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

    public Equipment Equipment { get; set; } = null!;
}