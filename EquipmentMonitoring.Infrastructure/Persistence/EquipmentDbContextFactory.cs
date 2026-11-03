using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public class EquipmentDbContextFactory : IDesignTimeDbContextFactory<EquipmentDbContext>
{
    public EquipmentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite("Data Source=equipmentmonitoring.db")
            .Options;

        return new EquipmentDbContext(options);
    }
}
