# 全键盘与 Numpad 重设计方案

> **状态**：v1.0 已实施｜2026-11-07｜代码、测试与 `docs/layouts.md` 已按本文落地
> **范围**：全键盘 AnsiStyle / PhoneHybrid、符号页、图标、5×4 Numpad、槽位映射
> **取代**：旧方案中的 D1（全键盘形态）与 D2（数字盘 A/C 双布局）以本文为准；`decisions.md` 的 D1/D2 已按本文更新。

---

## 0. 结论

你的思路整体可行，而且新的 5×4 Numpad 比当前布局更统一：

- 全键盘 AnsiStyle 需要**真正重做宽度**，不是把现有布局改个名字；
- PhoneHybrid 改成 4 行手机布局，符号页由全键盘负责；
- 退格/清空/回车/Shift/方向/增减都可以用矢量图标；
- 新 Numpad 是 5 列×4 行、所有键 1 单位、确定 1 单位在右下角；
- “空位时 0 变长 / 确定变高”需要真正的网格跨列/跨行；
- 数字盘不再负责符号页，符号页归全键盘；
- `Numpad` 作为唯一启用的数字盘；原 `Phone` 保留并更名为 `Sequence`。

### 命名与概念

| 概念 | 最终名称 | 说明 |
|---|---|---|
| 全键盘（严格 ANSI） | `AnsiStyle` | 原 `AnsiStrict` 更名；5 行、每行 15 单位 |
| 全键盘（手机式） | `PhoneHybrid` | 4 行、无数字行、带符号/数字入口 |
| 数字盘（唯一启用） | `Numpad` | 5×4 槽位布局，所有键 1 单位 |
| 数字盘（保留） | `Sequence` | 原 `Phone` 5×6 布局，暂不启用 |
| 图标 | `KeyGlyph` | 核心只给语义图标名，Avalonia 画矢量 |
| 全键盘页面 | `FullKeyboardPage` | `Main` / `Symbols` |
| 符号页 | `SymbolPage` | 只属于全键盘，数字盘不打开 |
| 数字盘槽位 | `NumpadSlots` | 场景驱动的左/右/小数位映射 |

---

## 1. AnsiStyle（严格 ANSI）

### 1.1 几何

5 行，所有行合计 **15 单位**。Tab / Caps / IME 用空白 Spacer 占位，没有命令、不可点击。

| 行 | 键与宽度 | 合计 |
|---|---|---|
| 0 | `` ` ``1 + `1..0`10 + `-`1 + `=`1 + `⌫`2 | 15 |
| 1 | **Tab 空位 1.5** + `Q..P`10 + `[`1 + `]`1 + `\`1.5 | 15 |
| 2 | **Caps 空位 1.75** + `A..L`9 + `;`1 + `'`1 + `Enter`2.25 | 15 |
| 3 | 左 `Shift`2.25 + `Z..M`7 + `, . /`3 + 右 `Shift`2.75 | 15 |
| 4 | `123`1.5 + `←`1.25 + `→`1.25 + `Space`5.5 + **IME 空位 1.5** + `清空`1.5 + `确定`2.5 | 15 |

说明：

- Tab/Caps 的位置与物理 ANSI 一致；QWERTY 行和 A 行不再“缺一块”。
- IME 空位放在 `Space` 与 `清空` 之间，后续加中/英切换时替换。
- `Enter`、`Shift`、`Backspace`、`Clear` 使用图标（见第 4 节）。
- `123` 进入 Numpad；AnsiStyle 的主键面已有全部 ANSI 符号，Shift 就是符号层，**v1 不另设独立符号页**。如果后续希望 AnsiStyle 也有符号页，可复用 PhoneHybrid 的 `SymbolPage`，但会多一个入口，需要再确认。
- 默认 `FullKeyboardLayout` 改为 `AnsiStyle`。

### 1.2 Spacer 语义

Tab/Caps/IME 使用 `KeyStyle.Spacer`，没有 `KeyCommand`：

- 不产生点击命中；
- 不进入焦点/无障碍树；
- 只参与宽度和几何对齐。

不建议把它们做成“禁用按钮”，否则会出现可聚焦的死键和无意义的读屏项。

---

## 2. PhoneHybrid（手机式，4 行）

### 2.1 主页面

4 行，每行 10 单位（底部行按 10 单位设计）：

| 行 | 键与宽度 | 合计 |
|---|---|---|
| 0 | `Q W E R T Y U I O P` 10 | 10 |
| 1 | `A S D F G H J K L` 9 | 9 |
| 2 | `Shift`1.5 + `Z X C V B N M`7 + `⌫`1.5 | 10 |
| 3 | `符号`1.25 + `123`1.25 + `←`1 + `→`1 + `Space`2.0 + `IME`空位1 + `清空`1 + `确定`1.5 | 10 |

- 无数字行、无 ANSI 标点行；数字/符号由底部入口进入。
- `符号` 在 `123` 左侧，满足“符号键入口在数字键入口左侧”。
- 第 1 行 9 个单位，按行缩放后键会比第 0 行略宽；这是手机键盘的常见做法。
- `IME` 空位同样用 Spacer 预留。
- `Shift`/`⌫`/`清空` 用图标；`符号`/`123` 用本地化文字。

### 2.2 符号页

符号页是**全键盘页面**，不是数字盘页面；由 `符号` 键进入，`ABC` 返回主页面，`123` 进入 Numpad。

建议 6 列×5 行：

```text
~ ! @ # $ %
^ & * ( ) _
- + = [ ] {
} \ | ; : '
ABC 123 Space ⌫ 清空 确定
```

- 24 个常用符号 + 1 行功能键。
- `ABC` 回主页面；`123` 进 Numpad；`Space`/`⌫`/`清空`/`确定` 保持可用。
- 这是“常用符号”的初始集合，后续可增删；不需要把 ANSI 全部符号塞进来。
- `FullKeyboardPage` 状态为 `Main` / `Symbols`；数字盘永远不切到 `Symbols`。
- 如果 AnsiStyle 以后也要独立符号页，复用同一 `SymbolPage`，入口另议。

---

## 3. Numpad（5×4，唯一启用的数字盘）

### 3.1 基础格子

所有键 1 单位，确定 1 单位在右下角：

```text
[L0 ▲/+]  [7] [8] [9] [R0 ⌫]
[L1 ▼/-]  [4] [5] [6] [R1 清空]
[L2 ABC/±/空] [1] [2] [3] [R2 场景/空]
[L3 ←]    [→] [0] [./,/:/空] [确定]
```

- 左列：数值增加、数值减少、返回全键盘/场景额外、光标向左。
- 右列：退格、清空、符号/场景键、确定。
- `1` 下方是 `→`，`2` 下方是 `0`，`3` 下方是小数/分隔符。
- 所有格子等宽；行总单位 5。
- `R0`/`R1` 用图标；`L3`/`→` 用图标；`L0`/`L1` 在步进模式下是 ▲/▼ 图标。

### 3.2 空位合并规则

| 情况 | 表现 |
|---|---|
| 底部小数位为空（整数、部分日期/IP） | `0` 跨 2 列（占 `2` 和 `3` 下方两格），即 `ColumnSpan=2` |
| 右列 Row2 为空（无场景键） | `确定` 跨 2 行（占 Row2/Row3 右列），即 `RowSpan=2` |
| 右列 Row2 有场景键 | `确定` 只占右下 1 格 |
| 左列 Row2 为空 | 只影响左列，不影响确定高度 |

这要求 Numpad 用**真正的 5×4 网格 + 跨列/跨行**，不能继续用“每行一个独立 StackPanel”的渲染方式。`KeyDefinition` 已有 `ColumnSpan`/`RowSpan` 字段，但当前 `KeyboardLayoutView` 没有使用它们；实现时要补一个 Numpad 网格视图（或把布局模型升级为 cell 模型）。

### 3.3 场景槽位映射

| 场景 | L0/L1 | L2（左 Row2） | R2（右 Row2） | 底部小点位 |
|---|---|---|---|---|
| 通用数字（文本字段） | `+` / `-`（ShowStepKeys=false） | `ABC` 返回全键盘 | 空（确定变高） | `.` |
| 正整数 | `▲` / `▼` | 空 | 空（确定变高） | 空（0 变长） |
| 有符号整数 | `▲` / `▼` | `±` | 空（确定变高） | 空（0 变长） |
| 正小数 | `▲` / `▼` | 空 | 空（确定变高） | `.` |
| 有符号小数 | `▲` / `▼` | `±` | 空（确定变高） | `.` |
| 日期 | `▲` / `▼` | 空 | 空/场景额外键 | `/` |
| IPv4 | `▲` / `▼` | 空 | 空/场景额外键 | `.` |
| 时间（未来） | `▲` / `▼` | 空 | 空/场景额外键 | `:` |

规则：

- `ShowStepKeys=true` 时，左顶两键是 ▲/▼；`false` 时替换为 `+`/`-`，对应你的第 4 条。
- 通用数字（来自全键盘）默认 `ShowStepKeys=false`，所以左上是 `+`/`-`；纯数字档默认开启步进。
- 通用数字不再有“符号切换”；右侧 Row2 为空，确定变高。符号由全键盘的 `符号` 入口负责。
- 纯数字场景不需要“返回全键盘”时，L2 用来放 `±` 或场景额外键；没有额外键就留空。
- 日期/IP 的小数位换成 `/` 或 `.`；右 Row2 可以留空让确定变高，也可以放场景额外键（例如日期分隔符的备选、时间冒号等）。
- 所有“空”都用 Spacer 或跨列/跨行，不生成 disabled 真键。

### 3.4 Sequence（保留）

- 原 `Phone` 5×6 数字/符号布局不删除，重命名为 `Sequence`。
- `NumericKeypadLayoutKind` 改为 `Numpad`（默认、唯一启用）和 `Sequence`（保留、暂不启用）。
- `Sequence` 暂不接入任何档位；保留代码和测试，后续用于序列号/特殊输入场景。

---

## 4. 图标方案

### 4.1 核心

新增 `KeyGlyph` 枚举，`KeyDefinition` 增加可选 `Glyph`。图标键仍保留 `KeyLabel.Resource(...)` 作为：

- 无障碍名称；
- 本地化 fallback；
- 未来字体/图标资源缺失时的兜底文字。

建议的 `KeyGlyph`：

```text
Backspace
Clear
Enter
Shift
ShiftFilled
ArrowLeft
ArrowRight
ArrowUp
ArrowDown
Keyboard      // 返回全键盘
Symbol        // 符号入口（可选）
Numeric       // 数字入口（可选）
Ime           // 预留
```

### 4.2 Avalonia

- `TouchKey` 用矢量 `Path`/`Geometry` 绘制图标，不用 Unicode `⌫`/`⏎`/`⇧`，避免字体覆盖差异。
- Shift 用空心/实心两态（或主题高亮）。
- `清空` 用橡皮/清除图形，和 `⌫` 明确区分。
- 图标键的 `AutomationProperties.Name` 使用本地化 resource key。
- 图标尺寸 24×24 设计，按键盘高缩放；文字键继续用现有 TextBlock。
- 退格、清空、回车、Shift、←/→、▲/▼ 全部图标化；`确定` 暂保留“确定”文字（1 单位在 1024 宽下足够；小屏可再评估 ✓ 图标）。
- IME 槽位暂时是 Spacer，不画图标；实现中英切换时替换为地球/中英图标。

---

## 5. 技术影响

### 5.1 布局模型

当前 `KeyboardLayout` 是“行 + 顺序键”，`KeyboardLayoutView` 每行一个独立 Grid：

- 可以表达 AnsiStyle/PhoneHybrid 的 Spacer 和宽度；
- **不能表达 Numpad 的 `0 跨 2 列`、`确定 跨 2 行`**，因为行与行之间是分开的 Grid。

建议：

- 全键盘继续用 row-based `KeyboardLayout`；
- Numpad 新增 `KeyboardGridLayout` / cell 模型（5 列×4 行 + ColumnSpan/RowSpan），或让 `KeyboardLayoutView` 支持一个真正的 Grid 模式；
- `NumpadSlots` 负责把场景映射成 cells；
- 复用 `KeyDefinition`，不复制键视图逻辑；
- `KeyboardLayoutView` 与新的 `NumpadGridView` 共用 `TouchKey` 和主题。

### 5.2 会话/页面状态

- `FullKeyboardPage`：`Main` / `Symbols`；
- 新增命令：`SwitchToSymbolPage`、`SwitchToMainPage`（或 `ToggleFullKeyboardPage`）；
- `KeyboardState` 增加当前全键盘页面；
- `KeyboardSession.BuildLayout()` 按 `Mode + FullKeyboardPage + LayoutKind` 选择布局；
- 数字盘永不进入 `Symbols`。

### 5.3 尺寸/Host

- 固定键盘区高度仍以 **5 行**为基准；
- AnsiStyle 5 行填满；
- PhoneHybrid 4 行、Numpad 4 行在区域中垂直居中，切换时面板高度不跳；
- `ResolveKeyHeight` 的 72/64/56 逻辑保留；
- 如果以后加 6 行符号页，需要重新评估固定高度；当前符号页设计为 5 行。

### 5.4 对现有代码的改动点

| 文件 | 改动 |
|---|---|
| `FullKeyboardLayoutKind.cs` | `AnsiStrict` → `AnsiStyle` |
| `NumericKeypadLayoutKind.cs` | `Phone` → `Sequence`；`Numpad` 语义改为新 5×4 |
| `KeyboardLayouts.cs` | 实现 AnsiStyle/PhoneHybrid/SymbolPage；重写 Numpad；保留 Sequence |
| `KeyDefinition.cs` | 增加 `KeyGlyph` |
| `KeyboardState.cs` / `KeyboardSession.cs` | 全键盘页面状态与切换命令 |
| `NumericKeypadOptions.cs` | 逐步被 `NumpadSlots` 取代；Sequence 保留旧选项 |
| `KeyboardLayoutView.cs` / `TouchKey.cs` | 网格跨列/跨行；图标绘制 |
| `TouchKeyboardHost.cs` | 全键盘页面切换、固定 5 行高度、图标主题 |
| 测试 | AnsiStyle 行宽/Spacer；PhoneHybrid 行数/入口；Numpad 槽位/跨列跨行；Sequence 保留；图标 glyph |

---

## 6. 验收标准

1. `Full(AnsiStyle)` 不再抛异常；5 行均为 15 单位；Tab/Caps/IME Spacer 无命令。
2. `PhoneHybrid` 4 行；`符号` 在 `123` 左侧；符号页 `ABC` 返回、`123` 进 Numpad。
3. Numpad：5×4；`1` 下 `→`、`2` 下 `0`、`3` 下分隔符；确定 1 单位右下。
4. 底部小点位为空时 `0` 跨 2 列；右 Row2 为空时确定跨 2 行。
5. `ShowStepKeys=false` 时左上变为 `+`/`-`；`true` 时 ▲/▼。
6. 数字盘不出现符号页；符号页只从全键盘进入。
7. `Sequence` 仍可构建但不在任何内置档位中启用。
8. 图标键渲染为矢量图标，且有无障碍名称。
9. Headless 渲染/交互测试和现有核心测试更新后全绿。

---

## 7. 实施顺序（建议）

1. **Core 命名/数据**：AnsiStyle、PhoneHybrid、NumpadSlots、SymbolPage、KeyGlyph、FullKeyboardPage。
2. **Core 会话/布局**：页面切换、Numpad 网格 cell、Sequence 保留、测试。
3. **Avalonia**：TouchKey 图标、Numpad 网格视图、Host 页面切换、高度适配。
4. **测试/文档**：布局矩阵、Headless 截图、README/决策记录更新。

这是 P4 规模的一次布局改版，不是小修；建议分两次提交：先数据和图标，再网格/符号页，便于回滚。

---

## 8. 仍需确认

1. AnsiStyle 是否也需要独立符号页？本文建议暂不需要（Shift 已覆盖全部 ANSI 符号）。
   - 暂不需要。
2. PhoneHybrid 符号页的 24 个符号集合是否合适？可以按现场常用符号替换。
   - 合适。
3. `确定` 在 Numpad 1 单位下用文字还是 ✓ 图标？本文暂用文字。
   - 保持文字。
4. 通用数字（文本字段）的 `+`/`-` 是否作为普通字符插入；本文按“ShowStepKeys=false 时替换”处理。
   - 作为字符。
5. `Sequence` 的未来用途是否就是序列号/特殊输入？本文暂保留、不启用。
   - 暂保留、不启用。
6. ←/→、▲/▼ 图表化设计方向？
   - 与现有图标风格一致、需要区分光标左右、数字增减含义。

---

## 9. 第二轮调整（2026-11-07）

方案落地后，按评审意见做了一轮视觉与交互细化，几何与主题以 `docs/layouts.md` 为准：

1. **暗色模式**：宿主默认跟随应用的实际主题变体（`ActualThemeVariant`），暗色壳自动切到 `TouchKeyboardTheme.Dark` 并在变体变化时实时重绘；`TouchKeyboardOptions.Theme` 留空即自动，显式设置可固定主题。暗色下确定/关闭的前景色分别由 `PrimaryKeyForeground`、`DangerKeyForeground` 保证。
2. **字符居中**：新增主题度量 `KeyLabelOffset`，按字号比例下移标签（默认 2px @ `KeyFontSize`），修正数字/大写偏上的观感。
3. **按压反馈**：按键与标题栏按钮不再有悬停态（它们都不是 Button）；按下时叠加 `PressedOverlay` 并缩放 `PressedScale`（默认 0.96）。
4. **退格图标**：把 X 在标签内部左移。
5. **方向/增减图标**：左右改为带杆箭头，上下改为箭头 + `+`/`−` 徽标；`sizeFactor` 让紧凑图标整体缩小。候选方案见 `docs/images/icon-variants.png`，目前采用带杆箭头方案。
6. **空格与密码显隐**：`KeyGlyph` 新增 `Space`、`ShowPassword`、`HidePassword`；空格键与密码显隐按钮都用矢量图标，按钮保留本地化无障碍名称。
7. **Demo**：提供全键盘样式（AnsiStyle / PhoneHybrid）与界面主题（跟随系统 / 浅色 / 深色）切换。
8. **PhoneHybrid 微调**：四行均按 10 单位；A 行左右各 0.5 空位；Z 行 Shift/退格 1.5；底行符号、数字、光标、清空、IME 均 1 单位，空格 2.5，确定 1.5；光标键移到空格两侧。
9. **符号页**：移除清空与数字入口，功能行改为 `ABC ← Space → ⌫ 确定`；`~` 移到末尾，其余符号前移一位。
