using System;
using System.Threading;
using System.Threading.Tasks;

namespace EquipmentMonitoring.Application.Abstractions;

public interface IEquipmentSerialNumberChecker
{
    Task<bool> IsSerialNumberUniqueAsync(
        string serialNumber,
        Guid? excludedEquipmentId,
        CancellationToken cancellationToken);
}
