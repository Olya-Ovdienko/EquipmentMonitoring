using System;
using System.Collections.Generic;

namespace EquipmentMonitoring.Application.Validators;

internal static class EquipmentTypeRules
{
    private static readonly HashSet<string> AllowedTypeExamples = new(StringComparer.Ordinal)
    {
        "Датчик",
        "Контролер",
        "Сервер"
    };

    internal static bool IsAllowed(string? type)
    {
        return type is not null && AllowedTypeExamples.Contains(type);
    }
}
