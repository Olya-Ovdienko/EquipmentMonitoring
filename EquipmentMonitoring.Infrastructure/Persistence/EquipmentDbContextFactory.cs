using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public class EquipmentDbContextFactory : IDesignTimeDbContextFactory<EquipmentDbContext>
{
    public EquipmentDbContext CreateDbContext(string[] args)
    {
        var apiDirectory = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(apiDirectory, "EquipmentMonitoring.API.csproj")))
        {
            apiDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "EquipmentMonitoring.API"));
        }

        var options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseSqlite($"Data Source={Path.Combine(apiDirectory, "equipmentmonitoring.db")}")
            .Options;

        return new EquipmentDbContext(options);
    }
}
