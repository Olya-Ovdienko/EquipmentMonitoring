using EquipmentMonitoring.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IEquipmentRegistrationService
{
    Task<EquipmentResponseDto> RegisterAsync(CreateEquipmentDto dto, CancellationToken cancellationToken);
}
