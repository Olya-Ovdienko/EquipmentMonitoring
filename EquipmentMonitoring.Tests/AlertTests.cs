using EquipmentMonitoring.Domain;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class AlertTests
{
    [Fact]
    public void Alert_Should_Create_And_Preserve_Properties()
    {
        var equipmentId = Guid.NewGuid();
        var measurementId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        const string message = "Temperature exceeded the allowed threshold.";

        var alert = new Alert(
            equipmentId,
            measurementId,
            message,
            AlertSeverity.Critical,
            timestamp);

        Assert.Equal(equipmentId, alert.EquipmentId);
        Assert.Equal(measurementId, alert.MeasurementId);
        Assert.Equal(message, alert.Message);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.Equal(timestamp, alert.Timestamp);
        Assert.False(alert.IsAcknowledged);
    }

    [Fact]
    public void Alert_Should_Generate_NonEmpty_Id()
    {
        var alert = CreateValidAlert();

        Assert.NotEqual(Guid.Empty, alert.Id);
    }

    [Fact]
    public void Alert_Should_Set_IsAcknowledged_To_False_By_Default()
    {
        var alert = CreateValidAlert();

        Assert.False(alert.IsAcknowledged);
    }

    [Fact]
    public void Alert_Should_Reject_Empty_EquipmentId()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Alert(
                Guid.Empty,
                Guid.NewGuid(),
                "Temperature exceeded the allowed threshold.",
                AlertSeverity.Warning,
                DateTime.UtcNow));
    }

    [Fact]
    public void Alert_Should_Reject_Empty_MeasurementId()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Alert(
                Guid.NewGuid(),
                Guid.Empty,
                "Temperature exceeded the allowed threshold.",
                AlertSeverity.Warning,
                DateTime.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Alert_Should_Reject_Empty_Or_Whitespace_Message(string? message)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Alert(
                Guid.NewGuid(),
                Guid.NewGuid(),
                message!,
                AlertSeverity.Warning,
                DateTime.UtcNow));
    }

    [Fact]
    public void Alert_Should_Reject_Unsupported_AlertSeverity()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Alert(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Temperature exceeded the allowed threshold.",
                (AlertSeverity)999,
                DateTime.UtcNow));
    }

    [Fact]
    public void Alert_Should_Preserve_Utc_Timestamp()
    {
        var timestamp = DateTime.UtcNow;

        var alert = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Temperature exceeded the allowed threshold.",
            AlertSeverity.Warning,
            timestamp);

        Assert.Equal(timestamp, alert.Timestamp);
        Assert.Equal(DateTimeKind.Utc, alert.Timestamp.Kind);
    }

    private static Alert CreateValidAlert()
    {
        return new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Temperature exceeded the allowed threshold.",
            AlertSeverity.Warning,
            DateTime.UtcNow);
    }
}
