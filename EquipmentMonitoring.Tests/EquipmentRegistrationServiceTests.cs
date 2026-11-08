using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using FluentValidation;
using Moq;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class EquipmentRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithValidDto_ReturnsMappedResponseWithGeneratedId()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = CreateUniqueSerialNumberChecker();
        var service = CreateService(repository, serialNumberChecker);
        var dto = CreateValidDto();
        repository.Setup(item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.RegisterAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Type, result.Type);
        Assert.Equal(dto.SerialNumber, result.SerialNumber);
        Assert.Equal(dto.Location, result.Location);
        Assert.Equal(nameof(EquipmentStatus.Available), result.Status);
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public async Task RegisterAsync_WithValidDto_CallsRepositoryAddOnce()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = CreateUniqueSerialNumberChecker();
        var service = CreateService(repository, serialNumberChecker);
        repository.Setup(item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.RegisterAsync(CreateValidDto(), TestContext.Current.CancellationToken);

        // Assert
        repository.Verify(
            item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyName_ThrowsValidationExceptionAndDoesNotSave()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = CreateUniqueSerialNumberChecker();
        var service = CreateService(repository, serialNumberChecker);
        var dto = CreateValidDto();
        dto.Name = string.Empty;

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.RegisterAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateEquipmentDto.Name));
        repository.Verify(
            item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithEmptySerialNumber_ThrowsValidationExceptionAndDoesNotSave()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = CreateUniqueSerialNumberChecker();
        var service = CreateService(repository, serialNumberChecker);
        var dto = CreateValidDto();
        dto.SerialNumber = string.Empty;

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.RegisterAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateEquipmentDto.SerialNumber));
        repository.Verify(
            item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithNameLongerThan100Characters_ThrowsValidationException()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = CreateUniqueSerialNumberChecker();
        var service = CreateService(repository, serialNumberChecker);
        var dto = CreateValidDto();
        dto.Name = new string('N', 101);

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.RegisterAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateEquipmentDto.Name));
        repository.Verify(
            item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithNameShorterThan3Characters_ThrowsValidationException()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = CreateUniqueSerialNumberChecker();
        var service = CreateService(repository, serialNumberChecker);
        var dto = CreateValidDto();
        dto.Name = "AB";

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.RegisterAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateEquipmentDto.Name));
        repository.Verify(
            item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithUnsupportedType_ThrowsValidationException()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = CreateUniqueSerialNumberChecker();
        var service = CreateService(repository, serialNumberChecker);
        var dto = CreateValidDto();
        dto.Type = "Unknown type";

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.RegisterAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateEquipmentDto.Type));
        repository.Verify(
            item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateSerialNumber_ThrowsConflictAndDoesNotSave()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = new Mock<IEquipmentSerialNumberChecker>();
        serialNumberChecker
            .Setup(checker => checker.IsSerialNumberUniqueAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(repository, serialNumberChecker);
        var dto = CreateValidDto();

        // Act
        await Assert.ThrowsAsync<DuplicateEquipmentSerialNumberException>(
            () => service.RegisterAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        repository.Verify(
            item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithValidSerialNumber_ChecksUniquenessWithExpectedParameters()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = CreateUniqueSerialNumberChecker();
        var service = CreateService(repository, serialNumberChecker);
        var dto = CreateValidDto();
        repository.Setup(item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.RegisterAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        serialNumberChecker.Verify(
            checker => checker.IsSerialNumberUniqueAsync(
                dto.SerialNumber,
                null,
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ForwardsCancellationTokenToDependencies()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var repository = new Mock<IEquipmentRepository>();
        var serialNumberChecker = CreateUniqueSerialNumberChecker();
        var service = CreateService(repository, serialNumberChecker);
        repository.Setup(item => item.AddAsync(It.IsAny<Equipment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.RegisterAsync(CreateValidDto(), cancellationToken);

        // Assert
        serialNumberChecker.Verify(
            checker => checker.IsSerialNumberUniqueAsync(
                It.IsAny<string>(),
                null,
                cancellationToken),
            Times.Once);
        repository.Verify(
            item => item.AddAsync(It.IsAny<Equipment>(), cancellationToken),
            Times.Once);
    }

    private static EquipmentRegistrationService CreateService(
        Mock<IEquipmentRepository> repository,
        Mock<IEquipmentSerialNumberChecker> serialNumberChecker)
    {
        var validator = new CreateEquipmentDtoValidator(serialNumberChecker.Object);
        return new EquipmentRegistrationService(validator, repository.Object);
    }

    private static Mock<IEquipmentSerialNumberChecker> CreateUniqueSerialNumberChecker()
    {
        var checker = new Mock<IEquipmentSerialNumberChecker>();
        checker
            .Setup(item => item.IsSerialNumberUniqueAsync(
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return checker;
    }

    private static CreateEquipmentDto CreateValidDto() => new()
    {
        Name = "Test Sensor",
        Type = "Датчик",
        SerialNumber = "SN-UNIT-001",
        Location = "Lab 1"
    };
}
