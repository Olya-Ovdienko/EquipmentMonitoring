using EquipmentMonitoring.Domain;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class MeasurementTests
{
    [Fact]
    public void Measurement_Should_Generate_NonEmpty_Id()
    {
        var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Temperature, 21.5);

        Assert.NotEqual(Guid.Empty, measurement.Id);
    }

    [Fact]
    public void Measurement_Should_Generate_Unique_Ids()
    {
        var equipmentId = Guid.NewGuid();

        var firstMeasurement = new Measurement(equipmentId, MeasurementType.Temperature, 21.5);
        var secondMeasurement = new Measurement(equipmentId, MeasurementType.Temperature, 22.0);

        Assert.NotEqual(firstMeasurement.Id, secondMeasurement.Id);
    }

    [Fact]
    public void Measurement_Should_Store_EquipmentId()
    {
        var equipmentId = Guid.NewGuid();

        var measurement = new Measurement(equipmentId, MeasurementType.Temperature, 21.5);

        Assert.Equal(equipmentId, measurement.EquipmentId);
    }

    [Theory]
    [InlineData(MeasurementType.Temperature)]
    [InlineData(MeasurementType.Voltage)]
    [InlineData(MeasurementType.Load)]
    public void Measurement_Should_Store_Type(MeasurementType type)
    {
        var measurement = new Measurement(Guid.NewGuid(), type, 21.5);

        Assert.Equal(type, measurement.Type);
    }

    [Fact]
    public void Measurement_Should_Store_Value()
    {
        const double value = -12.75;

        var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Temperature, value);

        Assert.Equal(value, measurement.Value);
    }

    [Theory]
    [InlineData(MeasurementType.Temperature, "°C")]
    [InlineData(MeasurementType.Voltage, "V")]
    [InlineData(MeasurementType.Load, "%")]
    public void Measurement_Should_Set_Unit_Based_On_Type(MeasurementType type, string expectedUnit)
    {
        var measurement = new Measurement(Guid.NewGuid(), type, 21.5);

        Assert.Equal(expectedUnit, measurement.Unit);
    }

    [Fact]
    public void Measurement_Should_Set_Timestamp_In_Utc()
    {
        var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Temperature, 21.5);

        Assert.Equal(DateTimeKind.Utc, measurement.Timestamp.Kind);
    }

    [Fact]
    public void Measurement_Timestamp_Should_Be_Between_Times_Before_And_After_Creation()
    {
        var beforeCreation = DateTime.UtcNow;

        var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Temperature, 21.5);

        var afterCreation = DateTime.UtcNow;

        Assert.InRange(measurement.Timestamp, beforeCreation, afterCreation);
    }
}
