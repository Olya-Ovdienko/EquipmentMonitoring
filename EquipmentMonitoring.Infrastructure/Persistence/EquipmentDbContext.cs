using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public class EquipmentDbContext(DbContextOptions<EquipmentDbContext> options) : DbContext(options)
{
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<Threshold> Thresholds => Set<Threshold>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Maintenance> Maintenances => Set<Maintenance>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ProviderAwareModelCacheKeyFactory>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new MeasurementConfiguration());
        modelBuilder.ApplyConfiguration(new ThresholdConfiguration());
        modelBuilder.ApplyConfiguration(new AlertConfiguration());
        modelBuilder.ApplyConfiguration(new MaintenanceConfiguration());

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var utcTicksConverter = new ValueConverter<DateTime, long>(
                timestamp => timestamp.Ticks,
                ticks => new DateTime(ticks, DateTimeKind.Utc));
            var nullableUtcTicksConverter = new ValueConverter<DateTime?, long?>(
                timestamp => timestamp.HasValue ? timestamp.Value.Ticks : null,
                ticks => ticks.HasValue ? new DateTime(ticks.Value, DateTimeKind.Utc) : null);

            modelBuilder.Entity<Equipment>().Property(equipment => equipment.CreatedAt)
                .HasConversion(utcTicksConverter);
            modelBuilder.Entity<Measurement>().Property(measurement => measurement.Timestamp)
                .HasConversion(utcTicksConverter);
            modelBuilder.Entity<Alert>().Property(alert => alert.Timestamp)
                .HasConversion(utcTicksConverter);
            modelBuilder.Entity<Maintenance>().Property(maintenance => maintenance.ScheduledDate)
                .HasConversion(utcTicksConverter);
            modelBuilder.Entity<Maintenance>().Property(maintenance => maintenance.PerformedDate)
                .HasConversion(nullableUtcTicksConverter);
        }

        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            modelBuilder.Entity<Equipment>().Property(equipment => equipment.CreatedAt)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<Measurement>().Property(measurement => measurement.Timestamp)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<Alert>().Property(alert => alert.Timestamp)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<Maintenance>().Property(maintenance => maintenance.ScheduledDate)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<Maintenance>().Property(maintenance => maintenance.PerformedDate)
                .HasColumnType("timestamp with time zone");
        }
    }
}
