using System;
using System.Collections.Generic;
using System.Linq;

namespace HorizonTuner.Models;

internal static class VelocityPresetNaming
{
    public static string MakeUniqueName(IEnumerable<VelocityPreset> presets, string desiredName)
    {
        var trimmed = (desiredName ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            trimmed = "预设";
        }

        var existing = new HashSet<string>(
            presets.Select(p => (p.Name ?? string.Empty).Trim()),
            StringComparer.CurrentCultureIgnoreCase);

        if (!existing.Contains(trimmed))
        {
            return trimmed;
        }

        for (var i = 1; i < 10000; i++)
        {
            var candidate = $"{trimmed} ({i})";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{trimmed} ({Guid.NewGuid():N})";
    }
}
