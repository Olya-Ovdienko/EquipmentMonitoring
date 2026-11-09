using EquipmentMonitoring.Domain;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class ThresholdTests
{
    [Fact]
    public void Threshold_Should_Create_With_Both_Limits()
    {
        var equipmentId = Guid.NewGuid();

        var threshold = new Threshold(
            equipmentId,
            MeasurementType.Temperature,
            10.0,
            90.0,
            AlertSeverity.Warning);

        Assert.Equal(equipmentId, threshold.EquipmentId);
        Assert.Equal(MeasurementType.Temperature, threshold.MeasurementType);
        Assert.Equal(10.0, threshold.MinValue);
        Assert.Equal(90.0, threshold.MaxValue);
        Assert.Equal(AlertSeverity.Warning, threshold.Severity);
    }

    [Fact]
    public void Threshold_Should_Create_With_Only_MinValue()
    {
        var threshold = new Threshold(
            Guid.NewGuid(),
            MeasurementType.Load,
            10.0,
            null,
            AlertSeverity.Warning);

        Assert.Equal(10.0, threshold.MinValue);
        Assert.Null(threshold.MaxValue);
    }

    [Fact]
    public void Threshold_Should_Create_With_Only_MaxValue()
    {
        var threshold = new Threshold(
            Guid.NewGuid(),
            MeasurementType.Voltage,
            null,
            90.0,
            AlertSeverity.Critical);

        Assert.Null(threshold.MinValue);
        Assert.Equal(90.0, threshold.MaxValue);
    }

    [Fact]
    public void Threshold_Should_Reject_When_Both_Limits_Are_Null()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Threshold(
                Guid.NewGuid(),
                MeasurementType.Temperature,
                null,
                null,
                AlertSeverity.Warning));
    }

    [Fact]
    public void Threshold_Should_Reject_When_MinValue_Is_Greater_Than_MaxValue()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Threshold(
                Guid.NewGuid(),
                MeasurementType.Temperature,
                90.0,
                10.0,
                AlertSeverity.Warning));
    }

    [Fact]
    public void Threshold_Should_Reject_When_MinValue_Equals_MaxValue()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Threshold(
                Guid.NewGuid(),
                MeasurementType.Temperature,
                10.0,
                10.0,
                AlertSeverity.Warning));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Threshold_Should_Reject_NonFinite_MinValue(double minValue)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Threshold(
                Guid.NewGuid(),
                MeasurementType.Temperature,
                minValue,
                90.0,
                AlertSeverity.Warning));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Threshold_Should_Reject_NonFinite_MaxValue(double maxValue)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Threshold(
                Guid.NewGuid(),
                MeasurementType.Temperature,
                10.0,
                maxValue,
                AlertSeverity.Warning));
    }

    [Fact]
    public void Threshold_Should_Reject_Empty_EquipmentId()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Threshold(
                Guid.Empty,
                MeasurementType.Temperature,
                10.0,
                90.0,
                AlertSeverity.Warning));
    }

    [Fact]
    public void Threshold_Should_Reject_Unsupported_MeasurementType()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Threshold(
                Guid.NewGuid(),
                (MeasurementType)999,
                10.0,
                90.0,
                AlertSeverity.Warning));
    }

    [Fact]
    public void Threshold_Should_Reject_Unsupported_AlertSeverity()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Threshold(
                Guid.NewGuid(),
                MeasurementType.Temperature,
                10.0,
                90.0,
                (AlertSeverity)999));
    }

    [Fact]
    public void Threshold_Should_Generate_NonEmpty_Id_And_Preserve_Properties()
    {
        var equipmentId = Guid.NewGuid();

        var threshold = new Threshold(
            equipmentId,
            MeasurementType.Voltage,
            110.0,
            240.0,
            AlertSeverity.Critical);

        Assert.NotEqual(Guid.Empty, threshold.Id);
        Assert.Equal(equipmentId, threshold.EquipmentId);
        Assert.Equal(MeasurementType.Voltage, threshold.MeasurementType);
        Assert.Equal(110.0, threshold.MinValue);
        Assert.Equal(240.0, threshold.MaxValue);
        Assert.Equal(AlertSeverity.Critical, threshold.Severity);
    }
}
