namespace EquipmentMonitoring.Domain;

public class Alert
{
    public Guid Id { get; }
    public Guid EquipmentId { get; }
    public Guid MeasurementId { get; }
    public string Message { get; }
    public AlertSeverity Severity { get; }
    public DateTime Timestamp { get; }
    public bool IsAcknowledged { get; private set; }

    public Alert(
        Guid equipmentId,
        Guid measurementId,
        string message,
        AlertSeverity severity,
        DateTime timestamp)
    {
        if (equipmentId == Guid.Empty)
        {
            throw new ArgumentException("EquipmentId cannot be empty.", nameof(equipmentId));
        }

        if (measurementId == Guid.Empty)
        {
            throw new ArgumentException("MeasurementId cannot be empty.", nameof(measurementId));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be null, empty, or whitespace.", nameof(message));
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unsupported alert severity.");
        }

        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be in UTC.", nameof(timestamp));
        }

        Id = Guid.NewGuid();
        EquipmentId = equipmentId;
        MeasurementId = measurementId;
        Message = message;
        Severity = severity;
        Timestamp = timestamp;
        IsAcknowledged = false;
    }

    public void Acknowledge()
    {
        IsAcknowledged = true;
    }
}
