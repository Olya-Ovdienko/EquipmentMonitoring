using EquipmentMonitoring.Application.DTOs;
using FluentValidation;

namespace EquipmentMonitoring.Application.Validators;

public class CreateMaintenanceDtoValidator : AbstractValidator<CreateMaintenanceDto>
{
    public CreateMaintenanceDtoValidator()
    {
        RuleFor(x => x.EquipmentId)
            .NotEmpty()
            .WithMessage("EquipmentId is required.");

        RuleFor(x => x.ScheduledDate)
            .NotEmpty()
            .WithMessage("ScheduledDate is required.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.");
    }
}