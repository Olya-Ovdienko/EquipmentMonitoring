using System.ComponentModel.DataAnnotations;
using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Mappings;
using EquipmentMonitoring.Domain;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MeasurementDtoTests
{
    [Fact]
    public void CreateMeasurementDto_Should_Expose_Expected_Property_Types()
    {
        var dtoType = typeof(CreateMeasurementDto);

        Assert.Equal(typeof(Guid?), dtoType.GetProperty(nameof(CreateMeasurementDto.EquipmentId))?.PropertyType);
        Assert.Equal(typeof(MeasurementType?), dtoType.GetProperty(nameof(CreateMeasurementDto.Type))?.PropertyType);
        Assert.Equal(typeof(double), dtoType.GetProperty(nameof(CreateMeasurementDto.Value))?.PropertyType);
        Assert.Equal(typeof(DateTime?), dtoType.GetProperty(nameof(CreateMeasurementDto.Timestamp))?.PropertyType);
    }

    [Theory]
    [InlineData(nameof(CreateMeasurementDto.EquipmentId))]
    [InlineData(nameof(CreateMeasurementDto.Type))]
    public void CreateMeasurementDto_Should_Require_EquipmentId_And_Type(string memberName)
    {
        var dto = new CreateMeasurementDto
        {
            Value = 12.5
        };

        Assert.Contains(Validate(dto), result => result.MemberNames.Contains(memberName));
    }

    [Fact]
    public void CreateMeasurementDto_Should_Store_Fractional_Value()
    {
        const double value = 21.75;
        var dto = new CreateMeasurementDto { Value = value };

        Assert.Equal(value, dto.Value);
    }

    [Fact]
    public void CreateMeasurementDto_Timestamp_Should_Be_Optional_And_Default_To_Null()
    {
        var dto = new CreateMeasurementDto();

        Assert.Null(dto.Timestamp);
    }

    [Fact]
    public void MeasurementResponseDto_Should_Expose_All_Properties_And_Types()
    {
        var dtoType = typeof(MeasurementResponseDto);

        Assert.Equal(typeof(Guid), dtoType.GetProperty(nameof(MeasurementResponseDto.Id))?.PropertyType);
        Assert.Equal(typeof(Guid), dtoType.GetProperty(nameof(MeasurementResponseDto.EquipmentId))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(MeasurementResponseDto.Type))?.PropertyType);
        Assert.Equal(typeof(double), dtoType.GetProperty(nameof(MeasurementResponseDto.Value))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(MeasurementResponseDto.Unit))?.PropertyType);
        Assert.Equal(typeof(DateTime), dtoType.GetProperty(nameof(MeasurementResponseDto.Timestamp))?.PropertyType);
    }

    [Fact]
    public void MeasurementResponseDto_Should_Store_All_Six_Fields()
    {
        var id = Guid.NewGuid();
        var equipmentId = Guid.NewGuid();
        var timestamp = new DateTime(2026, 11, 8, 12, 30, 0, DateTimeKind.Utc);
        var dto = new MeasurementResponseDto
        {
            Id = id,
            EquipmentId = equipmentId,
            Type = nameof(MeasurementType.Temperature),
            Value = 22.5,
            Unit = "°C",
            Timestamp = timestamp
        };

        Assert.Equal(id, dto.Id);
        Assert.Equal(equipmentId, dto.EquipmentId);
        Assert.Equal("Temperature", dto.Type);
        Assert.Equal(22.5, dto.Value);
        Assert.Equal("°C", dto.Unit);
        Assert.Equal(timestamp, dto.Timestamp);
    }

    [Fact]
    public void MeasurementMapper_ToDomain_Should_Map_CreateMeasurementDto_Properties()
    {
        var equipmentId = Guid.NewGuid();
        var dto = new CreateMeasurementDto
        {
            EquipmentId = equipmentId,
            Type = MeasurementType.Voltage,
            Value = 121.375
        };

        var measurement = MeasurementMapper.ToDomain(dto);

        Assert.Equal(equipmentId, measurement.EquipmentId);
        Assert.Equal(MeasurementType.Voltage, measurement.Type);
        Assert.Equal(121.375, measurement.Value);
    }

    [Fact]
    public void MeasurementMapper_ToDomain_Should_Use_Current_Utc_Time_When_Timestamp_Is_Not_Provided()
    {
        var beforeMapping = DateTime.UtcNow;
        var dto = new CreateMeasurementDto
        {
            EquipmentId = Guid.NewGuid(),
            Type = MeasurementType.Temperature,
            Value = 20.5
        };

        var measurement = MeasurementMapper.ToDomain(dto);

        var afterMapping = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Utc, measurement.Timestamp.Kind);
        Assert.InRange(measurement.Timestamp, beforeMapping, afterMapping);
    }

    [Fact]
    public void MeasurementMapper_ToDomain_Should_Preserve_Provided_Utc_Timestamp()
    {
        var timestamp = new DateTime(2026, 11, 8, 12, 30, 0, DateTimeKind.Utc);
        var dto = new CreateMeasurementDto
        {
            EquipmentId = Guid.NewGuid(),
            Type = MeasurementType.Load,
            Value = 47.25,
            Timestamp = timestamp
        };

        var measurement = MeasurementMapper.ToDomain(dto);

        Assert.Equal(timestamp, measurement.Timestamp);
        Assert.Equal(DateTimeKind.Utc, measurement.Timestamp.Kind);
    }

    [Fact]
    public void MeasurementMapper_ToResponse_Should_Map_All_Measurement_Properties()
    {
        var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Temperature, 23.625);

        var dto = MeasurementMapper.ToResponse(measurement);

        Assert.Equal(measurement.Id, dto.Id);
        Assert.Equal(measurement.EquipmentId, dto.EquipmentId);
        Assert.Equal(measurement.Value, dto.Value);
        Assert.Equal(measurement.Unit, dto.Unit);
        Assert.Equal(measurement.Timestamp, dto.Timestamp);
    }

    [Theory]
    [InlineData(MeasurementType.Temperature, "Temperature")]
    [InlineData(MeasurementType.Voltage, "Voltage")]
    [InlineData(MeasurementType.Load, "Load")]
    public void MeasurementMapper_ToResponse_Should_Map_Type_To_String(
        MeasurementType type,
        string expectedType)
    {
        var measurement = new Measurement(Guid.NewGuid(), type, 12.5);

        var dto = MeasurementMapper.ToResponse(measurement);

        Assert.Equal(expectedType, dto.Type);
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            validationResults,
            validateAllProperties: true);

        return validationResults;
    }
}
