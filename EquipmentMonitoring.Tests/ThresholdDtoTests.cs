using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Mappings;
using EquipmentMonitoring.Domain;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class ThresholdDtoTests
{
    [Fact]
    public void CreateThresholdDto_Should_Expose_Expected_Property_Types()
    {
        var dtoType = typeof(CreateThresholdDto);

        Assert.Equal(typeof(Guid), dtoType.GetProperty(nameof(CreateThresholdDto.EquipmentId))?.PropertyType);
        Assert.Equal(typeof(MeasurementType?), dtoType.GetProperty(nameof(CreateThresholdDto.MeasurementType))?.PropertyType);
        Assert.Equal(typeof(double?), dtoType.GetProperty(nameof(CreateThresholdDto.MinValue))?.PropertyType);
        Assert.Equal(typeof(double?), dtoType.GetProperty(nameof(CreateThresholdDto.MaxValue))?.PropertyType);
        Assert.Equal(typeof(AlertSeverity?), dtoType.GetProperty(nameof(CreateThresholdDto.Severity))?.PropertyType);
    }

    [Fact]
    public void ThresholdResponseDto_Should_Expose_Expected_Property_Types()
    {
        var dtoType = typeof(ThresholdResponseDto);

        Assert.Equal(typeof(Guid), dtoType.GetProperty(nameof(ThresholdResponseDto.Id))?.PropertyType);
        Assert.Equal(typeof(Guid), dtoType.GetProperty(nameof(ThresholdResponseDto.EquipmentId))?.PropertyType);
        Assert.Equal(typeof(MeasurementType), dtoType.GetProperty(nameof(ThresholdResponseDto.MeasurementType))?.PropertyType);
        Assert.Equal(typeof(double?), dtoType.GetProperty(nameof(ThresholdResponseDto.MinValue))?.PropertyType);
        Assert.Equal(typeof(double?), dtoType.GetProperty(nameof(ThresholdResponseDto.MaxValue))?.PropertyType);
        Assert.Equal(typeof(AlertSeverity), dtoType.GetProperty(nameof(ThresholdResponseDto.Severity))?.PropertyType);
    }

    [Fact]
    public void ThresholdMapper_ToDomain_Should_Map_CreateThresholdDto_Properties()
    {
        var equipmentId = Guid.NewGuid();
        var dto = new CreateThresholdDto
        {
            EquipmentId = equipmentId,
            MeasurementType = MeasurementType.Voltage,
            MinValue = 110.0,
            MaxValue = 240.0,
            Severity = AlertSeverity.Critical
        };

        var threshold = ThresholdMapper.ToDomain(dto);

        Assert.Equal(equipmentId, threshold.EquipmentId);
        Assert.Equal(MeasurementType.Voltage, threshold.MeasurementType);
        Assert.Equal(110.0, threshold.MinValue);
        Assert.Equal(240.0, threshold.MaxValue);
        Assert.Equal(AlertSeverity.Critical, threshold.Severity);
    }

    [Fact]
    public void ThresholdMapper_ToDomain_Should_Map_NonNullable_Enum_Values()
    {
        var dto = new CreateThresholdDto
        {
            EquipmentId = Guid.NewGuid(),
            MeasurementType = MeasurementType.Temperature,
            MinValue = 10.0,
            MaxValue = null,
            Severity = AlertSeverity.Warning
        };

        var threshold = ThresholdMapper.ToDomain(dto);

        Assert.Equal(MeasurementType.Temperature, threshold.MeasurementType);
        Assert.Equal(AlertSeverity.Warning, threshold.Severity);
    }

    [Fact]
    public void ThresholdMapper_ToResponse_Should_Map_All_Threshold_Properties()
    {
        var threshold = new Threshold(
            Guid.NewGuid(),
            MeasurementType.Load,
            null,
            75.0,
            AlertSeverity.Warning);

        var dto = ThresholdMapper.ToResponse(threshold);

        Assert.Equal(threshold.Id, dto.Id);
        Assert.Equal(threshold.EquipmentId, dto.EquipmentId);
        Assert.Equal(threshold.MeasurementType, dto.MeasurementType);
        Assert.Null(dto.MinValue);
        Assert.Equal(threshold.MaxValue, dto.MaxValue);
        Assert.Equal(threshold.Severity, dto.Severity);
    }
}
