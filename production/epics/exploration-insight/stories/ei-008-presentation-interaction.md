# ei-008: EI Presentation — 玩家追查交互 + InnerMonologue 显示 + reward UI 反馈

> **Status**: Ready
> **Last Updated**: 2026-06-29
> **Type**: Integration
> **Layer**: Presentation
> **Estimate**: 1.5 days (校准后预期 ~0.6d actual)
> **GDD 来源**: design/gdd/exploration-insight.md §4 洞察检定流程 step 5-6
> **TR-IDs**: TR-exploration-insight-004
> **ADR**: docs/architecture/adr-0018-exploration-insight.md
> **Manifest Version**: 2026-06-10
> **Depends On**: ei-007 (Presentation Adapter — ✅ Complete)

## Context

ei-007 已实现 `InsightDetectorBridge`（Godot ↔ Foundation 桥接）和 `InsightCueVisual`（水墨提示动画）。
当玩家接近 InsightNode 且 insight 满足门槛时，水墨提示会出现。

本 story 补完下半段：玩家**看到提示后选择追查** → 触发 narrative_context 独白 → 发放 reward → UI 反馈。

Foundation 层已就绪：
- `DiscoveryDispatcher.OnPlayerInvestigate(nodeId)` — 完整逻辑（验证 → 发布 MonologueRequestPendingEvent → 分派奖励 → CommittedEvent 或 CanceledEvent）
- `InsightNode.NarrativeContext` — 独白文本
- `InsightNode.DiscoveryType` + `InsightNode.Reward` — 奖励数据
- `MonologueRequestPendingEvent` / `CommittedEvent` / `CanceledEvent` — 两阶段事件

Presentation 层已就绪：
- `InteractionController` — 区域进入/退出 + 提示标签 + interact 输入处理
- `InsightDetectorBridge._activeCues` — 当前可见的 cue 字典
- `DialogueManager` / `DialoguePanel` — 文本展示

## Scope

**In scope:**
- 水墨提示出现时，注册交互区域让玩家可通过 interact 键追查
- 追查后调用 `DiscoveryDispatcher.OnPlayerInvestigate(nodeId)`
- 订阅 MonologueRequest 事件 → 在 DialoguePanel 展示 narrative_context
- 独白完成后根据 DiscoveryType 显示简短奖励反馈（PromptLabel 提示）
- 追查成功后移除对应 cue + 交互区域
- 追查期间冻结移动（复用 MovementFrozen）

**Out of scope:**
- 卷轴展开动画（ADR 规定 v0 用 DialoguePanel 文本，后续 sprint 做演出）
- GrantItem / MartialFragment 实际发放（v0 只发事件和设 flag）
- 独立的洞察发现日志 UI

## Acceptance Criteria

- AC-1: 当 `InsightCueShownEvent` 触发后，在 cue 位置注册交互区域，玩家靠近可见"追查"提示文字
- AC-2: 玩家在提示区域内按 interact 键 → 调用 `DiscoveryDispatcher.OnPlayerInvestigate(nodeId)` 并获得结果
- AC-3: 订阅 `MonologueRequestPendingEvent` → 冻结移动 + 在 DialoguePanel 显示 `InsightNode.NarrativeContext`
- AC-4: 订阅 `MonologueRequestCommittedEvent` → 独白结束后显示奖励反馈文本（如 "获得线索：[flag_id]"），2 秒后自动消失
- AC-5: 订阅 `MonologueRequestCanceledEvent` → 解冻移动 + 关闭 DialoguePanel（reward 分派失败的降级路径）
- AC-6: 追查成功（Committed）后，对应 cue 执行 FadeOut + 移除交互区域，不可重复追查
- AC-7: 在 BackMountainCliffCave 场景中配置 1 个测试 InsightNode（EnvironmentDetail 类型），端到端可玩

## Implementation Notes

- 在 `InsightDetectorBridge` 中扩展：OnCueShown 时同时注册交互区域，OnCueHidden 时移除
- 交互回调中直接调用 `DiscoveryDispatcher.OnPlayerInvestigate`
- MonologueRequest 事件处理可做成 `InsightMonologuePresenter` 独立节点（单一职责）
- DialoguePanel 独白模式：无需对话树，直接设文本 + 等待玩家确认或自动关闭
- 奖励反馈用 PromptLabel + Timer 自动清除，不引入新 UI 组件

## Files to Create/Modify

**Create:**
- `feng-zhi/scripts/exploration/InsightMonologuePresenter.cs` — 订阅 MonologueRequest 事件 → 驱动 DialoguePanel 展示独白 + 奖励反馈

**Modify:**
- `feng-zhi/scripts/exploration/InsightDetectorBridge.cs` — OnCueShown 增加交互区域注册；OnCueHidden 移除区域；追查回调调用 DiscoveryDispatcher
- `feng-zhi/scripts/SceneGameBase.cs` — 挂载 InsightMonologuePresenter（与 InsightDetectorBridge 同级）

## QA Test Cases

1. **端到端追查**: 场景有 1 个 threshold=5 的 EnvironmentDetail 节点（玩家 insight=10），接近 → 水墨提示出现 → 按 interact → 独白显示 → 确认后消失 → 提示消失，不可重复追查
2. **奖励反馈正确性**: Clue 类型节点追查后显示 "获得线索" 文字；EnvironmentDetail 仅显示独白无奖励反馈
3. **取消/失败降级**: 手动触发 CanceledEvent 时，DialoguePanel 关闭 + 移动解冻 + cue 仍保留（可再次追查）
4. **冻结期间无二次追查**: 追查独白显示期间，其他 cue 的交互区域禁用

## Test Evidence

- Integration: playtest record at `production/qa/evidence/ei-008-presentation-interaction-evidence.md`

## Engine Notes

- InteractionController 使用 Area2D 碰撞检测，新注册的追查区域需匹配现有模式
- DialoguePanel 独白模式可能需要轻量扩展（无 speaker portrait，单纯文本 + 确认按钮）
- Godot 4.7 Tween 管理：追查区域的 outline 效果复用 InteractionController 的 shader 路径

## Performance Notes

- 每个 cue 对应 1 个 Area2D 碰撞体，活跃 cue 预期 ≤3 个，不会超出 200 draw call budget
- MonologuePresenter 仅事件驱动，不占用 _Process 帧预算
