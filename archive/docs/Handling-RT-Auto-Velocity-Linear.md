# Handling：RT 自动速度/线性加速 技术记录

## 背景与问题现象

在 Handling 页面的“速度/轮速（RT自动）”功能中，出现过如下现象：

- 体感上“按住 RT 触发很慢、像几秒才生效一次”。
- 即使逻辑上有 `Task.Delay(16)`，实际写入/生效频率仍可能偏低。

该现象通常不是“Delay 没跑”，而是以下任一因素导致：

- 自动循环在 WPF UI 同步上下文中续体执行，受 Dispatcher/渲染帧率影响，常见上限约 30Hz。
- 写入路径每次都切换内存保护（VirtualProtectEx），高频写入成本过高导致频率下降。
- 后台线程直接读取 WPF 控件（跨线程访问）触发异常，循环被异常吞掉后只剩稀疏写入。
- detour（游戏侧 hook 点）调用频率低（这是另一条链路：即写得勤但游戏读得少）。

本次对话聚焦于“注入/写入频率与手感”这一侧，并在此基础上实现了可对比测试的“线性加速”新功能。

## 关键文件

- Handling 页面 UI：
  - [Handling.xaml](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.xaml)
- Handling 页面逻辑：
  - [Handling.xaml.cs](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.xaml.cs)
- 配置持久化：
  - [HandlingAutoConfigManager.cs](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Resources/Config/HandlingAutoConfigManager.cs)
- 新增多绑定转换器（用于“任一开关打开即可编辑输入框”）：
  - [EnabledIfAnyOnMultiConverter.cs](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Converters/EnabledIfAnyOnMultiConverter.cs)
  - 资源注册：[App.xaml](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/App.xaml)

## 设计目标与约束（来自需求迭代）

1. 写入频率不追求 60Hz，要求“温和速率”（约 20–35Hz），避免过度激进。
2. 线性加速不修改旧功能，而是新增开关，便于对比测试强度。
3. 状态栏调试信息（tick/秒、写入/秒、Detour 地址等）在完成验证后移除，保持界面简洁。
4. 线性加速初版按“速度强度(1–10)”线性会过快，需要更温和且可调的手感曲线。

## 实施变更概览

### 1) 自动循环调度与稳定性

- 自动循环从 UI 线程剥离：通过 `Task.Run(...)` 启动循环，避免 WPF Dispatcher 限制写入频率。
- 节拍器：从 `Task.Delay` 演进到 `PeriodicTimer`（之后统一到 33ms 约 30Hz）。
- 写入成本：对 detour 变量区写入调用 `WriteMemory(..., removeWriteProtection:false)`，避免每次 `VirtualProtectEx` 带来的开销。
- 线程安全：引入 UI 值缓存字段（例如 `_velocityBoostValue`、`_velocityLimitValue` 等），后台循环仅读取缓存，避免跨线程访问 WPF 控件。

### 2) 速度功能拆分为两套（互斥）

Handling 速度模块现在有两个开关（互斥）：

- `速度（RT自动）`：旧功能（固定强度）
- `线性加速（RT自动）`：新功能（曲线/比例可调，随 RT 力度变化）

互斥规则：

- 开启其中一个会自动关闭另一个，避免同一 detour 地址被两套逻辑同时写入而冲突。

### 3) 移除状态栏调试信息

- Handling 顶部状态栏保留最基础显示：`手柄已连接(index=...) RT=... LT=...`。
- tick/秒、写入/秒、错误计数等调试信息已移除。

## 速度（RT自动）旧功能：固定强度

### 触发判定

- 从 `ViewModel.ThrottleTriggerThresholdPercent` 计算 `thresholdByte`（0..255）。
- `pressed = (RT >= thresholdByte)`。

### 写入行为

- 在 `pressed` 期间按 tick（约 33ms）写：
  - `VelEnabled = 1`
- `VelBoost` / `VelLimit`：
  - 只在按下的首个 tick 写一次固定值（避免频繁写）
  - `VelBoost = 1 + VelocityStrength/1000`
  - `VelLimit = VelocityLimit`
- 松开时写：
  - `VelEnabled = 0`

对应逻辑入口：

- `VelocitySwitch_OnToggled`（开关事件）
- `RunVelocityAutoAsync`（后台循环）

## 线性加速（RT自动）新功能：比例 + gamma 曲线

### 参数

线性模式新增两个可调参数（仅线性开关开启时可编辑）：

- 比例（%）：`VelocityLinearScalePercent`，默认 40，范围 5–100
- 曲线（gamma）：`VelocityLinearGamma`，默认 2.2，范围 1.0–4.0

配置持久化到 `handling-auto.json`（LocalAppData\MA_FH5Trainer）。

### 触发判定与归一化力度 u

- `thresholdByte` 同上
- `u = clamp((RT - thresholdByte) / (255 - thresholdByte), 0..1)`（RT 从阈值开始线性映射到 0..1）

### boost 计算

- `maxDelta = (VelocityStrength/1000)`（沿用原强度尺度，便于对比）
- `scale = VelocityLinearScalePercent/100`
- `gamma = VelocityLinearGamma`
- `shaped = u ^ gamma`
- `VelBoost = 1 + maxDelta * scale * shaped`

解释：

- `scale` 控制整体强度（相当于“总油门倍率”）。
- `gamma` 控制曲线形状（缓入/更激进）。`gamma > 1` 会让轻按/半按更温和；满按仍能达到上限（因为 1^gamma=1）。

### 写入节拍与降频

- 循环 tick：约 33ms（30Hz）。
- `VelEnabled`：按 tick 写 1（保证一次性消费的 detour 也能持续生效）。
- `VelBoost/VelLimit`：降频写入，满足任一条件才写：
  - `u` 变化超过阈值（例如 0.01）
  - 距离上次写入超过 100ms

对应逻辑入口：

- `VelocityLinearSwitch_OnToggled`（线性开关事件）
- `RunVelocityLinearAutoAsync`（线性模式循环）
- `CalculateVelocityLinearBoost`（boost 曲线计算）

### 线性参数与强度修改的即时反馈

- 当线性模式开启且修改“强度”时：
  - 若能读取到当前手柄状态，会按当前 RT 立即重算并写入一次线性 boost。
  - 否则将 boost 重置为 1（等待下次按下 RT 时由循环写入）。

## 配置字段一览

文件：`handling-auto.json`（LocalAppData\MA_FH5Trainer\handling-auto.json）

- `VelocityAutoOn`：旧速度模式开关
- `VelocityLinearAutoOn`：线性模式开关
- `VelocityLinearScalePercent`：线性比例（默认 40）
- `VelocityLinearGamma`：线性曲线 gamma（默认 2.2）
- 其他已有字段：`WheelspeedAutoOn`、`ThrottleTriggerThresholdPercent`、`BrakeAssist...` 等

加载互斥纠正：

- 若 `VelocityLinearAutoOn=true`，则强制 `VelocityAutoOn=false`。

## 常见问题与排查建议

### 1) “仍然几秒才变化一次”

优先排查：

- 是否在后台线程读取了 UI 控件导致异常（本次已通过缓存字段规避）。
- detour 地址是否为 0（Hook 未成功）。
- XInput 是否能正常读到 RT/LT（状态栏有最简显示）。

进一步排查（若“写入频率正常但游戏效果仍慢”）：

- detour hook 点调用频率可能低：需要在 detour ASM 内做调用计数或更换 hook 点（本次对话未展开该部分实现）。

### 2) “线性模式太快/太猛”

调参建议：

- 先降低比例：40% → 30%/20%
- 再提高 gamma：2.2 → 2.6/3.0

经验法则：

- 想更温和：比例小 + gamma 大
- 想更直接：比例大 + gamma 小（接近 1.0 则接近真线性）

## 构建/运行注意事项

- `dotnet build` 可能出现 `MA_FH5Trainer.exe` 被占用导致的复制重试警告：通常是程序正在运行。关闭正在运行的 exe 后再构建可消除。

