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
}
