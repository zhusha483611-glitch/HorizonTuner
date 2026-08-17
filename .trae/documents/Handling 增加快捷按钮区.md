## 目标
- 在 Handling 页“速度修改”区域的最下方新增一个“快捷功能”区，把以下常用功能从其它页面完整复制过来，避免频繁切换：
  1) 实验性：名称伪装（NameSpoofer）
  2) 实验性：无限技能连击（UnbreakableSkillScore）
  3) 倍率区域：技能分数倍率（SkillScoreMultiplier）

## 现有实现位置（用于复制）
- 名称伪装 UI/事件：
  - XAML： [Misc.xaml:L23-L42](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Misc.xaml#L23-L42)
  - 事件逻辑： [Misc.xaml.cs:L29-L70](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Misc.xaml.cs#L29-L70)
  - detour： [MiscCheats.CheatName](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Cheats/MiscCheats.cs#L45-L102)
- 无限技能连击 UI/事件：
  - XAML： [Misc.xaml:L102-L114](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Misc.xaml#L102-L114)
  - 事件逻辑： [Misc.xaml.cs:L415-L438](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Misc.xaml.cs#L415-L438)
  - detour： [MiscCheats.CheatUnbreakableSkillScore](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Cheats/MiscCheats.cs#L374-L402)
- 技能分数倍率 UI/事件：
  - XAML： [Multipliers.xaml:L166-L202](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Multipliers.xaml#L166-L202)
  - 事件逻辑： [Multipliers.xaml.cs:L192-L227](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Multipliers.xaml.cs#L192-L227)
  - detour： [MiscCheats.CheatSkillScoreMultiplier](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Cheats/MiscCheats.cs#L164-L192)

## UI 方案（Handling.xaml）
- 在 [Handling.xaml](file:///d:/AI/MA_FH5Trainer_CN-main/MA_FH5Trainer/MA_FH5Trainer/Views/SubPages/SelfVehicle/Handling.xaml) 中，“速度”Border后、其他模块前插入一个新的 Border：
  - 标题/说明：例如“快捷功能（常用）”。
  - 内含三块控件：
    1) 名称伪装：TextBox（名字输入）+ ToggleSwitch（名称伪装开关）
    2) 无限技能连击：ToggleSwitch
    3) 技能分数倍率：NumericUpDown（倍率值）+ ToggleSwitch
- 资源文本：优先复用现有 `DynamicResource NameSpoofer` / `DynamicResource UnbreakableSkillScore`，技能倍率直接显示“技能分数倍率”（保持与 Multipliers 页面一致）。
- 可用性：统一用 `IsEnabled="{Binding ViewModel.AreUiElementsEnabled}"`（Handling 已有）控制扫描/初始化期间不可操作。

## 逻辑方案（Handling.xaml.cs）
- 新增 `MiscCheats` 获取入口：在 Handling 页面增加 `private static MiscCheats MiscCheatsFh5 => GetClass<MiscCheats>();`。
- 复制并改名实现 5 个事件处理方法（与 Handling 控件名对应）：
  - 名称伪装开关：等价于 `NameSpooferSwitch_OnToggled`：
    - 如果 `NameDetourAddress==0`：调用 `CheatName()`
    - 写 `NameDetourAddress + 0x55`（开关 byte）
    - 写 `NameDetourAddress + 0x56`（Unicode 名字 bytes）
  - 名称输入变化：等价于 `NameBox_OnTextChanged`：写入 `+0x56`
  - 无限技能连击开关：等价于 `UnbreakableSkillScoreSwitch_OnToggled`：
    - 如无地址调用 `CheatUnbreakableSkillScore()`
    - 写 `UnbreakableSkillScoreDetourAddress + 0x1A`
  - 技能分数倍率开关：等价于 `SkillHackToggle_Toggled`：
    - 如无地址调用 `CheatSkillScoreMultiplier()`
    - 写 `SkillScoreMultiplierDetourAddress + 0x1D`（int 值）
    - 写 `SkillScoreMultiplierDetourAddress + 0x1C`（开关 byte）
  - 技能分数倍率数值变化：等价于 `SkillBox_ValueChanged`：写 `+0x1D`
- 扫描期间的 UI 处理：沿用 Handling 的 `ViewModel.AreUiElementsEnabled=false/true` 包裹 AoB/Detour 初始化，保证交互一致。

## 命名与冲突避免
- Handling 新增控件使用独立 x:Name（例如 `QuickNameBox`、`QuickNameSpooferSwitch`、`QuickUnbreakableSkillSwitch`、`QuickSkillScoreBox`、`QuickSkillScoreToggle`），避免与现有控件名冲突。

## 验证
- 编译验证：`dotnet build`。
- 运行验证：在 Handling 页面底部直接操作三个功能，确认与原页面行为一致：
  - 名称伪装：开关后输入文本立即生效。
  - 无限技能连击：开关后连击不断。
  - 技能分数倍率：数值改变即时写入，开关控制启用。

按以上方案我会直接开始修改 Handling.xaml / Handling.xaml.cs，复制这些控件与事件逻辑到“速度修改最下面的位置”。