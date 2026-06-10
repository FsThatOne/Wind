# ADR-0009: Dynamic Music System

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.3 |
| **Domain** | Audio |
| **Knowledge Risk** | LOW — Godot Audio API 无 post-cutoff 破坏性变更 |
| **References Consulted** | `docs/engine-reference/godot/modules/audio.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0008 (泛型 FSM), ADR-0001 (EventBus) |
| **Enables** | Presentation/Audio 模块完整实现 |
| **Blocks** | Sprint 3+ (战斗音乐), Sprint 5+ (全场景音乐) |
| **Ordering Note** | None |

## Context

### Problem Statement

《风止》音频系统需要：6 状态动态音乐 FSM、自适应战斗音乐（6 段水平分层 + 小节线对齐切换）、BGM Override 栈（深度 3）、等功率 crossfade、女主 Motif 叠加。需要决定实现方案。

## Decision

采用 **自研 C# 音频状态机 + Godot AudioServer 总线**，不使用 AudioStreamInteractive（Godot 无此功能）。

### 总线架构

```
Master Bus (玩家可调)
├── BGM Bus (玩家可调)
│   ├── BGM_Main (当前播放)
│   └── BGM_Crossfade (淡出用)
├── Ambient Bus (玩家可调)
│   ├── Ambient_Terrain
│   ├── Ambient_Weather
│   └── Ambient_TimeOfDay
└── SFX Bus (玩家可调)
    └── SFX_Pool (8 路 AudioStreamPlayer)
```

### 6 状态 FSM

```csharp
enum AudioState { Exploration, Combat, Cutscene, Dialogue, Menu, Silence }
enum AudioTrigger { EnterCombat, ExitCombat, StartCutscene, EndCutscene,
                    StartDialogue, EndDialogue, OpenMenu, CloseMenu, ForceSilence, Resume }

// 使用 ADR-0008 泛型 FSM
var musicFsm = new FiniteStateMachine<AudioState, AudioTrigger>(AudioState.Exploration);
musicFsm.AddTransition(AudioState.Exploration, AudioTrigger.EnterCombat, AudioState.Combat);
musicFsm.AddTransition(AudioState.Combat, AudioTrigger.ExitCombat, AudioState.Exploration);
// ...
```

### BGM Override 栈

```csharp
// 最大深度 3: Exploration → Combat → Cutscene
private readonly Stack<BgmEntry> _overrideStack = new(3);

public void PushBgm(string trackId, AudioState state)
{
    if (_overrideStack.Count >= 3)
        _overrideStack.Pop(); // 溢出替换栈顶
    _overrideStack.Push(new BgmEntry(trackId, state));
    CrossfadeTo(trackId);
}

public void PopBgm()
{
    _overrideStack.Pop();
    var prev = _overrideStack.Peek();
    CrossfadeTo(prev.TrackId);
}
```

### 等功率 Crossfade

```csharp
// fade_out: cos(t/duration × π/2)
// fade_in:  sin(t/duration × π/2)
private async Task CrossfadeTo(string newTrack, float fadeOutMs = 1500, float fadeInMs = 800)
{
    var oldPlayer = _currentPlayer;
    var newPlayer = GetFreePlayer();
    newPlayer.Stream = LoadTrack(newTrack);
    newPlayer.Play();
    newPlayer.VolumeDb = -80; // 静音起始

    var startTime = Time.GetTicksMsec();
    while (elapsed < Math.Max(fadeOutMs, fadeInMs))
    {
        var t = (Time.GetTicksMsec() - startTime);
        if (t < fadeOutMs)
            oldPlayer.VolumeDb = LinearToDb(Mathf.Cos(t / fadeOutMs * Mathf.Pi / 2));
        if (t < fadeInMs)
            newPlayer.VolumeDb = LinearToDb(Mathf.Sin(t / fadeInMs * Mathf.Pi / 2));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    oldPlayer.Stop();
}
```

### 自适应战斗音乐（水平分段）

```csharp
enum CombatSegment { Prep, Clash, Advantage, Disadvantage, Desperation, Finisher }

// 每段为独立 OGG 文件，bar_length_ms 从 BPM 计算
// 切换逻辑：判定新段 → 等待当前小节结束 → crossfade 到新段
private async Task SwitchSegment(CombatSegment target)
{
    var remainMs = GetMsUntilNextBar();
    if (remainMs > BarBoundaryToleranceMs)
        await ToSignal(GetTree().CreateTimer(remainMs / 1000.0), "timeout");
    CrossfadeTo(GetSegmentTrack(target), fadeOutMs: 500, fadeInMs: 300);
}
```

### SFX 优先级仲裁

| 优先级 | 类型 | 规则 |
|--------|------|------|
| P0 | 系统音效（UI确认/错误） | 不可被淘汰 |
| P1 | 核心战斗（Burst/Read/伤害） | 不可被淘汰 |
| P2 | 武技/招式 | 8路满时淘汰最低优先级 |
| P3 | 环境互动 | 可被淘汰 |
| P4 | 氛围音效 | 可被淘汰 |

同一 SFX 50ms cooldown 防叠音，同源最多 2 路并发。

## Consequences

### Positive
- 完全控制切换时机（小节线对齐）— AudioStreamInteractive 无法提供此精度
- FSM 复用 ADR-0008 基类 — 一致性强
- Override 栈自然管理 BGM 优先级层级

### Negative
- 小节线对齐需要预知 BPM 和节拍位置 — 每首曲子需配置 `bar_length_ms`
- crossfade 手动实现 — 需精细调试等功率曲线参数
- 153 条音频资产管理量大

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| audio-system.md | 6 状态音频 FSM | AudioState enum + FiniteStateMachine |
| audio-system.md | Override 栈深度 3 | Stack\<BgmEntry\> 容量 3 |
| audio-system.md | 等功率 crossfade (cos/sin) | CrossfadeTo() 实现 |
| audio-system.md | 战斗 6 段水平分层 | CombatSegment enum + bar-boundary 切换 |
| audio-system.md | SFX 8 路并发 + 优先级 | SFX Pool + Priority 仲裁 |
| audio-system.md | 女主 Motif 叠加（非栈内） | 独立 overlay player + BGM duck 至 30% |
| audio-system.md | 同源 50ms cooldown | SFX 去重逻辑 |

## Validation Criteria
1. 状态切换：Exploration → Combat → Exploration 各 BGM 正确播放
2. Crossfade：无音量跳变，等功率曲线平滑
3. 小节线对齐：段切换不发生在旋律中间
4. SFX：8 路满时低优先级正确被淘汰
5. Override 栈：3 层 push/pop 正确恢复

## Related Decisions
- [ADR-0008](adr-0008-finite-state-machine.md) — 音乐 FSM 基类
- [ADR-0001](adr-0001-event-bus-architecture.md) — 战斗事件触发音乐状态切换
