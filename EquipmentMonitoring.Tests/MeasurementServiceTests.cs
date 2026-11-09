using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using FluentValidation;
using Moq;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MeasurementServiceTests
{
    private static readonly DateTime Timestamp = new(2026, 4, 18, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsMappedResponse()
    {
        // Arrange
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);
        var dto = CreateValidDto(equipment.Id);
        Measurement? savedMeasurement = null;
        measurementRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()))
            .Callback<Measurement, CancellationToken>((measurement, _) => savedMeasurement = measurement)
            .Returns(Task.CompletedTask);

        // Act
        var response = await service.CreateAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(savedMeasurement);
        Assert.Equal(savedMeasurement.Id, response.Id);
        Assert.Equal(equipment.Id, response.EquipmentId);
        Assert.Equal(nameof(MeasurementType.Voltage), response.Type);
        Assert.Equal(dto.Value, response.Value);
        Assert.Equal("V", response.Unit);
        Assert.Equal(Timestamp, response.Timestamp);
        Assert.Equal(DateTimeKind.Utc, response.Timestamp.Kind);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CallsMeasurementRepositoryOnceWithMappedMeasurement()
    {
        // Arrange
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);
        var dto = CreateValidDto(equipment.Id);
        Measurement? savedMeasurement = null;
        measurementRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()))
            .Callback<Measurement, CancellationToken>((measurement, _) => savedMeasurement = measurement)
            .Returns(Task.CompletedTask);

        // Act
        await service.CreateAsync(dto, TestContext.Current.CancellationToken);

        // Assert
        measurementRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(savedMeasurement);
        Assert.Equal(equipment.Id, savedMeasurement.EquipmentId);
        Assert.Equal(MeasurementType.Voltage, savedMeasurement.Type);
        Assert.Equal(dto.Value, savedMeasurement.Value);
        Assert.Equal("V", savedMeasurement.Unit);
        Assert.Equal(Timestamp, savedMeasurement.Timestamp);
    }

    [Fact]
    public async Task CreateAsync_ForwardsCancellationTokenToValidationAndSave()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        measurementRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);

        // Act
        await service.CreateAsync(CreateValidDto(equipment.Id), cancellationToken);

        // Assert
        equipmentRepository.Verify(
            repository => repository.GetByIdAsync(equipment.Id, cancellationToken),
            Times.Once);
        measurementRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Measurement>(), cancellationToken),
            Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateAsync_WithMissingOrEmptyEquipmentId_ThrowsValidationExceptionAndDoesNotSave(
        bool useEmptyId)
    {
        // Arrange
        var equipmentRepository = CreateEquipmentRepository(null);
        var measurementRepository = CreateMeasurementRepository();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);
        var dto = CreateValidDto(Guid.NewGuid());
        dto.EquipmentId = useEmptyId ? Guid.Empty : null;

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateMeasurementDto.EquipmentId));
        measurementRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
        equipmentRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenEquipmentDoesNotExist_ReportsEquipmentNotFoundAndDoesNotSave()
    {
        // Arrange
        var equipmentRepository = CreateEquipmentRepository(null);
        var measurementRepository = CreateMeasurementRepository();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);
        var dto = CreateValidDto(Guid.NewGuid());

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error =>
            error.PropertyName == nameof(CreateMeasurementDto.EquipmentId)
            && error.ErrorCode == MeasurementValidationErrorCodes.EquipmentNotFound);
        measurementRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithUnsupportedType_ThrowsValidationExceptionAndDoesNotSave()
    {
        // Arrange
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);
        var dto = CreateValidDto(equipment.Id);
        dto.Type = (MeasurementType)999;

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateMeasurementDto.Type));
        measurementRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(MeasurementType.Voltage)]
    [InlineData(MeasurementType.Load)]
    public async Task CreateAsync_WithNegativeValueForVoltageOrLoad_ThrowsAndDoesNotSave(MeasurementType type)
    {
        // Arrange
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);
        var dto = CreateValidDto(equipment.Id);
        dto.Type = type;
        dto.Value = -0.5;

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateMeasurementDto.Value));
        measurementRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithFutureTimestamp_ThrowsValidationExceptionAndDoesNotSave()
    {
        // Arrange
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);
        var dto = CreateValidDto(equipment.Id);
        dto.Timestamp = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateMeasurementDto.Timestamp));
        measurementRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_ForExistingEquipment_ReturnsMappedHistory()
    {
        // Arrange
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        IReadOnlyList<Measurement> measurements = new List<Measurement>
        {
            new Measurement(equipment.Id, MeasurementType.Temperature, -4.25, Timestamp),
            new Measurement(equipment.Id, MeasurementType.Load, 42.5, Timestamp.AddMinutes(1))
        };
        measurementRepository
            .Setup(repository => repository.GetByEquipmentIdAsync(
                equipment.Id,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(measurements);
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);

        // Act
        var result = await service.GetByEquipmentIdAsync(
            equipment.Id,
            null,
            null,
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(measurements[0].Id, result[0].Id);
        Assert.Equal(equipment.Id, result[0].EquipmentId);
        Assert.Equal(nameof(MeasurementType.Temperature), result[0].Type);
        Assert.Equal(-4.25, result[0].Value);
        Assert.Equal("°C", result[0].Unit);
        Assert.Equal(Timestamp, result[0].Timestamp);
        Assert.Equal(measurements[1].Id, result[1].Id);
        Assert.Equal(nameof(MeasurementType.Load), result[1].Type);
        Assert.Equal(42.5, result[1].Value);
        Assert.Equal("%", result[1].Unit);
        Assert.Equal(Timestamp.AddMinutes(1), result[1].Timestamp);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_WhenHistoryIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        measurementRepository
            .Setup(repository => repository.GetByEquipmentIdAsync(
                equipment.Id,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Measurement>());
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);

        // Act
        var result = await service.GetByEquipmentIdAsync(
            equipment.Id,
            null,
            null,
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_WhenEquipmentDoesNotExist_ThrowsAndDoesNotQueryMeasurements()
    {
        // Arrange
        var equipmentRepository = CreateEquipmentRepository(null);
        var measurementRepository = CreateMeasurementRepository();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);
        var equipmentId = Guid.NewGuid();

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.GetByEquipmentIdAsync(
                equipmentId,
                null,
                null,
                null,
                TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(exception.Errors, error =>
            error.PropertyName == nameof(CreateMeasurementDto.EquipmentId)
            && error.ErrorCode == MeasurementValidationErrorCodes.EquipmentNotFound);
        measurementRepository.Verify(
            repository => repository.GetByEquipmentIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<MeasurementType?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_ForwardsEquipmentAndFiltersAndCancellationToken()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var type = MeasurementType.Voltage;
        var startDate = Timestamp.AddDays(-2);
        var endDate = Timestamp.AddDays(2);
        measurementRepository
            .Setup(repository => repository.GetByEquipmentIdAsync(
                equipment.Id,
                type,
                startDate,
                endDate,
                cancellationToken))
            .ReturnsAsync(Array.Empty<Measurement>());
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object);

        // Act
        var result = await service.GetByEquipmentIdAsync(
            equipment.Id,
            type,
            startDate,
            endDate,
            cancellationToken);

        // Assert
        Assert.Empty(result);
        equipmentRepository.Verify(
            repository => repository.GetByIdAsync(equipment.Id, cancellationToken),
            Times.Once);
        measurementRepository.Verify(
            repository => repository.GetByEquipmentIdAsync(
                equipment.Id,
                type,
                startDate,
                endDate,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SavesMeasurementBeforeEvaluatingAlerts()
    {
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var alertService = new Mock<IMeasurementAlertService>();
        var calls = new List<string>();
        measurementRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()))
            .Callback<Measurement, CancellationToken>((_, _) => calls.Add("measurement"))
            .Returns(Task.CompletedTask);
        alertService
            .Setup(service => service.EvaluateAndPersistAsync(
                It.IsAny<Measurement>(),
                It.IsAny<CancellationToken>()))
            .Callback<Measurement, CancellationToken>((_, _) => calls.Add("alerts"))
            .Returns(Task.CompletedTask);
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object, alertService.Object);

        await service.CreateAsync(CreateValidDto(equipment.Id), TestContext.Current.CancellationToken);

        Assert.Equal(["measurement", "alerts"], calls);
    }

    [Fact]
    public async Task CreateAsync_PassesNewMeasurementToAlertEvaluation()
    {
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var alertService = new Mock<IMeasurementAlertService>();
        Measurement? evaluatedMeasurement = null;
        alertService
            .Setup(service => service.EvaluateAndPersistAsync(
                It.IsAny<Measurement>(),
                It.IsAny<CancellationToken>()))
            .Callback<Measurement, CancellationToken>((measurement, _) => evaluatedMeasurement = measurement)
            .Returns(Task.CompletedTask);
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object, alertService.Object);

        await service.CreateAsync(CreateValidDto(equipment.Id), TestContext.Current.CancellationToken);

        Assert.NotNull(evaluatedMeasurement);
        Assert.Equal(equipment.Id, evaluatedMeasurement.EquipmentId);
        Assert.Equal(MeasurementType.Voltage, evaluatedMeasurement.Type);
        Assert.Equal(230.75, evaluatedMeasurement.Value);
        Assert.Equal(Timestamp, evaluatedMeasurement.Timestamp);
    }

    [Fact]
    public async Task CreateAsync_ForwardsCancellationTokenToAlertEvaluation()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var alertService = new Mock<IMeasurementAlertService>();
        alertService
            .Setup(service => service.EvaluateAndPersistAsync(It.IsAny<Measurement>(), cancellationToken))
            .Returns(Task.CompletedTask);
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object, alertService.Object);

        await service.CreateAsync(CreateValidDto(equipment.Id), cancellationToken);

        alertService.Verify(
            item => item.EvaluateAndPersistAsync(It.IsAny<Measurement>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithAlertEvaluation_ReturnsTheUnchangedMeasurementResponse()
    {
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        measurementRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var alertService = new Mock<IMeasurementAlertService>();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object, alertService.Object);
        var dto = CreateValidDto(equipment.Id);

        var response = await service.CreateAsync(dto, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(equipment.Id, response.EquipmentId);
        Assert.Equal(nameof(MeasurementType.Voltage), response.Type);
        Assert.Equal(dto.Value, response.Value);
        Assert.Equal("V", response.Unit);
        Assert.Equal(Timestamp, response.Timestamp);
        Assert.Equal(DateTimeKind.Utc, response.Timestamp.Kind);
    }

    [Fact]
    public async Task CreateAsync_WhenValidationFails_DoesNotEvaluateAlerts()
    {
        var equipmentRepository = CreateEquipmentRepository(null);
        var measurementRepository = CreateMeasurementRepository();
        var alertService = new Mock<IMeasurementAlertService>();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object, alertService.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateValidDto(Guid.Empty), TestContext.Current.CancellationToken));

        alertService.Verify(
            item => item.EvaluateAndPersistAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenMeasurementRepositoryFails_DoesNotEvaluateAlerts()
    {
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        measurementRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Measurement persistence failed."));
        var alertService = new Mock<IMeasurementAlertService>();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object, alertService.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(CreateValidDto(equipment.Id), TestContext.Current.CancellationToken));

        alertService.Verify(
            item => item.EvaluateAndPersistAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task CreateAsync_WithNonUtcTimestamp_DoesNotSaveOrEvaluateAlerts(DateTimeKind kind)
    {
        var equipment = CreateEquipment();
        var equipmentRepository = CreateEquipmentRepository(equipment);
        var measurementRepository = CreateMeasurementRepository();
        var alertService = new Mock<IMeasurementAlertService>();
        var service = CreateService(equipmentRepository.Object, measurementRepository.Object, alertService.Object);
        var dto = CreateValidDto(equipment.Id);
        dto.Timestamp = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-1), kind);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(dto, TestContext.Current.CancellationToken));

        measurementRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
        alertService.Verify(
            item => item.EvaluateAndPersistAsync(It.IsAny<Measurement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static MeasurementService CreateService(
        IEquipmentRepository equipmentRepository,
        IMeasurementRepository measurementRepository)
    {
        var validator = new CreateMeasurementDtoValidator(equipmentRepository);
        var alertService = new Mock<IMeasurementAlertService>();
        return new MeasurementService(validator, equipmentRepository, measurementRepository, alertService.Object);
    }

    private static MeasurementService CreateService(
        IEquipmentRepository equipmentRepository,
        IMeasurementRepository measurementRepository,
        IMeasurementAlertService alertService)
    {
        var validator = new CreateMeasurementDtoValidator(equipmentRepository);
        return new MeasurementService(validator, equipmentRepository, measurementRepository, alertService);
    }

    private static Mock<IEquipmentRepository> CreateEquipmentRepository(Equipment? equipment)
    {
        var repository = new Mock<IEquipmentRepository>();
        repository
            .Setup(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => equipment?.Id == id ? equipment : null);
        return repository;
    }

    private static Mock<IMeasurementRepository> CreateMeasurementRepository() => new();

    private static Equipment CreateEquipment() => new(
        "Sensor",
        "Датчик",
        $"SN-{Guid.NewGuid():N}",
        "Unit test lab");

    private static CreateMeasurementDto CreateValidDto(Guid equipmentId) => new()
    {
        EquipmentId = equipmentId,
        Type = MeasurementType.Voltage,
        Value = 230.75,
        Timestamp = Timestamp
    };
}
