using System.Globalization;
using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Services;

public sealed class MeasurementAlertService(
    IThresholdRepository thresholdRepository,
    IAlertRepository alertRepository) : IMeasurementAlertService
{
    public async Task EvaluateAndPersistAsync(
        Measurement measurement,
        CancellationToken cancellationToken)
    {
        var thresholds = await thresholdRepository.GetByEquipmentAndMeasurementTypeAsync(
            measurement.EquipmentId,
            measurement.Type,
            cancellationToken);

        var alerts = new List<Alert>();
        foreach (var threshold in thresholds)
        {
            if (threshold.MinValue.HasValue && measurement.Value < threshold.MinValue.Value)
            {
                alerts.Add(new Alert(
                    measurement.EquipmentId,
                    measurement.Id,
                    CreateBelowMinimumMessage(measurement, threshold.MinValue.Value),
                    threshold.Severity,
                    measurement.Timestamp));
            }
            else if (threshold.MaxValue.HasValue && measurement.Value > threshold.MaxValue.Value)
            {
                alerts.Add(new Alert(
                    measurement.EquipmentId,
                    measurement.Id,
                    CreateAboveMaximumMessage(measurement, threshold.MaxValue.Value),
                    threshold.Severity,
                    measurement.Timestamp));
            }
        }

        if (alerts.Count > 0)
        {
            await alertRepository.AddRangeAsync(alerts, cancellationToken);
        }
    }

    private static string CreateBelowMinimumMessage(Measurement measurement, double minimumValue)
    {
        return $"{measurement.Type} value {measurement.Value.ToString(CultureInfo.InvariantCulture)} "
            + $"is below minimum threshold {minimumValue.ToString(CultureInfo.InvariantCulture)}.";
    }

    private static string CreateAboveMaximumMessage(Measurement measurement, double maximumValue)
    {
        return $"{measurement.Type} value {measurement.Value.ToString(CultureInfo.InvariantCulture)} "
            + $"is above maximum threshold {maximumValue.ToString(CultureInfo.InvariantCulture)}.";
    }
}
