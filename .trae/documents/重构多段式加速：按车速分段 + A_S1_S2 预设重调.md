## 现状定位
- 目前“多段式”分段变量 u 来自 RT 扳机力度（从阈值到满量程归一化），而不是车速：[Handling.Automation.cs:L113-L157](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L113-L157) + [TriggerMath.cs:L11-L24](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Services/Handling/Curves/TriggerMath.cs#L11-L24)
- 曲线分段固定按 u 的 0–0.30 / 0.30–0.70 / 0.70–1.00 选 Stage1/2/3：[VelocityCurves.cs:L16-L62](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Services/Handling/Curves/VelocityCurves.cs#L16-L62)
- 因为玩家常常“油门按到底”，u≈1 会直接进入 Stage3，所以你体感上会像“直接触发 S3/第三段”。

## 核心改动（解决“按到底直接 S3”）
1) 把“多段式分段依据”从 RT 改为“当前车速”。
- 数据来源：Hook 已保存本地车辆指针（detour + LocalPlayer），并且 Hook 里有读取速度向量 [rdi+0x20/+0x24/+0x28] 的证据：[CarCheats.cs:L7-L23](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L7-L23)
- 方案：在 multi-stage 自动循环里读取 localPlayerPtr，再读 vx/vy/vz（三个 float），计算 km/h（speed*3.6）。
2) 计算用于分段的 uSpeed：uSpeed = clamp(kmh / ModeMaxKmh, 0..1)。
- 这样 Stage1/2/3 就对应低速/中速/高速区间；油门按到底不再“瞬间跳到 Stage3”，而是随车速增长逐段进入。
3) 保留“油门轻踩/半踩”的可控性：再计算 uThrottle（现有 TriggerU），把最终增益按 uThrottle 混合：
- finalBoost = 1 + (speedBasedBoost - 1) * uThrottle
- 效果：轻踩油门仍然弱，油门到底则按车速分段输出。

## 预设重调（A / S1 / S2）
4) 预设数值改为“按车辆级别速度范围”设计：
- 为 A / S1 / S2 增加各自的 ModeMaxKmh（默认值可调），让分段阈值随级别合理缩放。
- 同时调整默认 Stage1/2/3 的 Gamma/Scale：A 级整体更强（Scale 更高、Gamma 更低一些），S1 更温和，S2 居中。
5) 映射关系保持不变：下拉框选择仍由 [ApplyVelocityPreset](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Velocity.cs#L510-L554) 把 S1/S2/A 的参数写入当前工作参数。

## 配置与 UI
6) 在 [HandlingAutoConfig](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Resources/Config/handlingautoconfigmanager.cs) 增加 3 个字段（S1/S2/A 的 MaxKmh）并提供默认值；加载旧配置时自动使用默认值，兼容已有用户配置。
7) 在 Handling 速度页面增加 1 组简单控件（每个模式一个 MaxKmh，或只显示“当前模式 MaxKmh”），用于你按体感微调不同级别车的速度段。

## 验证
8) 构建验证：编译整个解决方案（Debug/Release 都跑一次），确保无警告/错误。
9) 运行时验证（最小自测）：在游戏里油门到底观察 boost 是否随车速逐段变化；切换 A/S1/S2 模式，A 在同速区间输出更强。