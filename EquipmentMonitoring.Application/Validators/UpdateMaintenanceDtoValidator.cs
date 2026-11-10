using EquipmentMonitoring.Application.DTOs;
using FluentValidation;

namespace EquipmentMonitoring.Application.Validators;

public class UpdateMaintenanceDtoValidator : AbstractValidator<UpdateMaintenanceDto>
{
    public UpdateMaintenanceDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Опис є обов'язковим.")
            .MaximumLength(500)
            .WithMessage("Опис не повинен перевищувати 500 символів.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Некоректний статус обслуговування.");

        RuleFor(x => x.PerformedDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.PerformedDate.HasValue)
            .WithMessage("Фактична дата виконання не може бути в майбутньому.");
    }
}