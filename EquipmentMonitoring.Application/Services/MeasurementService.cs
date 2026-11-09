using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Mappings;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using FluentValidation;
using FluentValidation.Results;

namespace EquipmentMonitoring.Application.Services;

public sealed class MeasurementService(
    IValidator<CreateMeasurementDto> validator,
    IEquipmentRepository equipmentRepository,
    IMeasurementRepository measurementRepository,
    IMeasurementAlertService measurementAlertService) : IMeasurementService
{
    public async Task<MeasurementResponseDto> CreateAsync(
        CreateMeasurementDto dto,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var measurement = MeasurementMapper.ToDomain(dto);
        await measurementRepository.AddAsync(measurement, cancellationToken);
        await measurementAlertService.EvaluateAndPersistAsync(measurement, cancellationToken);

        return MeasurementMapper.ToResponse(measurement);
    }

    public async Task<IReadOnlyList<MeasurementResponseDto>> GetByEquipmentIdAsync(
        Guid equipmentId,
        MeasurementType? type,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var equipment = await equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(CreateMeasurementDto.EquipmentId), "Equipment not found.")
                {
                    ErrorCode = MeasurementValidationErrorCodes.EquipmentNotFound
                }
            ]);
        }

        var measurements = await measurementRepository.GetByEquipmentIdAsync(
            equipmentId,
            type,
            startDate,
            endDate,
            cancellationToken);

        return measurements.Select(MeasurementMapper.ToResponse).ToArray();
    }
}
