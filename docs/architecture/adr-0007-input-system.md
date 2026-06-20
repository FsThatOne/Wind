# ADR-0007: Input System Adaptation

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Input |
| **Knowledge Risk** | **MEDIUM** — SDL3 gamepad driver 是 4.5+ 新增，但对游戏代码透明 |
| **References Consulted** | `docs/engine-reference/godot/modules/input.md`, `docs/engine-reference/godot/breaking-changes.md` |
| **Post-Cutoff APIs Used** | SDL3 gamepad driver (engine-level, transparent to game code) |
| **Verification Required** | 验证 Steam Deck 手柄在 SDL3 下正确映射所有按键 |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0002 (UI dual-focus — InputModeDetector 已定义) |
| **Enables** | Platform/Settings 输入配置, 全部 UI 手柄导航 |
| **Blocks** | None (v1.0 不做 rebinding) |
| **Ordering Note** | 优先级降低 — v1.0 使用固定映射即可 |

## Context

### Problem Statement

原架构规划中 ADR-007 预设为"SDL3 rebinding 方案"，但经 GDD 审查（settings-options.md），v1.0 明确不支持键位重映射。SDL3 作为引擎底层 gamepad driver 对游戏代码完全透明。实际决策简化为：确认 v1.0 的输入架构方案。

## Decision

**v1.0 使用 Godot 内置 InputMap + 固定 Action 映射，不做运行时 rebinding。**

### 输入架构

```
玩家物理输入 (键盘/鼠标/手柄)
    ↓ (SDL3 driver, 引擎层, 透明)
Godot InputEvent 系统
    ↓
InputMap Actions (项目设置中预定义)
    ↓
游戏代码: Input.IsActionPressed("action_name")
    ↓
ADR-0002 InputModeDetector (检测当前输入设备类型)
```

### Action 映射表（固定）

| Action | 键盘 | 手柄 | 用途 |
|--------|------|------|------|
| move_up/down/left/right | WASD / 方向键 | 左摇杆 / D-Pad | 移动 |
| interact | E / Enter | A (Xbox) / Cross | 交互 |
| cancel | Esc | B (Xbox) / Circle | 取消/返回 |
| pause | Esc | Start | 暂停菜单 |
| burst_confirm | Space | X (Xbox) / Square | 战斗 Burst 确认 |
| read_action | R | Y (Xbox) / Triangle | 战斗 Read |
| ui_navigate | Tab | LB/RB | UI 页签切换 |
| quick_save | F5 | — | 快速存档 |
| quick_load | F9 | — | 快速读档 |

### v1.1 Rebinding 预留

```csharp
// Platform/Settings/ISettingsProvider.cs 已定义接口
// v1.0 实现为 no-op
public void ApplyInputRemap(string action, InputEvent key)
{
    // v1.0: 不实现，保持固定映射
    // v1.1: 调用 InputMap.ActionEraseEvents() + ActionAddEvent()
    throw new NotImplementedException("Rebinding deferred to v1.1");
}
```

## Consequences

### Positive
- 极简实现，零额外代码
- SDL3 对游戏层完全透明 — 无兼容风险
- Steam Deck 通过 Steam Input 层可自行重映射（补偿 v1.0 无内置 rebinding）

### Negative
- v1.0 无内置键位自定义 — 玩家只能通过 Steam Input 外部重映射
- v1.1 需要补实现 rebinding UI

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| settings-options.md | v1.0 不支持键位重映射 | 确认固定 InputMap 方案 |
| settings-options.md | 暂停: Esc / Start | Action 映射表中 pause action |
| settings-options.md | 一击决胜期间屏蔽暂停 | CombatSystem.IsInCinematicLock() 查询 |

## Validation Criteria
1. 键盘全部 Action 响应正确
2. Xbox 手柄全部 Action 映射正确
3. Steam Deck 手柄通过 Steam Input 可重映射
4. InputModeDetector 正确检测设备切换

## Related Decisions
- [ADR-0002](adr-0002-ui-framework-dual-focus.md) — InputModeDetector 定义
