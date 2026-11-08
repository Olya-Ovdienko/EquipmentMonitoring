namespace EquipmentMonitoring.Application.DTOs;

public class UpdateEquipmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
