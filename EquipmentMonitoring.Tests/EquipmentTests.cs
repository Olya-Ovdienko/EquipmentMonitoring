using EquipmentMonitoring.Domain;
using Xunit;

namespace EquipmentMonitoring.Tests;

public class EquipmentTests
{
    [Fact]
    public void Equipment_Should_Generate_NonEmpty_Id()
    {
        var equipment = new Equipment("Laptop", "Computer", "SN-001", "Office 1");
        var anotherEquipment = new Equipment("Monitor", "Display", "SN-002", "Office 1");

        Assert.IsType<Guid>(equipment.Id);
        Assert.NotEqual(Guid.Empty, equipment.Id);
        Assert.NotEqual(equipment.Id, anotherEquipment.Id);
    }

    [Fact]
    public void EquipmentStatus_Should_Contain_Expected_Values()
    {
        Assert.True(typeof(EquipmentStatus).IsEnum);
        Assert.Equal(
            new[] { "Available", "InUse", "Maintenance", "Disabled" },
            Enum.GetNames<EquipmentStatus>());
    }

    [Fact]
    public void Equipment_Status_Should_Be_Of_EquipmentStatus_Type()
    {
        Assert.Equal(
            typeof(EquipmentStatus),
            typeof(Equipment).GetProperty(nameof(Equipment.Status))?.PropertyType);
    }

    [Fact]
    public void Equipment_Should_Set_CreatedAt_In_Utc()
    {
        var beforeCreation = DateTime.UtcNow;
        var equipment = new Equipment("Laptop", "Computer", "SN-001", "Office 1");
        var afterCreation = DateTime.UtcNow;

        Assert.InRange(equipment.CreatedAt, beforeCreation, afterCreation);

        Assert.Equal(DateTimeKind.Utc, equipment.CreatedAt.Kind);

    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Equipment_Should_Reject_Empty_Or_Whitespace_Name(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Equipment(name!, "Computer", "SN-001", "Office 1"));
    }

    [Fact]
    public void Equipment_Should_Accept_Name_Of_100_Characters()
    {
        var name = new string('A', 100);

        var equipment = new Equipment(name, "Computer", "SN-001", "Office 1");

        Assert.Equal(name, equipment.Name);
    }

    [Fact]
    public void Equipment_Should_Reject_Name_Of_101_Characters()
    {
        var name = new string('A', 101);

        Assert.ThrowsAny<ArgumentException>(() =>
            new Equipment(name, "Computer", "SN-001", "Office 1"));
    }
}
