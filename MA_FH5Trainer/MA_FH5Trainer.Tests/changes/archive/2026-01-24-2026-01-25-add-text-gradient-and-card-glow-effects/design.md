## Context

MA_FH5Trainer 是一个 Forza Horizon 5 的游戏修改器，使用 WPF 框架构建。当前主界面存在以下问题：

1. **文字字号偏小**：当前使用的字号（如 11.5、12.5、13.5）在长时间使用时容易产生视觉疲劳，可读性不足
2. **缺乏视觉吸引力**：界面缺少动态效果和视觉亮点，用户体验较为单调
3. **缺少视觉反馈**：功能区卡片（Expander）没有明显的悬停或激活状态的视觉反馈

通过添加文字渐变效果、增大字号以及卡片边框发光效果，可以提升界面的视觉吸引力和用户使用体验。

## Goals / Non-Goals

### Goals
- 为应用标题和重要文字添加渐变色效果，增强视觉吸引力
- 统一增大各区域的字体字号，提升可读性和用户体验
- 为功能区卡片添加边框发光效果，提供更好的视觉反馈
- 确保深色和浅色主题下的效果一致性和适配性
- 保持现有功能的完整性，不影响现有功能的使用

### Non-Goals
- 不改变现有的主题系统架构
- 不添加新的主题（仅增强现有主题）
- 不改变现有的 MVVM 架构
- 不添加复杂的动画效果（仅添加简单的发光效果）
- 不改变现有的窗口布局和结构

## Decisions

### Decision 1: 文字渐变效果实现方式

**决策**: 使用 WPF 的 `LinearGradientBrush` 为文字添加渐变效果

**实现方式**:
```xml
<LinearGradientBrush x:Key="TitleGradientBrush" StartPoint="0,0" EndPoint="1,0">
    <GradientStop Color="#FF6B6B" Offset="0"/>
    <GradientStop Color="#FF8E53" Offset="0.5"/>
    <GradientStop Color="#FFC75F" Offset="1"/>
</LinearGradientBrush>
```

**应用方式**:
- 为 TextBlock 的 `Foreground` 属性绑定渐变色资源
- 仅应用于应用标题和 Expander 标题等重要文字
- 普通文本保持单色，确保可读性

**Alternatives considered**:
- 使用 `RadialGradientBrush` - 会产生圆形渐变，不适合文字
- 使用 `DrawingBrush` - 实现复杂，性能开销大
- 使用第三方库 - 增加依赖，不必要

### Decision 2: 字号调整策略

**决策**: 在主题资源中定义新的字号常量，统一使用资源键而非硬编码值

**新增字号资源**:
```xml
<system:Double x:Key="FontSizeExtraSmall">10</system:Double>
<system:Double x:Key="FontSizeSmall">12</system:Double>
<system:Double x:Key="FontSizeNormal">14</system:Double>
<system:Double x:Key="FontSizeMedium">16</system:Double>
<system:Double x:Key="FontSizeLarge">18</system:Double>
<system:Double x:Key="FontSizeExtraLarge">20</system:Double>
<system:Double x:Key="FontSizeHuge">24</system:Double>
```

**字号映射**:
- 应用标题: `FontSizeHuge` (24)
- 信息文本: `FontSizeSmall` (12)
- 侧边栏标签标题: `FontSizeMedium` (16)
- 侧边栏标签内容: `FontSizeNormal` (14)
- Expander 标题: `FontSizeNormal` (14)
- 子页面功能项: `FontSizeSmall` (12)

**Alternatives considered**:
- 保持硬编码值 - 不利于主题切换和维护
- 使用相对字号单位 - WPF 支持有限，兼容性问题
- 动态计算字号 - 增加复杂度，性能开销

### Decision 3: 卡片边框发光效果实现方式

**决策**: 使用 WPF 的 `DropShadowEffect` 实现卡片边框发光效果

**实现方式**:
```xml
<DropShadowEffect x:Key="CardGlowEffect"
                  Color="#FF8E53"
                  BlurRadius="5"
                  ShadowDepth="0"
                  Direction="0"
                  Opacity="0.5" />
```

**应用方式**:
- 为 Expander 的 `Effect` 属性绑定发光效果资源
- 在悬停时增强发光效果（增加 Opacity）
- 在激活/展开状态使用更强的发光效果

**Alternatives considered**:
- 使用 `OuterGlowBitmapEffect` - 已过时，性能差
- 使用自定义 ShaderEffect - 实现复杂，兼容性问题
- 使用 Border 嵌套 - 增加布局复杂度，性能开销

### Decision 4: 渐变色方案

**决策**: 为深色和浅色主题分别定义不同的渐变色方案

**深色主题渐变色**:
- 标题渐变: `#FF6B6B` → `#FF8E53` → `#FFC75F` (暖色调渐变)
- 发光颜色: `#FF8E53` (橙色)

**浅色主题渐变色**:
- 标题渐变: `#FF6B6B` → `#FF8E53` → `#FFC75F` (与深色主题相同，确保一致性)
- 发光颜色: `#FF8E53` (橙色)

**Alternatives considered**:
- 深色和浅色主题使用完全不同的渐变色 - 增加维护成本
- 使用蓝色系渐变 - 不符合游戏修改器的暖色调风格
- 使用彩虹渐变 - 过于花哨，影响可读性

## Risks / Trade-offs

### Risk 1: 性能影响

**风险**: 添加渐变效果和发光效果可能影响 UI 渲染性能

**缓解措施**:
- 仅对少量重要元素应用渐变效果
- 使用硬件加速（WPF 默认启用）
- 限制发光效果的 BlurRadius 和 ShadowDepth
- 在低性能设备上提供禁用选项（可选）

### Risk 2: 兼容性问题

**风险**: 某些旧版本的 Windows 或显卡可能不支持某些效果

**缓解措施**:
- 使用 WPF 标准效果，避免使用实验性 API
- 提供降级方案（如效果不支持时使用纯色）
- 在多种 Windows 版本上测试

### Risk 3: 可读性问题

**风险**: 渐变色可能影响文字的可读性

**缓解措施**:
- 选择高对比度的渐变色
- 仅对标题和大字号文字应用渐变
- 保持普通文本为单色
- 在浅色背景下使用较深的渐变色

### Risk 4: 主题一致性

**风险**: 新效果可能与现有主题风格不协调

**缓解措施**:
- 参考现有的主题配色方案
- 使用与现有主题协调的颜色
- 在深色和浅色主题上都进行测试
- 保持设计风格的一致性

## Migration Plan

### 实施步骤

1. **阶段 1: 设计规范定义**
   - 定义新的字号常量
   - 定义渐变色方案
   - 定义发光效果规范

2. **阶段 2: 主题资源实现**
   - 修改 DarkThemeResources.xaml
   - 修改 LightThemeResources.xaml
   - 创建新的样式资源

3. **阶段 3: UI 组件更新**
   - 更新 MainWindow.xaml
   - 更新 ExpandersView.xaml
   - 更新 CommonBorderStyle.xaml

4. **阶段 4: 测试和验证**
   - 测试深色主题
   - 测试浅色主题
   - 测试主题切换
   - 性能测试

### 回滚方案

如果新效果存在问题，可以通过以下方式回滚：

1. **快速回滚**: 注释掉新添加的样式和效果资源
2. **部分回滚**: 仅禁用发光效果，保留字号调整
3. **完全回滚**: 恢复到变更前的状态（通过 Git 版本控制）

### 验收标准

- [ ] 所有文字字号已增大，可读性明显提升
- [ ] 应用标题和 Expander 标题显示渐变色效果
- [ ] 功能区卡片在悬停时显示边框发光效果
- [ ] 深色和浅色主题下的效果一致
- [ ] 主题切换时效果正常
- [ ] UI 渲染性能无明显下降
- [ ] 所有页面和组件显示正常

## Open Questions

1. **发光效果的强度**: 当前设计的发光效果强度是否合适？是否需要提供可配置选项？
2. **渐变色的选择**: 当前选择的暖色调渐变是否符合用户的审美偏好？
3. **性能监控**: 是否需要添加性能监控机制，在性能下降时自动降级？
4. **用户反馈**: 是否需要添加用户反馈机制，收集用户对新效果的意见？