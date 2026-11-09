# 变更记录

[English](CHANGELOG.md) · **简体中文**

本文件记录项目的全部重要变更。格式基于
[Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循
[语义化版本](https://semver.org/lang/zh-CN/)。

GitHub Release 说明由本文件的对应版本段落生成；英文完整历史见
[CHANGELOG.md](CHANGELOG.md)。

## [Unreleased]

## [0.3.0] - 2026-11-09

补全 PhoneHybrid 符号页。

### 新增

- PhoneHybrid 符号页现在收录完整的 32 个 US ANSI 符号，包括此前缺少的反引号、
  双引号、`<`、`>`、逗号、句号、问号和斜杠。
- 符号页功能行新增 `ABC` 右侧的 `123` 数字入口，以及退格左侧的清空键。
- 接入 IME 引擎且档位允许 IME 时，符号页清空槽位变成中/英切换键（`中/英` 或
  `CN/EN`）；没有引擎时仍是清空键。
- 中文 IME 模式下，有映射的符号键帽跟随语言显示全角标点（例如 `.`→`。`、
  `,`→`，`、`?`→`？`、`!`→`！`，以及方括号/花括号对）。命令仍插入 ASCII 源
  字符并由会话映射；英文模式或 `FullWidthPunctuation = false` 时键帽保持 ASCII。

### 变更

- 符号页改为 4×8 符号网格加一行独立的 8 键功能行：
  `ABC 123 ← Space → 清空 ⌫ 确定`；全部 40 个格子均为 1 单位宽。
- 反引号和波浪号是前两个符号；单引号、双引号、`<`、`>`、逗号、句号、`/` 和
  `?` 按这个顺序排在最后 8 个位置。
- 符号页行宽从 6 单位增加到 8 单位，符号网格和功能行共用同一条对齐宽度。

### 说明

- 没有删除任何公共类型或方法；布局 id `full-symbol-page`、
  `FullKeyboardPage.Symbols` 状态和现有关键命令保持不变。

## [0.2.1] - 2026-11-09

维护版本，同时补齐中英文键面与平台字体。

### 新增

- `KeyboardLanguage`（`Auto`、`Chinese`、`English`）和
  `TouchKeyboardOptions.KeyLanguage`：切换按键文案、标题行和校验消息，不改变 US
  ANSI QWERTY 键位几何。`Auto` 先跟随 `Culture`，再跟随当前 UI culture；自定义
  `ITouchKeyboardLocalizer` 仍然优先。
- 完整英文键面：`Confirm`、`Clear`、`Symbols`、`Space`、`Enter`，IME 键显示
  `CN`/`EN` 而不是 中/英；英文校验消息和无障碍名称保持原意。
- `TouchKeyboardFonts` 平台默认回退链：Windows 为 `Segoe UI, Microsoft YaHei
  UI, Microsoft YaHei`，macOS 为 `Helvetica Neue, PingFang SC, Hiragino Sans
  GB`，Linux 为 `Noto Sans, Noto Sans CJK SC, WenQuanYi Micro Hei, DejaVu
  Sans`。
- `TouchKeyboardTheme.FontFamily` 和 `TouchKeyboardStyle.FontFamily`。字体统一
  应用于键帽、标题/单位/错误行、编辑面、候选编号/文字、候选提示和切页字形。
- 中文版接入指引 `docs/avalonia.zh.md`、输入档位参考 `docs/profiles.zh.md`、
  布局参考 `docs/layouts.zh.md`、演示程序说明、各 NuGet 包说明和本变更记录。
- `AGENTS.md` 及贡献规则：所有面向使用者的文档必须同步提供中文版本。

### 变更

- 默认键盘字体改为平台相关回退链，不再固定使用 Avalonia 默认字体；NuGet 包不
  内置字体文件，机器上仍需安装 CJK 字体才能显示中文。
- 英文本地化：IME 激活标签由 中 改为 `CN`，全英文宿主不再出现中文键帽。
- Avalonia 包以 12.0.5 为兼容下限并声明 `Avalonia >= 12.0.5`；CI 同时验证
  12.1.5。
- `Microsoft.NET.Test.Sdk` 从 17.12.0 更新到 18.11.0。
- CI/Release 使用的 GitHub Actions 更新为 `actions/checkout@v7`、
  `actions/setup-dotnet@v6` 和 `actions/upload-artifact@v7`。
- 文档记录 Windows、Linux、macOS 三平台基础验收已于 2026 年 11 月完成；平台
  相关细节通过 GitHub issue 和真机清单跟踪。

### 说明

- `KeyLanguage` 不改变键位、行宽、IME 槽宽或候选行高度，只改变文字。
- `KeyboardLanguage.English` 不会自动关闭可选拼音引擎；全英文宿主可以不安装
  `TouchKeyboard.Ime.Pinyin`、不调用 `UseIme(...)`，也可以保留引擎并使用英文
  键面和 CN/EN 切换键。
- GitHub Release 说明由本文件的对应版本段落生成。

## [0.2.0] - 2026-11-08

首个公开版本。

### 新增

- `TouchKeyboard.Core`：十个内置输入档位（`text`、`password`、`ascii-token`、
  `integer`、`positive-integer`、`decimal`、`positive-decimal`、`date-slash`、
  `date-dot`、`ip-address`）、大小写不敏感的档位注册表、文化中立的校验错误和
  提交归一化。
- 不可变键盘布局：严格 15 单位 `AnsiStyle` 全键盘、带符号页的四行
  `PhoneHybrid` 全键盘，以及支持列/行合并的 5×4 `Numpad` 网格、
  `NumpadSlots` 场景映射和保留的 `Sequence` 数字面。
- `KeyboardSession` 与不可变 `KeyboardState` 快照：插入、退格、清空、光标移动、
  按位步进、正负号切换、粘性 Shift、模式/页面切换、最大长度和数字范围校验、
  提交与取消。
- 语义按键模型：`KeyDefinition`、`KeyLabel`、`KeyStyle`、`KeyGlyph` 和
  `KeyCommand`，核心只描述图标和动作，前端负责绘制。
- Core 中的可选 IME 契约：`IImeEngine`、`ImeCandidate`、
  `ImeCompositionState`、组合路由、候选分页和标点映射；
  `InputProfile.AllowsIme` 控制档位是否可用，默认只有 `text` 开启。
- `TouchKeyboard.Avalonia`：窗口内浮层宿主、遮罩、标题/单位/错误行、自绘单行
  编辑面、可变高度键盘区和固定候选行。
- Avalonia 交互：自动附加控制器、指针捕获触摸键、长按重复、物理键盘处理、
  关闭后恢复焦点，以及通过自绘编辑面抑制系统 OSK。
- Avalonia 布局与渲染：行/网格渲染、矢量 `KeyGlyph`、自动明暗主题、触屏优先
  的头部控件和本地化无障碍名称。
- Avalonia 集成扩展点：`IEditorAdapter`（内置 `TextBox`、`NumericUpDown`、
  `CalendarDatePicker` 适配器）、自定义适配器注册表、`IEditorSurface` 注入和
  带内置 `zh-CN`/`en-US` 文案的本地化接口。
- 外观 API：`TouchKeyboardOptions.Style` 的整体/字体/图标缩放和语义色、完整
  `TouchKeyboardTheme` 覆盖，以及标题行长按拖动和 `ResetPanelPosition()`。
- 可选语言包：`TouchKeyboard.Ime.Pinyin`（常用词库）和
  `TouchKeyboard.Ime.Pinyin.Extended`（扩展词库），均为托管、词典驱动。
- Avalonia IME UI：中/英键、候选行、候选分页、带下划线的内联组合态、IME 提示
  和全角标点路由。
- 演示程序：覆盖全部内置档位、可选拼音引擎、布局/主题选择和外观控件。
- 文档：架构、Avalonia 接入、输入档位、布局参考、中文输入、外观定制和公共 API。

### 说明

- 编辑面为单行；不提供选择、剪贴板、撤销/重做、多行布局、F 键、NumLock
  小键盘块、Ctrl/Alt/Win 组合或系统功能键。
- 可选拼音引擎不桥接系统 IME，也不注入按键；不支持五笔、手写、模糊音、云输入、
  自学习或繁简转换。
- 候选质量与速度按 best-effort 提供；范围和许可见
  [docs/ime.md](docs/ime.md)。
- WPF 已规划但未包含在本版本中。

[Unreleased]: https://github.com/Yubickari/TouchKeyboard/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.3.0
[0.2.1]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.2.1
[0.2.0]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.2.0
