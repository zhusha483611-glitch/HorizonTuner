using System.Text.Json.Serialization;

namespace HorizonTuner.Models;

/// <summary>
/// 多段式速度加速预设
/// </summary>
public class VelocityPreset
{
    /// <summary>
    /// 预设唯一标识（用于跨重命名稳定追踪）
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 预设名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 起步段 Gamma 曲线
    /// </summary>
    [JsonPropertyName("stage1Gamma")]
    public double Stage1Gamma { get; set; } = 3.0;

    /// <summary>
    /// 中速段 Gamma 曲线
    /// </summary>
    [JsonPropertyName("stage2Gamma")]
    public double Stage2Gamma { get; set; } = 1.8;

    /// <summary>
    /// 高速段 Gamma 曲线
    /// </summary>
    [JsonPropertyName("stage3Gamma")]
    public double Stage3Gamma { get; set; } = 1.3;

    /// <summary>
    /// 起步段强度比例
    /// </summary>
    [JsonPropertyName("stage1Scale")]
    public double Stage1Scale { get; set; } = 0.3;

    /// <summary>
    /// 中速段强度比例
    /// </summary>
    [JsonPropertyName("stage2Scale")]
    public double Stage2Scale { get; set; } = 0.5;

    /// <summary>
    /// 高速段强度比例
    /// </summary>
    [JsonPropertyName("stage3Scale")]
    public double Stage3Scale { get; set; } = 0.7;

    /// <summary>
    /// 第一段结束位置
    /// </summary>
    [JsonPropertyName("stage1End")]
    public double Stage1End { get; set; } = 0.26;

    /// <summary>
    /// 第二段结束位置
    /// </summary>
    [JsonPropertyName("stage2End")]
    public double Stage2End { get; set; } = 0.65;

    /// <summary>
    /// 第一段目标分数比例
    /// </summary>
    [JsonPropertyName("stage1TargetFrac")]
    public double Stage1TargetFrac { get; set; } = 0.78;

    /// <summary>
    /// 第二段目标分数比例
    /// </summary>
    [JsonPropertyName("stage2TargetFrac")]
    public double Stage2TargetFrac { get; set; } = 0.97;

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdTime")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 使用次数（成功应用次数累计）
    /// </summary>
    [JsonPropertyName("useCount")]
    public int UseCount { get; set; }

    /// <summary>
    /// 最近一次使用时间（UTC；显示时建议转本地时间）
    /// </summary>
    [JsonPropertyName("lastUsedTimeUtc")]
    public DateTime? LastUsedTimeUtc { get; set; }
}
