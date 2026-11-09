using EquipmentMonitoring.Application.DTOs;
using EquipmentMonitoring.Application.Mappings;
using EquipmentMonitoring.Domain;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class AlertDtoTests
{
    [Fact]
    public void AlertResponseDto_Should_Expose_Expected_Property_Types()
    {
        var dtoType = typeof(AlertResponseDto);

        Assert.Equal(typeof(Guid), dtoType.GetProperty(nameof(AlertResponseDto.Id))?.PropertyType);
        Assert.Equal(typeof(Guid), dtoType.GetProperty(nameof(AlertResponseDto.EquipmentId))?.PropertyType);
        Assert.Equal(typeof(Guid), dtoType.GetProperty(nameof(AlertResponseDto.MeasurementId))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(AlertResponseDto.Message))?.PropertyType);
        Assert.Equal(typeof(string), dtoType.GetProperty(nameof(AlertResponseDto.Severity))?.PropertyType);
        Assert.Equal(typeof(DateTime), dtoType.GetProperty(nameof(AlertResponseDto.Timestamp))?.PropertyType);
        Assert.Equal(typeof(bool), dtoType.GetProperty(nameof(AlertResponseDto.IsAcknowledged))?.PropertyType);
    }

    [Theory]
    [InlineData(AlertSeverity.Warning, "Warning")]
    [InlineData(AlertSeverity.Critical, "Critical")]
    public void AlertMapper_ToResponse_Should_Map_Severity_To_String(
        AlertSeverity severity,
        string expectedSeverity)
    {
        var alert = CreateAlert(severity);

        var dto = AlertMapper.ToResponse(alert);

        Assert.Equal(expectedSeverity, dto.Severity);
    }

    [Fact]
    public void AlertMapper_ToResponse_Should_Map_All_Alert_Properties()
    {
        var alert = CreateAlert(AlertSeverity.Critical);

        var dto = AlertMapper.ToResponse(alert);

        Assert.Equal(alert.Id, dto.Id);
        Assert.Equal(alert.EquipmentId, dto.EquipmentId);
        Assert.Equal(alert.MeasurementId, dto.MeasurementId);
        Assert.Equal(alert.Message, dto.Message);
        Assert.Equal("Critical", dto.Severity);
        Assert.Equal(alert.Timestamp, dto.Timestamp);
        Assert.Equal(alert.IsAcknowledged, dto.IsAcknowledged);
    }

    private static Alert CreateAlert(AlertSeverity severity) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Temperature exceeded the allowed threshold.",
        severity,
        new DateTime(2026, 11, 8, 12, 30, 0, DateTimeKind.Utc));
}
