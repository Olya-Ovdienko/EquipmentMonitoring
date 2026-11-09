using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using FluentValidation.Results;
using Moq;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MeasurementValidationTests
{
    private static readonly Equipment ExistingEquipment = CreateEquipment();

    [Fact]
    public async Task Validator_Should_Reject_Missing_EquipmentId()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.EquipmentId = null;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.EquipmentId));
        repository.Verify(
            item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Validator_Should_Reject_Empty_EquipmentId()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.EquipmentId = Guid.Empty;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.EquipmentId));
        repository.Verify(
            item => item.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Validator_Should_Reject_Unknown_EquipmentId()
    {
        // Arrange
        var repository = new Mock<IEquipmentRepository>();
        repository
            .Setup(item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipment?)null);

        // Act
        var result = await ValidateAsync(CreateValidDto(), repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.EquipmentId));
    }

    [Fact]
    public async Task Validator_Should_Accept_Existing_Equipment()
    {
        // Arrange
        var equipment = CreateEquipment();
        var repository = CreateRepositoryWithExistingEquipment(equipment);

        // Act
        var result = await ValidateAsync(CreateValidDto(equipment.Id), repository.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validator_Should_Check_Equipment_Existence_Through_Repository_Asynchronously()
    {
        // Arrange
        var equipment = CreateEquipment();
        var repository = CreateRepositoryWithExistingEquipment(equipment);
        var dto = CreateValidDto(equipment.Id);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await ValidateAsync(dto, repository.Object, cancellationToken);

        // Assert
        Assert.True(result.IsValid);
        repository.Verify(
            item => item.GetByIdAsync(equipment.Id, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Validator_Should_Forward_CancellationToken_To_Repository()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var equipment = CreateEquipment();
        var repository = CreateRepositoryWithExistingEquipment(equipment);

        // Act
        var result = await ValidateAsync(
            CreateValidDto(equipment.Id),
            repository.Object,
            cancellationToken);

        // Assert
        Assert.True(result.IsValid);
        repository.Verify(
            item => item.GetByIdAsync(equipment.Id, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Validator_Should_Reject_Missing_Type()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Type = null;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Type));
    }

    [Theory]
    [InlineData(MeasurementType.Temperature)]
    [InlineData(MeasurementType.Voltage)]
    [InlineData(MeasurementType.Load)]
    public async Task Validator_Should_Accept_Supported_Measurement_Types(MeasurementType type)
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Type = type;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validator_Should_Reject_Unsupported_Measurement_Type()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Type = (MeasurementType)999;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Type));
    }

    [Fact]
    public async Task Validator_Should_Accept_Fractional_Value()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Value = 21.75;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validator_Should_Accept_Negative_Temperature()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Type = MeasurementType.Temperature;
        dto.Value = -12.5;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validator_Should_Reject_Negative_Voltage()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Type = MeasurementType.Voltage;
        dto.Value = -0.5;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Value));
    }

    [Fact]
    public async Task Validator_Should_Reject_Negative_Load()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Type = MeasurementType.Load;
        dto.Value = -0.5;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task Validator_Should_Reject_NonFinite_Value(double value)
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Value = value;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Value));
    }

    [Fact]
    public async Task Validator_Should_Accept_Null_Timestamp()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Timestamp = null;

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validator_Should_Accept_Past_Utc_Timestamp()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Timestamp = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validator_Should_Accept_Utc_Timestamp()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Timestamp = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(DateTimeKind.Utc, dto.Timestamp.Value.Kind);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task Validator_Should_Reject_NonUtc_Timestamp(DateTimeKind kind)
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Timestamp = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-1), kind);

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Timestamp));
    }

    [Fact]
    public async Task Validator_Should_Reject_Future_Utc_Timestamp()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.Timestamp = DateTime.UtcNow.AddHours(1);

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Timestamp));
    }

    [Fact]
    public async Task Validator_Should_Report_Readable_Errors_For_All_Invalid_Fields()
    {
        // Arrange
        var repository = CreateRepositoryWithExistingEquipment();
        var dto = CreateValidDto();
        dto.EquipmentId = Guid.Empty;
        dto.Type = (MeasurementType)999;
        dto.Value = double.NaN;
        dto.Timestamp = DateTime.UtcNow.AddHours(1);

        // Act
        var result = await ValidateAsync(dto, repository.Object);

        // Assert
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.EquipmentId));
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Type));
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Value));
        AssertFieldHasMessage(result, nameof(CreateMeasurementDto.Timestamp));
    }

    private static CreateMeasurementDto CreateValidDto(Guid? equipmentId = null) => new()
    {
        EquipmentId = equipmentId ?? ExistingEquipment.Id,
        Type = MeasurementType.Temperature,
        Value = 20.5,
        Timestamp = null
    };

    private static Equipment CreateEquipment() => new(
        "Sensor",
        "Датчик",
        $"SN-{Guid.NewGuid():N}",
        "Lab 1");

    private static Mock<IEquipmentRepository> CreateRepositoryWithExistingEquipment(Equipment? equipment = null)
    {
        equipment ??= ExistingEquipment;
        var repository = new Mock<IEquipmentRepository>();
        repository
            .Setup(item => item.GetByIdAsync(equipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);
        return repository;
    }

    private static async Task<ValidationResult> ValidateAsync(
        CreateMeasurementDto dto,
        IEquipmentRepository repository,
        CancellationToken? cancellationToken = null)
    {
        var validator = new CreateMeasurementDtoValidator(repository);
        return await validator.ValidateAsync(
            dto,
            cancellationToken ?? TestContext.Current.CancellationToken);
    }

    private static void AssertFieldHasMessage(ValidationResult result, string propertyName)
    {
        var fieldErrors = result.Errors.Where(error => error.PropertyName == propertyName).ToList();

        Assert.NotEmpty(fieldErrors);
        Assert.All(fieldErrors, error => Assert.False(string.IsNullOrWhiteSpace(error.ErrorMessage)));
    }
}
