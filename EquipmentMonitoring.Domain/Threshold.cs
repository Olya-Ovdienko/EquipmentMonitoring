namespace EquipmentMonitoring.Domain;

public class Threshold
{
    public Guid Id { get; }
    public Guid EquipmentId { get; }
    public MeasurementType MeasurementType { get; }
    public double? MinValue { get; }
    public double? MaxValue { get; }
    public AlertSeverity Severity { get; }

    public Threshold(
        Guid equipmentId,
        MeasurementType measurementType,
        double? minValue,
        double? maxValue,
        AlertSeverity severity)
    {
        if (equipmentId == Guid.Empty)
        {
            throw new ArgumentException("EquipmentId cannot be empty.", nameof(equipmentId));
        }

        if (!Enum.IsDefined(measurementType))
        {
            throw new ArgumentOutOfRangeException(nameof(measurementType), measurementType, "Unsupported measurement type.");
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unsupported alert severity.");
        }

        if (minValue is null && maxValue is null)
        {
            throw new ArgumentException("At least one threshold limit must be specified.", nameof(minValue));
        }

        if (minValue is double min && (double.IsNaN(min) || double.IsInfinity(min)))
        {
            throw new ArgumentException("MinValue must be a finite number.", nameof(minValue));
        }

        if (maxValue is double max && (double.IsNaN(max) || double.IsInfinity(max)))
        {
            throw new ArgumentException("MaxValue must be a finite number.", nameof(maxValue));
        }

        if (minValue is not null && maxValue is not null && minValue >= maxValue)
        {
            throw new ArgumentException("MinValue must be less than MaxValue.", nameof(minValue));
        }

        Id = Guid.NewGuid();
        EquipmentId = equipmentId;
        MeasurementType = measurementType;
        MinValue = minValue;
        MaxValue = maxValue;
        Severity = severity;
    }
}
