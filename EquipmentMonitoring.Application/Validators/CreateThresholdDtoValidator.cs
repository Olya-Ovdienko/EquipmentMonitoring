using EquipmentMonitoring.Application.DTOs;
using FluentValidation;

namespace EquipmentMonitoring.Application.Validators;

public class CreateThresholdDtoValidator : AbstractValidator<CreateThresholdDto>
{
    public CreateThresholdDtoValidator()
    {
        RuleFor(threshold => threshold.EquipmentId)
            .NotEqual(Guid.Empty)
            .WithMessage("EquipmentId must not be empty.");

        RuleFor(threshold => threshold.MeasurementType)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("MeasurementType is required.")
            .Must(type => type.HasValue && Enum.IsDefined(type.Value))
            .WithMessage("MeasurementType is not supported.");

        RuleFor(threshold => threshold.Severity)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Severity is required.")
            .Must(severity => severity.HasValue && Enum.IsDefined(severity.Value))
            .WithMessage("Severity is not supported.");

        RuleFor(threshold => threshold.MinValue)
            .Must((threshold, minValue) => minValue.HasValue || threshold.MaxValue.HasValue)
            .WithMessage("At least one threshold limit is required.");

        RuleFor(threshold => threshold.MinValue)
            .Must(value => !value.HasValue || double.IsFinite(value.Value))
            .WithMessage("MinValue must be a finite number.");

        RuleFor(threshold => threshold.MaxValue)
            .Must(value => !value.HasValue || double.IsFinite(value.Value))
            .WithMessage("MaxValue must be a finite number.");

        RuleFor(threshold => threshold.MinValue)
            .Must((threshold, minValue) =>
                !minValue.HasValue
                || !threshold.MaxValue.HasValue
                || minValue.Value < threshold.MaxValue.Value)
            .WithMessage("MinValue must be less than MaxValue.");
    }
}
