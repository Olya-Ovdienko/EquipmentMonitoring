using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public class EquipmentDbContext(DbContextOptions<EquipmentDbContext> options) : DbContext(options)
{
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Measurement> Measurements => Set<Measurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new MeasurementConfiguration());
    }
}
