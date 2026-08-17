## 现象判断
- 你贴的报错源是 `Application.Current.DispatcherUnhandledException`，而应用在该回调里会直接弹窗并退出。
- 堆栈里出现 `coerceWithDeferredReference`，这类通常是 WPF 依赖属性在做值校验/强制转换时抛异常。
- 本次新增的“RT/LT触发力度(%)”使用 MahApps `NumericUpDown`，其 `Value` 通常是 `double/double?`。
- 目前 ViewModel 里的 `ThrottleTriggerThresholdPercent/BrakeTriggerThresholdPercent` 是 `int`，TwoWay 绑定时控件会把 `double` 回写到 `int` setter，可能触发运行期转换异常，从而导致 DispatcherUnhandledException。

## 修复思路
### 1) 消除绑定类型不匹配（核心）
- 将 `HandlingViewModel` 的两个阈值属性类型从 `int` 改为 `double`（或 `double?`），与 MahApps `NumericUpDown.Value` 对齐。
- 在 `Handling.xaml.cs` 中统一把 ViewModel 的阈值值“取整并 clamp 到 0~100”，再换算成 0~255 的 byte 阈值。
- `handling-auto.json` 仍保存为 `int` 百分比（不改变存储格式）。

### 2) 增强 UI 定时刷新容错（防止任何 UI Tick 异常导致退出）
- 在手柄诊断 `DispatcherTimer.Tick` 内部增加 try/catch，出现异常时只显示“诊断不可用”，避免触发全局 DispatcherUnhandledException。

### 3) 验证
- 重新编译。
- 启动程序进入 Handling 页面，确认：
  - 不再弹出 DispatcherUnhandledException。
  - RT/LT 触发力度 NumericUpDown 可正常编辑、可保存到配置。

## 受影响文件
- HandlingViewModel.cs：阈值属性类型调整。
- Handling.xaml.cs：阈值读取/转换逻辑调整、Timer Tick 容错。

如果你确认，我就按以上方案直接修改并编译验证。