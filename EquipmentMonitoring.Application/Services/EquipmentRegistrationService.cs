using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Mappings;
using EquipmentMonitoring.Application.Validators;
using FluentValidation;
using FluentValidation.Results;

namespace EquipmentMonitoring.Application.Services;

public sealed class EquipmentRegistrationService(
    IValidator<CreateEquipmentDto> validator,
    IEquipmentRepository repository) : IEquipmentRegistrationService
{
    public async Task<EquipmentResponseDto> RegisterAsync(
        CreateEquipmentDto dto,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            if (validationResult.Errors.Any(error =>
                    error.ErrorCode == EquipmentValidationErrorCodes.DuplicateSerialNumber))
            {
                throw new DuplicateEquipmentSerialNumberException();
            }

            throw new ValidationException(validationResult.Errors);
        }

        var equipment = EquipmentMapper.ToDomain(dto);
        await repository.AddAsync(equipment, cancellationToken);

        return EquipmentMapper.ToResponse(equipment);
    }
}
