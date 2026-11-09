using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IMeasurementAlertService
{
    Task EvaluateAndPersistAsync(
        Measurement measurement,
        CancellationToken cancellationToken);
}
