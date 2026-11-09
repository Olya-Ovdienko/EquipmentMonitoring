using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Domain;
using FluentValidation;

namespace EquipmentMonitoring.Application.Validators;

public class CreateMeasurementDtoValidator : AbstractValidator<CreateMeasurementDto>
{
    public CreateMeasurementDtoValidator(IEquipmentRepository equipmentRepository)
    {
        RuleFor(measurement => measurement.EquipmentId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("EquipmentId is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("EquipmentId must not be empty.");

        RuleFor(measurement => measurement.EquipmentId)
            .MustAsync(async (measurement, equipmentId, cancellationToken) =>
                await equipmentRepository.GetByIdAsync(equipmentId!.Value, cancellationToken) is not null)
            .When(measurement => measurement.EquipmentId.HasValue
                && measurement.EquipmentId.Value != Guid.Empty)
            .WithMessage("EquipmentId does not refer to an existing equipment item.")
            .WithErrorCode(MeasurementValidationErrorCodes.EquipmentNotFound);

        RuleFor(measurement => measurement.Type)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Type is required.")
            .Must(type => type.HasValue && Enum.IsDefined(type.Value))
            .WithMessage("Type is not supported.");

        RuleFor(measurement => measurement.Value)
            .Must(double.IsFinite)
            .WithMessage("Value must be a finite number.");

        RuleFor(measurement => measurement.Value)
            .Must((measurement, value) => value >= 0)
            .When(measurement => measurement.Type != MeasurementType.Temperature)
            .WithMessage("Value must not be negative unless the measurement type is temperature.");

        RuleFor(measurement => measurement.Timestamp)
            .Cascade(CascadeMode.Stop)
            .Must(timestamp => !timestamp.HasValue || timestamp.Value.Kind == DateTimeKind.Utc)
            .WithMessage("Timestamp must be in UTC.")
            .Must(timestamp => !timestamp.HasValue || timestamp.Value <= DateTime.UtcNow)
            .WithMessage("Timestamp must not be in the future.");
    }
}
