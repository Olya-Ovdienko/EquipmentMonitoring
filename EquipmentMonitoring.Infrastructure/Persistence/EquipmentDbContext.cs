using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public class EquipmentDbContext(DbContextOptions<EquipmentDbContext> options) : DbContext(options)
{
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<Threshold> Thresholds => Set<Threshold>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Maintenance> Maintenances => Set<Maintenance>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new MeasurementConfiguration());
        modelBuilder.ApplyConfiguration(new ThresholdConfiguration());
        modelBuilder.ApplyConfiguration(new AlertConfiguration());
    }
}
