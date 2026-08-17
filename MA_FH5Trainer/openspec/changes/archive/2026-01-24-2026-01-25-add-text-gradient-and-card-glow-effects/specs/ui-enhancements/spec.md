## ADDED Requirements

### Requirement: 文字渐变效果
应用程序 SHALL 为应用标题和 Expander 标题等重要文字添加渐变色效果，以增强视觉吸引力。

#### Scenario: 应用标题显示渐变色
- **WHEN** 应用程序启动并显示主窗口
- **THEN** 应用标题文字 SHALL 显示为渐变色（从红色到橙色到黄色）
- **AND** 渐变色 SHALL 在深色和浅色主题下保持一致

#### Scenario: Expander 标题显示渐变色
- **WHEN** 用户查看功能区卡片
- **THEN** 每个 Expander 的标题文字 SHALL 显示为渐变色
- **AND** 渐变色 SHALL 与应用标题使用相同的配色方案

#### Scenario: 普通文本保持单色
- **WHEN** 用户查看信息文本或普通内容
- **THEN** 普通文本 SHALL 保持单色显示
- **AND** 普通文本 SHALL 不应用渐变色效果

### Requirement: 字号增强
应用程序 SHALL 统一增大各区域的字体字号，以提升可读性和用户体验。

#### Scenario: 应用标题字号增大
- **WHEN** 应用程序显示主窗口
- **THEN** 应用标题字号 SHALL 为 24 (FontSizeHuge)
- **AND** 字号 SHALL 使用主题资源中定义的常量，而非硬编码值

#### Scenario: 信息文本字号增大
- **WHEN** 用户查看信息文本（如版本信息、状态文本）
- **THEN** 信息文本字号 SHALL 为 12 (FontSizeSmall)
- **AND** 字号 SHALL 比原来的 11.5 增大 0.5

#### Scenario: 侧边栏标签字号增大
- **WHEN** 用户查看侧边栏
- **THEN** 侧边栏标签标题字号 SHALL 为 16 (FontSizeMedium)
- **AND** 侧边栏标签内容字号 SHALL 为 14 (FontSizeNormal)

#### Scenario: Expander 标题字号增大
- **WHEN** 用户查看功能区卡片
- **THEN** Expander 标题字号 SHALL 为 14 (FontSizeNormal)
- **AND** 字号 SHALL 比原来的 11.5 增大 2.5

#### Scenario: 子页面功能项字号增大
- **WHEN** 用户展开 Expander 查看功能项
- **THEN** 子页面功能项字号 SHALL 为 12 (FontSizeSmall)
- **AND** 字号 SHALL 比原来的默认值增大

### Requirement: 卡片边框发光效果
应用程序 SHALL 为功能区卡片（Expander）添加边框发光效果，以提供更好的视觉反馈。

#### Scenario: 卡片默认状态显示轻微发光
- **WHEN** 用户查看功能区卡片
- **THEN** 每个 Expander 卡片 SHALL 显示轻微的边框发光效果
- **AND** 发光颜色 SHALL 为橙色 (#FF8E53)
- **AND** 发光强度 SHALL 为中等 (Opacity=0.5)

#### Scenario: 卡片悬停时显示增强发光
- **WHEN** 用户将鼠标悬停在 Expander 卡片上
- **THEN** 卡片边框发光效果 SHALL 增强
- **AND** 发光强度 SHALL 增加 (Opacity=0.8)
- **AND** 发光效果 SHALL 提供明显的视觉反馈

#### Scenario: 卡片展开时显示强发光
- **WHEN** 用户点击并展开 Expander 卡片
- **THEN** 卡片边框 SHALL 显示强发光效果
- **AND** 发光强度 SHALL 达到最大 (Opacity=1.0)
- **AND** 发光效果 SHALL 持续显示，直到卡片收起

#### Scenario: 发光效果在主题切换时保持一致
- **WHEN** 用户切换主题（深色 ↔ 浅色）
- **THEN** 卡片边框发光效果 SHALL 保持一致
- **AND** 发光颜色 SHALL 在深色和浅色主题下相同
- **AND** 发光强度 SHALL 在深色和浅色主题下相同

### Requirement: 主题资源扩展
应用程序 SHALL 在主题资源文件中定义新的字号常量、渐变色资源和发光效果资源。

#### Scenario: 深色主题包含新资源
- **WHEN** 应用程序使用深色主题
- **THEN** DarkThemeResources.xaml SHALL 包含新的字号常量
- **AND** DarkThemeResources.xaml SHALL 包含文字渐变色资源
- **AND** DarkThemeResources.xaml SHALL 包含发光效果资源

#### Scenario: 浅色主题包含新资源
- **WHEN** 应用程序使用浅色主题
- **THEN** LightThemeResources.xaml SHALL 包含新的字号常量
- **AND** LightThemeResources.xaml SHALL 包含文字渐变色资源
- **AND** LightThemeResources.xaml SHALL 包含发光效果资源

#### Scenario: 资源键命名规范
- **WHEN** 开发人员定义新的资源
- **THEN** 资源键 SHALL 使用 PascalCase 命名
- **AND** 资源键 SHALL 包含描述性名称（如 TitleGradientBrush、CardGlowEffect）
- **AND** 资源键 SHALL 在深色和浅色主题中保持一致