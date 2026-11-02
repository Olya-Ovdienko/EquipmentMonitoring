namespace EquipmentMonitoring.Domain;

public class Equipment
{
    public Guid Id { get; }
    public string Name { get; }
    public string Type { get; }
    public string SerialNumber { get; }
    public string Location { get; }
    public EquipmentStatus Status { get; }
    public DateTime CreatedAt { get; }

    public Equipment(string name, string type, string serialNumber, string location)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        SerialNumber = serialNumber;
        Location = location;
        CreatedAt = DateTime.UtcNow;
    }
}
