using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Validators;
using EquipmentMonitoring.Domain;
using FluentValidation.Results;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class ThresholdValidationTests
{
    [Fact]
    public void Validator_Should_Accept_Threshold_With_Both_Limits()
    {
        var result = Validate(CreateValidDto());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_Should_Accept_Threshold_With_Only_MinValue()
    {
        var dto = CreateValidDto();
        dto.MaxValue = null;

        var result = Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_Should_Accept_Threshold_With_Only_MaxValue()
    {
        var dto = CreateValidDto();
        dto.MinValue = null;

        var result = Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_Should_Reject_Empty_EquipmentId()
    {
        var dto = CreateValidDto();
        dto.EquipmentId = Guid.Empty;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.EquipmentId));
    }

    [Fact]
    public void Validator_Should_Reject_Missing_MeasurementType()
    {
        var dto = CreateValidDto();
        dto.MeasurementType = null;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.MeasurementType));
    }

    [Fact]
    public void Validator_Should_Reject_Unsupported_MeasurementType()
    {
        var dto = CreateValidDto();
        dto.MeasurementType = (MeasurementType)999;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.MeasurementType));
    }

    [Fact]
    public void Validator_Should_Reject_Missing_Severity()
    {
        var dto = CreateValidDto();
        dto.Severity = null;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.Severity));
    }

    [Fact]
    public void Validator_Should_Reject_Unsupported_Severity()
    {
        var dto = CreateValidDto();
        dto.Severity = (AlertSeverity)999;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.Severity));
    }

    [Fact]
    public void Validator_Should_Reject_When_Both_Limits_Are_Missing()
    {
        var dto = CreateValidDto();
        dto.MinValue = null;
        dto.MaxValue = null;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.MinValue));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validator_Should_Reject_NonFinite_MinValue(double value)
    {
        var dto = CreateValidDto();
        dto.MinValue = value;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.MinValue));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validator_Should_Reject_NonFinite_MaxValue(double value)
    {
        var dto = CreateValidDto();
        dto.MaxValue = value;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.MaxValue));
    }

    [Fact]
    public void Validator_Should_Reject_When_MinValue_Is_Greater_Than_MaxValue()
    {
        var dto = CreateValidDto();
        dto.MinValue = 90.0;
        dto.MaxValue = 10.0;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.MinValue));
    }

    [Fact]
    public void Validator_Should_Reject_When_MinValue_Equals_MaxValue()
    {
        var dto = CreateValidDto();
        dto.MinValue = 10.0;
        dto.MaxValue = 10.0;

        var result = Validate(dto);

        AssertFieldHasMessage(result, nameof(CreateThresholdDto.MinValue));
    }

    private static CreateThresholdDto CreateValidDto() => new()
    {
        EquipmentId = Guid.NewGuid(),
        MeasurementType = MeasurementType.Temperature,
        MinValue = 10.0,
        MaxValue = 90.0,
        Severity = AlertSeverity.Warning
    };

    private static ValidationResult Validate(CreateThresholdDto dto)
    {
        return new CreateThresholdDtoValidator().Validate(dto);
    }

    private static void AssertFieldHasMessage(ValidationResult result, string propertyName)
    {
        var fieldErrors = result.Errors.Where(error => error.PropertyName == propertyName).ToList();

        Assert.NotEmpty(fieldErrors);
        Assert.All(fieldErrors, error => Assert.False(string.IsNullOrWhiteSpace(error.ErrorMessage)));
    }
}
