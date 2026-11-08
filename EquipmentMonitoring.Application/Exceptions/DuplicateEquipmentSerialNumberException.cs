namespace EquipmentMonitoring.Application.Exceptions;

public sealed class DuplicateEquipmentSerialNumberException : Exception
{
    private const string DefaultMessage = "An equipment with this serial number already exists.";

    public DuplicateEquipmentSerialNumberException()
        : base(DefaultMessage)
    {
    }

    public DuplicateEquipmentSerialNumberException(Exception innerException)
        : base(DefaultMessage, innerException)
    {
    }
}
