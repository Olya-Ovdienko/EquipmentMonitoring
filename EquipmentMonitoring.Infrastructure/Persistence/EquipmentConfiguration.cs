using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.HasKey(equipment => equipment.Id);
        builder.Property(equipment => equipment.Id).ValueGeneratedNever();
        builder.Property(equipment => equipment.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(equipment => equipment.Type);
        builder.Property(equipment => equipment.SerialNumber);
        builder.Property(equipment => equipment.Location);
        builder.Property(equipment => equipment.Status);
        builder.Property(equipment => equipment.CreatedAt);
        builder.HasIndex(equipment => equipment.SerialNumber).IsUnique();
    }
}
