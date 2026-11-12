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
        // Arrange
        var dto = new CreateMaintenanceDto
        {
            EquipmentId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = "Планове технічне обслуговування"
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateMaintenanceDto_PastDate_FailsValidation()
    {
        // Arrange
        var dto = new CreateMaintenanceDto
        {
            EquipmentId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(-2),
            Description = "Тест"
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMaintenanceDto.ScheduledDate));
    }

    [Fact]
    public void CreateMaintenanceDto_InvalidEquipmentId_FailsValidation()
    {
        // Arrange
        var dto = new CreateMaintenanceDto
        {
            EquipmentId = Guid.Empty,
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = "Тест"
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMaintenanceDto.EquipmentId));
    }

    [Fact]
    public void CreateMaintenanceDto_EmptyDescription_FailsValidation()
    {
        // Arrange
        var dto = new CreateMaintenanceDto
        {
            EquipmentId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Description = string.Empty
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMaintenanceDto.Description));
    }

    [Fact]
    public void UpdateMaintenanceDto_FuturePerformedDate_FailsValidation()
    {
        // Arrange
        var dto = new UpdateMaintenanceDto
        {
            Description = "Оновлення опису",
            Status = MaintenanceStatus.Completed,
            PerformedDate = DateTime.UtcNow.AddDays(5)
        };

        // Act
        var result = _updateValidator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMaintenanceDto.PerformedDate));
    }
}