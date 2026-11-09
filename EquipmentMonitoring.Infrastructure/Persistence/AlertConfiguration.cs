using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(alert => alert.Id);
        builder.Property(alert => alert.Id).ValueGeneratedNever();
        builder.Property(alert => alert.EquipmentId).IsRequired();
        builder.Property(alert => alert.MeasurementId).IsRequired();
        builder.Property(alert => alert.Message).IsRequired();
        builder.Property(alert => alert.Severity).IsRequired();
        builder.Property(alert => alert.Timestamp)
            .IsRequired()
            .HasConversion(new ValueConverter<DateTime, long>(
                timestamp => timestamp.Ticks,
                ticks => new DateTime(ticks, DateTimeKind.Utc)));
        builder.Property(alert => alert.IsAcknowledged).IsRequired();

        builder.HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(alert => alert.EquipmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Measurement>()
            .WithMany()
            .HasForeignKey(alert => alert.MeasurementId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(alert => new { alert.EquipmentId, alert.Timestamp });
        builder.HasIndex(alert => new { alert.EquipmentId, alert.IsAcknowledged });
    }
}
