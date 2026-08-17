## 目标
- 优化 Handling 页面中 RT/LT 阈值输入框背景观感与可读性。
- 优化“名称伪装（QuickNameBox）”输入框背景（避免过亮/过突兀），并与页面其它输入一致。
- 让页面内输入框“数值/文字”在深色主题下呈现白色且更清晰（避免 12px 导致偏小或发虚）。
- 优化轮速组件（模式下拉、数值/限制输入、开关）的整体一致性。
- 全面检查 Handling 页面文字效果：统一字体、字号、ClearType/Display 渲染；把过小或模糊的文本提升到 FontSizeNormal。

## 现状定位（已确认）
- RT/LT 阈值 NumericUpDown 当前使用 HandlingAccentNumericUpDownStyle（背景为 AccentBase、文字为 #000000）：[Handling.xaml](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.xaml#L34-L69)
- “名称伪装”输入框 QuickNameBox 使用 HandlingAccentTextBoxStyle（同样是 AccentBase + 黑字）：[Handling.xaml](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.xaml#L970-L987)
- 轮速组件 WheelspeedModeBox / WheelspeedValueBox / WheelspeedLimit 使用 Accent 风格，且部分 ToggleSwitch 仍手写 FontSize=12：同文件 Wheelspeed 区域与后续区块。
- Handling 页面仍有少量 ToggleSwitch/Label 未接入统一样式，存在 12px 文本偏小的问题（例如超级刹车、刹车增强、部分快捷开关）：[Handling.xaml](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.xaml#L723-L878)

## 方案（不改动业务逻辑，仅样式/排版）
### 1) 为“深色主题白字可读”新增一套输入控件样式（HandlingResources）
- 在 [HandlingResources.xaml](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/HandlingResources.xaml) 新增：
  - `HandlingCompactNumericUpDownStyle`：基于 HandlingNumericUpDownStyle，保持当前页面使用习惯（宽度较窄、TextAlignment/ContentAlignment 居中、Padding 更紧凑），Foreground 使用 `ForegroundBrush`（深色主题下为白）。
  - `HandlingCompactComboBoxStyle`：基于 HandlingComboBoxStyle，保证下拉框文字与边框/背景一致，ItemContainerStyle 统一。
  - `HandlingCompactTextBoxStyle`：基于 HandlingTextBoxStyle，让 QuickNameBox/输入窗 TextBox 背景更克制（InputBackgroundBrush），文字更清晰。

### 2) RT/LT 阈值输入框背景优化
- 将 RT/LT 阈值 NumericUpDown 从 `HandlingAccentNumericUpDownStyle` 切换到 `HandlingCompactNumericUpDownStyle`。
- 目标效果：背景回归输入底色（Input/ControlBackgroundBrush）、焦点/悬停时边框高亮，深色主题下数字为白色。

### 3) “名称伪装”输入框背景优化
- 将 QuickNameBox 从 `HandlingAccentTextBoxStyle` 切换到 `HandlingCompactTextBoxStyle`。
- 同步让文字遵循 `FontSizeNormal` 与 Display/ClearType，避免发虚。

### 4) 轮速组件样式优化
- WheelspeedModeBox 改用 `HandlingCompactComboBoxStyle`（并统一 ItemContainerStyle）。
- WheelspeedValueBox / WheelspeedLimit 改用 `HandlingCompactNumericUpDownStyle`，确保深色主题数值为白。
- WheelSwitch 及同区 ToggleSwitch 全部改用 `HandlingToggleSwitchCompactStyle`，移除手写 `FontSize="12"`，统一到 `FontSizeNormal`。

### 5) 全页面文字清晰度与统一性巡检
- Handling.xaml 中所有仍手写 `FontSize="12"` 的 ToggleSwitch/Label 全部切换为现有统一样式（`HandlingToggleSwitchCompactStyle`、`HandlingInlineLabelStyle`、`HandlingTextBlockStyle`/`HandlingDiagnosticTextBlockStyle`）。
- 对未套用 `HandlingSliderStyle` 的 Slider（例如 StopSlider）补齐样式，保证轨道/Thumb 与深浅主题一致。

### 6) 同步修正输入弹窗（用于重命名预设）
- [InputWindow.xaml](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/Windows/InputWindow.xaml) 的 InputTextBox/按钮接入 HandlingResources 的统一 TextBox/Button 样式，使“名称相关输入框”整体一致。

## 影响范围
- 仅影响 Handling 页面及与其强相关的输入弹窗/预设管理窗口的视觉，不改动任何内存写入/热键/逻辑代码。

## 验证
- 编译解决方案（Debug）确保 XAML/样式无资源缺失。
- 手动检查（深色/浅色主题切换）确认：
  - RT/LT 阈值背景更舒适、数字清晰。
  - QuickNameBox 背景不刺眼，文字不模糊。
  - 轮速区控件在同一视觉体系内。
  - 页面无 12px 过小文本残留。

如果你确认该方案，我会按上述步骤落地修改相关 XAML/样式资源并完成编译验证。