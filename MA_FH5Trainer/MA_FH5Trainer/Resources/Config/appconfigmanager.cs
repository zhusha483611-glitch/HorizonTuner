using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
 
namespace HorizonTuner.Resources.Config;
 
public sealed class AppConfig
{
    public double WindowWidth { get; set; } = 800;
    public double WindowHeight { get; set; } = 816;
    public string WindowState { get; set; } = "Normal";
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
}
 
public static class AppConfigManager
{
    private const string ConfigFileName = "app-config.json";
 
    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MA_FH5Trainer",
        ConfigFileName
    );
 
    private static readonly object LockObj = new();
    private static AppConfig? _cached;
 
    public static AppConfig Get()
    {
        lock (LockObj)
        {
            _cached ??= LoadInternal();
            return _cached;
        }
    }
 
    public static void Save(AppConfig config)
    {
        lock (LockObj)
        {
            _cached = config;
            SaveInternal(config);
        }
    }
 
    private static AppConfig LoadInternal()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return new AppConfig();
            }
 
            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json);
            return config ?? new AppConfig();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load app config: {ex.Message}");
            return new AppConfig();
        }
    }
 
    private static void SaveInternal(AppConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
 
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save app config: {ex.Message}");
        }
    }
}
