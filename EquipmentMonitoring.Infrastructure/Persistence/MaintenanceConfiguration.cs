using EquipmentMonitoring.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentMonitoring.Infrastructure.Persistence.Configurations;

public class MaintenanceConfiguration : IEntityTypeConfiguration<Maintenance>
{
    public void Configure(EntityTypeBuilder<Maintenance> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Status)
            .IsRequired();

        builder.Property(m => m.ScheduledDate)
            .IsRequired();

        // Налаштування зв'язку та зовнішнього ключа
        builder.HasOne(m => m.Equipment)
            .WithMany()
            .HasForeignKey(m => m.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Індекси для оптимізації пошуку та фільтрації
        builder.HasIndex(m => m.EquipmentId);
        builder.HasIndex(m => m.ScheduledDate);
        builder.HasIndex(m => m.Status);
    }
}