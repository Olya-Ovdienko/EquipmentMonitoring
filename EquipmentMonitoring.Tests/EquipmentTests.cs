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
}
