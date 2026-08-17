# Agent 知识备忘

> 给后续 AI 代理的警示条目。详细复盘见 `.trae/documents/`，本文件只记录"异常/易混淆"约束。

### [环境] 本工作区无法编译/验证 C# 源码
**问题描述**
- WSL 环境无 dotnet SDK，且项目目标框架为 `net8.0-windows`（WPF），即使安装 dotnet 也无法在 Linux 构建。
- 无 C# LSP（lsp_status 显示 csharp: missing），无法做类型检查。
- 测试项目 `MA_FH5Trainer.Tests` 已删除（待重建）。

**影响范围**
- 所有修改主程序源码（`MA_FH5Trainer/MA_FH5Trainer/**`、`Memory/**`）的操作都无法本地验证。
- 项目 `TreatWarningsAsErrors=true`，任何编译错误/警告在 Windows 上都会变成构建失败。

**建议方案**
- 源码级改动（删除方法/字段、重构、改 catch 等）一律先出清单，在 Windows 环境（有 dotnet）执行并 `dotnet build` 验证。
- 本环境只允许执行**静态可证明安全**的清理：
  - 行尾空格清理（先确认文件无 verbatim/raw 字符串 `@"` / `"""`，否则字符串内容可能被破坏）
  - 删除注释掉的代码行（不影响编译；但保留带信息量的 WHY 注释和"被禁用的功能"标记）
  - 其余（超大文件拆分、未使用 using、catch 精化）→ 输出审计清单，不修改。
- 历史教训：`catch { return false; }` 在内存写入路径是**返回值防御模式（KEEP）**，不是 slop；config/hotkey 边界的 `catch (Exception ex)` 同理。