# Change: 添加文字渐变效果和卡片边框发光效果

## Why

当前主界面的文字字号偏小，导致可读性不足，特别是在长时间使用时容易产生视觉疲劳。同时，界面缺乏视觉吸引力强的动态效果，用户体验较为单调。通过添加文字渐变效果、增大字号以及卡片边框发光效果，可以提升界面的视觉吸引力和用户使用体验。

## What Changes

- 为应用标题、Expander 标题等重要文字添加渐变色效果
- 统一增大各区域的字体字号（标题、信息文本、侧边栏标签等），提升可读性
- 为功能区卡片（Expander）添加边框发光效果，增强视觉反馈
- 在主题资源中定义新的字号常量和渐变色资源
- 确保深色和浅色主题下的效果一致性和适配性

## Impact

- **Affected specs**: `ui-enhancements`
- **Affected code**:
  - `Resources/Theme/DarkThemeResources.xaml` - 添加新的字号和渐变色资源
  - `Resources/Theme/LightThemeResources.xaml` - 添加新的字号和渐变色资源
  - `Views/Windows/MainWindow.xaml` - 更新文字样式和字号
  - `Views/ExpandersView.xaml` - 添加卡片边框发光效果
  - `Resources/Theme/CommonBorderStyle.xaml` - 更新边框样式以支持发光效果