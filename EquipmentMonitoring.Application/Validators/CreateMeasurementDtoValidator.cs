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
            .WithMessage("EquipmentId does not refer to an existing equipment item.");

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
            .When(measurement => measurement.Type is MeasurementType.Voltage or MeasurementType.Load)
            .WithMessage("Value must not be negative for voltage or load measurements.");

        RuleFor(measurement => measurement.Timestamp)
            .Must(timestamp => !timestamp.HasValue || timestamp.Value <= DateTime.UtcNow)
            .WithMessage("Timestamp must not be in the future.");
    }
}
