# Change: 添加深色主题和 UI 设计规范

## Why
当前应用程序仅支持浅色主题，用户在夜间使用时可能感到视觉疲劳。同时，项目缺乏统一的 UI 设计规范，导致不同页面和组件的视觉风格不一致。添加深色主题和设计规范可以提升用户体验，建立统一的视觉语言。

## What Changes
- 添加深色/浅色主题切换功能
- 定义统一的 UI 设计规范（颜色、字体、间距、圆角等）
- 实现主题切换持久化（用户选择保存到配置）
- 更新所有现有页面以支持主题切换
- 创建主题资源字典（ThemeResources.xaml）
- 添加主题切换按钮到主界面

## Impact
- Affected specs: ui-theme（新增能力）
- Affected code:
  - `Resources/Theme/` - 新增主题资源文件
  - `ViewModels/Windows/MainWindowViewModel.cs` - 添加主题切换逻辑
  - `Views/Windows/MainWindow.xaml` - 添加主题切换按钮
  - `Resources/Translations/` - 添加主题相关翻译
  - 所有 XAML 视图文件 - 更新以支持主题