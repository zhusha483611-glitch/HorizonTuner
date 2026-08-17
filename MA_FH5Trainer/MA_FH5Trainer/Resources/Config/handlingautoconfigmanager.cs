using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
 
namespace HorizonTuner.Resources.Config;
 
public enum VelocityMode
{
    S1Car,      // S1车模式：起步更温和
    S2Car,      // S2车模式：平衡体验
    ACar,       // A车模式：更快加速
    Custom      // 自定义模式：用户手动调整
}

public sealed class HandlingAutoConfig
{
    public bool VelocityAutoOn { get; set; }
    public bool VelocityLinearAutoOn { get; set; }
    public bool VelocityMultiStageAutoOn { get; set; }
    public bool WheelspeedAutoOn { get; set; }
    public bool JumpAutoOn { get; set; }
    public bool BrakeAutoOn { get; set; }
    public bool BrakeAssistAutoOn { get; set; }

    public double ThrottleTriggerThresholdPercent { get; set; } = 12;
    public double BrakeTriggerThresholdPercent { get; set; } = 12;

    public string JumpButton { get; set; } = "A";

    public double BrakeAssistNormalStrengthPercent { get; set; } = 20;
    public double BrakeAssistPanicThresholdPercent { get; set; } = 85;
    public double BrakeAssistPanicStrengthPercent { get; set; } = 60;

    // 线性加速参数（保留兼容性）
    public double VelocityLinearScalePercent { get; set; } = 40;
    public double VelocityLinearGamma { get; set; } = 2.2;

    // 多段式曲线参数
    public VelocityMode VelocityMode { get; set; } = VelocityMode.S2Car;
    public double VelocityS1MaxKmh { get; set; } = 320;
    public double VelocityS2MaxKmh { get; set; } = 360;
    public double VelocityAMaxKmh { get; set; } = 260;
    public double VelocityCustomMaxKmh { get; set; } = 320;
    public double VelocityStage1Gamma { get; set; } = 3.0;   // 起步段曲线（默认 3.0，更温和）
    public double VelocityStage2Gamma { get; set; } = 1.8;   // 中速段曲线（默认 1.8，接近线性）
    public double VelocityStage3Gamma { get; set; } = 1.3;   // 高速段曲线（默认 1.3，更线性）
    public double VelocityStage1Scale { get; set; } = 0.3;   // 起步段强度比例（30%）
    public double VelocityStage2Scale { get; set; } = 0.5;   // 中速段强度比例（50%）
    public double VelocityStage3Scale { get; set; } = 0.7;   // 高速段强度比例（70%）
    public double VelocityStage1End { get; set; } = 0.26;
    public double VelocityStage2End { get; set; } = 0.65;
    public double VelocityStage1TargetFrac { get; set; } = 0.78;
    public double VelocityStage2TargetFrac { get; set; } = 0.97;

    public double VelocityS1Stage1Gamma { get; set; } = 1.1;
    public double VelocityS1Stage2Gamma { get; set; } = 1.6;
    public double VelocityS1Stage3Gamma { get; set; } = 2.8;
    public double VelocityS1Stage1Scale { get; set; } = 0.25;
    public double VelocityS1Stage2Scale { get; set; } = 0.45;
    public double VelocityS1Stage3Scale { get; set; } = 0.65;
    public double VelocityS1Stage1End { get; set; } = 0.28;
    public double VelocityS1Stage2End { get; set; } = 0.68;
    public double VelocityS1Stage1TargetFrac { get; set; } = 0.72;
    public double VelocityS1Stage2TargetFrac { get; set; } = 0.95;

    public double VelocityS2Stage1Gamma { get; set; } = 1.0;
    public double VelocityS2Stage2Gamma { get; set; } = 1.5;
    public double VelocityS2Stage3Gamma { get; set; } = 2.6;
    public double VelocityS2Stage1Scale { get; set; } = 0.3;
    public double VelocityS2Stage2Scale { get; set; } = 0.5;
    public double VelocityS2Stage3Scale { get; set; } = 0.7;
    public double VelocityS2Stage1End { get; set; } = 0.26;
    public double VelocityS2Stage2End { get; set; } = 0.65;
    public double VelocityS2Stage1TargetFrac { get; set; } = 0.78;
    public double VelocityS2Stage2TargetFrac { get; set; } = 0.97;

    public double VelocityAStage1Gamma { get; set; } = 0.9;
    public double VelocityAStage2Gamma { get; set; } = 1.3;
    public double VelocityAStage3Gamma { get; set; } = 2.4;
    public double VelocityAStage1Scale { get; set; } = 0.55;
    public double VelocityAStage2Scale { get; set; } = 0.75;
    public double VelocityAStage3Scale { get; set; } = 0.95;
    public double VelocityAStage1End { get; set; } = 0.22;
    public double VelocityAStage2End { get; set; } = 0.60;
    public double VelocityAStage1TargetFrac { get; set; } = 0.84;
    public double VelocityAStage2TargetFrac { get; set; } = 0.98;

    // 用户自定义预设列表
    public List<HorizonTuner.Models.VelocityPreset> CustomVelocityPresets { get; set; } = new();

    public bool VelocityChaseLimitOn { get; set; }
    public double VelocityChaseKp { get; set; } = 1.2;
    public double VelocityChaseMinDelta { get; set; } = 0.01;

    public bool ShowAdvancedVelocityDiagnostics { get; set; }

    // 速度强度和限制
    public double VelocityStrength { get; set; } = 0.10;
    public double VelocityLimitKmh { get; set; } = 300;

    // 轮速设置
    public double WheelspeedValue { get; set; } = 10;
    public double WheelspeedLimitKmh { get; set; } = 300;

    // 加速和重力修改器
    public double AccelModifierPercent { get; set; } = 100;
    public double GravityModifierPercent { get; set; } = 100;

    // 跳跃修改
    public double JumpHackValue { get; set; } = 1;

    // 超级刹车
    public double SuperBrakeValue { get; set; } = 3;

    // 技能分数倍率
    public double QuickSkillScoreMultiplier { get; set; } = 1;
}
 
public static class HandlingAutoConfigManager
{
    private const string ConfigFileName = "handling-auto.json";
 
    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MA_FH5Trainer",
        ConfigFileName
    );
 
    private static readonly object LockObj = new();
    private static HandlingAutoConfig? _cached;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
 
    public static HandlingAutoConfig Get()
    {
        lock (LockObj)
        {
            _cached ??= LoadInternal();
            return _cached;
        }
    }
 
    public static void Save(HandlingAutoConfig config)
    {
        lock (LockObj)
        {
            _cached = config;
            SaveInternal(config);
        }
    }
 
    private static HandlingAutoConfig LoadInternal()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return new HandlingAutoConfig();
            }
 
            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<HandlingAutoConfig>(json, JsonOptions) ?? new HandlingAutoConfig();
            var changed = HandlingAutoConfigMigration.NormalizeConfig(config);
            if (changed)
            {
                SaveInternal(config);
            }

            return config;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load handling auto config: {ex.Message}");
            BackupCorruptConfigFile();
            return new HandlingAutoConfig();
        }
    }
 
    private static void SaveInternal(HandlingAutoConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
 
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save handling auto config: {ex.Message}");
        }
    }

    private static void BackupCorruptConfigFile()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return;
            }

            var dir = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var backupPath = $"{ConfigFilePath}.corrupt.{stamp}.bak";

            try
            {
                File.Move(ConfigFilePath, backupPath, overwrite: true);
            }
            catch
            {
                File.Copy(ConfigFilePath, backupPath, overwrite: true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to backup corrupt handling auto config: {ex.Message}");
        }
    }
}
