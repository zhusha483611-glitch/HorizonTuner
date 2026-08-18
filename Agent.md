# Agent 知识备忘

> 给后续 AI 代理的警示条目。详细复盘见 `.trae/documents/`，本文件只记录"异常/易混淆"约束。

### [环境] 本工作区可编译但无法运行 C# 测试
**问题描述**
- 目标框架为 `net8.0-windows`（WPF）。本机已装 dotnet 8 SDK（`~/.dotnet`），Linux 上构建需附加 `-p:EnableWindowsTargeting=true`（restore/build 都要），已验证 Release/Debug 全 sln 编译通过。
- **运行测试仍不可能**：`Microsoft.WindowsDesktop.App` 运行时仅存在于 Windows，`dotnet test` 在 Linux 上必然 "Test Run Aborted"。测试执行验证必须在 CI（`windows-latest`）或 Windows 机器上做。
- 无 C# LSP（lsp_status 显示 csharp: missing），无法做类型检查。

**影响范围**
- 所有修改主程序源码（`MA_FH5Trainer/MA_FH5Trainer/**`、`Memory/**`）的操作只能本地编译验证，行为验证须在 Windows。
- 项目 `TreatWarningsAsErrors=true`，任何编译错误/警告在 Windows 上都会变成构建失败。

**建议方案**
- 本地验证流程：`dotnet build MA_FH5Trainer/HorizonTuner.sln -c Release -p:Platform=x64 -p:EnableWindowsTargeting=true`。
- 纯计算逻辑（`Services/Handling/Curves/*`）的断言期望值可在 Linux 控制台项目复刻验证（复制源码 + 断言，跑通后删除）；WPF 依赖类型（转换器/DispatcherTimer）只能靠签名核对。
- 测试运行验证走 CI：推送后查看 `.github/workflows/ci.yml` 的 Test 步骤结果。
- 历史教训：`catch { return false; }` 在内存写入路径是**返回值防御模式（KEEP）**，不是 slop；config/hotkey 边界的 `catch (Exception ex)` 同理。

### [测试] 测试项目重建溯源
**问题描述**
- 历史测试项目 `MA_FH5Trainer.Tests`（6 个测试文件 + csproj）在 git 提交 `10647b3~1` 完整存在，删除提交为 `10647b3`/`3f28176`（2026-08-17，更名前）。
- 2026-08-17 已重建为 `HorizonTuner.Tests`（`MA_FH5Trainer/HorizonTuner.Tests/`，xunit 2.9.3，含历史 6 文件 + Handling Curves 3 文件），sln/CI 已同步。

**影响范围**
- 恢复任何被删测试/对比历史行为时，先查 `git log` 与 `10647b3~1`。

**建议方案**
- 改动被测试逻辑（转换器/配置迁移/预设/节流/Handling 曲线）后必须 `dotnet test` 通过（CI 自动运行）。