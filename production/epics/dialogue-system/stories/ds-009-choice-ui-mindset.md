# Story: ds-009 — 选择 UI、心境暗示与特殊选项呈现

> **Epic**: dialogue-system
> **类型**: UI
> **优先级**: P1 — 选择重量与武侠味表达
> **Estimate**: M（约 6h）
> **依赖**: ds-003, ds-005, ds-006, ds-008
> **阻塞**: 无
> **ADR 指引**: ADR-0005（Choice / InsightPrompt / CodePhrase），ADR-0001（UI 输入事件边界），ADR-0002（Godot Control / FocusManager / dual-focus）
> **GDD 来源**: design/gdd/dialogue-system.md §3 选择系统, §4 洞察机制, §5 暗号系统, §Visual/Audio Requirements, §UI Requirements, §Acceptance Criteria
> **TR-ID**: TR-dialogue-system-009
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete

## 目标

实现选择 UI 和特殊选项呈现，让标准选择、心境选择、洞察追查入口、暗号选项在视觉上清晰区分，并确保选择确认后不可撤销。

## 范围

### 包含
- Choice 节点显示可见选项列表
- 选项按编辑顺序显示
- 心境选择显示文学暗示，不显示具体数值
- 暗号选项使用独特样式标记
- 洞察提示出现后，玩家可选择追查或推进
- 选择确认后短暂高亮并立即沿分支推进
- 无确认对话框、无撤销按钮
- 键鼠和手柄选项导航、确认

### 不包含
- 条件过滤逻辑 → ds-003
- 洞察门槛逻辑 → ds-005
- 暗号匹配逻辑 → ds-006
- 对话框核心名牌、立绘、打字机、信笺通道 → ds-008
- 心境位移数值结算、关系变化、下游事件派发 → ds-004 及对应 Feature 系统
- 对话历史回看，该问题仍由 GDD Open Questions 跟踪，不进入本 story
- 选择界面、洞察效果的完整 UX spec → 后续 `/ux-design`，不阻塞本 MVP 选择 UI
- 最终音效资产接入

## 技术说明

- 本 story 只消费运行时已计算出的可见选项 DTO，不重复实现条件过滤、洞察判定或暗号匹配
- UI 不应显示不可用选项，避免通过灰化泄露隐藏分支
- 可见选项必须按 YAML 中的编辑顺序稳定显示；暗号额外选项由 ds-006 注入后同样进入同一列表渲染
- 心境暗示用文学语言表现，不暴露 delta 数值
- 心境暗示仅在选项会触发 `mindset_shift` 时显示；显示文本采用括号内武侠文学语气，例如"（此言带几分执念）"，不得显示轴名、公式或数值
- 洞察提示视觉建议遵守 GDD：关键文字渐显高亮、水墨晕染或若隐若现的“眼”图标
- 洞察追查入口只在 Speech / Narration 的 WaitingForInput 阶段出现；选择节点期间不触发新的洞察暗示
- 洞察提示出现后无限期持续，无倒计时；玩家选择追查进入 InsightPrompt 分支，选择推进视为忽略线索
- 暗号选项使用独特样式标记，例如加引号与特殊底色，让玩家知道这是"江湖暗语"，但不暴露匹配规则
- 选择确认后只允许短暂高亮反馈，然后立即调用运行时选择接口推进分支；不得弹出二次确认、撤销按钮或回退入口
- 若可见选项列表为空，UI 不自行制造业务分支；应显示运行时提供的兜底选项或错误态，并记录日志，避免空面板挂起
- Control Manifest 约束：选择面板基于 Godot Control 节点树，继承 `BaseUiPanel`，所有可交互选项 `focus_mode = FOCUS_ALL`，通过 `FocusManager.PushFocus` / `PopFocus` 管理焦点
- UI 刷新使用脏标记模式，禁止每帧无条件重建选项列表；选项高亮、洞察提示和暗号样式可用 tween，但需控制同屏 tween 数量
- 同场景 UI 输入使用 Godot `[Signal]` delegate；跨层选择结果不得由 UI 直接改写业务状态，必须调用 Core/Dialogue 暴露的选择入口或发布受控事件
- Engine note：Godot 4.6 dual-focus 属 post-cutoff 高风险；实现时必须验证鼠标 hover 与手柄焦点可同时稳定工作，且不会造成双重确认

## 验收标准

- [x] Choice 节点到达时，选项面板显示所有可见选项
- [x] 可见选项按编辑顺序排列，不可用选项完全不显示且无灰化占位
- [x] 心境选择旁显示武侠文学暗示，与标准选项视觉区分明确，且不显示 axis、delta、公式或具体数值
- [x] 洞察提示出现时无限期持续，玩家可选择"追查"或"推进"，无倒计时
- [x] 暗号选项以独特样式标记，与标准选项视觉区分明确，且不暴露暗号匹配规则
- [x] 玩家选择并确认后，选中项短暂高亮，对话立即沿该分支推进，无确认对话框、撤销按钮或回退入口
- [x] 键鼠和手柄均可导航与确认选项，输入模式切换后焦点不丢失且不会双重触发

## QA 手动检查

- **AC-1**：标准选择
  - Setup：进入含 3 个可见选项的 Choice 节点
  - Verify：选项列表按编辑顺序显示，不出现灰化隐藏选项
  - Pass condition：键鼠和手柄都能选择并确认

- **AC-2**：心境暗示
  - Setup：进入 Mindset 选择节点
  - Verify：选项文字旁出现文学暗示，未显示数值
  - Pass condition：玩家能辨认它与普通选项不同，但不会看到公式或 delta

- **AC-3**：洞察与暗号特殊选项
  - Setup：进入同时包含洞察提示和暗号选项的测试对话
  - Verify：洞察可追查或推进，暗号有独特样式
  - Pass condition：洞察提示无倒计时；追查/暗号选择后立即进入对应分支，无法撤销

- **AC-4**：输入与焦点
  - Setup：用键鼠 hover 一个选项后切换手柄导航，再切回键鼠
  - Verify：高亮、焦点和确认目标始终可见且唯一
  - Pass condition：不会丢失焦点、不会一次确认触发两次选择

## 测试证据路径

`production/qa/evidence/ds-009-choice-ui-evidence.md`

## 依赖关系

- Depends on: ds-003, ds-005, ds-006, ds-008
- Unlocks: 无

## 完成记录

- 扩展 `DialogueUiPresenter`，为 Choice 节点输出 `DialogueUiOptionSnapshot` 列表，保留可见选项顺序、原始编辑下标、当前选择和确认高亮状态。
- 新增 `DialogueUiOptionStyle`，区分 Standard、Mindset、CodePhrase 和 Fallback 选项，供 Godot Control 层选择不同样式。
- 心境选项从 `mindset_shift` 事件推导文学暗示，只显示武侠语气文案，不暴露 axis、delta、公式或具体数值。
- 洞察提示通过 `HasInsightPrompt`、`InsightPromptText` 与 `InsightPrompt` 焦点目标表达；`InvestigateInsight()` 沿追查分支推进，普通确认仍视为推进/忽略。
- 暗号选项保留 `CodePhraseId` 和 CodePhrase 样式标记，不暴露匹配上下文或匹配规则。
- `ConfirmSelection()` 返回选中项短暂高亮快照，并立即调用运行时 `SelectOption()` 沿分支推进；不提供确认对话框、撤销或回退入口。
- 键鼠与手柄导航/确认共用输入来源枚举和选择接口；真实 Godot hover/dual-focus 行为仍需在正式 UI 场景接入后做手动验证。
