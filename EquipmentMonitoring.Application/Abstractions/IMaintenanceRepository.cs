using EquipmentMonitoring.Domain;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IMaintenanceRepository
{
    Task<Maintenance?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Maintenance>> GetByEquipmentIdAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    Task CreateAsync(Maintenance maintenance, CancellationToken cancellationToken = default);
    Task UpdateAsync(Maintenance maintenance, CancellationToken cancellationToken = default);
}