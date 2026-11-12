using EquipmentMonitoring.Application.Abstractions;
using EquipmentMonitoring.Domain;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence;

public class MaintenanceRepository : IMaintenanceRepository
{
    private readonly EquipmentDbContext _context;

    public MaintenanceRepository(EquipmentDbContext context)
    {
        _context = context;
    }

    public async Task<Maintenance?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Maintenances
            .Include(m => m.Equipment)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Maintenance>> GetByEquipmentIdAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Maintenances
            .Include(m => m.Equipment)
            .Where(m => m.EquipmentId == equipmentId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Maintenance maintenance, CancellationToken cancellationToken = default)
    {
        await _context.Maintenances.AddAsync(maintenance, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Maintenance maintenance, CancellationToken cancellationToken = default)
    {
        _context.Maintenances.Update(maintenance);
        await _context.SaveChangesAsync(cancellationToken);
    }
}