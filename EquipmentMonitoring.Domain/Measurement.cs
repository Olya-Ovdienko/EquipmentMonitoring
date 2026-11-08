namespace EquipmentMonitoring.Domain;

public class Measurement
{
    public Guid Id { get; }
    public Guid EquipmentId { get; }
    public MeasurementType Type { get; }
    public double Value { get; }
    public string Unit { get; }
    public DateTime Timestamp { get; }

    public Measurement(Guid equipmentId, MeasurementType type, double value)
    {
        Id = Guid.NewGuid();
        EquipmentId = equipmentId;
        Type = type;
        Value = value;
        Unit = type switch
        {
            MeasurementType.Temperature => "°C",
            MeasurementType.Voltage => "V",
            MeasurementType.Load => "%",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported measurement type.")
        };
        Timestamp = DateTime.UtcNow;
    }
}
