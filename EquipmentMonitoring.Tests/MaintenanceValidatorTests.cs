using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MaintenanceValidatorTests
{
    private readonly CreateMaintenanceDtoValidator _createValidator = new();
    private readonly UpdateMaintenanceDtoValidator _updateValidator = new();

    [Fact]
    public void CreateValidator_EmptyEquipmentId_ShouldHaveValidationError()
    {
        var dto = new CreateMaintenanceDto
        {
            EquipmentId = Guid.Empty,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = "Test"
        };

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMaintenanceDto.EquipmentId));
    }

    [Fact]
    public void CreateValidator_ValidDto_ShouldPassValidation()
    {
        var dto = new CreateMaintenanceDto
        {
            EquipmentId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = "Valid description"
        };

        var result = _createValidator.Validate(dto);

        Assert.True(result.IsValid);
    }
}