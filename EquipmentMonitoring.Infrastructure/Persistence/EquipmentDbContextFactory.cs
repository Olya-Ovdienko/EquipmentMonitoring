using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public class EquipmentDbContextFactory : IDesignTimeDbContextFactory<EquipmentDbContext>
{
    public EquipmentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__EquipmentDatabase")
            ?? "Host=localhost;Port=5432;Database=equipmentmonitoring;Username=postgres";
        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new EquipmentDbContext(options);
    }
}
