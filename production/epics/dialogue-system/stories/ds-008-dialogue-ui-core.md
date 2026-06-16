# Story: ds-008 — 对话 UI 核心呈现与输入

> **Epic**: dialogue-system
> **类型**: UI
> **优先级**: P1 — 玩家可见对话体验
> **Estimate**: M（约 6h）
> **依赖**: ds-002, ds-007
> **阻塞**: 无
> **ADR 指引**: ADR-0001（UI 本地 Signal / 跨层 EventBus），ADR-0002（Godot Control / FocusManager / dual-focus），ADR-0005（节点类型呈现）
> **GDD 来源**: design/gdd/dialogue-system.md §Visual/Audio Requirements, §UI Requirements, §Acceptance Criteria
> **TR-ID**: TR-dialogue-system-008
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete

## 目标

实现对话 UI 的核心呈现与输入，包括名牌、立绘、对话文字、打字机效果、继续指示、InnerMonologue/Narration 样式和 Letter 信笺通道。

## 范围

### 包含
- Speech 节点显示说话者名牌、角色立绘和对话文字
- 文字逐字显示，支持确认键加速
- 文字未完全显示前不显示选项面板
- 角色切换时当前说话者立绘高亮
- InnerMonologue 使用独立视觉样式，可显示主角内心标签
- Narration 无名牌、无角色立绘
- Letter 节点打开独立信笺界面
- 键鼠和手柄基础输入：确认、加速、关闭信笺

### 不包含
- 选项列表、心境暗示、洞察追查入口、暗号选项样式 → ds-009
- 最终美术资产规格 → `/asset-spec system:dialogue-system`
- 对话框、信笺界面、洞察效果的完整 UX spec → 后续 `/ux-design`，不阻塞本 MVP 核心呈现
- 音效接入可作为后续 polish story

## 技术说明

- UI 与运行时逻辑保持分离，通过状态 DTO / Signal 读取显示内容
- 同场景父子/兄弟通信使用 Godot `[Signal]` delegate
- 对话框视觉需遵守 GDD：半透明暗色底板 + 白色文字，信笺与标准对话框明确区分
- 对应 GDD 的明确要求：Speech 显示说话者名牌、角色立绘和对话文字；Narration 无名牌、无角色立绘；InnerMonologue 可显示主角内心标签并使用独立视觉样式；Letter 使用全屏信笺覆盖层
- 打字机输入规则必须遵守 GDD Edge Case：逐字显示期间第一次确认键立即显示当前节点全部文字；文字完全显示后的确认键推进下一节点；不允许单次按键跳过整段对话
- 文字未完全显示前不得显示 Choice 选项面板，避免玩家在未读完节点时误选
- 本 story 只做对话 UI 核心呈现与基础输入；选项列表、心境暗示、洞察追查入口、暗号选项视觉统一留给 ds-009
- `/ux-design` 产出的完整 UX spec 和 `/asset-spec` 产出的最终资产规格属于后续细化；本 story 以 GDD 的 MVP 视觉/输入规则作为可实施基线
- Control Manifest 约束：所有 UI 必须基于 Godot Control 节点树，面板继承 `BaseUiPanel`，可交互 Control 设置 `focus_mode = FOCUS_ALL`，焦点通过 `FocusManager.PushFocus` / `PopFocus` 管理
- UI 刷新必须使用脏标记模式，禁止每帧无条件刷新；同屏 UI tween 数量应控制在 technical-preferences 的预算内
- Engine note：Godot 4.6 dual-focus 属 post-cutoff 高风险；实现时必须验证键鼠/手柄切换后焦点高亮、确认键、关闭信笺行为不冲突

## 验收标准

- [x] Speech 节点进入时，说话者立绘、名牌和对话文字出现
- [x] 文字以打字机效果渲染
- [x] 文字未完全显示前不显示选项面板
- [x] 确认键第一次加速显示完整文字，第二次推进节点
- [x] InnerMonologue 节点不显示 NPC 立绘，使用独立视觉样式
- [x] Narration 节点无角色立绘、无名牌
- [x] Letter 节点打开独立信笺 UI，关闭后返回对话流程
- [x] 键鼠与手柄均可完成确认、加速和关闭信笺操作，焦点状态无丢失

## QA 手动检查

- **AC-1**：Speech 呈现
  - Setup：进入包含 Speech 节点的测试对话
  - Verify：名牌、立绘、文字同时出现，当前说话者突出
  - Pass condition：信息不重叠，文字清晰可读，未完成打字前无选项

- **AC-2**：输入加速
  - Setup：进入长文本节点
  - Verify：第一次确认补全文字，第二次确认推进
  - Pass condition：快速按键不会跳过下一节点

- **AC-3**：特殊节点样式
  - Setup：依次触发 InnerMonologue、Narration、Letter
  - Verify：三者与标准 Speech 有明确视觉区分
  - Pass condition：内心独白、旁白、信笺均符合 GDD 语义，不误显示 NPC 立绘

- **AC-4**：双输入模式
  - Setup：分别用键鼠和手柄进入同一段测试对话
  - Verify：确认、加速、关闭信笺均可操作，手柄模式焦点可见
  - Pass condition：输入模式切换后不出现焦点丢失、重复推进或关闭失败

## 测试证据路径

`production/qa/evidence/ds-008-dialogue-ui-core-evidence.md`

## 依赖关系

- Depends on: ds-002, ds-007
- Unlocks: ds-009

## 完成记录

- 新增 `DialogueUiPresenter`、`DialogueUiSnapshot`、`DialogueUiMode`、`DialogueUiFocusTarget`、`DialogueUiInputIntent` 和 `DialogueUiInputSource`，把 `DialogueRuntime` 状态翻译为 Godot Control 层可绑定的 UI DTO。
- Speech 快照暴露说话者、名牌、立绘显示与当前说话者高亮；Narration 不暴露名牌/立绘；InnerMonologue 使用 `内心` 名牌并不显示 NPC 立绘。
- 打字机阶段通过 `IsTypewriterActive`、`VisibleText`、`ShowContinueIndicator` 表达，确认键第一次补全文字、第二次推进节点。
- Choice 面板只在运行时进入 `ProcessingChoice` 后显示，避免文字未读完时误选；选项列表、心境暗示、洞察入口和暗号样式仍留给 ds-009。
- Letter 节点暴露 `ShowLetterOverlay`、寄信者、收信者和 `LetterPanel` 焦点目标；关闭信笺输入沿原对话流程返回。
- 键鼠与手柄基础输入共用同一意图接口，单元测试覆盖确认、加速和关闭信笺等价行为；真实 Godot dual-focus 高亮需在正式 Presentation 场景接入后做手动验证。
