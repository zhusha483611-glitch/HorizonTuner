## 目标与不变项
- 保持行为完全一致：所有 Toggle/ValueChanged/SelectionChanged/Click/TextChanged 触发的“注入/写内存/启动循环/写配置/弹窗/诊断文本更新”均不改变语义。
- 保持现有绑定与引用：`DataContext=this`、`ViewModel.*` 绑定、所有 `x:Name`、所有事件名不变（先不改为 Command，避免一次性大迁移）。

## 现状拆分点（基于代码梳理）
- 业务算法：速度线性/多段式曲线、超级刹车、刹车增强、阈值归一化、诊断文本构建。
- 后台循环：Velocity(固定/线性/多段式)、Wheelspeed、Jump、Brake、BrakeAssist、GamepadStatus。
- 外部依赖：`Cheats.GetClass<T>()`、`Memory.GetInstance().WriteMemory`、`XInput`、`HotkeysManager`、`MainWindow.Instance.ViewModel.Hotkeys`、`HandlingAutoConfigManager`。
- UI 事件：大量事件处理器直接包含“注入 + 写内存 + 配置落盘 + 开停循环”。

## 模块化方案（优先 low-risk、可增量验证）
### 1) code-behind 拆为 partial（不改 XAML 事件名）
- 保留 `Views/SubPages/SelfVehicle/Handling.xaml` 与主 `Handling.xaml.cs`，将其余逻辑按职责拆到同命名空间的多个新文件（同为 `public partial class Handling`）：
  - `Handling.Lifecycle.cs`：Loaded/Unloaded、初始化/收尾、UI 初始值灌入与互斥修正。
  - `Handling.Hotkeys.cs`：热键注册/注销、与 MainWindow Hotkeys 列表交互。
  - `Handling.Velocity.cs`：速度三模式事件处理、参数缓存更新、预设选择/保存/管理入口。
  - `Handling.Wheelspeed.cs`、`Handling.Jump.cs`、`Handling.Brake.cs`、`Handling.BrakeAssist.cs`、`Handling.Misc.cs`：对应模块事件处理。
  - `Handling.AutomationLoops.cs`：所有 Start/Stop 与后台 loop（先原样迁移，后续再抽 service）。
  - `Handling.Diagnostics.cs`：GamepadStatus loop、`BuildVelocityDiagnosticsText`、统计窗口等。
  - `Handling.UiCache.cs`：`UpdateCached*` 与阈值字节换算、Sanitize/Clamp 等小工具。

### 2) 抽出“纯算法”到独立类（可测试、无 UI 依赖）
- 新增目录与命名空间：`MA_FH5Trainer/MA_FH5Trainer/Services/Handling/Curves/`（或 `Domain/Handling`，按现有项目习惯取更贴近的命名）。
- 拆出纯函数：
  - `VelocityCurves`：`CalculateVelocityLinearBoost`、`CalculateVelocityMultiStageBoost`（参数显式传入，不再依赖字段）。
  - `BrakeCurves`：`CalculateSuperBrakeBoost`、`GetSuperBrakeMinBoost`、`CalculateBrakeAssist`、`SmoothStep/Lerp`。
  - `TriggerMath`：`CalculateTriggerU`、百分比→byte 阈值换算。
- page 内仅保留“读 UI/VM → 组参数 → 调用曲线函数 → 写内存/更新诊断”。

### 3) 引入关键契约（接口）并提供默认实现（降低耦合，便于后续迁移 DI）
- 新增 `Services/Handling/Abstractions/`：
  - `IMemoryWriter`（封装 WriteMemory）、`IGamepadReader`（封装 XInput）、`ICarCheatsFacade`/`IMiscCheatsFacade`（封装 detour 地址与 Cheat* 调用）、`IHandlingConfigStore`（封装 HandlingAutoConfigManager）。
- 新增 `Services/Handling/Implementations/` 默认实现，内部仍调用当前静态单例/管理器，保证行为不变。
- `Handling` 页面内部改为依赖这些接口（先用 `App.GetRequiredService<T>()` 或本地创建默认实现作为过渡），后续可以把 Page 创建也纳入 DI。

## XAML 重构（先做“资源拆分”，保证绑定/事件零变化）
- 新增资源字典（例如 `Views/SubPages/SelfVehicle/Handling/HandlingResources.xaml` 或放入 `Resources/Theme/`）：
  - 提取重复的控件外观属性（NumericUpDown/Button/TextBox/ComboBox/ToggleSwitch 的 CornerRadius、Background、BorderBrush 等）到 Style，减少 Handling.xaml 重复行。
  - 不改变任何 `x:Name`、事件绑定、MultiBinding 逻辑与布局层级。
- 视情况再做二阶段（可选）：等事件逐步迁移到 VM Command 后，再把大块 UI 拆成 UserControl（VelocityPanel/BrakePanel/MiscPanel 等），避免“子控件事件只能写在子控件 code-behind”导致的耦合回潮。

## 增量实施与验证（每步都可编译回归）
1. 仅做 partial 拆分：搬移代码到新文件、保持方法签名/字段一致；编译通过。
2. 抽曲线纯函数：替换 page 内实现为调用新类；编译通过，手动检查诊断文本与 boost 数值一致。
3. 引入接口与默认实现：替换直接静态调用点；编译通过。
4. 引入 XAML 资源字典：应用 Style 并删除重复属性；编译通过，运行检查 UI 外观不变。
5. 最终集成验证：`dotnet build MA_FH5Trainer/MA_FH5Trainer.sln -c Debug`，并进行页面级烟测（开关互斥、阈值、三种速度模式、轮速/跳跃/刹车/刹车增强、诊断文本刷新、预设保存/管理窗口）。

## 交付物（你将看到的文件结构变化）
- 拆分后的多个 `Handling.*.cs` partial 文件（同目录同命名空间）。
- 新增 `Services/Handling/...`（Curves + Abstractions + Implementations）。
- 新增 `HandlingResources.xaml`（资源字典）并在 `Handling.xaml` 引用；Handling.xaml 行数显著下降但布局不变。

## 风险点与防回归策略
- 互斥开关（速度三模式、超级刹车 vs 刹车增强）保持现有逻辑不动，仅搬家。
- 仍依赖 `x:Name` 的缓存更新（`UpdateCachedUiValues` 等）在拆分后保持调用时机一致。
- 后台循环异常吞掉的行为保持不变（避免改变运行时容错特性）。