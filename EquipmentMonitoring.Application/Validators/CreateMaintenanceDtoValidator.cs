using EquipmentMonitoring.Application.DTOs;
using FluentValidation;

namespace EquipmentMonitoring.Application.Validators;

public class CreateMaintenanceDtoValidator : AbstractValidator<CreateMaintenanceDto>
{
    public CreateMaintenanceDtoValidator()
    {
        RuleFor(x => x.EquipmentId)
            .GreaterThan(0)
            .WithMessage("Ідентифікатор обладнання повинен бути більше 0.");

        RuleFor(x => x.ScheduledDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Запланована дата не може бути в минулому.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Опис є обов'язковим.")
            .MaximumLength(500)
            .WithMessage("Опис не повинен перевищувати 500 символів.");
    }
}