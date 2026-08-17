## 需求确认（按你的描述落地）
- 让多段式组件的 **S1车 / S2车 / A车** 这三个“内置模式”也能像自定义一样 **记住你当前调过的 Gamma/比例数值**，下次再选回该模式会恢复为你上次保存的值，而不是固定常量。

## 现状与问题根因
- 目前 `ApplyVelocityPreset` 对 S1/S2/A 使用硬编码常量，见 [Handling.xaml.cs:L1428-L1474](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.xaml.cs#L1428-L1474)。
- 且当前的 `VelStage*Gamma/Scale` 的 `ValueChanged` 会强制把模式切到 `Custom`（我们之前为“预设不跳回自定义”做了抑制，但逻辑仍是“用户改值=Custom”）。这会导致你在 S1/S2/A 下调整后无法“留在该模式并保存到该模式”。

## 实施方案（最小改动且兼容旧配置）
### 1) 扩展 HandlingAutoConfig：为 S1/S2/A 增加独立参数存储
- 在 [handlingautoconfigmanager.cs](../../MA_FH5Trainer/MA_FH5Trainer/Resources/Config/handlingautoconfigmanager.cs) 的 `HandlingAutoConfig` 中新增字段：
  - `VelocityS1Stage1/2/3Gamma`、`VelocityS1Stage1/2/3Scale`
  - `VelocityS2Stage1/2/3Gamma`、`VelocityS2Stage1/2/3Scale`
  - `VelocityAStage1/2/3Gamma`、`VelocityAStage1/2/3Scale`
- 这些字段提供默认值（沿用现有硬编码常量），从而保证老用户不更新配置文件也能正常工作。

### 2) 修改 ApplyVelocityPreset：从配置读取“该模式上次保存的值”
- `ApplyVelocityPreset(S1/S2/A)` 不再写死常量，而是读取上一步新增的字段；然后像现在一样把当前生效值写入 `VelocityStage*` 并保存。
- `Custom` 分支仍保持“不修改参数”。

### 3) 修改 6 个 ValueChanged：按当前模式保存到对应的模式槽位
- 当用户在 **S1/S2/A** 模式下调整任意 `VelStage*Gamma/Scale`：
  - 不切换到 Custom。
  - 把新值写入该模式对应字段（例如当前是 S1，就写 `VelocityS1Stage2Gamma` 等）。
  - 同步写入当前生效字段 `VelocityStage*`，并保持 `VelocityMode` 为当前模式。
- 当用户在 **Custom** 模式下调整：保持现有行为（写入 `VelocityStage*`，并保持 `VelocityMode=Custom`）。
- 保持现有 `_suppressVelocityModeUiEvents` 机制，避免程序回填 UI 时反向触发保存。

## 验证方式
- 编译验证：`dotnet build MA_FH5Trainer/HorizonTuner.sln -c Debug`。
- 行为验证点：
  - 选择 S1，改动一项参数→模式仍显示 S1；切换到 S2 再切回 S1→恢复为你刚才修改后的 S1 参数。
  - S2、A 同理；Custom 行为不变。

## 涉及文件
- [handlingautoconfigmanager.cs](../../MA_FH5Trainer/MA_FH5Trainer/Resources/Config/handlingautoconfigmanager.cs)
- [Handling.xaml.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.xaml.cs)