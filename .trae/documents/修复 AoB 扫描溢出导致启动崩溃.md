## 问题定位
- 崩溃源头在 [AoB.cs](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/Memory/Methods/AoB.cs) 的 `Mem.GetEligibleMemoryRegions` / `Mem.AoBScan`。
- 项目开启了溢出检查（/checked），因此一旦出现“长整型/指针运算或显式转换”超界，会直接抛 `OverflowException`，最终触发 `DispatcherUnhandledException` 使程序退出。
- 当前实现里存在两类必现风险：
  - 64 位区域大小 `RegionSize`（long）被强转为 `int` 用于 `IntPtr.Add(..., (int)memInfo.RegionSize)` 与 `Scanner(..., (int)region.Size)`；当区域 > 2GB 时会直接溢出抛异常（或截断导致地址回退/死循环）。
  - `while` 条件里做了 `currentAddress.ToInt64() + memInfo.RegionSize`，该加法本身也可能在 checked 环境下溢出。

## 修复目标
- 任何情况下 AoB 扫描都不因“大内存区/异常 RegionSize”而崩溃；最多是跳过不可扫描区或以分块方式扫描。
- 维持现有功能语义：`SmartAobScanAllProcessExecutable` 仍可在全进程可执行页中查找签名（例如 `CheatLocalPlayer`）。

## 具体改动（将要编辑的代码）
### 1) 让 GetEligibleMemoryRegions 的地址推进不再依赖 int
- 修改 [AoB.cs:GetEligibleMemoryRegions](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/Memory/Methods/AoB.cs#L107-L183)：
  - 用 `nuint/ulong` 做地址与大小运算，避免 `IntPtr.Add + (int)RegionSize`。
  - 计算 `nextAddress = BaseAddress + RegionSize` 时加入保护：
    - `RegionSize <= 0` 直接跳过/按最小步长前进，避免死循环。
    - 若 `nextAddress <= BaseAddress`（回绕）则 break，防止无限循环。
  - `while` 条件不再使用可能溢出的 `current + RegionSize`；改成“每轮内部算 nextAddress 并验证必须前进”。

### 2) 让 AoBScan 支持超大 Region：分块读取 + 重叠扫描
- 修改 [AoB.cs:AoBScan](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/Memory/Methods/AoB.cs#L17-L72)：
  - 不再对整个 `region.Size` 一次性 `Alloc` / `ReadProcessMemory`，也不再 `(int)region.Size`。
  - 引入固定块大小（例如 8–16MB）进行循环读取。
  - 为避免签名跨块边界丢失，块与块之间保留 `patternLength - 1` 的重叠区；对落在“重叠前缀”的匹配做去重。
  - `Scanner` 的长度始终是单块大小（<= int.MaxValue），从根本上消除溢出。

### 3) 加一道防线：扫描异常不应导致 UI 崩溃
- 在 `AoBScan` 的 Task 逻辑内捕获 `OverflowException`/`OutOfMemoryException` 等，记录为“该区域跳过并继续”或直接返回空结果。
- 或在 [CarCheats.CheatLocalPlayer](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs#L44-L130) 外围加 try/catch，把异常转换成 `ShowError(...)` + `return false`，避免开关初始化/自动开关触发时直接退程序。

## 验证方式
- 编译验证：`dotnet build MA_FH5Trainer/MA_FH5Trainer.sln -c Debug`，确保无警告/错误。
- 运行验证：
  - 启动程序并附加到 FH5。
  - 触发 `CheatLocalPlayer` 路径（例如打开 Handling 并切换轮速/跳跃开关，或让其自动开）。
  - 确认不再弹 “Arithmetic operation resulted in an overflow”，功能仍能找到签名并正常写入 detour。

## 影响评估
- 扫描性能：全进程扫描会更稳定且内存占用显著下降；分块扫描可能略慢但不会 OOM/溢出。
- 行为变化：以前可能因为一次性读不满 region 就直接跳过；分块后读不满也可扫描已读部分（更健壮）。

如果你确认，我将按以上步骤直接修改 `AoB.cs`（必要时补充 `CarCheats.cs` 的异常兜底），然后编译验证并回报结果。