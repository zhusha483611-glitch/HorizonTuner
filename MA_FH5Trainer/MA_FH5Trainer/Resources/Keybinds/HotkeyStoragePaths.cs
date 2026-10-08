using System.IO;

namespace HorizonTuner.Resources.Keybinds;

public static class HotkeyStoragePaths
{
    public static string GetSavePath(string name, string? localAppDataRoot = null)
    {
        var root = string.IsNullOrWhiteSpace(localAppDataRoot)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : localAppDataRoot;

        var hotkeyDir = Path.Combine(root, "MA_FH5Trainer", "GlobalHotkeys");
        return Path.Combine(hotkeyDir, $"{name}.json");
    }
}
