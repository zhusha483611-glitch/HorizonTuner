# Merika's FH5 修改器（简体中文版）

Forza Horizon 5（`forzahorizon5.exe`）开源修改器，简体中文，基于 [MA_FH5Trainer](https://github.com/szaaamerik/MA_FH5Trainer)。

- **平台**：Steam / UWP / OnlineFix · 仅 Windows x64
- **运行时**：.NET 8（WPF + MVVM，MahApps.Metro）

## 功能特性

- **开源软件**：基于 GNU 通用公共许可证 v3.0 完全开源。

- **车辆作弊**
  - 速度修改、刹车修改、跳跃修改、轮速调整和瞬间停止
  - 重力修改、航点传送、冻结AI、无水阻力和穿墙

- **解锁作弊**
  - 即时获得金币、经验值、技能点、抽奖、赛季点数和系列点数

- **调校作弊**
  - 直接编辑空气动力、外倾角、前束、防倾杆、悬挂高度和限制值

- **友好的用户界面**
  - 便捷的修改/作弊开关和滑块，快速调整

- **社区支持**
  - Discord集成和故障排除链接

## 构建

```bash
dotnet restore MA_FH5Trainer/MA_FH5Trainer.sln
dotnet build  MA_FH5Trainer/MA_FH5Trainer.sln --configuration Debug
dotnet test   MA_FH5Trainer/MA_FH5Trainer.sln
dotnet run --project MA_FH5Trainer/MA_FH5Trainer/MA_FH5Trainer.csproj
```

单文件自包含发布：

```bash
dotnet publish MA_FH5Trainer/MA_FH5Trainer/MA_FH5Trainer.csproj \
  --configuration Release --runtime win-x64 --self-contained true --output ./publish \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## 文档

- `docs/` — 技术文档（多段式加速预设系统、速度Boost算法、深浅主题规范、预设管理系列）
- `.trae/documents/README.md` — 开发复盘文档主题索引

## 免责声明

- 本工具仅供个人非商业用途使用。
- 作者不对因使用本工具而导致的任何封禁、数据丢失或损害承担责任。

## 需要更多帮助？

如需支持，请访问我们的Discord服务器或在GitHub上提交问题。

Discord: [Discord服务器链接](https://discord.gg/rHzev9brJ3)<br/>
GitHub Issues: [Github问题链接](https://github.com/szaaamerik/MA_FH5Trainer/issues/new/choose)<br/>

## 许可证

本应用程序基于 GPL-3.0 许可证。您可以在[此处](LICENSE)找到许可证副本。贡献指引见 [CONTRIBUTING.md](CONTRIBUTING.md)。
