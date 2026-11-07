# 中文输入与语言包扩展（开发计划草案）

> **状态**：v0.1 草案 · 2026-11-08 · **本文只做规划与选项整理，不作任何决策**
> **范围**：中文（拼音）输入的可选接入方式、跨平台方案、最终呈现效果、体积与语言包插件化
> **未改动**：`docs/design/decisions.md` 的 D12 与现有 ADR 保持原样；路线、词典、包结构、版本目标均为**开放问题**，见第 9 节。
> **本地存档说明**：原《2026-11-06 触摸键盘独立化方案》位于未入库的 `docs/local/`，本文不链接它。

---

## 0. 摘要

- 现状：v1 只支持英文/数字/符号；核心已为输入法预留 `spacer.ime` 空位与 `KeyGlyph.Ime`，但没有 IME 机制。
- 原计划的“中文输入”只留下两条衔接：**D12 暂不做**、**未来可选注入包**；参考实现的 X11 XTEST + IBus 注入恰恰是原方案明确删除的部分。
- 本文整理三条候选路线（托管拼音 / 系统 IME 桥接 / 按键注入），给出若选托管拼音时的接入设计、跨平台做法和最终交互效果，并回答三个补充问题：
  1. **体积影响**：按当前包基线估算，中文预览通常增加约 **0.2–0.5 MB**（小词典）到 **1.3–1.5 MB**（5 万词级）；完整大词典可到数 MB。详见第 6 节。
  2. **语言包插件化**：可行。基础前端不依赖任何语言包；中文、未来其他语言各自是独立 NuGet 包，只装需要的语言。英文直接走现有缓冲编辑，不需要引擎包。详见第 7 节。
  3. **范围收敛**：产品核心输入只需**中文 + 英文**；其他语言只保留扩展空间，不承诺实现，也不在预览范围。详见第 8 节。

> 上述为**初评倾向**，不是已确认决策。第 9 节列出待评审的开放问题。

---

## 1. 背景与原计划约束

| 位置 | 原约定 |
|---|---|
| 原方案 §1.2 非目标 | 不做中文/拼音/五笔输入法引擎、不做候选词、不接 IBus/IME；不做 XTEST/SendInput/CGEvent 注入 |
| D12 | 中文输入：v1 不做；未来可选注入包 |
| 原方案 §2.3 风险表 | 参考实现用 X11 XTEST + IBus 切中英，“平台坑多”；v1 整层删除，未来需要再做可选注入包 |
| 原方案 §6.7 | 自绘编辑面 + Host 持有焦点，避免触发系统 OSK/IME |
| 原方案 §9.4 | 已知限制：只支持英文/数字/符号，不支持中文 IME |

现有代码中可复用的预留点：

- `KeyboardLayouts` 的 `spacer.ime`：AnsiStyle 位于空格与清空之间（1.5 单位），PhoneHybrid 位于右光标与清空之间（1 单位）；符号页没有 IME 槽位。
- `KeyGlyph.Ime` 已预留，但尚未有布局使用。
- `text` 档位的中间态校验只拒绝控制字符，中文可以正常写入会话缓冲并由适配器写回；数字/日期/IP/密码/ASCII 档位不适合中文输入。
- `TouchKeyboardEditor` 默认自绘，未接入系统文本输入；`IEditorSurface` 提供注入点。
- `InputProfile` 目前没有 IME 能力标记；`KeyCommandKind` 没有 IME 开关命令；编辑面没有组合态渲染。

因此，**原计划没有可执行的 IME 设计方案**，只有扩展位和“以后可选注入包”的意向；加中文输入需要新的决策记录（修订 D12，新增 ADR），本文暂不写入。

---

## 2. 目标、范围与非目标

### 2.1 目标（若实施）

- 中文用户可在 `text` 档位使用拼音输入并选择候选词。
- 英文、数字、符号、现有档位、主题、无障碍与提交语义不受影响。
- Windows / Linux / macOS 行为一致，不依赖平台 IME、不触发系统 OSK、不注入按键。
- 语言能力以**可选 NuGet 包**形式交付：只用中文时只安装中文包。
- 为未来语言保留扩展契约，但不实现、不承诺其他语言。

### 2.2 非目标（当前规划不做）

- 五笔、手写、语音、整句预测、云输入、候选词自学习云同步。
- 系统输入法接管、按键注入默认开启、跨进程输入。
- 多语言切换 UI、语言市场、动态插件发现/热加载。
- 其他语言（日文、韩文、法文等）的具体实现；只保留接口空间。

---

## 3. 候选路线对比（不选型）

| 维度 | A. 托管拼音引擎 | B. 系统 IME 桥接 | C. 按键注入（原 D12 的“注入包”） |
|---|---|---|---|
| 机制 | 纯托管拼音词典 + 组合态 + 自绘候选栏 | 实现 Avalonia 12 `TextInputMethodClient`，交给平台 IME | SendInput / CGEvent / XTEST，把按键注入目标控件，由宿主 IME 处理 |
| 跨平台一致性 | **强**：同一套代码，无原生依赖 | 取决于 Avalonia 后端与系统输入法环境 | **弱**：三套原生实现；Wayland、macOS 辅助功能权限、Windows 完整性级别都是坑 |
| 候选/组合 UI | 完全可控、可主题化、触摸尺寸可调 | 系统绘制，风格不可控，可能与面板主题不一致 | 完全依赖宿主系统 IME |
| 工控/kiosk 适配 | 高：不依赖系统 IME，不触发 OSK | 低：锁定设备常无系统输入法；可能重新触发 OSK | 最低：正是原方案删除的那层 |
| 输入质量 | 自研，取决于词典与排序 | 高（系统拼音通常成熟） | 高（宿主 IME 的完整能力） |
| 主要风险 | 词典许可、质量、体积、维护 | 平台差异、OSK 行为、候选窗不可控 | 平台差异、权限、安全与焦点 |
| 估算工作量 | 约 2–3 周（拼音 MVP） | 约 3–5 天编码 + 三平台真机验证 | 独立包，每平台单独验证 |

**初评倾向（未决）**：A 作为默认可选路线；B 作为桌面环境已有输入法时的可选适配器；C 继续留在“未来可选注入包”，不进默认路径。

---

## 4. 方案 A 的接入设计（若采纳）

### 4.1 分层与包结构

```text
TouchKeyboard.Core (netstandard2.0)
  ├─ 新增：ImeComposition（UI 无关的组合态状态机）
  ├─ 新增：IImeEngine / ImeCandidate（引擎抽象）
  ├─ 新增：KeyCommandKind.ToggleIme
  └─ 新增：InputProfile.AllowsIme（默认 false，text 为 true）

TouchKeyboard.Ime.Pinyin (netstandard2.0, 可选语言包)
  └─ 拼音解析、分段、排序、候选分页、词频；词典数据随包

TouchKeyboard.Avalonia (现有前端)
  ├─ IME 键：替换 spacer.ime（中/英状态、无障碍名称）
  ├─ 候选栏控件（主题化、触摸尺寸、翻页）
  ├─ TouchKeyboardEditor 组合态渲染（拼音下划线、光标）
  └─ Host 按键路由：组合态优先拦截，再交给 Session
```

语言包只依赖 Core 的 IME 抽象；基础 Avalonia 包不依赖任何语言包。英文不需要引擎包，沿用现有缓冲编辑。

### 4.2 核心与前端接入点

**Core（小改动，保持 UI-free）**

- `InputProfile.AllowsIme`：`virtual bool AllowsIme => false`；`TextInputProfile` 返回 `true`。数字/日期/IP/密码/ASCII 默认 `false`，自定义档位可显式打开。
- `KeyCommandKind.ToggleIme`：与 `Submit`/`Close` 一样由 Host 处理，`KeyboardSession.Execute` 不消费。
- `ImeComposition`：输入串、候选、分页、选中项、`Append/Backspace/NextPage/PreviousPage/Select/Cancel`；不进入会话缓冲。
- `IImeEngine`/`ImeCandidate`：语言无关的查询契约；语言包实现它。

**Avalonia 前端**

- `TouchKeyboardOptions` 增加（名称待定）：
  ```csharp
  public IImeEngine? ImeEngine { get; set; }        // null = 不启用（默认，行为不变）
  public string? ImeDefaultLanguage { get; set; }   // "zh-CN" / "en-US"
  public int ImeCandidatePageSize { get; set; } = 6;
  ```
- `KeyboardLayoutView` 增加 `ImeSlot`（`KeyDefinition?`）：启用 IME 且档位 `AllowsIme` 时，把 `spacer.ime` 替换为“中/英”键；不启用时保持现状。核心布局数据不变。
- `TouchKeyboardEditor`：增加组合态渲染（组合文本、光标、下划线/高亮）；`IEditorSurface` 增加可选的组合态接口，自定义编辑面不实现时降级为“候选栏显示拼音”。
- Host 按键路由：组合态优先交给 `ImeComposition`；选中候选后 `session.Execute(KeyCommand.Insert(text))`；未消费再走原 Session 命令。
- 本地化新增：`key.ime.chinese`（中）、`key.ime.english`（英）、`ime.candidates`、`ime.previous`/`ime.next`、`ime.no-candidate` 等。
- 主题新增：`CompositionText`、`CompositionUnderline`、`CandidateBackground`、`CandidateSelectedBackground`，明暗两套预设。

**宿主接入（若采用 A）**

```csharp
// TouchKeyboard.Avalonia（已有）+ TouchKeyboard.Ime.Pinyin（仅中文时安装）
KeyboardHost.Configure(options =>
{
    options.ImeEngine = new PinyinImeEngine();
    options.ImeDefaultLanguage = "zh-CN";
    options.ImeCandidatePageSize = 6;
});

// text 档位自动支持；自定义档位按需打开
public override bool AllowsIme => true;
```

适配器、校验、提交不变：候选文本走既有 `Insert`，最终仍由 `TryCommit` → `IEditorAdapter.TryCommit` 写回。

### 4.3 组合态按键规则（草案）

| 组合中操作 | 行为 |
|---|---|
| `a–z` | 追加拼音（上限 32 字符），刷新候选 |
| `1–9` / 点候选 | 选中对应候选并提交 |
| `空格` | 提交第 1 个候选 |
| `回车` | 提交当前选中候选，不提交字段 |
| `确定` | 先完成组合（提交候选），再走字段校验/提交 |
| `退格` | 删除一个拼音字母；清空后退出组合态 |
| `←`/`→` | 移动候选高亮（配合翻页） |
| `Esc` / 遮罩 / 关闭 | 取消组合并丢弃会话 |
| `中/英` | 先完成组合，再切换语言 |
| `123` / `符号` | 先完成组合，再切换模式/页面 |
| `清空` | 清空组合与缓冲 |
| 标点/符号 | 先完成组合，再插入；中文模式可映射全角标点（可配置） |
| 无候选 | 候选栏提示“无候选”，允许原样提交拼音字母 |

### 4.4 最终呈现效果

```text
┌ 名称 ──────────────────────────────────────────────┐
│ 你好世界▍                                          │  ← 编辑面；组合中的拼音带下划线
│ 1 你好   2 尼豪   3 你   4 拟   5 呢    ‹  ›      │  ← 候选栏（主题化、触摸尺寸）
├────────────────────────────────────────────────────┤
│ ` 1 2 3 …                                         │
│ q w e …                                           │
│ a s d …                                           │
│ Z X C V …                                         │
│ 123  ←  →  [ 空格 ] [ 中 ] 清空  确定              │  ← IME 键占用预留槽位
└────────────────────────────────────────────────────┘
```

1. zh-CN 环境默认显示“中”，英文环境默认“英”；点按切换。数值/日期/IP/密码/ASCII 档位不显示 IME 键。
2. 中文模式输入 `n i h a o`：编辑面在光标处显示带下划线的 `nihao`，候选栏出现编号候选；拼音不进入会话缓冲。
3. 点候选或按 1–9、空格选第一个：拼音替换为“你好”，候选栏隐藏，光标移到词后，`IsDirty` 才置位。
4. 候选超过一页显示 ‹ ›；长拼音自动分段（`woaibeijing` → 我爱北京）；首字母缩写（`bj` → 北京）为可选增强。
5. 切英文继续普通输入；中文模式下标点可自动全角。
6. 组合中按“确定”先提交候选再校验字段；超长导致候选插不进去时保留组合并提示；关闭/切字段/遮罩丢弃组合。
7. 候选栏、IME 键、组合下划线跟随明暗主题；触摸目标 ≥44px；面板高度在组合开始/结束时不变（预留候选行或覆盖层，待定）。
8. IME 键与候选都有本地化无障碍名称。

---

## 5. 跨平台支持

### 5.1 方案 A

- 引擎、组合态、词典全部是 `netstandard2.0` 托管代码，三平台行为一致。
- 不调用 IME/注入/原生 API，不触发系统 OSK，符合原方案 §6.7。
- 词典以 UTF-8 资源/数据文件分发，不依赖区域设置。
- 唯一平台差异是 **CJK 字体**：Linux 精简镜像可能缺少中文字体，需要字体回退（Windows：Microsoft YaHei UI；macOS：PingFang SC；Linux：Noto Sans CJK SC），并列入真机清单。
- 输入走现有 `KeyCommand` 通道，无触摸/鼠标差异。

### 5.2 方案 B

- 通过 Avalonia 12 的 `TextInputMethodClient` / `TextInputMethodManager` / `TextInputMethodClientRequestedEvent` 接入平台 IME。
- Windows 后端相对成熟；macOS 取决于 IMK；Linux 取决于 Avalonia 后端与 IBus/FCITX 环境。
- 候选窗由系统绘制，样式不可控；实现 IME 客户端后是否重新触发 OSK 需要逐平台实测。
- 适合“桌面环境已有输入法”的可选模式，不适合锁定设备默认模式。

### 5.3 方案 C

- Windows SendInput、macOS CGEvent、Linux XTEST/uinput，各自处理权限（macOS 辅助功能、Linux uinput 权限/Wayland 限制）与焦点。
- 只作为宿主已有系统 IME、且必须由目标控件自身处理输入时的独立可选包；不进核心和默认前端。

---

## 6. 补充分析 1：方案 A 的程序体积影响

### 6.1 当前实测基线（2026-11-08）

| 产物 | 大小 |
|---|---|
| `TouchKeyboard.Core` nupkg | 约 39 KB |
| `TouchKeyboard.Avalonia` nupkg | 约 72 KB |
| `TouchKeyboard.Core.dll` | 47 KB |
| `TouchKeyboard.Avalonia.dll`（每个 TFM） | 56 KB |

### 6.2 方案 A 的组成估算

以下为**估算**，实际取决于词典格式、词条数、词频编码与压缩方式：

| 组成 | 估算 | 说明 |
|---|---|---|
| 拼音引擎程序集 | 30–80 KB | 解析、分词、排序、分页；纯 IL |
| 基础前端新增 IME UI | 20–40 KB | 候选栏、组合态渲染、IME 键；若放独立包则为 0 |
| 小词典（3k–5k 词） | 原始 0.12–0.20 MiB / 压缩 0.07–0.12 MiB | 约 70–120 KB |
| 中词典（2 万词） | 原始 0.80 MiB / 压缩 0.48 MiB | 约 0.5 MB |
| 大词典（5 万词） | 原始 2.0 MiB / 压缩 1.2 MiB | 约 1.2 MB |
| 超大词典（20 万词） | 原始 8.0 MiB / 压缩 4.8 MiB | 不推荐随包 |
| 语言包元数据/许可见 `NOTICE` | <10 KB | 视许可要求 |

估算假设：平均词长 3 个汉字（UTF-8 每字 3 字节）、拼音键平均 9 字符、词频 2 字节、索引/偏移开销 ×2、deflate 压缩率约 0.6。

### 6.3 不同交付方式对体积的影响

| 方式 | 基础包 | 应用新增 |
|---|---|---|
| 只装英文（现状） | 39 KB / 72 KB | 0 |
| 中文小词典，UI 在基础前端 | 基础 Avalonia +20–40 KB | 引擎 + 小词典 ≈ 0.1–0.2 MB |
| 中文小词典，UI 独立包 | 基础包不变 | UI + 引擎 + 小词典 ≈ 0.15–0.25 MB |
| 中文 2 万词 | 视 UI 归属 | ≈ 0.6–0.7 MB |
| 中文 5 万词 | 视 UI 归属 | ≈ 1.3–1.5 MB |
| 未来每增加一种语言 | 基础包不变 | 只增加该语言包（词典为主） |

补充说明：

- NuGet 包大小与最终应用发布大小不同：自包含/单文件发布会再压缩，但嵌入资源的数据量不会消失；裁剪（trimming）主要减代码，不会自动减词典数据。
- 压缩格式、词频编码（1/2 字节）、前缀共享/最小完美哈希都能显著影响最终体积；具体格式确定后再实测。
- 词典可放独立数据包或由宿主提供，允许“程序不带词典、运行时加载”。这是后续可选项，不在本计划中决定。

---

## 7. 补充分析 2：语言包与插件化（只装中文包）

### 7.1 可行性结论

**可以**。语言包以 NuGet 包形式交付即可满足“只装中文时只下载中文包”：

```xml
<!-- 只用英文：现状，无 IME 包 -->
<PackageReference Include="TouchKeyboard.Avalonia" Version="..." />

<!-- 需要中文：额外只装中文包 -->
<PackageReference Include="TouchKeyboard.Avalonia" Version="..." />
<PackageReference Include="TouchKeyboard.Ime.Pinyin" Version="..." />
```

英文不需要引擎包：字母直接写入会话缓冲，现有键盘已经支持。中文包不被引用时，NuGet 不会下载、应用不会包含其引擎与词典；基础前端在未配置引擎时也不显示 IME 键（行为与今天一致）。

### 7.2 包与契约（待定，候选）

| 方案 | 结构 | 优点 | 代价 |
|---|---|---|---|
| P1 | Core 放 IME 抽象；`TouchKeyboard.Ime.Pinyin` 实现引擎并自带词典；前端含 IME UI | 包少、接入简单；只装中文 | 基础前端 +20–40 KB IME UI（即使不用） |
| P2 | Core 抽象；`TouchKeyboard.Avalonia` 不含 IME UI；`TouchKeyboard.Avalonia.Ime` 提供候选栏/组合渲染；语言包只放引擎+词典 | 基础包零增量；只有需要时才带 UI | 包更多、宿主接线更复杂 |
| P3 | 一个总包包含所有语言 | 接入最简单 | 违背“只装需要的语言”，体积最大；不推荐 |

建议（未决）：P1 或 P2 二选一，取决于“基础包是否愿意承担 IME UI 代码”的取舍。

语言包契约草案：

```csharp
public interface IImeEngine
{
    string LanguageTag { get; }                 // "zh-Hans"
    string DisplayName { get; }                 // 本地化显示名（由前端本地化）
    bool TryQuery(string input, int offset, int maxCandidates,
                  out IReadOnlyList<ImeCandidate> candidates);
}

public sealed class ImeCandidate
{
    public string Text { get; }
    public double Score { get; }
}
```

- **显式注册**：宿主 `new PinyinImeEngine()` 或通过一个很薄的注册表注册；不做程序集扫描/反射发现，避免裁剪、AOT 与启动顺序问题。
- **版本对齐**：语言包依赖 Core 的 IME 抽象（同一 major/minor），词典更新可用 patch 独立发布。
- **许可与署名**：每个语言包携带自己的词典许可与 `NOTICE`；中文词典需先解决“自建/公开许可”问题（见风险）。
- **多语言（未来）**：宿主可注册多个引擎并提供一个语言切换 UI；当前范围不需要，不在预览实现。
- **体积**：语言包体积基本等于“引擎 + 该语言词典”；只装中文时，其他语言包为零。

### 7.3 英文的位置

英文不需要语言包，也不需要 IME 引擎；`text`/`password` 等档位继续直接写缓冲。IME 键仅在配置了至少一个引擎且档位 `AllowsIme` 时出现；没有引擎时与今天完全一致。

---

## 8. 补充分析 3：范围收敛（中文 + 英文为核心需求）

产品核心输入需求是**中文 + 英文**；其他语言只是未来升级空间，不承诺实现。本计划据此约束设计：

- 只定义一个最小的语言包契约，不为“语言市场/动态加载/DI 扫描”做基础设施。
- 英文为内置直写，不做语言包。
- 中文只做拼音（是否包含五笔/手写，见开放问题）。
- 多语言切换 UI、语言列表、语言自动检测均不在预览范围；宿主可未来自行注册多个引擎。
- 文档与 roadmap 把其他语言列为“future / possible”，不列入待办承诺。

这样既满足“以后加语言只加包”的扩展性，又避免为不存在的需求过度设计。

---

## 9. 开放决策清单（全部未决）

| ID | 问题 | 候选 | 影响 |
|---|---|---|---|
| D-IME-1 | 路线 | A / B / C / A+B 混合 | 架构、跨平台、OSK、工作量 |
| D-IME-2 | 中文输入范围 | 仅拼音 / 拼音+五笔 / 手写 | 引擎复杂度、词典、UI |
| D-IME-3 | 词典来源与许可 | 自建高频词 / 公开许可数据 / 宿主自备 | 合规（clean-room）、体积、质量 |
| D-IME-4 | 包结构 | P1（UI 在基础前端）/ P2（独立 UI 包） | 基础包体积、包数量、接线复杂度 |
| D-IME-5 | 候选栏位置 | 预留行（高度稳定）/ 浮层覆盖 | 小屏可用性、实现复杂度 |
| D-IME-6 | 默认语言与记忆 | 跟随系统 / 宿主配置 / 会话记忆 | 交互一致性 |
| D-IME-7 | 系统 IME 桥接 | 不做 / 作为可选适配器 | 平台验证成本、OSK 风险 |
| D-IME-8 | 注入包 | 继续不做 / 作为独立可选包 | 平台差异、权限、安全 |
| D-IME-9 | 中文标点 | 不映射 / 映射全角 / 可配置 | 输入体验 |
| D-IME-10 | 发布版本 | 并入 `0.1.0-preview.1` / `preview.2` / `0.2.0` | 发布节奏与测试范围 |

本次不改 `docs/design/decisions.md` 与 ADR；上述任一项确认后，按第 12 节联动更新。

---

## 10. 条件式里程碑与工作量（取决于第 9 节决策）

| 里程碑 | 内容 | 估算（方案 A） | 依赖 |
|---|---|---|---|
| M0 | 路线、词典许可、包结构、范围确认 | 1–3 天（评审 + 法务/数据确认） | 第 9 节决策 |
| M1 | Core IME 抽象 + 组合态状态机 + 假引擎 + 单测 | 2–3 天 | M0 |
| M2 | 拼音引擎 + 小词典 + 排序/分词 + 单测 | 4–6 天 | M1、词典数据 |
| M3 | Avalonia IME 键、候选栏、组合渲染、按键路由 + Headless 测试 | 4–6 天 | M1 |
| M4 | Demo、本地化、主题、文档、真机清单、发布检查 | 3–4 天 | M2、M3 |
| M5（可选） | 系统 IME 桥接或注入包 | 3–5 天 + 三平台验证 | M0 |

方案 B 的编码量较小（3–5 天），但需要三平台真机验证系统 IME/OSK；方案 C 每平台单独验证，不建议作为默认路线。总工期（A）约 **2–3 周**，不含词典质量长期打磨。

---

## 11. 测试与验收草案（若实施 A）

- Core 单测：`AllowsIme` 门控、`ImeComposition` 状态机、拼音解析/分段/排序/分页、无候选回退、最大长度边界。
- 语言包契约测试：引擎元数据、查询边界、分页、空输入。
- Avalonia Headless：IME 键可见性矩阵（text vs 数字/日期/密码）、组合渲染帧、候选点击/数字选择/空格提交、确定前完成组合、模式切换提交、暗色主题、面板高度不变。
- 真机清单新增：三平台中文输入、CJK 字体回退、候选触摸尺寸、系统 OSK 不弹出、词典加载耗时。
- 包消费 smoke：只安装 `TouchKeyboard.Avalonia` + 中文包时可用；不安装语言包时行为与今天一致。
- 性能目标（待确认）：词典懒加载 <50 ms，按键查询 <5 ms（2 万词级），组合输入上限 32 字符。

---

## 12. 决策后的联动更新（现在不做）

任一路线确认后，需要同步：

- `docs/design/decisions.md`：修订 D12（以及 D3/D4/D9 中受影响项）。
- 新增 ADR（例如 `docs/adr/0004-ime-route.md`），记录路线、词典许可与包结构。
- `docs/roadmap.md`：把中文输入从“候选特性”调整为对应里程碑；其他语言保持 future。
- `docs/architecture.md`、`docs/profiles.md`、`docs/avalonia.md`、`docs/release.md`：补 IME 抽象、`AllowsIme`、候选栏、包清单与发布检查。
- `README.md` / `README.zh.md`：更新状态、已知限制与安装示例。
- `CHANGELOG.md`、版本号、`build/preflight.sh` 包清单、CI/release 的包 smoke。
- NuGet 包 ID 预占（`TouchKeyboard.Ime.*`），并在发布文档记录。

---

## 13. 风险与待确认资料

| 风险/待确认 | 影响 | 备注 |
|---|---|---|
| 词典许可与 clean-room | **发布阻断级** | 不得复制参考实现数据；需明确自建或公开许可数据及署名方式 |
| 中文词典质量 | 体验 | 词频、分词、缩写、模糊音；MVP 可先小词典 |
| Linux 字体 | 显示 | 精简镜像可能无 CJK 字体；需字体回退与真机验证 |
| 1024×600 候选栏高度 | 布局 | 预留行会加高面板；浮层需避免遮挡编辑面 |
| 与最大长度/校验的交互 | 正确性 | 组合文本不计长度；候选插入可能被拒绝，需要提示 |
| 系统 IME 桥接触发 OSK | 工控适配 | 需三平台实测；默认关闭 |
| 多语言范围膨胀 | 维护 | 只做 zh+en；其他语言不承诺 |
| AOT/裁剪 | 发布 | 避免反射发现；嵌入资源保持；发布验证 |
| 包体积 | 分发 | 小词典优先；大词典独立包/宿主自备 |

需要补充的资料（供评审）：

1. 目标设备是否已有可用系统输入法（决定 B 是否有意义）。
2. 可接受的中文词典体积与词条规模。
3. 可接受的中文词典数据来源与许可。
4. 是否需要五笔/手写（当前建议仅拼音）。
5. 预览版本的时间窗口（决定 D-IME-10）。

---

## 14. 参考

- [决策记录](decisions.md)（D3、D4、D5、D6、D9、D12）
- [架构](../architecture.md)（核心纯度、会话、编辑面）
- [输入档位](../profiles.md)（text 与数字档位差异）
- [布局参考](../layouts.md)（IME 预留槽位位置）
- [发布流程](../release.md)（新增包与包检查）
- [路线图](../roadmap.md)（候选特性与版本计划）
- [Avalonia 接入](../avalonia.md)（选项、编辑面注入、适配器）
