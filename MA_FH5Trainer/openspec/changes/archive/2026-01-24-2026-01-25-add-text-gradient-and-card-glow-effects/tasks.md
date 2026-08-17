## 1. 设计规范定义
- [ ] 1.1 定义文字渐变色方案（深色主题和浅色主题）
- [ ] 1.2 定义新的字号常量（FontSizeExtraLarge, FontSizeHuge 等）
- [ ] 1.3 定义卡片边框发光效果的规范（颜色、强度、触发条件）

## 2. 主题资源实现（深色主题）
- [ ] 2.1 在 DarkThemeResources.xaml 中添加新的字号资源
- [ ] 2.2 在 DarkThemeResources.xaml 中添加文字渐变色资源
- [ ] 2.3 在 DarkThemeResources.xaml 中添加发光效果资源（DropShadowEffect）
- [ ] 2.4 创建文字渐变样式（TextGradientStyle）

## 3. 主题资源实现（浅色主题）
- [ ] 3.1 在 LightThemeResources.xaml 中添加新的字号资源
- [ ] 3.2 在 LightThemeResources.xaml 中添加文字渐变色资源
- [ ] 3.3 在 LightThemeResources.xaml 中添加发光效果资源（DropShadowEffect）
- [ ] 3.4 创建文字渐变样式（TextGradientStyle）

## 4. 主界面UI更新
- [ ] 4.1 更新 MainWindow.xaml 中的应用标题字号
- [ ] 4.2 更新 MainWindow.xaml 中的信息文本字号
- [ ] 4.3 为应用标题添加文字渐变效果
- [ ] 4.4 更新侧边栏标签的字号

## 5. 功能区卡片更新
- [ ] 5.1 更新 ExpandersView.xaml 中的 Expander 样式
- [ ] 5.2 为 Expander 添加边框发光效果
- [ ] 5.3 更新 Expander 标题字号
- [ ] 5.4 添加卡片悬停时的发光效果增强

## 6. 样式组件更新
- [ ] 6.1 更新 CommonBorderStyle.xaml 添加发光效果支持
- [ ] 6.2 为子页面的功能项添加边框发光效果

## 7. 翻译支持
- [ ] 7.1 检查是否需要添加新的翻译键（如需要）
- [ ] 7.2 更新 ChineseSimplified.xaml（如需要）
- [ ] 7.3 更新 English.xaml（如需要）

## 8. 测试和验证
- [ ] 8.1 测试深色主题下的文字渐变效果
- [ ] 8.2 测试浅色主题下的文字渐变效果
- [ ] 8.3 测试字号调整后的可读性
- [ ] 8.4 测试卡片边框发光效果
- [ ] 8.5 测试主题切换时的效果一致性
- [ ] 8.6 测试性能影响（确保动画流畅）
- [ ] 8.7 验证所有页面和组件的显示效果