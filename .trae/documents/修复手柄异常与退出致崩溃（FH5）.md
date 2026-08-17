## 现象复现与触发条件（基于现有代码可推导的高概率场景）
### 场景 A：手柄操作异常（最典型：残留/抖动/偶发持续生效）
- 前置：打开 Handling 页面，启用“多段式加速（RT自动）”(VelMultiStageSwitch)。
- 操作：按住 RT（超过阈值）→ 在按住 RT 的同时关闭多段式开关 / 切换页面。
- 预期异常：
  - VelEnabled 可能被写成 1 后未被及时清 0，出现“加速残留/手感抽搐/切换后仍生效”。
- 代码依据：
  - 诊断线程会在 MultiOn 时直接写 VelBoost/VelLimit/VelEnabled（每 200ms），并且当 MultiOn 变为 false 时不会补写 VelEnabled=0：见 [Handling.Diagnostics.cs:L78-L104](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Diagnostics.cs#L78-L104)。
  - 多段式主循环停止只做 Cancel/Dispose，不能保证后台任务立刻退出，存在“停止后又写回 1”的竞态：见 [Handling.Automation.cs:L112-L131](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L112-L131) 与循环体写入 [Handling.Automation.cs:L165-L203](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L165-L203)。

### 场景 B：退出程序导致游戏崩溃（高概率）
- 前置：Handling 任意自动化（速度/轮速/刹车/刹车增强/跳跃）处于开启状态；最好在退出瞬间按住 RT/LT，让循环更可能写入。
- 操作：直接关闭 Trainer 主窗口触发退出。
- 预期异常：FH5 进程出现“直接崩溃/无响应后崩溃”。
- 代码依据（关键竞态链）：
  - Handling 的后台循环是 fire-and-forget（不保存 Task），停止时仅 Cancel+Dispose，不等待任务真正退出：例如 [Handling.Automation.cs:L16-L35](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L16-L35)。
  - App 退出时直接调用 `DisconnectFromGame()`，遍历所有 Cheats 的 `Cleanup()` 并 `CloseHandle`：见 [App.xaml.cs:L45-L52](../../MA_FH5Trainer/MA_FH5Trainer/App.xaml.cs#L45-L52) 与 [App.xaml.cs:L118-L125](../../MA_FH5Trainer/MA_FH5Trainer/App.xaml.cs#L118-L125)。
  - CarCheats.Cleanup 会恢复 hook 原始字节后立刻 `VirtualFreeEx` 释放 LocalPlayerHookDetourAddress：见 [CarCheats.Cleanup](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L396-L439)。
  - 但后台循环仍可能在退出窗口期继续向 `LocalPlayerHookDetourAddress + offset` 写入（VelEnabled/VelBoost/…），甚至会调用 `CheatLocalPlayer()` 重新建立 detour：见 [EnsureLocalPlayerDetourAsync](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L574-L616)。
  - 结果：写入/执行发生在“已释放/被游戏重新分配”的页上时，极易破坏游戏内存或触发访问违规 → 游戏崩溃。

## 根本原因分析（从资源释放、内存管理、线程与竞态角度）
### 1) 写入线程未被可靠停止（退出时仍在写）
- 多个自动化循环使用 `Task.Run` 丢弃返回 Task；Stop 仅 Cancel/Dispose CTS。
- `PeriodicTimer.WaitForNextTickAsync(token)` 在取消时会抛 `OperationCanceledException`，由于 Task 未被保存/await，属于“潜在未观察异常 + 非确定退出时机”。
- 即便取消成功，仍存在“Stop 写 0 后，循环在同一 tick 继续写 1”的窗口。

### 2) 诊断线程做了“写入职责”，并且缺少关闭/状态切换的收尾
- `RunGamepadStatusAsync` 在 MultiOn 情况下写 VelEnabled/VelBoost/VelLimit（诊断逻辑变成第二写入源）。
- 当 MultiOn 关闭时不会补写 VelEnabled=0，形成残留风险。

### 3) detour 内存释放策略不安全（与游戏线程并发执行）
- `Mem.CreateDetour` 在游戏进程内 `VirtualAllocEx` 分配代码洞并写入跳转；见 [Memory.CreateDetour](../../MA_FH5Trainer/Memory/Memory.cs#L250-L302)。
- Cleanup 采取“恢复原始字节 + 立即 VirtualFreeEx”策略。
- 这无法保证游戏线程没有正在 detour/trampoline 中执行（并发/高频函数最危险），因此存在真实的“执行到已释放页”的崩溃路径。

## 定位过程（建议的调试工具与方法，含关键观测点）
### 1) 复现场景 A（手柄异常）
- 在 VS 中以 Debug 启动 Trainer，设置断点：
  - [Handling.Diagnostics.cs:L78-L104](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Diagnostics.cs#L78-L104)（诊断写入）
  - [Handling.Automation.cs:L165-L203](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L165-L203)（多段式写入）
  - [Handling.Lifecycle.cs:L161-L171](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Lifecycle.cs#L161-L171)（Unload 停止）
- 观察点：
  - MultiOn 从 true→false 时，是否仍有线程写 VelEnabled=1。
  - `_velocityDiagPrevDown` 在 MultiOn 关闭后是否被清理（目前不会）。

### 2) 复现场景 B（退出崩溃）
- 建议用 ProcDump 或 WER LocalDumps 捕获 forzahorizon5.exe 的崩溃 dump（便于确认 AV 地址是否落在 detour 页附近）。
- 同时在 Trainer 中对以下点下断点/记录：
  - `CarCheats.Cleanup` 的 `Free(LocalPlayerHookDetourAddress)` 处： [CarCheats.cs:L436-L439](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L436-L439)
  - `EnsureLocalPlayerDetourAsync` 内的 `CheatLocalPlayer()`： [Handling.Automation.cs:L600-L606](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L600-L606)
  - 各循环写 `VelEnabled` 的位置：例如 [Handling.Automation.cs:L74-L80](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs#L74-L80)
- 目标结论：验证“Free 发生后仍存在写入/重建 detour”的时序证据。

## 分步骤修复方案（最小侵入、优先止血，再逐步收敛）
### Step 1（止血）：让诊断线程纯读，不再写游戏内存
- 修改 [Handling.Diagnostics.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Diagnostics.cs)：
  - 删除/禁用 `if (hasState && snapshot.MultiOn) { ... Write(VelBoost/VelLimit/VelEnabled) ... }` 块。
  - 将 `_velocityDiagPrevDown` 仅用于显示逻辑，或在 MultiOn=false 时强制清零（但不写内存）。
- 预期效果：
  - 立刻消除“诊断线程造成的第二写入源”，减少抖动与残留。

### Step 2（关键）：让所有自动化循环可控、可等待退出，并消除取消导致的未观察异常
- 修改 [Handling.State.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.State.cs)：为每个 loop 增加 `Task? _xxxTask;` 字段（速度/线性/多段式/轮速/跳跃/超级刹车/刹车增强/手柄状态）。
- 修改 [Handling.Automation.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs)：
  - `StartXxxLoop` 改为保存 Task：`_xxxTask = Task.Run(...);`
  - 在每个 `RunXxxAsync` 最外层捕获 `OperationCanceledException` 并正常退出，避免 Task faulted：
    - 形态：`try { while (await timer.WaitForNextTickAsync(token)) { ... } } catch (OperationCanceledException) { }`
  - 在写内存前增加 `token.ThrowIfCancellationRequested()` 或 `if (token.IsCancellationRequested) break;`（放在 try 内、写入前）。
- 新增一个统一的 `StopAllAutomationAsync()`：
  - 取消所有 CTS。
  - `await Task.WhenAny(task, Task.Delay(timeout))` 等待任务退出（逐个/并行均可），并吞掉取消异常。
  - 等待完成后再写一次所有 enabled=0（防止“最后一写”覆盖）。

### Step 3（退出链路修复）：在释放 detour 之前先完成“写入源停止”
- 修改 [App.xaml.cs](../../MA_FH5Trainer/MA_FH5Trainer/App.xaml.cs) 的 `App_OnExit`：
  - 引入全局 Shutdown 协调器（见 Step 4），在 `DisconnectFromGame()` 前 `await ShutdownCoordinator.StopAllAsync()`。
  - 提前调用 `HotkeysManager.ShutdownSystemHook()`（取消注释并确保幂等）。
- 修改 [MainWindow.xaml.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/Windows/MainWindow.xaml.cs) 的 Closing/Closed：
  - Closing 阶段标记“正在退出”（见 Step 4 的全局标志），避免再启动新 loop/重建 detour。

### Step 4（防竞态）：引入全局“退出中”标志 + 退出协调器
- 新增 `Services/ShutdownCoordinator.cs`（或同等位置）：
  - `Register(Func<Task> stopper)`：页面/模块在 Loaded 时注册停止动作，在 Unloaded 时注销。
  - `StopAllAsync()`：并行执行所有 stopper 并等待完成（带总超时）。
- 新增 `Services/AppShutdownState.cs`：
  - `public static volatile bool IsShuttingDown;`
  - 在 MainWindow Closing 或 App_OnExit 第一时间置 true。
- 修改 `EnsureLocalPlayerDetourAsync`：当 `IsShuttingDown` 为 true 时直接 return（禁止重建/重挂 hook）。
- 修改 Hotkeys 回调（见 [Handling.State.cs:L85-L127](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.State.cs#L85-L127)）：若 `IsShuttingDown` 为 true 直接 return（退出期间不再写内存）。

### Step 5（降低游戏崩溃概率到接近 0）：调整 detour 的清理策略（至少在“应用退出”场景）
- 修改 [CarCheats.Cleanup](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L396-L439)：
  - “退出应用”时：只恢复原始字节（unhook），不 `VirtualFreeEx` 释放 detour 页（尤其 LocalPlayerHookDetourAddress）。
  - 可选：将 Free 延迟到进程生命周期结束（对 Trainer 来说已无意义，但能避免游戏线程执行到已释放页）。
  - 同理评估其它 detour（Accel/Gravity/NoClip…）是否也应在退出时跳过 Free。
- 技术理由：detour 页属于游戏进程地址空间；在 Trainer 退出时保留这块页对系统成本很低，但对稳定性收益极大。

## 测试用例设计（验证修复效果）
### 手柄功能测试
- TC-GP-01：RT/LT 阈值边界（0%、50%、99%）下按压/松开，确认 VelEnabled/BrakeHackEnabled 不残留。
- TC-GP-02：多段式开关在按住 RT 时切换 on→off→on，确认不抖动、不出现持续加速。
- TC-GP-03：页面切换/最小化/恢复后，自动化仍按预期工作且无多线程打架（可通过 UI 诊断文本的 write/tick 计数稳定性验证）。
- TC-GP-04：手柄断连/重连（不同 XInput index），诊断显示与功能一致。

### 退出稳定性测试
- TC-EXIT-01：开启任意自动化（速度/轮速/刹车增强），按住 RT/LT 的同时退出 Trainer，FH5 不崩溃（循环 30 次）。
- TC-EXIT-02：开启多段式 + 打开高级诊断（仅显示，不写入），退出 Trainer，FH5 不崩溃（循环 30 次）。
- TC-EXIT-03：按住热键（触发 HotkeysManager 的 while），同时退出 Trainer，FH5 不崩溃。
- 观察指标：FH5 无 AV/无闪退；Trainer 退出无异常弹窗（尤其避免 UnobservedTaskException 触发）。

## 预防与最佳实践（避免同类问题复发）
- 禁止“诊断线程写游戏内存”：诊断必须纯读；写入只能在单一控制平面完成。
- 所有后台循环必须：保存 Task、可取消、可等待退出、取消不产生 faulted Task。
- 退出为“两阶段”协议：
  1) 进入 shutting down（停止所有写入源/禁止重建 detour）
  2) 再做 detour unhook（必要时跳过 VirtualFreeEx）
- detour 释放策略：默认不在“仍可能被目标线程执行”的窗口释放远程页；需要释放则必须有可靠的 quiesce 机制（例如暂停目标线程/等待安全点/延迟释放）。
- 建议增加结构化日志与崩溃转储采集开关（Release 下可选启用），把“释放/写入/重建 detour”的时间序列记录下来，便于回归与快速定位。

---
如果你确认按以上方案推进，我将按 Step 1→Step 5 的顺序落地代码改动，并在本地构建通过后给出每处修改的精确文件与行范围，以及对应的回归测试执行记录。