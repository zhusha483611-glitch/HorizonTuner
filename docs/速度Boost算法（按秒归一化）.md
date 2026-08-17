# 速度 Boost 算法（按秒归一化）

本文总结 Handling 页“速度（RT 自动）/线性加速/多段式加速”的核心算法与本次优化的数学形式，重点解释为什么强度很小仍会“像火箭”，以及如何通过“按秒归一化”修正。

## 1. 背景：为什么 0.01 也会像火箭

速度 detour 的消费方式是“每次命中都把速度向量乘一次 `VelBoost` 并写回”，属于复利：

若 `VelBoost = 1.01`，命中频率约 `60 Hz`，则 1 秒等效倍率约：

`1.01^60 ≈ 1.816`

2 秒后约：

`1.01^120 ≈ 3.296`

因此只要 `VelBoost > 1`，哪怕很小，都会在短时间内指数级冲到限速。解决方向是：把写入 detour 的倍率从“每帧倍率”改成“按秒感知后换算得到的每次命中倍率”。

## 2. 输入信号与归一化

### 2.1 油门（RT）归一化

- 原始右扳机：`rt ∈ [0, 255]`
- 阈值：`thr = PercentToTriggerByte(thrPct)`
- 油门归一化：

`u_throttle = clamp((rt - thr) / (255 - thr), 0, 1)`

实现参考：
- [TriggerMath](../MA_FH5Trainer/MA_FH5Trainer/Services/Handling/Curves/TriggerMath.cs)
- [Handling.Automation.cs](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L196-L220)

### 2.2 车速归一化

读取本地玩家速度向量（只用水平分量 vx/vz）：

- `speed_mps = sqrt(vx^2 + vz^2)`
- `speed_kmh = 3.6 * speed_mps`

按当前模式选择分段上限 `maxKmh`，并归一化：

`u_speed = clamp(speed_kmh / maxKmh, 0, 1)`

实现参考：
- [TryReadSpeedKmh/CalculateVelocitySpeedU](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L196-L248)

## 3. 强度语义（用户输入）

强度输入（UI）定义为 `Δ ∈ [0, 1]`，语义为“每秒目标倍率的额外增量”：

`B = 1 + Δ`

其中 `B` 是基础倍率上限（按秒语义），理论范围 `[1, 2]`。

实现参考：
- [Handling.xaml 强度输入](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.xaml#L86-L104)
- [UpdateCachedVelocityValues](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.UiCache.cs#L110-L116)

## 4. 线性加速（按秒目标倍率）

线性模式采用幂函数塑形：

- `scale = clamp(scalePct, 0, 100) / 100`
- `g = clamp(gamma, 1, 4)`
- `maxΔ = clamp(B - 1, 0, 1)`
- `shaped = (u_throttle)^g`

按秒目标倍率：

`boost_per_sec = 1 + maxΔ * scale * shaped`

实现参考：
- [VelocityCurves.CalculateLinearBoost](../MA_FH5Trainer/MA_FH5Trainer/Services/Handling/Curves/VelocityCurves.cs)
- [CalculateVelocityLinearBoost](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Velocity.cs#L536-L539)

## 5. 多段式加速（按秒目标倍率：目标点插值）

多段式先按车速构造“按秒目标倍率曲线”，再按油门做混合。

### 5.1 形状函数

- `smoothstep(t) = 3t^2 - 2t^3`
- `shape(t, g) = smoothstep(t^g)`
- 线性插值：`lerp(a, b, t) = a + (b - a) * t`

### 5.2 目标点

分段边界：`0 < e1 < e2 < 1`

目标点（单位：倍率）：

- `b0 = 1`
- `b1 = 1 + maxΔ * f1`
- `b2 = 1 + maxΔ * f2`
- `b3 = 1 + maxΔ`

其中 `f1, f2` 是“在 Stage1End/Stage2End 的目标进度比例”，用于制造“低速冲劲 / 高速平台”的手感。

### 5.3 分段插值（按车速）

令 `u = u_speed`：

- 若 `u ≤ e1`：`t = u / e1`，`boost = lerp(b0, b1, shape(t, g1))`
- 若 `e1 < u ≤ e2`：`t = (u - e1) / (e2 - e1)`，`boost = lerp(b1, b2, shape(t, g2))`
- 若 `u > e2`：`t = (u - e2) / (1 - e2)`，`boost = lerp(b2, b3, shape(t, g3))`

这个 `boost` 即“按车速得到的按秒目标倍率”，记作 `boost_speed_per_sec`。

实现参考：
- [VelocityCurves.CalculateMultiStageBoostWithTargets](../MA_FH5Trainer/MA_FH5Trainer/Services/Handling/Curves/VelocityCurves.cs)
- [CalculateVelocityMultiStageBoost](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Velocity.cs#L541-L557)

### 5.4 按油门混合（防止轻点就满）

最终按秒目标倍率：

`boost_per_sec = 1 + (boost_speed_per_sec - 1) * u_throttle`

实现参考：
- [RunVelocityMultiStageAutoAsync](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs)
- [RunGamepadStatusAsync 诊断兜底写入](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Diagnostics.cs#L31-L112)

## 6. 关键修正：按秒归一化写入 detour（避免复利爆炸）

### 6.1 detour 命中频率估计（applyHz）

读取 detour 内计数器 `c = VelApplyCounter`，对时间窗做差分：

`hz_inst = (c - c_prev) / (t - t_prev)`

对 `hz_inst` 进行 EMA 平滑并 clamp：

- `hz_inst = clamp(hz_inst, 20, 240)`
- `hz = hz*(1-α) + hz_inst*α`，推荐 `α = 0.2`

实现参考：
- [UpdateDetourApplyHzEstimate/GetDetourApplyHzEstimate](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.UiCache.cs#L7-L51)
- [BuildVelocityDiagnosticsText 中更新计数](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Diagnostics.cs#L198-L236)

### 6.2 从“按秒倍率”换算到“每次命中倍率”

把 `boost_per_sec` 转为 detour 需要的“每次命中倍率”：

`boost_tick = exp( ln(boost_per_sec) / hz )`

这样可保证在稳定 `hz` 下，连续命中 1 秒后的复利效果约等于 `boost_per_sec`。

实现参考：
- [ToBoostPerApply](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.UiCache.cs#L53-L60)

### 6.3 数值稳定的可选改进

当 `boost_per_sec = 1 + δ` 且 `δ` 很小，可用更稳定的形式：

`boost_tick = expm1( log1p(δ) / hz ) + 1`

实现建议：
- 若运行时提供 `log1p/expm1`，直接使用即可。
- 若不可用，可以实现数值稳定近似：\n  - `log1p(x)`：在 `|x|` 很小时使用泰勒展开 `x - x^2/2 + x^3/3 - ...`\n  - `expm1(x)`：在 `|x|` 很小时使用泰勒展开 `x + x^2/2 + x^3/6 + ...`\n+代码实现参考（本项目已落地）：\n+- [Log1p/Expm1/ToBoostPerApply](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.UiCache.cs#L53-L115)

## 7. 诊断与可观测

Handling 页速度区块底部会显示 `VelocityDiagnosticsText`。开启“高级诊断”后，会额外显示：

- `applyHz`：detour 命中频率估计（EMA）
- `boostTick`：真正写入 detour 的每次命中倍率

这两个指标能直接验证“强度 0.01 不会被复利放大成火箭”。

实现参考：
- [高级诊断输出](../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Diagnostics.cs)
- [高级诊断开关配置](../MA_FH5Trainer/MA_FH5Trainer/Resources/Config/handlingautoconfigmanager.cs)

## 8. 调参建议（面向手感）

- 想“更冲起步”：提高 `f1` 或降低 `e1`；并适当降低 `g1`（更早抬升）\n+- 想“高速更平台”：降低 `f2` 或提高 `g3`（后段更缓）\n+- 想“轻点不窜、深踩才来”：对 `u_throttle` 做低通/滞回，或使用 `u_throttle^p`（p>1）\n+- 想“不同帧率一致”：保持按秒归一化，不要直接写 `boost_per_sec` 到 detour\n+
