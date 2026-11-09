using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Mappings;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using FluentValidation;
using FluentValidation.Results;

namespace EquipmentMonitoring.Application.Services;

public sealed class AlertService(
    IAlertRepository alertRepository,
    IEquipmentRepository equipmentRepository) : IAlertService
{
    public async Task<IReadOnlyList<AlertResponseDto>> GetAsync(
        AlertSeverity? severity,
        bool? isAcknowledged,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        ValidateFilters(severity, fromDate, toDate);

        var alerts = await alertRepository.GetAsync(
            equipmentId: null,
            severity,
            isAcknowledged,
            fromDate,
            toDate,
            cancellationToken);

        return alerts.Select(AlertMapper.ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<AlertResponseDto>> GetByEquipmentIdAsync(
        Guid equipmentId,
        AlertSeverity? severity,
        bool? isAcknowledged,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        ValidateFilters(severity, fromDate, toDate);

        var equipment = await equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(AlertResponseDto.EquipmentId), "Equipment not found.")
                {
                    ErrorCode = MeasurementValidationErrorCodes.EquipmentNotFound
                }
            ]);
        }

        var alerts = await alertRepository.GetAsync(
            equipmentId,
            severity,
            isAcknowledged,
            fromDate,
            toDate,
            cancellationToken);

        return alerts.Select(AlertMapper.ToResponse).ToArray();
    }

    public async Task<AlertResponseDto?> AcknowledgeAsync(
        Guid alertId,
        CancellationToken cancellationToken)
    {
        var alert = await alertRepository.GetByIdAsync(alertId, cancellationToken);
        if (alert is null)
        {
            return null;
        }

        alert.Acknowledge();
        await alertRepository.SaveChangesAsync(cancellationToken);

        return AlertMapper.ToResponse(alert);
    }

    private static void ValidateFilters(
        AlertSeverity? severity,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var failures = new List<ValidationFailure>();

        if (severity.HasValue && !Enum.IsDefined(severity.Value))
        {
            failures.Add(new ValidationFailure(nameof(severity), "Severity is not supported."));
        }

        if (fromDate.HasValue && fromDate.Value.Kind != DateTimeKind.Utc)
        {
            failures.Add(new ValidationFailure(nameof(fromDate), "FromDate must be in UTC."));
        }

        if (toDate.HasValue && toDate.Value.Kind != DateTimeKind.Utc)
        {
            failures.Add(new ValidationFailure(nameof(toDate), "ToDate must be in UTC."));
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            failures.Add(new ValidationFailure(nameof(toDate), "FromDate must not be later than ToDate."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}
