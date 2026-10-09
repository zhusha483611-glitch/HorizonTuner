# 修复 detour 并发重建竞态（资源级串行化_关闭路径并发化）

> 落地：commit `2f80610`（2026-10-09）。选型依据：[竞态与循环架构技术选型](../../竞态与循环架构技术选型.md)（方案 A+；B/C 未做）。
> 来源：《项目全面分析报告》W5/W6、[复核《项目全面分析报告》并细化优先级与落地顺序](复核《项目全面分析报告》并细化优先级与落地顺序.md) §3.2/§4.1；修复前另复核实证 4 条事实（竞态面 12 调用点 / 最多 5 并发循环 / 首挂接窗口 / 并发建洞泄漏与地址竞态），详见选型文档首节。

## 问题

`CheatLocalPlayer()`（`Cheats/CarCheats.cs`）的维护路径被 6 条入口 / 12 个调用点共享：

- 循环侧：7 个轮询循环经 `EnsureLocalPlayerDetourAsync`（`Handling.Automation.cs:735`，调用点 `:92/202/418/502/566/619/683`；受速度三模式互斥约束，任一时刻最多 5 个并发循环）；
- UI 侧：5 个懒初始化入口（`CarCheats.cs:155/197/236/272/337`，Accel/Gravity/Waypoint/FreezeAi/NoClip）。

重建路径先清零地址（`CarCheats.cs:47-48`）→ 全模块 AoB 扫描 → `CreateDetour` 分配代码洞（`Memory.cs:233-285`）。1 秒限速门（`Handling.Automation.cs:763-769`）为"检查-写入"非原子，多线程可同时通过（首次挂接时门全开，概率最高）。交错后果：代码洞孤儿（每次 4KB）、钩子点活代码被多路重写（半写窗口 ×N）、`TryReapply` 与 rebuild 互相覆盖、并发 `CreateDetour(0,…)`。

关闭侧：`StopAllAutomationAsync`（`Handling.Lifecycle.cs:252-259`）顺序 await 8×`CancelLoopAsync(500ms)`，最坏 4s > `ShutdownCoordinator` 默认 3000ms（`ShutdownCoordinator.cs:23`）。

## 修复（方案 A+）

1. **`CarCheats` 维护门**：`SemaphoreSlim(1,1)` 串行化整条维护路径；`CheatLocalPlayer` 入口 + 门内双重检查（`LocalPlayerHookDetourAddress > 0 && IsLocalPlayerHookActive()` 即复用），折叠并发重复重建；重建主体抽为私有 `RebuildLocalPlayerDetourAsync`，公开签名不变。
2. **`TryReapplyLocalPlayerHook` 非阻塞跳过**：`Wait(0)` 抢不到门（重建进行中）直接返回 false，由下个 tick 重试——不再与 rebuild 抢写。
3. **关闭路径并发化**：8 个 Cancel 任务收集后 `Task.WhenAll`，最坏 4s → ≈0.5s，回到 3s 预算内。

## 验证

- `dotnet build`（Debug/Release，SDK 8.0.425）：0 警告 0 错误（警告即错误门禁）。
- `dotnet test`：开发机受限进程上下文无法启动 testhost（`OpenProcess` 访问被拒，bash/非沙箱/PowerShell 三通道一致），属环境限制；本改动路径不在测试覆盖内。测试套件由 CI（windows-latest）执行。
- 待实机验证清单：首次挂接 / 游戏重启重连 / 菜单失败窗口（racePtr=0）/ UI 与循环并发 / 退出关闭 ≤3s。

## 残余与后续

- 重建**失败**窗口：排队调用者串行重试（扫描冗余、无并发写）——失败冷却归方案 B；
- 半写指令窗口理论存在（单写者后最小化，技术本质不可归零）；
- 队列等待未传 token（关闭最坏多等一次重建时长）——方案 B 传 token 消除；
- 方案 B（detour 生命周期服务 + 单测）为下一迭代候选；方案 C（循环收敛、统一调度）冻结。

## 涉及文件

- [CarCheats.cs](../../MA_FH5Trainer/MA_FH5Trainer/Cheats/CarCheats.cs) — 维护门 / 双重检查 / TryReapply 跳过
- [Handling.Lifecycle.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Lifecycle.cs) — StopAllAutomationAsync 并发取消
- [Handling.Automation.cs](../../MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.Automation.cs) — Ensure/限速门（本次未改动，方案 B 目标）
- [ShutdownCoordinator.cs](../../MA_FH5Trainer/MA_FH5Trainer/Services/ShutdownCoordinator.cs) — 3s 预算（未改动）
