using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public sealed class MeasurementConfiguration : IEntityTypeConfiguration<Measurement>
{
    public void Configure(EntityTypeBuilder<Measurement> builder)
    {
        builder.ToTable("Measurements");
        builder.HasKey(measurement => measurement.Id);
        builder.Property(measurement => measurement.Id).ValueGeneratedNever();
        builder.Property(measurement => measurement.EquipmentId).IsRequired();
        builder.Property(measurement => measurement.Type).IsRequired();
        builder.Property(measurement => measurement.Value).IsRequired();
        builder.Property(measurement => measurement.Unit).IsRequired();
        builder.Property(measurement => measurement.Timestamp)
            .IsRequired()
            .HasConversion(new ValueConverter<DateTime, long>(
                timestamp => timestamp.Ticks,
                ticks => new DateTime(ticks, DateTimeKind.Utc)));

        builder.HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(measurement => measurement.EquipmentId)
            .IsRequired();

        builder.HasIndex(measurement => new { measurement.EquipmentId, measurement.Timestamp });
    }
}
