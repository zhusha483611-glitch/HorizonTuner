我对项目的主题系统进行了全面的分析，制定了以下重构计划。

### 1. 架构重构：引入 Design Tokens
目前 `DarkThemeResources.xaml` 和 `LightThemeResources.xaml` 中存在大量重复的非颜色定义（如字体、间距、圆角）。我们将引入 **Design Tokens** 概念，将这些通用定义提取到独立文件中。

*   **新增 `DesignTokens.xaml`**：包含字体、字号、字重、间距、圆角、阴影深度、动画时间、图标大小等通用常量，以及不依赖特定颜色的通用样式。
*   **重命名与精简**：
    *   `DarkThemeResources.xaml` -> **`DarkTheme.xaml`**：仅包含深色主题特有的颜色（Brushes）、渐变和特定的效果。
    *   `LightThemeResources.xaml` -> **`LightTheme.xaml`**：仅包含浅色主题特有的颜色。
*   **清理**：删除 `ThemeConstants.xaml` 和 `CommonBorderStyle.xaml`，将其内容整合进 `DesignTokens.xaml`。

### 2. 代码优化与统一数据源
目前 `Theming.cs` 硬编码了颜色值，与 XAML 资源文件中的颜色定义分离，导致维护困难且存在不一致风险。

*   **重构 `Theming.cs`**：不再硬编码 Hex 颜色值，而是作为资源的“强类型访问器”，通过 `Application.Current.Resources` 动态获取颜色。这确保了 C# 代码和 XAML 视图使用同一套颜色定义。
*   **标准化命名**：在 `DarkTheme.xaml` 和 `LightTheme.xaml` 中添加语义化的 Brush Key（如 `WindowBackgroundBrush`, `SideBarBackgroundBrush`），以匹配 `Theming.cs` 中使用的颜色逻辑。

### 3. 主题切换增强
*   **优化 `ThemeManager.cs`**：
    *   改进资源字典的加载和卸载逻辑，使其更健壮。
    *   确保在应用启动时自动加载 `DesignTokens.xaml`。
    *   优化主题切换时的资源刷新机制。

### 4. 具体执行步骤
1.  **创建 `DesignTokens.xaml`**：提取所有通用资源。
2.  **创建 `DarkTheme.xaml` & `LightTheme.xaml`**：基于现有文件重构，移除重复项，补充缺失的语义化 Brush。
3.  **重构 `Theming.cs`**：改为动态资源查找模式。
4.  **重构 `ThemeManager.cs`**：适配新的文件结构。
5.  **更新 `App.xaml`**：引入 `DesignTokens.xaml` 并移除旧引用。
6.  **清理旧文件**：删除 `ThemeConstants.xaml`, `CommonBorderStyle.xaml` 及旧的主题资源文件。

### 5. 预期成果
*   **代码量减少**：消除数百行重复的 XAML 代码。
*   **一致性提升**：UI 颜色完全由资源文件控制，修改一处即可全局生效。
*   **可维护性**：新增样式只需在 `DesignTokens.xaml` 定义一次。
*   **性能**：减少不必要的资源字典解析。

我已准备好执行上述重构。
