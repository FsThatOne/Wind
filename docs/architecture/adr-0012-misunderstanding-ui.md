# ADR-0012: Misunderstanding UI — Transparency Signal Presentation

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | UI Presentation, Shader Effects, Audio Cues |
| **Knowledge Risk** | **HIGH** — 依赖 ADR-0002 dual-focus 体系；ShaderMaterial 动态 uniform 更新行为需验证 |
| **References Consulted** | `docs/engine-reference/godot/modules/ui.md`, `design/gdd/misunderstanding-system.md`, `design/gdd/blurred-ui.md` |
| **Post-Cutoff APIs Used** | Dual-focus system (4.6), Recursive Control disable (4.5) |
| **Verification Required** | 1) 验证 ShaderMaterial uniform 动态更新在 Control 节点上的每帧性能; 2) 验证 CanvasModulate 色调叠加与朦胧化 UI 色调偏移的交互行为; 3) 验证 AudioStreamPlayer 低频循环在场景切换时正确释放 |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (EventBus — 订阅误会状态事件), ADR-0002 (UI Framework — BaseUiPanel, Blurred UI 架构, CanvasLayer 层级), ADR-0009 (Dynamic Audio — 音频状态机集成) |
| **Enables** | Sprint 5+ Misunderstanding UI 表现层实现 |
| **Blocks** | 误会系统的"玩家感知"功能——无此 ADR 时误会仅在数据层运作，玩家无法察觉 |
| **Ordering Note** | 必须在 ADR-0002 spike 通过后、Feature/Misunderstanding 逻辑层实现后开始 |

## Context

### Problem Statement

误会系统 (#18) 的核心设计原则是**信息差**——玩家能感知"有什么不对了"，但不知道具体原因。这要求 UI 层在不暴露系统内部状态（严重度、窗口倒计时、澄清条件）的前提下，通过间接信号传达误会的存在和紧迫度。

具体挑战：
- 三阶段透明度（HIDDEN → HINTED → PERCEIVED → URGENT）需要不同强度的视觉/音频信号
- 所有信号必须遵循朦胧化 UI 的文学化原则（无数字、无进度条、无明确提示）
- URGENT 阶段需传达紧迫感但不泄露具体天数
- 称呼回退需与对话系统无缝集成，不引入额外 UI 元素
- `force_break` 诀别演出需绕过朦胧化延迟规则，立即触发

需要决定：如何在遵守朦胧化原则的前提下，设计一套分层次的间接信号系统。

### Constraints

- **不显示**：误会倒计时、澄清条件列表、严重度等级、误会日志
- 所有视觉信号必须符合朦胧化 UI 的文学美学（水墨风、低饱和、意象化）
- 关系面板标记不得使用高饱和色或警告图标（不是 HUD 警报）
- URGENT 阶段的紧迫感通过氛围传达（音调、色温），不通过倒计时或闪烁红色
- 称呼回退必须无额外 UI chrome——靠文本本身传达
- 必须兼容手柄导航（关系面板标记需在手柄焦点时可感知）

### Requirements (from GDD #18)

- HINTED: 称呼回退 + 对话语气着色 + 暗示音
- PERCEIVED: 关系面板"心有疑云"标记 + 态度措辞附加修饰
- URGENT: 面板标记脉动 + 文学信号推送 + 环境音调变暗
- force_break: 强制叙事演出，绕过延迟规则

## Decision

采用 **分层信号管道 + Blurred UI Channel 扩展 + 环境氛围渲染** 方案。

### 核心架构

```
┌────────────────────────────────────────────────────────────┐
│            MisunderstandingSignalRouter                      │
│            (信号路由器 — 将透明度变化分发到各表现通道)        │
│                                                             │
│  输入：MisunderstandingTransparencyChangedEvent             │
│  输出：按 transparency level 路由到下方各通道               │
└───────────┬───────────────┬──────────────────┬─────────────┘
            │               │                  │
            ▼               ▼                  ▼
┌───────────────┐  ┌────────────────┐  ┌──────────────────┐
│ DialogueChannel│  │ PanelChannel   │  │ AmbienceChannel  │
│ (称呼/语气)   │  │ (关系面板标记) │  │ (色温/音效)      │
│               │  │                │  │                  │
│ HINTED+       │  │ PERCEIVED+     │  │ URGENT           │
└───────────────┘  └────────────────┘  └──────────────────┘
```

### 1. MisunderstandingSignalRouter

```csharp
// Presentation/BlurredUi/MisunderstandingSignalRouter.cs
public partial class MisunderstandingSignalRouter : Node
{
    [Export] private DialogueSignalChannel _dialogueChannel;
    [Export] private RelationshipPanelChannel _panelChannel;
    [Export] private AmbienceSignalChannel _ambienceChannel;

    public override void _Ready()
    {
        Services.EventBus.Subscribe<MisunderstandingTransparencyChangedEvent>(OnTransparencyChanged);
        Services.EventBus.Subscribe<MisunderstandingResolvedEvent>(OnResolved);
        Services.EventBus.Subscribe<ForceBreakTriggeredEvent>(OnForceBreak);
    }

    private void OnTransparencyChanged(MisunderstandingTransparencyChangedEvent e)
    {
        switch (e.NewTransparency)
        {
            case Transparency.Hinted:
                _dialogueChannel.ActivateForNpc(e.NpcId, SignalIntensity.Subtle);
                break;

            case Transparency.Perceived:
                _dialogueChannel.ActivateForNpc(e.NpcId, SignalIntensity.Clear);
                _panelChannel.ShowCloudMarker(e.NpcId);
                break;

            case Transparency.Urgent:
                _dialogueChannel.ActivateForNpc(e.NpcId, SignalIntensity.Clear);
                _panelChannel.PulseCloudMarker(e.NpcId);
                _ambienceChannel.ActivateUrgency(e.NpcId);
                break;
        }
    }

    private void OnResolved(MisunderstandingResolvedEvent e)
    {
        _dialogueChannel.DeactivateForNpc(e.NpcId);
        _panelChannel.HideCloudMarker(e.NpcId);
        _ambienceChannel.DeactivateUrgency(e.NpcId);
        _ambienceChannel.PlayResolveChime();
    }

    private void OnForceBreak(ForceBreakTriggeredEvent e)
    {
        // 绕过所有延迟规则，立即请求演出
        Services.EventBus.Publish(new CutsceneRequestEvent(
            cutsceneId: e.BreakCutsceneId,
            priority: CameraPriority.Cutscene,
            immediate: true
        ));
    }
}
```

### 2. DialogueSignalChannel (称呼回退 + 语气着色)

不引入额外 UI 元素，纯数据层驱动对话文本选择：

```csharp
// Presentation/BlurredUi/Channels/DialogueSignalChannel.cs
public partial class DialogueSignalChannel : Node
{
    // NPC → 当前信号强度映射
    private readonly Dictionary<string, SignalIntensity> _activeSignals = new();

    public void ActivateForNpc(string npcId, SignalIntensity intensity)
    {
        _activeSignals[npcId] = intensity;
    }

    public void DeactivateForNpc(string npcId)
    {
        _activeSignals.Remove(npcId);
    }

    /// <summary>
    /// 对话系统在生成文本前查询此接口，决定使用哪套称呼表。
    /// </summary>
    public AddressMode GetAddressMode(string npcId)
    {
        if (!_activeSignals.TryGetValue(npcId, out var intensity))
            return AddressMode.Normal;

        return intensity switch
        {
            SignalIntensity.Subtle => AddressMode.FormalRetreat,  // "停云"→"阁下"
            SignalIntensity.Clear => AddressMode.ColdRetreat,    // "停云"→"那位少侠"
            _ => AddressMode.Normal
        };
    }

    /// <summary>
    /// 对话系统在选择语气变体时查询此接口。
    /// </summary>
    public ToneVariant GetToneVariant(string npcId)
    {
        if (!_activeSignals.TryGetValue(npcId, out var intensity))
            return ToneVariant.Warm;

        return intensity switch
        {
            SignalIntensity.Subtle => ToneVariant.Neutral,
            SignalIntensity.Clear => ToneVariant.Cold,
            _ => ToneVariant.Warm
        };
    }
}

public enum AddressMode { Normal, FormalRetreat, ColdRetreat }
public enum ToneVariant { Warm, Neutral, Cold }
public enum SignalIntensity { Subtle, Clear }
```

### 3. RelationshipPanelChannel (心有疑云标记)

```csharp
// Presentation/BlurredUi/Channels/RelationshipPanelChannel.cs
public partial class RelationshipPanelChannel : Node
{
    private readonly Dictionary<string, CloudMarkerState> _markers = new();

    [Export] private PackedScene _cloudMarkerScene;  // 水墨云雾图标预制体

    public void ShowCloudMarker(string npcId)
    {
        if (_markers.ContainsKey(npcId)) return;

        _markers[npcId] = new CloudMarkerState
        {
            IsVisible = true,
            IsPulsing = false
        };

        // 通知关系面板刷新
        Services.EventBus.Publish(new RelationshipPanelDirtyEvent(npcId));
    }

    public void PulseCloudMarker(string npcId)
    {
        if (!_markers.TryGetValue(npcId, out var state)) return;
        state.IsPulsing = true;

        Services.EventBus.Publish(new RelationshipPanelDirtyEvent(npcId));
    }

    public void HideCloudMarker(string npcId)
    {
        _markers.Remove(npcId);
        Services.EventBus.Publish(new RelationshipPanelDirtyEvent(npcId));
    }

    /// <summary>
    /// 关系面板在渲染 NPC 条目时调用，获取标记状态。
    /// </summary>
    public CloudMarkerState? GetMarkerState(string npcId)
    {
        return _markers.TryGetValue(npcId, out var state) ? state : null;
    }
}

public class CloudMarkerState
{
    public bool IsVisible { get; set; }
    public bool IsPulsing { get; set; }
}
```

**云雾图标视觉规格**：

| 属性 | 值 |
|------|-----|
| 尺寸 | 16×16 px 基础，放大 2x 显示 |
| 风格 | 水墨晕染的淡灰云团，边缘不规则 |
| 静态色 | `#8A8A8A`，alpha 0.6 |
| 脉动动画 | alpha 在 0.4–0.8 间正弦波动，周期 2s |
| URGENT 脉动 | alpha 在 0.3–0.9 间正弦波动，周期 1s（加快）|
| 手柄焦点反馈 | 聚焦到该 NPC 条目时，tooltip 显示"心有疑云" |

### 4. AmbienceSignalChannel (URGENT 环境氛围)

```csharp
// Presentation/BlurredUi/Channels/AmbienceSignalChannel.cs
public partial class AmbienceSignalChannel : Node
{
    [Export] private AudioStreamPlayer _urgencyDrone;  // 低频氛围循环
    [Export] private CanvasModulate _sceneModulate;

    private readonly HashSet<string> _urgentNpcs = new();
    private Color _baseModulateColor = Colors.White;
    private bool _urgencyActive;

    /// <summary>
    /// 当玩家进入与 URGENT 误会相关的 NPC 区域时激活。
    /// </summary>
    public void ActivateUrgency(string npcId)
    {
        _urgentNpcs.Add(npcId);
        if (!_urgencyActive) StartUrgencyAmbience();
    }

    public void DeactivateUrgency(string npcId)
    {
        _urgentNpcs.Remove(npcId);
        if (_urgentNpcs.Count == 0) StopUrgencyAmbience();
    }

    private void StartUrgencyAmbience()
    {
        _urgencyActive = true;

        // 色温微降：整体偏冷灰，传达"有什么快碎了"的压抑感
        var tween = CreateTween();
        var coldTint = new Color(0.92f, 0.92f, 0.96f, 1.0f);  // 微蓝冷调
        tween.TweenProperty(_sceneModulate, "color", coldTint, 2.0f);

        // 低频氛围音循环（不是警报，是隐约的不安感）
        _urgencyDrone.Play();
        _urgencyDrone.VolumeDb = -20f;
        var audioTween = CreateTween();
        audioTween.TweenProperty(_urgencyDrone, "volume_db", -12f, 3.0f);
    }

    private void StopUrgencyAmbience()
    {
        _urgencyActive = false;

        var tween = CreateTween();
        tween.TweenProperty(_sceneModulate, "color", _baseModulateColor, 1.5f);

        var audioTween = CreateTween();
        audioTween.TweenProperty(_urgencyDrone, "volume_db", -40f, 2.0f);
        audioTween.TweenCallback(Callable.From(() => _urgencyDrone.Stop()));
    }

    public void PlayResolveChime()
    {
        // 清澈琴弦回响 — "冰释前嫌"
        Services.EventBus.Publish(new PlaySfxEvent("sfx_misunderstanding_resolve"));
    }
}
```

### 5. 态度措辞附加修饰

当关系面板渲染态度文字时，根据误会状态追加文学修饰：

```csharp
// Presentation/BlurredUi/AttitudeDisplayFormatter.cs
public static class AttitudeDisplayFormatter
{
    /// <summary>
    /// 为态度标签追加误会修饰词。
    /// 示例："以礼相待" → "以礼相待 · 心有疑云"
    /// </summary>
    public static string Format(string baseAttitudeLabel, string npcId,
                                 RelationshipPanelChannel panelChannel)
    {
        var marker = panelChannel.GetMarkerState(npcId);
        if (marker is not { IsVisible: true })
            return baseAttitudeLabel;

        var suffix = marker.IsPulsing ? "疑云渐深" : "心有疑云";
        return $"{baseAttitudeLabel} · {suffix}";
    }
}
```

### 6. 信号触发时序（与 Blurred UI CH-3 集成）

误会系统的透明度信号通过 Blurred UI 的 CH-3（关系通道）`pending_reveals` 队列传递，遵循事件驱动延迟揭示规则：

| 透明度变化 | Blurred UI 行为 | 实际对玩家的呈现时机 |
|-----------|----------------|-------------------|
| → HINTED | 写入 CH-3 `pending_reveals`，type=`称呼回退` | 下次与该 NPC 对话时生效 |
| → PERCEIVED | 直接更新关系面板标记（不走延迟队列）| 立即可见（但玩家需主动打开面板） |
| → URGENT | 直接激活环境氛围 + 面板脉动加速 | 立即可感知（进入相关区域时） |
| → RESOLVED | 清除所有信号 + 播放释怀音 | 立即生效 |
| force_break | **绕过队列**，立即触发演出请求 | 立即强制呈现 |

**与 CH-3 的边界**：
- CH-3 负责常规感情变化的延迟揭示（彗星事件、态度缓慢变化）
- 误会系统的 HINTED 信号借用 CH-3 的延迟机制
- PERCEIVED/URGENT 绕过 CH-3 延迟，因为设计意图是"玩家在窗口期内必须能感知到"

### 7. 音效触发点

| GDD 需求 | 触发条件 | SFX ID | 描述 |
|----------|----------|--------|------|
| 关系变化暗示音 | HIDDEN → HINTED | `sfx_mis_hinted` | 短促低沉弦乐，0.8s |
| 窗口紧迫音 | DayAdvanced 且 URGENT 状态 | `sfx_mis_urgency_tick` | 低频单音，每日一次 |
| 澄清释怀音 | → RESOLVED | `sfx_mis_resolve` | 清澈琴弦回响，1.5s |
| 定型遗憾音 | → PERMANENT | `sfx_mis_permanent` | 低沉余韵，2s |

音效通过 ADR-0009 的音频状态机接口 `IAudioDirector.PlaySfx(sfxId)` 播放。

## Alternatives Considered

### Alternative 1: 专用误会 HUD 面板

- **Description**: 创建独立的"误会追踪"UI 面板，显示活跃误会列表
- **Pros**: 信息清晰，玩家无需猜测
- **Cons**: 直接违反 GDD #18 的"不做的事"列表（不显示误会日志）；破坏朦胧化 UI 的信息差原则
- **Rejection Reason**: GDD 明确禁止此方案，误会系统的核心体验就是"感知而非知道"

### Alternative 2: 通知 Toast 提示

- **Description**: 误会状态变化时弹出 Toast 通知（如"白苓似乎对你有所误解"）
- **Pros**: 确保玩家不错过信息；简单实现
- **Cons**: 过于直白，破坏"隐隐不对劲"的氛围感；频繁 Toast 导致通知疲劳；不符合武侠叙事节奏
- **Rejection Reason**: 误会的感知应通过与 NPC 的下次交互中"发现"，不是被系统告知

### Alternative 3: 纯对话层表现（无面板标记）

- **Description**: 仅通过称呼回退和语气变化传达误会，不在关系面板显示任何标记
- **Pros**: 最纯粹的叙事体验；零额外 UI 元素
- **Cons**: 玩家可能完全错过误会的存在（尤其是不频繁对话的 NPC）；URGENT 阶段无法有效传达紧迫感；可访问性差
- **Rejection Reason**: GDD 明确要求 PERCEIVED 阶段在面板显示"心有疑云"标记，纯对话方案不满足需求

## Consequences

### Positive

- 三通道分层设计清晰对应三级透明度，职责单一
- 对话通道零 UI 开销——仅切换数据查表，不创建新节点
- 环境氛围通道复用 CanvasModulate（已被 Blurred UI 色调系统使用）
- 云雾标记使用 ShaderMaterial 实现脉动，不依赖 AnimationPlayer
- 与 Blurred UI CH-3 的集成边界清晰（HINTED 借用延迟 / PERCEIVED+ 绕过）

### Negative

- MisunderstandingSignalRouter 是额外的中间层——增加事件路由复杂度
- AmbienceChannel 的 CanvasModulate 修改可能与 Blurred UI 的心境色调冲突（需要优先级仲裁）
- 云雾标记的脉动频率变化（PERCEIVED vs URGENT）需要状态追踪

### Risks

| 风险 | 缓解 |
|------|------|
| CanvasModulate 色调冲突：心境色调 + 误会冷调同时生效 | 两者通过 Tween 叠加计算最终值：`final = mindset_tint * urgency_tint`；urgency_tint 接近白色(0.92)，不显著覆盖心境色调 |
| 玩家仍然错过 PERCEIVED 标记（不打开面板） | URGENT 阶段引入环境氛围（色温+音效）作为被动感知通道，不依赖玩家主动查看 |
| 称呼回退在翻译/本地化时丢失语义 | 称呼表按语言独立维护；中文的"阁下/少侠/停云"对应英文的"Sir/Young Hero/Tingyun" |
| 低频 urgency drone 在某些设备上不可闻 | 同时通过色温偏移提供视觉通道冗余；确保非纯音频依赖 |
| force_break 演出请求与其他演出冲突 | 使用 CameraRequestBus (ADR-0011) 的 Cutscene 优先级(80)，高于所有游戏内演出 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| misunderstanding-system.md | HINTED 称呼回退 + 语气着色 | DialogueSignalChannel.GetAddressMode() / GetToneVariant() |
| misunderstanding-system.md | PERCEIVED 关系面板"心有疑云"标记 | RelationshipPanelChannel + 云雾 ShaderMaterial |
| misunderstanding-system.md | URGENT 面板脉动 + 文学信号 | PulseCloudMarker() + AmbienceSignalChannel |
| misunderstanding-system.md | force_break 绕过延迟立即演出 | CutsceneRequestEvent(immediate: true) |
| misunderstanding-system.md | 不显示倒计时/条件/严重度 | 架构中无任何数值暴露接口 |
| misunderstanding-system.md | 暗示音 / 紧迫音 / 释怀音 / 遗憾音 | 4 个 SFX ID 通过 IAudioDirector 播放 |
| blurred-ui.md | CH-3 事件驱动延迟揭示 | HINTED 写入 pending_reveals；PERCEIVED+ 绕过 |
| blurred-ui.md | 信息差模糊原则 | 所有信号均为间接意象化表达，零数值暴露 |

## Performance Implications

- **CPU**: DialogueSignalChannel 查表 O(1)；云雾 shader 每帧 1 次 sin() 计算 — 可忽略
- **Memory**: 最多 5 NPC 同时有 CloudMarkerState（GDD 限制 max 5 核心 NPC）≈ 可忽略
- **Audio**: urgency drone 为单个 AudioStreamPlayer，VolumeDb 渐变不额外消耗
- **Shader**: 云雾标记 shader 仅在关系面板打开时运行（面板关闭时节点不可见 → _Process 不执行）

## Migration Plan

首次实现，无迁移需求。依赖 Feature/Misunderstanding 逻辑层完成后实现表现层。

## Validation Criteria

1. **称呼回退测试**：创建 HINTED 误会 → 下次对话时 NPC 使用疏远称呼；解除后恢复亲密称呼
2. **面板标记测试**：透明度到 PERCEIVED → 打开关系面板 → "心有疑云"标记可见 + 态度文字追加修饰
3. **脉动测试**：URGENT 状态 → 云雾图标脉动周期从 2s 缩短到 1s
4. **环境氛围测试**：URGENT 状态 + 玩家进入相关区域 → 色温微降 + 低频音循环
5. **释怀音测试**：误会 RESOLVED → 清澈琴弦音效播放 + 所有视觉信号清除
6. **force_break 测试**：触发 force_break → 立即进入演出（不走延迟队列）
7. **色调叠加测试**：心境在"执念+入世"(暗铁红) + URGENT 冷调同时生效 → 最终色调合理（不全灰/不全红）
8. **手柄测试**：关系面板中手柄导航到有标记的 NPC 条目 → 可通过焦点高亮感知"心有疑云"文字

## Related Decisions

- [ADR-0001](adr-0001-event-bus-architecture.md) — 误会状态变化通过 EventBus 发布
- [ADR-0002](adr-0002-ui-framework-dual-focus.md) — UI 框架、Blurred UI 架构、CanvasLayer 层级
- [ADR-0009](adr-0009-dynamic-audio.md) — 音效播放接口
- [ADR-0011](adr-0011-combat-ui-animation.md) — CameraRequestBus 优先级体系（force_break 演出复用）
