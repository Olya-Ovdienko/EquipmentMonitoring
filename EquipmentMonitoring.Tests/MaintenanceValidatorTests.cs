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
    public void CreateMaintenanceDto_ValidData_PassesValidation()
    {
        var dto = new CreateMaintenanceDto
        {
            EquipmentId = 1,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = "Планове технічне обслуговування"
        };

        var result = _createValidator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateMaintenanceDto_PastDate_FailsValidation()
    {
        var dto = new CreateMaintenanceDto
        {
            EquipmentId = 1,
            ScheduledDate = DateTime.UtcNow.AddDays(-2),
            Description = "Тест"
        };

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMaintenanceDto.ScheduledDate));
    }

    [Fact]
    public void CreateMaintenanceDto_InvalidEquipmentId_FailsValidation()
    {
        var dto = new CreateMaintenanceDto
        {
            EquipmentId = 0,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = "Тест"
        };

        var result = _createValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMaintenanceDto.EquipmentId));
    }

    [Fact]
    public void UpdateMaintenanceDto_FuturePerformedDate_FailsValidation()
    {
        var dto = new UpdateMaintenanceDto
        {
            Description = "Оновлення опису",
            Status = MaintenanceStatus.Completed,
            PerformedDate = DateTime.UtcNow.AddDays(5)
        };

        var result = _updateValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMaintenanceDto.PerformedDate));
    }
}