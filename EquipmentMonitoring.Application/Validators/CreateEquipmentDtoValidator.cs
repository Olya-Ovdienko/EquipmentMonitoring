using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using FluentValidation;

namespace EquipmentMonitoring.Application.Validators;

public class CreateEquipmentDtoValidator : AbstractValidator<CreateEquipmentDto>
{
    public CreateEquipmentDtoValidator(IEquipmentSerialNumberChecker serialNumberChecker)
    {
        RuleFor(equipment => equipment.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .MinimumLength(3)
            .WithMessage("Name must be at least 3 characters.")
            .MaximumLength(100)
            .WithMessage("Name must be at most 100 characters.");

        RuleFor(equipment => equipment.Type)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Type is required.")
            .Must(EquipmentTypeRules.IsAllowed)
            .WithMessage("Type is not supported.");

        RuleFor(equipment => equipment.SerialNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("SerialNumber is required.")
            .Must(serialNumber => !string.IsNullOrWhiteSpace(serialNumber))
            .WithMessage("SerialNumber is required.")
            .MustAsync((_, serialNumber, cancellationToken) =>
                serialNumberChecker.IsSerialNumberUniqueAsync(serialNumber, null, cancellationToken))
            .WithMessage("SerialNumber must be unique.")
            .WithErrorCode(EquipmentValidationErrorCodes.DuplicateSerialNumber);

        RuleFor(equipment => equipment.Location)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Location is required.")
            .Must(location => !string.IsNullOrWhiteSpace(location))
            .WithMessage("Location is required.");
    }
}
