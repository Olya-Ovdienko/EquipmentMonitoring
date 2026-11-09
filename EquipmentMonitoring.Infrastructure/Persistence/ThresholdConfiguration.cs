using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public sealed class ThresholdConfiguration : IEntityTypeConfiguration<Threshold>
{
    public void Configure(EntityTypeBuilder<Threshold> builder)
    {
        builder.ToTable("Thresholds");
        builder.HasKey(threshold => threshold.Id);
        builder.Property(threshold => threshold.Id).ValueGeneratedNever();
        builder.Property(threshold => threshold.EquipmentId).IsRequired();
        builder.Property(threshold => threshold.MeasurementType).IsRequired();
        builder.Property(threshold => threshold.MinValue);
        builder.Property(threshold => threshold.MaxValue);
        builder.Property(threshold => threshold.Severity).IsRequired();

        builder.HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(threshold => threshold.EquipmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(threshold => new
        {
            threshold.EquipmentId,
            threshold.MeasurementType,
            threshold.Severity
        }).IsUnique();
    }
}
