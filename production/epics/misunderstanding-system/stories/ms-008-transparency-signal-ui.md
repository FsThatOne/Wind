# Story: ms-008 — Transparency Signal UI (ADR-0012)

> **Epic**: misunderstanding-system
> **Status**: Complete (Router logic; Presentation layer pending ADR-0012 spike)
> **Last Updated**: 2026-06-30
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Priority**: P2
> **Estimate**: 2.5 days
> **Manifest Version**: 2026-06-30
> **Blocked By**: ADR-0012 实现（需 ADR-0002 spike 通过）, NPC 状态管理 (#10)
> **GDD 来源**: design/gdd/misunderstanding-system.md §Visual/Audio Requirements, §UI Requirements
> **TR-ID**: 待 architecture-review 分配

## Context

误会系统的表现层需要在不暴露系统数值的前提下，通过间接信号让玩家感知误会的存在和紧迫度。这包括：对话中的称呼回退、关系面板的"心有疑云"标记、URGENT 阶段的脉动效果和朦胧化文学信号。此 story 是 HIGH-risk 的 Presentation 层工作，依赖 ADR-0012 中定义的 SignalRouter 架构和 ADR-0002 的 UI 框架。

**ADR Governing Implementation**: ADR-0012 (Misunderstanding UI — Transparency Signal Presentation)
**Engine**: Godot 4.7-stable | **Risk**: HIGH
**Engine Notes**: 需要验证 ShaderMaterial uniform 动态更新性能、CanvasModulate 色调叠加行为、AudioStreamPlayer 低频循环释放。

## Acceptance Criteria

- [ ] AC-1: HINTED 阶段 — NPC 对话中使用疏远称呼替代亲密称呼（称呼表切换），无额外 UI 提示。
- [ ] AC-2: PERCEIVED 阶段 — 关系面板中 NPC 条目出现"心有疑云"水墨风云雾图标，态度描述附加修饰词。
- [ ] AC-3: URGENT 阶段 — 关系面板标记脉动加快 + 推送朦胧化文学信号（如"你隐约觉得，若再不做些什么，某种东西就要碎了"）。
- [ ] AC-4: 所有视觉信号遵循朦胧化 UI 原则：无数字、无进度条、无倒计时、无警告色。
- [ ] AC-5: `force_break` 诀别触发时绕过朦胧化延迟规则，立即呈现全屏叙事演出。
- [ ] AC-6: 音频信号：HIDDEN→HINTED 变化暗示音、窗口紧迫音（window_remaining≤2）、澄清释怀音、定型遗憾音。

## Implementation Notes

**Control Manifest Rules (Presentation Layer)**:
- Required: 接收 ms-004 的 `ITransparencySignalEmitter` 通知，转换为具体的 Godot UI 操作。
- Required: 称呼表为数据驱动（每 NPC 有亲密/疏远称呼对照表）。
- Required: 云雾图标使用 ShaderMaterial + 动态 uniform 控制脉动频率。
- Required: 文学信号文本从本地化表读取，不硬编码。
- Required: force_break 演出通过 CutsceneSystem 触发，本 story 仅发出触发信号。
- Forbidden: 不显示误会倒计时天数、澄清条件列表、严重度等级、误会日志。
- Guardrail: ShaderMaterial uniform 更新频率不超过每帧 1 次（ADR-0012 性能约束）。

**ADR-0012 架构**:
- SignalRouter: 接收逻辑层透明度变化事件，路由到对应 channel
- Dialogue Channel: 注入称呼回退 + 语气着色
- Panel Channel: 更新关系面板标记
- Ambience Channel: 控制音频状态 + 色温偏移

## Files to Create/Modify

- `src/FengZhi.Presentation/Misunderstanding/TransparencySignalRouter.cs`
- `src/FengZhi.Presentation/Misunderstanding/DialogueSignalChannel.cs`
- `src/FengZhi.Presentation/Misunderstanding/PanelSignalChannel.cs`
- `src/FengZhi.Presentation/Misunderstanding/AmbienceSignalChannel.cs`
- `assets/shaders/misunderstanding_cloud.gdshader`
- `data/misunderstanding/address_tables/` (称呼对照表)

## Test Evidence

**Required evidence**:
- `tests/integration/misunderstanding/TransparencySignalRouterTest.cs` (逻辑路由验证)
- Manual evidence: Godot 运行截图/录屏验证视觉效果

**Test cases**:
- 逻辑测试：EmitHint → DialogueChannel 收到通知
- 逻辑测试：EmitPerceived → PanelChannel 收到通知
- 逻辑测试：EmitUrgent → AmbienceChannel 收到通知 + PanelChannel 脉动加快
- 手工验证：关系面板云雾图标渲染正确
- 手工验证：称呼从"停云"切换到"阁下"
- 手工验证：force_break 立即触发全屏演出（无延迟）
- 手工验证：所有 UI 无数字/进度条/倒计时

## Out of Scope

- 诀别全屏演出的具体内容制作（cutscene-system epic）
- 具体 NPC 的称呼表内容（ms-009/prologue-content）
- 传闻面板集成（living-jianghu-layer epic）

## Dependencies

- Depends on: ms-004; ADR-0012 实现, ADR-0002 spike, NPC 状态管理 (#10)
- Unlocks: ms-009
