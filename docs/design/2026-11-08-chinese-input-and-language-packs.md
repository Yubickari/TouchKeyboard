# 中文输入与语言包扩展（已决策方案）

> **状态**：v1.1 已决策并在 `0.2.0` 实现；真机验证待执行。`0.2.0` 为首个发布目标，`0.1.0` 不发布。
> **范围**：托管拼音输入、Core/Avalonia/语言包分层、公共 API、候选栏与组合态、无 IME 布局基线、词典与许可、体积、测试和发布节奏
> **决策归属**：`docs/design/decisions.md` 的 D12 已按本文修订，并新增 D17/D18/D19；D7 简化为宽松/署名数据许可与 MIT 免责；ADR 0003 是未发布的 v1 历史决策，由 [ADR 0004](../adr/0004-managed-pinyin-ime.md) 取代。
> **说明**：本文是实施基线。包名、类型名、默认值和交互规则已经确定；实现中若发现必须调整，先回写本文与 ADR，再改代码。

---

## 0. 结论

| 问题 | 最终决定 |
|---|---|
| 路线 | **A：纯托管拼音引擎**，作为 0.2.0 的可选能力；不接系统 IME，不做按键注入 |
| Core | 保持 `netstandard2.0`、无 UI/平台依赖；新增 IME 契约、组合态状态机和档位门控 |
| Avalonia | 提供**语言中立**的 IME 前端支持：IME 键、候选栏、组合态渲染；不引用任何语言包 |
| 语言包 | `TouchKeyboard.Ime.Pinyin`（常用词库）为可选包；`TouchKeyboard.Ime.Pinyin.Extended` 为可选扩展文本包 |
| 中文范围 | 仅拼音：全拼、分词、常用简拼；不做五笔、手写、模糊音、云输入、自学习 |
| 其他语言 | 通过同一个 `IImeEngine` 契约扩展，不改 Core 和 Avalonia；0.2.0 不实现、不承诺具体语言 |
| 接入方式 | **一行**：`options.UseIme(new PinyinImeEngine())`；不装语言包时行为与 `0.2.0` 无 IME 状态一致 |
| 无 IME 布局（0.2.0 内） | 无 IME 时移除 `spacer.ime`：AnsiStyle 空格 7.0、PhoneHybrid 空格 3.5，直接填充原槽位；有 IME 键时恢复 5.5/2.5 |
| 候选栏 | 默认 44 px 的预留行；PhoneHybrid 26 键主页/通用数字页作为首行使用键高，符号页隐藏；候选词完整显示，候选键按宽度自适应分页，不显示滚动条 |
| 发布 | `0.2.0` 为首个发布目标；不单独发布 `0.1.0`（可选先发 `0.2.0-preview.1` 现场试用） |

一句话：**框架能力放 Avalonia，语言能力放可选包，输入逻辑放 Core；调用方只多装一个包、多加一行配置。**

---

## 1. 背景与约束

### 1.1 现状

v1 只支持英文、数字和符号。现有实现为 IME 预留了两个位置：

- `KeyboardLayouts` 在 AnsiStyle 底行保留 `spacer.ime`（1.5 单位），在 PhoneHybrid 底行保留 `spacer.ime`（1 单位）；符号页没有 IME 槽位。
- `KeyGlyph.Ime` 已存在，但没有布局使用。
- `text` 档位的中间态校验只拒绝控制字符，所以 CJK 文本可以写入会话缓冲；数字、日期、IP、密码、ASCII 档位不适合中文。
- `TouchKeyboardEditor` 默认自绘，不触发系统 OSK/IME；`IEditorSurface` 允许宿主注入自己的编辑面。
- `InputProfile` 没有 IME 能力标记；`KeyCommandKind` 没有 IME 开关命令；编辑面没有组合态渲染。

> `0.1.0` 不再发布；`0.2.0` 已移除惰性槽位，见 1.4。实现结果、包体积和本机性能记录在
> [ime.md](../ime.md)。

### 1.2 输入需求

| 需求 | 设计响应 |
|---|---|
| 基本同意方案 A | D-IME-1：只走托管拼音路线 |
| Core 保持跨平台 | Core 仍为 `netstandard2.0`，无原生/平台/UI 依赖；组合态状态机放在 Core |
| Avalonia 包提供 Avalonia 框架支持 | 候选栏、IME 键、组合态渲染、按键路由都在 `TouchKeyboard.Avalonia`，语言中立 |
| IME 作为选配包按需接入 | 拼音引擎和词典放在 `TouchKeyboard.Ime.Pinyin`，基础包不引用它 |
| 中文覆盖常用文本、词汇 | 常用词库约 2 万词；支持全拼、分词和常用简拼 |
| 更复杂的文本需求可继续装文本包 | `TouchKeyboard.Ime.Pinyin.Extended` 作为可选扩展词库；采用同一引擎、同一接入方式 |
| 其他语言也可照此扩展 | 语言包实现 `IImeEngine`，复用 Avalonia 的 IME UI；不改 Core |
| API 清晰、易用，不增加调用方负担 | 一行 `UseIme(...)`；`text` 档位自动启用；没有引擎时完全无感 |
| 0.2.0 先做无 IME 布局基线 | 1.4/D19：Space 扩展为 7.0/3.5；有 IME 键时恢复 5.5/2.5，行总单位不变 |
| 不单独发布 0.1.0 | 直接以 `0.2.0` 为最终目标；未发布的基线不单独发版 |

### 1.3 约束

- 目标设备是 8–10 寸工控/自助触摸屏，常无可用系统输入法；不能用系统 IME、不能触发 OSK。
- 不引入 `SendInput`、`CGEvent`、X11 XTEST/uinput、IBus/FCITX 依赖。
- Core 不能引用 Avalonia、字体、画刷、计时器、调度器或平台 API；架构测试继续强制。
- 词典必须可审计、可署名，不能复制参考实现的数据；发布前必须通过许可检查。
- 面板在 1024×600 上仍要可用；候选栏不能让键盘超出屏幕或遮挡编辑面。
- 库以 MIT 发布；数据包按各自许可携带 `NOTICE`，不把 GPL/AGPL 或 share-alike 数据带进任何包。

### 1.4 0.2.0 无 IME 布局基线

`0.1.0` 不发布；`0.2.0` 是首个发布目标。`0.2.0` 先把无 IME 的布局做干净，再加 IME：不再保留惰性的 `spacer.ime`，而是让空格键扩展覆盖该槽位。

| 布局 | 无 IME（0.2.0 没有引擎/档位不允许） | 有 IME（0.2.0，引擎 + 档位允许） |
|---|---|---|
| AnsiStyle | `Space` 7.0，无 IME 键 | `Space` 5.5 + `ime.toggle` 1.5 |
| PhoneHybrid | `Space` 3.5，无 IME 键 | `Space` 2.5 + `ime.toggle` 1.0 |

- 两种状态的行总单位不变（15 / 10），只改空格与 IME 槽位的分配。
- “无 IME”指**没有 IME 键**：`0.2.0` 中引擎未配置或档位不允许 IME 时适用；不单独存在 `0.1.x` 发布线。
- `0.2.0` 中引擎已配置且档位允许时，即使当前是英文模式，IME 键仍保留（用于切回中文），所以 Space 保持基础宽度；中/英切换不改变布局。
- 符号页没有 IME 槽位，Space 宽度不变；数字键盘没有 Space/IME 键。
- 这是 `0.2.0` 的布局基线，对应 `decisions.md` D19，交付范围见第 11 节。
- 该布局基线和 IME 已在 0.2.0 实现中落地；无 IME 状态不再生成 `spacer.ime`。

---

## 2. 路线决策

### 2.1 三条路线与结论

| 维度 | A. 托管拼音引擎 | B. 系统 IME 桥接 | C. 按键注入 |
|---|---|---|---|
| 机制 | 纯托管词典 + 组合态 + 自绘候选栏 | Avalonia `TextInputMethodClient` 交给平台 IME | SendInput / CGEvent / XTEST 注入目标控件 |
| 跨平台一致性 | **强**：同一套代码 | 取决于后端与系统输入法环境 | **弱**：三套原生实现 |
| 候选/组合 UI | 完全可控、可主题化、触摸尺寸可调 | 系统绘制，风格不可控 | 完全依赖宿主输入法 |
| 工控/kiosk | **高**：不依赖系统 IME，不触发 OSK | 低：锁定设备常无输入法，可能触发 OSK | 最低：正是原方案删除的那层 |
| 主要风险 | 词典许可、质量、体积 | 平台差异、OSK 行为、焦点 | 平台权限、焦点、安全 |
| 决定 | **采用** | **0.2.0 不做**，不承诺未来 | **不做** |

### 2.2 为什么不做 B/C

- B 的候选窗由系统绘制，和工控面板主题不一致；Windows/macOS/Linux 的输入法环境差异大，还要逐平台验证 OSK 是否被重新触发。它适合“桌面环境已有输入法”的宿主，不适合作为工控默认路径。
- C 需要三套原生注入，涉及 macOS 辅助功能权限、Linux uinput/Wayland 限制、Windows 完整性级别和焦点竞争；这与“跨平台、可无 UI 单测”的库目标冲突。
- A 把不确定性收在词典和排序质量上，而这是可以通过小词库、扩展包和后续调优逐步改进的；平台行为在 0.2.0 保持不变。

B/C 的保留方式：文档保留“未来可由宿主或独立包实现”的空间，但 0.2.0 不预留公共 API、不列入版本承诺，也不在 `TouchKeyboard.Avalonia` 里增加平台分支。

### 2.3 跨平台边界

```text
宿主应用（Avalonia，未来 WPF）
        │
        ├─ IEditorAdapter / IEditorSurface
        │
TouchKeyboard.Avalonia（net8.0;net10.0）
        │  浮层、IME 键、候选栏、组合态渲染、主题、本地化
        │
        ├─ IImeEngine（可选语言包实现）
        │
TouchKeyboard.Core（netstandard2.0）
        │  会话、组合态、档位、布局、校验
        │
TouchKeyboard.Ime.Pinyin（netstandard2.0，可选）
        │  拼音解析、分词、排序、常用词库
        │
TouchKeyboard.Ime.Pinyin.Extended（netstandard2.0，可选）
           扩展词库（依赖常用包）
```

依赖方向是单向的：Core 不依赖任何前端或语言包；Avalonia 不依赖任何语言包；语言包只依赖 Core（扩展包再依赖常用包）。

---

## 3. 包结构

### 3.1 四个包

| 包 | TFM | 依赖 | 内容 |
|---|---|---|---|
| `TouchKeyboard.Core` | `netstandard2.0` | 无 | `IImeEngine`、`ImeCandidate`、`ImeCompositionState`、`ImeComposition`（内部）、`AllowsIme`、`ToggleIme`、会话集成 |
| `TouchKeyboard.Avalonia` | `net8.0;net10.0` | Core、Avalonia 12 | IME 键槽位、候选栏、组合态渲染、主题/本地化、主机路由；不引用语言包 |
| `TouchKeyboard.Ime.Pinyin` | `netstandard2.0` | Core | `PinyinImeEngine`、拼音解析/分词/排序、常用词库（约 2 万词） |
| `TouchKeyboard.Ime.Pinyin.Extended` | `netstandard2.0` | Core、`TouchKeyboard.Ime.Pinyin` | `ExtendedPinyinImeEngine`、扩展词库（目标总量约 10 万词） |

未来语言包沿用同一形状，例如 `TouchKeyboard.Ime.Japanese`；它们只实现 `IImeEngine`，不需要改 Avalonia。

### 3.2 为什么是 P1+，不是 P2

原始草案的 P2 是把 IME UI 再拆成 `TouchKeyboard.Avalonia.Ime`。不采用，原因：

1. **接入负担**：P2 需要宿主多装一个 UI 包、多写一行接线，和“不为调用方增加负担”直接冲突。
2. **框架支持本来就在 Avalonia**：候选栏、组合态渲染、IME 键都是框架 UI 能力，不是语言数据；放在 Avalonia 包里语义正确。
3. **体积可接受**：IME UI 估算增加 25–40 KB；不装语言包时没有词典数据，行为也不变。
4. **语言包保持纯净**：`TouchKeyboard.Ime.Pinyin` 仍是 `netstandard2.0`，不引用 Avalonia，未来 WPF 可以直接复用。

代价是基础 Avalonia 包多一小段暂不执行的 UI 代码。如果现场证明体积敏感，可以再评估拆分；0.2.0 不为此增加包数量。

### 3.3 包版本与兼容

- 语言包与 Core/Avalonia 使用同一版本线（0.2.x），语言包依赖同 minor 的 Core。
- 扩展包依赖同版本的常用包；词典数据可以用 patch 版本独立更新。
- 所有包启用 XML 文档、符号包、SourceLink 和包内容检查；语言包额外检查词库资源与 `NOTICE`。
- NuGet 预留 `TouchKeyboard.Ime.*` ID 前缀，避免未来语言包命名冲突。

---

## 4. 决策清单

### 4.1 十三个已定决策

| ID | 决策 | 理由/影响 |
|---|---|---|
| **D-IME-1** | 路线只采用 **A（托管拼音）**；B 在 0.2.0 不做且不承诺，C 不做 | 跨平台一致、工控可用、无权限/焦点风险；不确定性留在词典质量 |
| **D-IME-2** | 中文只做**拼音**：全拼、分词、常用简拼；不做五笔、手写、模糊音、云输入、自学习 | 覆盖常用文本；控制引擎复杂度和词典体积 |
| **D-IME-3** | 词库采用**简单许可的常用词库**：Unihan 字音 + jieba 词表/词频（MIT 声明 + `NOTICE`）+ 人工多音字覆盖；所有包只允许宽松/署名许可（CC0/MIT/BSD/Apache/Unicode/CC-BY 4.0），不含 CC-BY-SA/GPL/AGPL/来源不明数据 | 所有人都可免费使用；不设单独审核流程，来源不清就替换 |
| **D-IME-4** | 包结构采用 **P1+**：Core 抽象 + Avalonia 通用 IME UI + 可选拼音包 + 可选扩展包；不设 `TouchKeyboard.Avalonia.Ime` | 接入最简单；基础包增量小；语言包不依赖 UI |
| **D-IME-5** | 候选栏是**预留行**，默认 44 px；PhoneHybrid 中作为 26 键主页/通用数字页的首行，高度等于键高，符号页隐藏，从而与 5 行符号页等高；IME-capable 会话在组合/中英切换/翻页时不改变同模式面板高度 | 触摸面板不能跳高；浮层会遮挡按键；1024×600 可容纳；减少 PhoneHybrid 高度占用 |
| **D-IME-6** | **单引擎、无语言切换 UI**；配置引擎后 `text` 默认中文；主机在应用会话内记住中/英，重启重置；`DefaultEnabled` 可覆盖 | 符合当前范围；避免每次切字段都重新切中英 |
| **D-IME-7** | 系统 IME 桥接 0.2.0 **不做**；保留未来可选适配器空间，不承诺 API | 工控默认路径不需要；避免 OSK/平台差异 |
| **D-IME-8** | 按键注入包 **不做**，也不规划 SendInput/CGEvent/XTEST | 与跨平台、无权限目标冲突；如需要由宿主/第三方实现 |
| **D-IME-9** | 中文标点由**引擎映射**为常用全角标点；默认开启；技术符号保持 ASCII；`FullWidthPunctuation=false` 可关 | 符合中文输入习惯；语言规则留在语言包；不影响数字/日期档位 |
| **D-IME-10** | 发布版本 **0.2.0**（首个发布目标；可选 `0.2.0-preview.1` 现场试用）；不单独发布 `0.1.0` | 直接以最终目标交付，不维护一条无 IME 的 0.1.x 发布线 |
| **D-IME-11** | **组合态归 Core 会话**；`KeyboardState.Ime` 是唯一渲染源；`ToggleIme` 由会话消费，不是 Host action | 所有前端共享一套状态机；避免 Avalonia/WPF 各写一遍路由 |
| **D-IME-12** | 激活 API 是**一行** `options.UseIme(new PinyinImeEngine())`；显式注册、无反射/DI 扫描；没有引擎时行为与今天完全一致 | 调用方负担最小；AOT/裁剪友好；不引入隐藏初始化 |
| **D-IME-13** | 兼容策略**只增不改**：新构造重载、新方法重载、`init` 属性；`KeyCommandKind.ToggleIme` 是新增枚举值 | 现有调用方不破；已有 `KeyboardState`/`KeyDefinition` 构造代码继续编译运行 |

另有 `decisions.md` D19 记录 `0.2.0` 的无 IME 布局基线（不属于 IME 的 D-IME 编号），D7 记录许可政策（MIT + 宽松/署名数据，不写复杂法律文本）。

### 4.2 组合态为什么放在 Core（D-IME-11）

原始草案让 Host 持有 `ImeComposition`、组合态优先拦截按键。改为 Core 会话持有，原因：

- **单一状态源**：`KeyboardState` 已经包含文本、光标、布局和校验错误；把组合态也放进去，前端仍然只渲染快照。
- **按键路由只写一遍**：`a–z`、空格、数字、退格、方向、标点、模式切换、提交等规则都在 `KeyboardSession.Execute`/`TryCommit` 中；Avalonia 只负责视图事件和 `Submit`/`Close` 等 host action。
- **可无 UI 测试**：组合、翻页、选中、提交、取消、超长都能在 Core 单测覆盖，不依赖 Headless。
- **未来 WPF 免费获得**：WPF 前端只渲染 `KeyboardState.Ime`，不需要重新实现输入法逻辑。

`ImeComposition` 本身是 Core 内部类，公开快照是 `ImeCompositionState`；这样 API 面更小，状态机实现可以调整。

### 4.3 为什么候选栏不浮层（D-IME-5）

- 浮层会盖住最上面一行按键，触摸时容易误触；工控面板更需要可预期。
- 组合开始/结束如果让面板跳高，键盘位置变化会打断连续输入。
- 固定行只在“配置了引擎且档位允许 IME”时出现；数字/日期/密码档位没有候选行，面板与今天一致。
- 固定行始终存在（未组合时显示弱化的模式提示），所以组合开始、结束、中英切换都不改变几何。

### 4.4 默认语言与记忆（D-IME-6）

- 当前只支持一个活动引擎；没有语言列表、语言市场、自动检测或热加载。
- 配置了中文引擎且档位为 `text` 时，新会话默认中文；`TouchKeyboardImeOptions.DefaultEnabled = false` 可改为默认英文。
- 主机在**应用会话内**记住用户最后一次中/英选择，切字段沿用；应用重启后回到默认。`RememberMode = false` 可关闭记忆。
- 不写配置文件、不跨进程同步、不做每字段持久化。
- 未来多语言时，宿主可以在启动时选择引擎；语言切换 UI 不在 0.2.0 范围。

### 4.5 中文标点（D-IME-9）

引擎通过 `TryMapPunctuation` 提供语言自己的映射，Core 只负责在“IME 开启 + 档位允许 + 未在组合中”时调用：

| 输入 | 中文模式输出 | 说明 |
|---|---|---|
| `,` `.` | `，` `。` | 最常用 |
| `?` `!` | `？` `！` | 句末 |
| `:` `;` | `：` `；` | 常用 |
| `(` `)` | `（` `）` | 括号 |
| `[` `]` | `【` `】` | 中文方括号 |
| `{` `}` | `｛` `｝` | 全角花括号 |
| `<` `>` | `《` `》` | 书名号 |
| 其他 ASCII 标点 | 保持原样 | `@ # $ % ^ & * - _ = + / \`、竖线、`~` 等技术符号不转换 |

- 引号 `"`/`'` 在 0.2.0 不自动配对，保持 ASCII，避免成对状态机和回退问题。
- 映射只在 IME 开启时生效；英文模式完全按 ASCII 输入。
- `FullWidthPunctuation = false` 时完全跳过引擎映射，适合邮箱、路径、编码等 `text` 字段。

### 4.6 兼容策略（D-IME-13）

| 现有 API | 变更方式 | 兼容性 |
|---|---|---|
| `KeyboardState` | 新增带 `ImeCompositionState? ime` 的构造重载；旧构造委托并传 `null` | 旧代码/二进制不破 |
| `KeyboardLayouts.Full` | 新增带 `KeyDefinition? imeKey` 的重载；旧重载保留 | 旧调用不破 |
| `KeyDefinition` | 新增带 `KeyLabel? accessibleName` 的构造重载；旧构造保留；`AccessibleName` 只读 | 旧代码/二进制不破；Core 不需要 `init` polyfill |
| `InputProfile` | 新增 `virtual bool AllowsIme => false` | 旧子类自动继承 false |
| `KeyCommandKind` | 新增 `ToggleIme` 值，**追加在末尾**，现有数值不变 | 旧 switch 可继续走 default；不会改变已持久化/比较的旧值 |
| `KeyboardSessionOptions` | 新增 `ImeEngine`、`ImeEnabled`、`ImeCandidatePageSize`、`ImeMaxCompositionLength`、`ImeFullWidthPunctuation` | 旧代码不破；Core 不引用 Avalonia |
| `KeyboardSession` | 新增 `SelectImeCandidate`、`ChangeImePage` | 旧代码不破 |
| `TouchKeyboardOptions` | 新增 `Ime` 属性和 `UseIme` 方法 | 旧代码不破 |
| `TouchKeyboardTheme` | 新增候选/组合态画刷与度量 | 旧主题实例取默认值 |

---

## 5. 公共 API

### 5.1 最小接入

```bash
dotnet add package TouchKeyboard.Avalonia
dotnet add package TouchKeyboard.Ime.Pinyin
```

```csharp
using TouchKeyboard.Avalonia;
using TouchKeyboard.Ime.Pinyin;

KeyboardHost.Configure(options =>
{
    options.UseIme(new PinyinImeEngine());
});
```

就这些。`text` 档位的字段自动出现“中/英”键和候选栏；数字、日期、IP、密码、ASCII 档位不出现。不装拼音包、没有调用 `UseIme` 时，行为与 `0.2.0` 无 IME 状态一致。

### 5.2 常用配置

```csharp
KeyboardHost.Configure(options =>
{
    options.UseIme(new PinyinImeEngine());

    options.Ime.DefaultEnabled = false;      // 默认英文；null = 配置引擎后默认中文
    options.Ime.RememberMode = true;         // 应用会话内记住中/英
    options.Ime.CandidatePageSize = 9;       // 1–9；UI 可按宽度缩小
    options.Ime.MaxCompositionLength = 32;   // 8–64
    options.Ime.FullWidthPunctuation = true; // 中文模式常用全角标点
});
```

| 选项 | 默认 | 说明 |
|---|---|---|
| `Engine` | `null` | 语言包引擎；`null` 时不启用 IME |
| `DefaultEnabled` | `null` | `null` = 有引擎时默认中文；`false` = 默认英文 |
| `RememberMode` | `true` | 应用会话内记住最后一次中/英；重启重置 |
| `CandidatePageSize` | `9` | 每页候选数上限，范围 1–9；Avalonia 按宽度自适应缩小 |
| `MaxCompositionLength` | `32` | 原始拼音最大长度；超出后忽略新字母 |
| `FullWidthPunctuation` | `true` | 中文模式下是否调用引擎的标点映射 |

`TouchKeyboardImeOptions` 是宿主门面；控制器在创建会话时把它映射到 `KeyboardSessionOptions` 的 `Ime*` 属性，Core 不引用 Avalonia 类型。

### 5.3 Core 契约

```csharp
namespace TouchKeyboard.Core;

public interface IImeEngine
{
    /// <summary>BCP-47 语言标签，例如 "zh-Hans"。</summary>
    string LanguageTag { get; }

    /// <summary>为输入串返回最多 maxCandidates 个候选；无候选返回 false。</summary>
    bool TryQuery(
        string input,
        int maxCandidates,
        out IReadOnlyList<ImeCandidate> candidates);

    /// <summary>把语言相关的标点映射为目标文本；返回 false 表示保持原字符。</summary>
    bool TryMapPunctuation(char input, out string mapped);
}

public sealed class ImeCandidate
{
    public ImeCandidate(string text) => Text = text;
    public string Text { get; }   // 只暴露显示文本；排序和分数留在引擎内部
}

public sealed class ImeCompositionState
{
    public bool IsEnabled { get; }          // 中/英状态；false = 英文直写
    public bool IsComposing { get; }        // 是否有未提交的拼音
    public string Input { get; }            // 原始拼音，未组合时为空
    public IReadOnlyList<ImeCandidate> Candidates { get; } // 当前页
    public int SelectedIndex { get; }       // 当前页内 0-based；无候选为 -1
    public int PageIndex { get; }
    public int PageCount { get; }
    public int CandidateCount { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }
}
```

`ImeCompositionState` 由 Core 构造并放入 `KeyboardState.Ime`，前端不直接 new。

```csharp
public abstract class InputProfile
{
    public virtual bool AllowsIme => false;
}

public enum KeyCommandKind
{
    // ... 现有值 ...
    ToggleIme,
}

public sealed class KeyboardSessionOptions
{
    // ... 现有属性 ...
    public IImeEngine? ImeEngine { get; set; }
    public bool? ImeEnabled { get; set; }
    public int ImeCandidatePageSize { get; set; } = 9;
    public int ImeMaxCompositionLength { get; set; } = 32;
    public bool ImeFullWidthPunctuation { get; set; } = true;
}

public sealed class KeyboardState
{
    // ... 现有属性 ...
    public ImeCompositionState? Ime { get; }
}

public sealed class KeyboardSession
{
    // ... 现有成员 ...
    public void SelectImeCandidate(int index);
    public void ChangeImePage(int delta);
}
```

Avalonia 侧的门面：

```csharp
namespace TouchKeyboard.Avalonia;

public sealed class TouchKeyboardOptions
{
    // ... 现有属性 ...
    public TouchKeyboardImeOptions Ime { get; } = new();

    public TouchKeyboardOptions UseIme(IImeEngine engine)
    {
        Ime.Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        return this;
    }
}

public sealed class TouchKeyboardImeOptions
{
    public IImeEngine? Engine { get; set; }
    public bool? DefaultEnabled { get; set; }   // null = 有引擎时默认中文
    public bool RememberMode { get; set; } = true;
    public int CandidatePageSize { get; set; } = 9;      // 1–9；UI 自适应
    public int MaxCompositionLength { get; set; } = 32;  // 8–64
    public bool FullWidthPunctuation { get; set; } = true;
}
```

### 5.4 档位门控

| 档位 | `AllowsIme` | 说明 |
|---|---|---|
| `text` | **true** | 中文、英文、数字、标点都允许 |
| `password` | false | 仍是可打印 ASCII，避免掩码/组合态混用 |
| `ascii-token` | false | 标识符不应出现 CJK |
| `integer` / `positive-integer` | false | 数字键盘不出现 IME 键 |
| `decimal` / `positive-decimal` | false | 同上 |
| `date-slash` / `date-dot` | false | 日期只接受数字/分隔符 |
| `ip-address` | false | IPv4 只接受数字/点 |
| 自定义档位 | 默认 false | 需要中文时显式 `override bool AllowsIme => true;` |

`AllowsIme` 只表示“这个档位允许 IME”；是否真的出现 IME 键还要求 `TouchKeyboardOptions.Ime.Engine != null`。两者都满足才启用。IME 还需要档位允许全键盘（`AllowsFullKeyboard`）；纯数字/受限档位即使误设 `AllowsIme = true` 也不会出现 IME 键或候选栏——没有字母输入面，组合态没有意义。

0.2.0 不增加“每字段 IME 开关”的附加属性；不需要中文的 `text` 字段改用 `ascii-token` 或自定义档位。`FullWidthPunctuation=false` 只关闭标点映射，不关闭中文输入。

### 5.5 扩展词库与自定义引擎

扩展文本包只需要换一个引擎类型，接入仍然是一行：

```csharp
using TouchKeyboard.Ime.Pinyin;
using TouchKeyboard.Ime.Pinyin.Extended;

KeyboardHost.Configure(options =>
{
    options.UseIme(new ExtendedPinyinImeEngine());
});
```

自定义引擎实现 `IImeEngine` 即可，和拼音包平级：

```csharp
public sealed class MyImeEngine : IImeEngine
{
    public string LanguageTag => "ja-Jpan";

    public bool TryQuery(string input, int maxCandidates, out IReadOnlyList<ImeCandidate> candidates)
    {
        candidates = new ImeCandidate[0];
        return false; // 或返回真实候选
    }

    public bool TryMapPunctuation(char input, out string mapped)
    {
        mapped = string.Empty;
        return false;
    }
}

KeyboardHost.Configure(options => options.UseIme(new MyImeEngine()));
```

不扫描程序集、不依赖 DI 容器、不要求模块初始化；引擎由宿主显式创建并传入。`PinyinImeEngine`/`ExtendedPinyinImeEngine` 的词典采用嵌入资源懒加载，首次查询时初始化，之后只读、可跨会话共享；引擎实例的懒加载线程安全，查询路径只读。

### 5.6 没有引擎时

- `State.Ime == null`。
- 不存在 IME 键；全键盘的 Space 扩展为 7.0/3.5 覆盖原槽位，行总单位不变。
- 不存在候选行，面板几何与 `0.2.0` 无 IME 状态一致。
- 物理键盘、数字/日期/密码档位、主题、本地化、适配器都不受影响。

这条是包消费 smoke 的硬性验收项，不只是“应该”。

### 5.7 API 设计原则

1. **默认无感**：不配置引擎时库的行为不变；不因为装了基础包就出现候选栏。
2. **一行接入**：语言包通过 `UseIme(engine)` 激活；扩展包只换引擎类型。
3. **显式优于魔法**：不做程序集扫描、反射发现、语言市场或热加载。
4. **状态只读**：前端渲染 `KeyboardState`；组合态也不允许前端直接改。
5. **语言规则归语言包**：拼音解析、排序、标点映射在语言包；Core 只做状态机和门控。
6. **不破坏既有 API**：按 D-IME-13 的“只增不改”执行。
7. **配置时机**：`UseIme`/`Ime` 选项在首次打开面板前设置；面板打开期间修改只影响下一次会话。

---

## 6. Core 组合态设计

### 6.1 状态机

`ImeComposition`（Core 内部）持有以下状态：

```text
ImeComposition
  ├─ enabled          中/英开关（中 = true）
  ├─ input            原始拼音，小写，最大 32
  ├─ candidates       最近一次查询的全部候选（最多 100 个）
  ├─ pageIndex        当前页
  ├─ pageSize         每页候选数
  └─ selectedIndex    当前页内选中项；默认 0
```

生命周期：

```text
英文直写 --ToggleIme--> 中文待输入 --字母--> 组合中
组合中 --空格/数字/点选--> 提交候选（写回会话缓冲）
组合中 --回车/确定--> 提交候选，再提交字段
组合中 --退格到空--> 中文待输入
组合中 --中/英--> 先提交组合，再切换
组合中 --模式/页面/标点--> 先提交组合，再执行
组合中 --清空--> 清空组合与缓冲
组合中 --关闭/遮罩/Esc--> 丢弃组合与会话
```

`ImeComposition` 不直接改会话缓冲；它通过 `CommitComposition()` 把选中候选（或原始输入）交给会话的 `Insert` 路径，复用现有的最大长度和中间态校验。

### 6.2 快照

每次状态变化，`KeyboardSession.Publish()` 重新生成 `KeyboardState`，其中 `Ime` 为新的 `ImeCompositionState`。快照是只读的，候选列表是当前页切片；前端不需要知道引擎、词典或分页实现。

`IsDirty` 仍以会话缓冲是否变化为准：只组合、没提交候选时为 `false`；候选写入缓冲后才为 `true`。

### 6.3 按键路由

| 操作 | 组合中行为 | 非组合行为 |
|---|---|---|
| `a–z` / `A–Z` | 追加小写字母，刷新候选；到 32 字符上限后忽略 | 正常插入 |
| `1–9` | 选中当前页第 N 个候选并提交；越界忽略 | 正常插入数字 |
| `0` | 忽略（拼音中没有 0） | 正常插入 |
| `空格` | 提交当前选中候选；无候选则提交原始输入 | 插入空格 |
| `回车` / `确定` | **先提交候选，再提交字段** | 提交字段 |
| `退格` | 删除一个拼音字母；清空后回到待输入 | 删除光标前字符 |
| `←` / `→` | 移动候选高亮；到页边界时翻页并环绕 | 移动光标 |
| `↑` / `↓`（物理键盘） | 上一个/下一个候选，等价于 `←`/`→` | 数字步进（原行为） |
| `中/英` | 先提交候选，再切换；提交失败则保持组合 | 切换中/英 |
| `123` / `符号` / `ABC` / `Shift` | 先提交候选，再切换 | 正常切换 |
| `清空` | 清空组合与缓冲 | 清空缓冲 |
| 标点 / 符号 | 先提交候选，再插入（可能全角映射） | 正常插入 |
| 候选点按 | 选中并提交 | — |
| 候选栏 `‹` / `›` | 上一页/下一页；`selectedIndex` 重置为 0 | — |
| `Esc` / 遮罩 / 关闭 | 丢弃组合与会话 | 关闭会话 |
| 关闭/切字段 | 丢弃组合，不提交 | — |
| 无候选 | 候选栏提示；空格/回车提交原始输入 | — |

补充规则：

- 只有**单个 ASCII 字母**会进入组合态；`InsertText` 含多个字符（粘贴、物理键盘上屏）时，先提交当前组合，再按普通文本插入。未组合时，IME 开启且插入单个 ASCII 字母会开始组合；空格、标点和多字符文本按普通插入。
- `1–9` 选候选只在有数字行的布局（AnsiStyle）上直接可用；PhoneHybrid 主页面没有数字行，候选选择以点按、空格和方向键为主，候选栏上的编号仍可点按。
- 物理键盘的 `Enter` 和触摸 `确定` 走同一个 `Submit` 路径，都是“先完成组合，再校验/提交字段”。原草案中“回车只提交候选、不提交字段”的说法作废。
- 组合中按 `确定` 时，如果候选插入超出字段最大长度，**保留组合**并显示 `MaxLengthExceeded`；用户可退格或换短候选。
- 组合中按 `中/英`、模式键、页面键或标点时，如果组合提交失败，**不继续执行**后续动作，保持组合并显示错误。
- `Esc`/遮罩/关闭始终丢弃整个会话，这与现有“关闭 = 放弃”的语义一致。

### 6.4 提交、取消与长度

- `CommitComposition()`：选中候选（无候选用原始输入）→ `Insert` → 清空组合；插入失败则保留组合并设置错误。
- `TryCommit()`：先 `CommitComposition()`；成功后走现有 `MaxLength`、`TryValidate`、范围校验；失败返回 `false`，不写回适配器。
- `Cancel()`：清空组合并丢弃会话；不写回。
- 组合长度上限 32 是**拼音长度**，不是字段长度；字段最大长度仍由会话的 `MaxLength` 控制。
- 候选插入失败时：长度问题用 `MaxLengthExceeded`；其他中间态/校验拒绝用 `CommitRejected`，组合保留，用户可退格或换候选。
- 组合中的文本不进入 `State.Text`，所以不会出现在适配器/字段里，直到候选提交。

### 6.5 布局中的 IME 键

- 当 `Profile.AllowsIme && Profile.AllowsFullKeyboard && ImeEngine != null` 时，`KeyboardSession.BuildLayout()` 用 IME 键占据槽位，Space 回到基础宽度 5.5/2.5；否则不生成 IME 键，Space 扩展为 7.0/3.5，直接覆盖槽位。
- `0.2.0` 无 IME 状态，所有全键盘使用扩展空格；有引擎/档位允许时恢复 IME 键。无 IME 时不再生成 `spacer.ime`（已实现）。
- 键 id 固定为 `ime.toggle`；命令为 `KeyCommand.ToggleIme`；样式为 `KeyStyle.Action`；宽度由布局槽位决定（AnsiStyle 1.5、PhoneHybrid 1.0），布局工厂负责使用槽位宽度，保证每行总单位不变。
- 标签根据状态使用 `key.ime.active`（中）或 `key.ime.inactive`（英）；无障碍名称使用 `key.ime.active.name`/`key.ime.inactive.name`，通过 `KeyDefinition` 的新构造重载写入只读属性 `AccessibleName`。
- `KeyGlyph.Ime` 保留但 0.2.0 不使用；内置布局用文字“中/英”，比地球图标更直接。
- 符号页没有 IME 槽位：切到符号页会先提交组合；返回主页面后 IME 键恢复。
- 数字键盘永远没有 IME 键；`AllowsIme=false` 的档位也没有。

### 6.6 物理键盘

- `HandlePhysicalKeyboard = true` 时，物理键盘文本也走 `KeyboardSession.Execute(Insert)`，所以中文模式下物理键盘字母同样进入组合态。
- `Escape` 关闭/丢弃；`Enter` 提交字段；`Backspace`/方向键按第 6.3 节路由。
- 物理键盘上的系统 IME 可能先于宿主处理按键；这是平台行为，不在库内拦截。需要系统 IME 的宿主应关闭 `HandlePhysicalKeyboard` 或使用自己的编辑面。
- 触摸输入仍是主路径；物理键盘是便利功能，不承诺与系统 IME 组合。

### 6.7 错误与空状态

- 组合开始时清除上一次校验错误；候选提交失败时显示对应错误。
- 无候选时 `Candidates` 为空、`PageCount == 0`；候选栏显示 `ime.no-candidate`，空格/回车按原始输入提交。
- 引擎查询失败（`TryQuery` 返回 false）按“无候选”处理；引擎实现不应抛异常，语言包在加载时对损坏数据 fail fast。
- 空输入不查询引擎，不创建组合。

---

## 7. Avalonia 前端

### 7.1 面板结构

```text
┌ 标题 / 单位 / 错误 ─────────────────────────────── × ┐
│ 编辑面（缓冲文本 + 组合态下划线 + 光标）              │
│ 候选栏（默认 44 px；未组合时显示模式提示）            │
├──────────────────────────────────────────────────────┤
│ 全键盘或数字键盘                                      │
└──────────────────────────────────────────────────────┘
```

- 候选栏在 `Grid` 中单独一行，默认 44；PhoneHybrid 26 键主页/通用数字页使用键高作为首行，符号页隐藏；`IsVisible` 只在 `State.Ime != null` 且不是符号页时为 true。
- 候选行隐藏时同时折叠该行与行间距，避免非 IME 档位多出 10 px，保证面板几何与无 IME 状态一致。
- 组合开始/结束、中英切换都不增删行，只替换内容，因此面板高度不变。
- 数字/日期/密码/ASCII 档位没有 `State.Ime`，候选行隐藏，面板几何与无 IME 状态一致。
- 对于 `text` 档位临时切到数字键盘的情况，`State.Ime` 仍存在，候选行保持可见但为空（不显示拼音提示），避免模式切换时面板跳高；数字键盘本身没有 IME 键。

### 7.2 IME 键

- 占用无 IME 时由 Space 覆盖的槽位；有 IME 键时 Space 回到 5.5/2.5。
- 显示当前状态：中文“中”，英文“英”（en-US 本地化为“中”/“EN”）。
- `KeyStyle.Action`，与 `123`/`符号` 等功能键同色；按下有主题按压反馈，无 hover。
- 无障碍名称是完整句子：“中文输入，切换到英文”/“英文输入，切换到中文”。
- 点击走 `KeyCommand.ToggleIme`；组合中先提交候选再切换。

### 7.3 候选栏

- 默认 44（主题可调，但同一会话内不随组合内容变化）；PhoneHybrid 首行模式使用键高；触摸目标 ≥44 px。
- 内容：当前页候选（编号 1–9）→ `‹`/`›` 翻页 → 页码（多页时）。拼音文本已显示在编辑窗口中，候选行左侧不重复。
- 默认每页 6 个；候选按钮等宽自适应，长词用省略号；每个候选是 `TouchButton` 风格的无焦点触摸目标。
- 第一个候选默认高亮；点按直接提交；`‹`/`›` 只翻页，不提交。
- 无候选时显示“无候选，按空格原样输入”。
- 未组合时（全键盘页面）显示弱化的模式提示（`ime.hint.chinese` / `ime.hint.english`），保留高度，不做空白条；数字键盘页面显示为空。
- 候选栏不进入 Tab 焦点链；读屏通过无障碍名称读取“第 N 个候选，文本”。
- 候选栏控件在 0.2.0 作为 Avalonia 内部控件实现；公共扩展点是 `ImeCompositionState`，不是候选栏类型。

### 7.4 组合态渲染

- 默认 `TouchKeyboardEditor` 在光标处绘制组合拼音，带 1–2 px 下划线；光标移到组合末尾。
- 水平滚动跟随组合末尾；组合很长时保持可见。
- 组合文本用 `CompositionText` 画刷，下划线用 `CompositionUnderline`；与缓冲文本区分但不刺眼。
- 自定义 `IEditorSurface` 不实现组合渲染时，候选栏不重复拼音；组合文本是否显示由编辑面自行决定。不新增 `IEditorSurface` 成员，避免破坏既有实现。
- 密码档位不允许 IME，不存在组合掩码问题。

### 7.5 主题、本地化与无障碍

主题新增（明暗两套预设都要有值）：

| 成员 | 用途 |
|---|---|
| `CandidateBarHeight` | 候选栏高度，默认 44；PhoneHybrid 首行模式使用键高 |
| `CandidateBackground` | 候选按钮底色 |
| `CandidateSelectedBackground` | 默认/选中候选高亮 |
| `CandidateText` / `CandidateSelectedText` | 候选文字 |
| `CandidateMutedText` | 页码、无候选提示 |
| `CompositionText` | 编辑面组合文本 |
| `CompositionUnderline` | 组合下划线 |
| `CandidateCornerRadius` / `CandidateFontSize` | 候选按钮外观 |

`CandidateBarHeight` 默认 44；宿主可以改成一个不同的固定值，但同一会话内不随组合内容变化。

本地化新增键：

| 键 | zh-CN | en-US |
|---|---|---|
| `key.ime.active` | 中 | 中 |
| `key.ime.inactive` | 英 | EN |
| `key.ime.active.name` | 中文输入，切换到英文 | Chinese input, switch to English |
| `key.ime.inactive.name` | 英文输入，切换到中文 | English input, switch to Chinese |
| `ime.candidates` | 候选词 | Candidates |
| `ime.previous` | 上一页候选 | Previous candidates |
| `ime.next` | 下一页候选 | Next candidates |
| `ime.no-candidate` | 无候选，按空格原样输入 | No candidates; press space to insert as typed |
| `ime.hint.chinese` | 拼音输入 | Pinyin input |
| `ime.hint.english` | 英文输入 | English input |
| `ime.candidate.name` | 第 {0} 个候选，{1} | Candidate {0}, {1} |
| `ime.page` | {0}/{1} | {0}/{1} |
| `ime.composing` | 拼音 | Pinyin |

带 `{0}` 的资源由前端用当前 UI culture 格式化；`ITouchKeyboardLocalizer` 接口不变，自定义本地化器只需返回模式串。自定义本地化器需要补这些键；`Get` 的现有回退是返回 resource key，不会自动翻译。

无障碍：

- IME 键、候选按钮、翻页按钮都有本地化无障碍名称和 `AutomationControlType.Button`。
- 候选栏整体可命名为“候选词”。
- 组合态的下划线不能只靠颜色表达；同时有候选栏文本，读屏用户能知道正在组合。

### 7.6 1024×600 尺寸核算

以默认主题、`KeyHeight=56`（窗口高度 <640 时自动选择）估算：

| 区域 | 高度 |
|---|---|
| 标题行 | 48 |
| 编辑面 | 60 |
| 候选栏（AnsiStyle） | 44 |
| 键盘区（5 行 ×56 + 4×6 间隙） | 304 |
| 行间距（3×10） | 30 |
| 面板内边距（2×16） | 32 |
| 底部外边距 | 10 |
| **合计** | **528** |

600 px 高度下仍有约 72 px 余量。即使宿主固定 `KeyHeight=72`，合计约 608 px，需要宿主自行调小；自动键高在 IME 模式下仍按窗口高度选择 56/64/72，不额外加高。验收时在 1024×600 上分别测 AnsiStyle/PhoneHybrid、明暗主题、组合/非组合、单页/多页候选。

`0.2.0` 无 IME 状态没有候选行，面板高度与无 IME 布局一致；空格扩展只影响宽度，不改变高度。只有 IME 会话才增加候选行高度。

### 7.7 自定义编辑面

- 自定义 `IEditorSurface` 继续按现有契约工作；组合态渲染是可选的。
- 不实现组合渲染时，候选栏不重复拼音；字段最终提交仍走同一 `TryCommit`/适配器。
- 如果自定义编辑面是真实 `TextBox`，可能触发平台 OSK/IME；这是宿主选择，文档继续提示。

---

## 8. 语言包与词典

### 8.1 语言包与文本包

| 类型 | 内容 | 例子 | 接入 |
|---|---|---|---|
| 语言包 | 引擎 + 该语言基础词库 | `TouchKeyboard.Ime.Pinyin` | `UseIme(new PinyinImeEngine())` |
| 文本包 | 在语言包基础上增加词库/领域词 | `TouchKeyboard.Ime.Pinyin.Extended` | `UseIme(new ExtendedPinyinImeEngine())` |
| 未来语言包 | 实现 `IImeEngine` 的引擎 + 词库 | `TouchKeyboard.Ime.Japanese` | 同一个 `UseIme(...)` |

文本包不是“运行时插件市场”，而是编译期 NuGet 包；安装后通过显式 engine 类型启用。这样 AOT/裁剪、启动顺序和版本对齐都可控。文本包只扩展词库，不改变排版、字体、换行或富文本能力；这些仍由宿主前端负责。

### 8.2 拼音范围（MVP）

- 全拼：`nihao` → 你好；`beijing` → 北京。
- 分词/整句：`woaibeijing` → 我爱北京（最佳路径组合，最多 4 段）。
- 常用简拼：`bj` → 北京；`nihao` 也可作为简拼匹配，但排序低于全拼。
- 词库：常用词约 2 万；扩展包目标总量约 10 万（含常用词）。
- 不做：五笔、手写、模糊音（zh/z、ch/c、sh/s、n/l、in/ing）、云输入、用户自学习、整句语言模型、繁体/简体转换。
- 候选排序：精确全拼 > 简拼；高频词 > 低频词；长词在同等频率下略优先；整句候选排在单词候选之后。

### 8.3 词典格式与索引

- 词条：`pinyin`（无调、小写、音节用 `'` 分隔）+ `text` + `frequency`（ushort）+ `flags`。
- 索引：
  - 全拼前缀索引：排序数组或 trie，支持前缀范围查询；
  - 简拼索引：首字母串 → 词条；
  - 音节切分：音节表 + 动态规划，支持 `woaibeijing` 这类连续输入；
  - 每个前缀节点预计算 top-N（N=100）候选，保证查询延迟。
- 词典以嵌入资源分发（deflate 压缩的二进制），懒加载；不依赖文件系统、区域设置或反射。
- 扩展包复用常用包的加载器，追加自己的词条；排序时统一去重、按频率合并。

### 8.4 数据来源与许可

**简单原则**：代码 MIT；数据只用可免费再分发的宽松/署名许可（CC0、MIT、BSD、Apache-2.0、Unicode License、CC-BY 4.0），并在 `NOTICE` 记录来源；不使用 CC-BY-SA、GPL/AGPL、专有或来源不明的数据。不写自定义法律文本；MIT 的免责条款适用于代码，数据按其自身许可提供。

| 数据 | 来源 | 许可 | 用法 |
|---|---|---|---|
| 字音 | Unihan `kMandarin`（经 `pinyin-data` 提取） | Unicode License | 单字普通话读音 |
| 词表/词频 | `jieba` `dict.txt` | MIT（仓库声明）+ `NOTICE` | 常用词与频率初值 |
| 词组读音 | 由字音生成 + 人工多音字覆盖表 | 本项目 MIT | 避免复制第三方词组拼音库 |
| 扩展词库 | 自建或 CC-BY 4.0 数据 | 按数据包 + `NOTICE` | 仅扩展包 |

简单规则：

- 每个语言包携带 `NOTICE`/`THIRD-PARTY-NOTICES`，列出数据来源和许可。
- 如果 `jieba` 词表的来源/许可无法确认，改用 5k–10k 自建常用词；扩展包顺延。
- 如果某个来源的许可无法确认，就替换为自建或 CC0/MIT 数据；不设单独的审核流程。
- 不复制参考实现、商业输入法或系统 IME 的数据。
- 不使用 CC-BY-SA、GPL/AGPL 词典（例如部分 Rime/libpinyin 数据）。

### 8.5 排序与质量

- 频率初值来自词表；人工校正高频常用词（你好、谢谢、中国、北京、时间、温度等）。
- 排序稳定、可复现；同一输入同一词典版本结果一致。
- 建立 golden set（100–200 条常用词/短语），验收 top-1/top-3 命中率；MVP 目标 top-1 ≥80%、top-3 ≥95%（常用集），扩展包不降低该指标。
- **这些是开发目标，不是对最终效果或稳定性的保证**；见 8.9。
- 多音字覆盖表先覆盖高频词（银行、长城、音乐、重庆、厦门等），后续按现场反馈补充。
- 不做上下文语言模型；整句候选只在词典/分词能给出合理路径时出现。

### 8.6 体积与性能目标

| 项 | 目标 |
|---|---|
| Core 增量 | +10–20 KB（组合态、契约、IME 键） |
| Avalonia 增量（不含语言包） | +25–40 KB（候选栏、组合渲染、本地化） |
| 拼音引擎程序集 | 40–80 KB（不含嵌入词典；`0.2.0` 的 DLL 内含词典，实测 226 KB / 1.13 MB） |
| 常用词库（2 万词） | 压缩后约 0.4–0.6 MB |
| 扩展词库（总量 10 万词） | 压缩后约 1.5–2.5 MB（待实测） |
| 常用词典首次加载 | <50 ms（目标，低端设备实测） |
| 扩展词典首次加载 | <200 ms（目标） |
| 单次查询 | p95 <5 ms（常用）/ <10 ms（扩展） |
| 组合按键到重绘 | <50 ms（含候选栏） |
| 常用引擎内存 | <10 MB（目标，实测后记录） |

体积以实际 pack 结果为准；`0.2.0` 本机实测见 [ime.md](../ime.md#包体积与性能本机实测非保证)（约 45 KB Core、83 KB Avalonia、427 KB Pinyin、2.2 MB Extended）。

### 8.7 其他语言

- 语言包只需实现 `IImeEngine`；Avalonia 的候选栏、IME 键、组合渲染、主题、无障碍都可复用。
- 该契约面向“罗马字/拼音输入 → 候选”的模型（中文、日文罗马字等）；像韩文谚文字母组合这类不同模型，未来可能需要扩展契约，0.2.0 不承诺。
- 语言特定的标点、排序、词典都在语言包内；Core 不出现语言分支。
- 0.2.0 不实现日语/韩语等具体语言，也不做语言切换 UI；宿主可以在启动时选择一个引擎。
- 未来多语言切换的扩展点在宿主层（选择 engine + 重建会话），不在 Core 内做语言市场。

### 8.8 版本与发布

- 语言包与 Core/Avalonia 同一 minor；补丁版本可独立更新词典。
- 扩展包依赖同版本常用包；不允许跨 minor 混装。
- 每个语言包独立 README、`NOTICE`、包图标和包内容检查。
- NuGet 预留 `TouchKeyboard.Ime.*`；发布文档记录 ID 和版本对齐规则。

### 8.9 许可与支持

- 代码 MIT；数据使用宽松/署名许可；不设商业限制或付费层，任何人都可以免费使用、修改和再分发。
- 软件和数据按各自许可以 **“as is”** 提供；不承诺候选质量、词库覆盖、性能或稳定性；维护为 best-effort，无 SLA。
- 不单独写复杂免责声明：MIT 的 `LICENSE` 加上数据包的 `NOTICE` 即可；如果某个数据来源的许可说不清，就不使用它。

---

## 9. 跨平台、字体与发布形态

### 9.1 纯托管

- Core、拼音包、扩展包都是 `netstandard2.0` 托管代码；无 P/Invoke、无原生库、无平台条件编译。
- 不调用系统 IME、不注入按键、不读取系统输入法状态。
- 输入仍走现有 `KeyCommand`/会话/适配器通道，三平台一致。

### 9.2 CJK 字体

- Windows：Microsoft YaHei UI；macOS：PingFang SC；Linux：Noto Sans CJK SC。
- Avalonia 默认字体回退在精简 Linux 镜像上可能缺字；发布文档和真机清单要求安装/打包 CJK 字体，或由宿主设置 `FontFamily` 回退链。
- 候选词、组合文本、字段标题都可能含 CJK；三者都要验证，不只验证键盘字母。
- 字体缺失时不允许出现方框/空白；验收时在无 CJK 字体的最小镜像上确认回退行为并记录。

### 9.3 AOT/裁剪

- 词典是嵌入资源；加载不依赖反射、动态代码或程序集扫描。
- 引擎类型由宿主显式 `new`，裁剪器可保留。
- 发布 smoke 增加 AOT/trim 发布检查：能加载常用/扩展词典、能查询、能渲染候选栏。
- `KeyCommandKind.ToggleIme` 是普通枚举值，无反射依赖。

### 9.4 OSK/IME 抑制

- 默认编辑面继续是自绘 `TouchKeyboardEditor`；它不创建平台文本输入，因此不会触发 OSK/IME。
- 候选栏是普通 Avalonia 控件，不获取文本输入焦点。
- IME 键、候选按钮都不进入 Tab 焦点链，避免触发平台输入法。
- 系统 IME 桥接和按键注入不在 0.2.0；需要它们的宿主仍走自定义编辑面/第三方方案。

---

## 10. 测试与验收

### 10.1 测试矩阵

| 层 | 覆盖 |
|---|---|
| Core 单测 | `AllowsIme` 门控；组合态开始/追加/退格/上限/翻页/选中/提交/取消；空格、数字、标点、中英、模式、页面、回车/确定；无候选回退；超长保留组合；`KeyboardState.Ime` 快照；IME 键布局与标签；**无 IME 时 Space 7.0/3.5、有 IME 键时 5.5/2.5、行总单位不变** |
| 兼容性测试 | 旧 `KeyboardState` 构造、旧 `KeyboardLayouts.Full` 调用、无引擎布局与 `0.2.0` 无 IME 状态一致；`KeyCommandKind` 新值不破坏现有 switch |
| 拼音引擎测试 | `nihao`/`beijing`/`bj`/`woaibeijing`；多音字；空输入；无候选；分页；top-N 上限；标点映射；排序确定性 |
| 语言包契约测试 | `LanguageTag`、查询边界、`maxCandidates`、异常/损坏数据 fail fast、`NOTICE` 存在 |
| Avalonia Headless | IME 键可见性矩阵（text vs 数字/日期/密码/ASCII）；中/英标签与无障碍名；**无 IME 空格扩展与有 IME 键恢复**；**中/英切换宽度不变**；候选栏固定高度；组合下划线帧；候选点按/数字/空格/回车；翻页；暗色主题；自定义编辑面降级；1024×600 帧 |
| 包消费 smoke | 只装 Avalonia（无 IME 行为、Space 扩展）；Avalonia + Pinyin（一行可用、Space 恢复）；Avalonia + Extended（一行可用）；不装语言包时包依赖不含拼音 |
| AOT/trim | 发布后加载词典、查询、渲染候选栏 |
| 真机 | Windows/Linux/macOS 中文输入、CJK 字体、OSK 不弹出、触摸尺寸、DPI、1024×600、空格/IME 键宽度 |

测试允许按包/特性拆分：Core、Pinyin、Avalonia、Extended、包 smoke、AOT/真机可以各自独立项目或 CI job；不要求维护一个巨型测试项目。

### 10.2 验收标准

1. 不装语言包、不调用 `UseIme` 时，行为和 `0.2.0` 无 IME 状态完全一致（含 Space 扩展）；包依赖图里没有拼音包。
2. `0.2.0` 无 IME 时，AnsiStyle Space 为 7.0、PhoneHybrid 为 3.5，行总单位仍为 15/10；不再有惰性 IME 槽位。
3. 装 `TouchKeyboard.Ime.Pinyin` 后，只加一行 `UseIme(new PinyinImeEngine())`，`text` 字段即可中文输入；有 IME 键时 Space 回到 5.5/2.5，中/英切换宽度不变。
4. 常用 golden set 的 top-1/top-3 达到第 8.5 节目标（开发目标，不是保证）。
5. 候选栏在组合/非组合、中英切换、单页/多页下高度不变；1024×600 不溢出、不遮挡编辑面。
6. 数字/日期/IP/密码/ASCII 档位没有 IME 键、没有候选栏、行为不变。
7. 组合中按确定/回车先完成候选再提交字段；超长时保留组合并提示。
8. 关闭/遮罩/Esc 丢弃组合和会话，不写回。
9. 明暗主题下候选栏、IME 键、组合下划线可读；候选/翻页目标 ≥44 px；无障碍名完整。
10. 三平台 CJK 字体正常、不弹系统 OSK、触摸输入可用。
11. 语言包 `NOTICE` 存在并记录数据来源/许可；README 含一行“不保证质量/稳定性”的说明；不要求单独的审核流程。

### 10.3 性能验收

- 常用词典首次加载 <50 ms；扩展 <200 ms（目标，低端设备实测记录）。
- 查询 p95 <5 ms（常用）/ <10 ms（扩展）。
- 组合按键到重绘 <50 ms。
- 面板打开到候选栏首帧无可感知延迟；候选翻页无卡顿。
- 常用引擎内存 <10 MB；扩展包实测后记录。

### 10.4 真机清单新增

- 8 寸 1280×800、10 寸 1024×600 Windows 触摸；Ubuntu X11/Wayland；macOS。
- DPI 100/125/150/200%。
- 中文输入：单字、常用词、整句、无候选、翻页、中英切换、全角标点。
- CJK 字体回退；无字体最小镜像。
- 系统 OSK 不弹出；物理键盘隔离；候选栏触摸尺寸。
- 词典加载耗时和内存。

---

## 11. 交付范围

| 范围 | 状态 |
|---|---|
| 无 IME 布局基线：移除 `spacer.ime`，Space 7.0/3.5，IME 键恢复 5.5/2.5 | 已实现 |
| Core：IME 契约、组合态、会话路由、IME 键、兼容重载 | 已实现 |
| 拼音包：常用/扩展词库、词典构建、`NOTICE`、golden set | 已实现 |
| Avalonia：IME 键、候选栏、组合渲染、主题、本地化、无障碍 | 已实现 |
| Demo、文档、四包 preflight/smoke、AOT/trim | 已实现 |
| Windows/Linux/macOS 真机、字体、DPI、OSK、性能实测 | 待执行 |

词典许可采用宽松/署名政策：使用 MIT、Unicode、CC-BY 4.0 等可再分发数据，并在
`NOTICE` 记录来源；来源无法确认时替换为自建数据。

## 12. 风险与缓解

| 风险 | 影响 | 缓解 |
|---|---|---|
| 数据许可不清 | 分发风险 | 只用宽松/署名数据；`NOTICE` 记录来源；来源不清就替换为自建/CC0 数据 |
| 词典质量不足 | 体验 | golden set、高频词人工校正、扩展包、现场反馈迭代 |
| 0.2.0 无 IME 布局变更 | 截图/测试/文档不一致 | 先改布局、测试和渲染；有 IME 键时恢复 5.5/2.5；行总单位验收 |
| 包体积 | 分发 | 常用 2 万词优先；扩展包可选；发布前实测并更新文档 |
| 基础 Avalonia 包 +25–40 KB | 体积敏感宿主 | 0.2.0 接受；若现场证明不可接受，再评估 P2 拆分，不提前增加包数 |
| Linux 缺 CJK 字体 | 显示 | 字体回退链、发布文档、真机清单 |
| 1024×600 候选行 | 布局 | 默认 44 px；PhoneHybrid 首行=键高、符号页隐藏、数字专用 4 行、Headless 帧验收 |
| 最大长度/候选插入冲突 | 正确性 | 保留组合 + `MaxLengthExceeded`；不静默截断 |
| AOT/裁剪 | 发布 | 嵌入资源、无反射、AOT smoke |
| 低端设备查询慢 | 体验 | 前缀索引/top-N 预计算、懒加载、性能基准 |
| 物理键盘 + 系统 IME | 平台差异 | 触摸为主；文档说明；必要时关闭 `HandlePhysicalKeyboard` |
| 多语言范围膨胀 | 维护 | 0.2.0 只做 zh+en；其他语言只保留契约 |
| 自定义编辑面无组合渲染 | 降级体验 | 组合文本由编辑面自行显示，候选栏不重复；不破坏 `IEditorSurface` 契约 |
| 用户对效果/稳定性的预期 | 信任/支持成本 | README 一行说明不承诺质量/稳定性；维护 best-effort |

---

## 13. 交付内容

### 13.1 代码

- **0.2.0 布局基线**：`KeyboardLayouts.Full` 在无 IME 时移除 `spacer.ime`，Space 扩展为 7.0/3.5；有 IME 键时恢复 5.5/2.5。同步更新布局矩阵、Avalonia 帧测试和渲染图。
- Core：`IImeEngine`、`ImeCandidate`、`ImeComposition`（内部）、`ImeCompositionState`、`AllowsIme`、`ToggleIme`、会话方法、布局 IME 键、兼容重载。
- Avalonia：`TouchKeyboardImeOptions`、`UseIme`、候选栏、IME 键、组合渲染、主题、本地化、无障碍、物理键盘路由。
- `TouchKeyboard.Ime.Pinyin`：引擎、解析/分词/排序、常用词库、数据构建脚本。
- `TouchKeyboard.Ime.Pinyin.Extended`：扩展引擎/词库。
- Demo：增加中文 `text` 字段和明暗/候选翻页演示；Demo 引用拼音包。
- 测试：Core/Pinyin/Avalonia/包 smoke/AOT/真机；可按包拆成独立项目和 CI job。

### 13.2 文档

- 新增 `docs/ime.md`：安装、一行接入、候选栏、主题、故障排查、包体积、许可/NOTICE。
- 更新 `docs/architecture.md`：Core 组合态、Avalonia IME UI、语言包边界、无 IME 布局基线。
- 更新 `docs/avalonia.md`：IME 选项、候选栏、组合渲染、自定义编辑面。
- 更新 `docs/profiles.md`：`AllowsIme` 矩阵。
- 更新 `docs/layouts.md`：无 IME 空格扩展与 IME 键恢复规则。
- 更新 `docs/release.md`：新包清单、包内容检查、许可/NOTICE。
- 更新 `docs/testing/device-checklist.md`：0.2.0 空格宽度、IME 中文输入、CJK 字体/OSK 项。
- 更新 `README.md` / `README.zh.md`：状态、安装示例、包表、已知限制、许可/质量说明。
- 更新 `CHANGELOG.md` 和版本号；`build/preflight.sh` 增加语言包包检查。
- ADR 0004、decisions D12/D17/D18/D19、roadmap 0.2.0 同步。

### 13.3 发布

- NuGet 预留 `TouchKeyboard.Ime.Pinyin`、`TouchKeyboard.Ime.Pinyin.Extended`。
- 每个包：README、icon、XML docs、符号包、`NOTICE`（数据来源/许可）。
- 包 smoke：只装 Avalonia / +Pinyin / +Extended 三种组合。
- CI：NOTICE/许可检查、AOT/trim、三平台矩阵。
- 发布前：golden set、真机清单、体积/性能数据、许可/NOTICE 检查。

### 13.4 明确不做（0.2.0）

- 系统 IME 桥接、按键注入、跨进程输入。
- 五笔、手写、语音、云输入、自学习、模糊音、繁简转换。
- 多语言切换 UI、语言市场、动态插件发现/热加载。
- 每字段持久化中英状态、跨进程同步。
- 其他语言的具体实现（只保留 `IImeEngine` 契约）。
- 公开的候选栏扩展 API/第三方 UI 插件。

---

## 14. 参考

- [决策记录](decisions.md)（D3、D4、D6、D7、D9、D12、D17、D18、D19）
- [ADR 0003 — v1 不做 IME](../adr/0003-no-ime-v1.md)（未发布的历史 v1 决策，已由 ADR 0004 取代）
- [ADR 0004 — 可选托管拼音 IME](../adr/0004-managed-pinyin-ime.md)
- [架构](../architecture.md)（核心纯度、会话、编辑面）
- [输入档位](../profiles.md)（text 与数字档位差异）
- [布局参考](../layouts.md)（IME 槽位位置）
- [Avalonia 接入](../avalonia.md)（选项、编辑面注入、适配器）
- [发布流程](../release.md)（新增包与包检查）
- [路线图](../roadmap.md)（0.2.0 版本计划）
- 数据许可：[Unicode License](https://www.unicode.org/license.txt)、[pinyin-data（MIT）](https://github.com/mozillazg/pinyin-data)、[jieba（MIT）](https://github.com/fxsjy/jieba)、[CC-CEDICT（CC BY-SA 4.0，仅作排除说明）](https://cc-cedict.org/wiki/)
