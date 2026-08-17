# 贡献指南

欢迎为 MA_FH5Trainer（简体中文版）做出贡献。

## 项目约定

- 本工具**仅供个人非商业用途**，使用存在游戏封禁风险，项目不承担任何责任。
- 许可证：GPL-3.0，衍生作品必须保持开源。
- 仅支持 Windows x64，必须 .NET 8，发布须单文件。

## 构建与测试

```bash
dotnet restore MA_FH5Trainer/MA_FH5Trainer.sln
dotnet build  MA_FH5Trainer/MA_FH5Trainer.sln --configuration Debug
dotnet test   MA_FH5Trainer/MA_FH5Trainer.sln
```

> `TreatWarningsAsErrors=true`：**警告即错误**，提交前必须构建 + 测试全绿。

## 开发规范

- 遵循 `.editorconfig` 与 `AGENTS.md` 中的编码规范（命名、注释、分层边界）。
- 新作弊模块实现 `ICheatsBase`；需要恢复原值实现 `IRevertBase`。
- Handling 扩展走 `Services/Handling/`（Abstractions → Curves → Implementations），**不要改 UI 代码**。
- 内存操作：读写前检查 `Memory.Attached`，try-catch 兜底，退出调用 `Cleanup()`。
- 敏感改动前先读 `.trae/documents/README.md` 索引中的相关主题文档。

## 提交 PR

1. Fork 并创建功能分支。
2. 提交信息使用简洁的英文（`fix:` / `feat:` / `chore:` 前缀）。
3. 确保 `dotnet build` + `dotnet test` 通过。
4. 在 PR 描述中说明改动动机、影响范围与验证结果。

## 问题报告

请通过 GitHub Issues 提交，包含：

- 游戏版本（Steam / UWP / OnlineFix）
- 问题现象与复现步骤
- 诊断面板输出（如有）