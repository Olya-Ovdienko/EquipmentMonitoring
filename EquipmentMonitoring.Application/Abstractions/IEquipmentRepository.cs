using EquipmentMonitoring.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IEquipmentRepository
{
    Task AddAsync(Equipment equipment, CancellationToken cancellationToken);
}
