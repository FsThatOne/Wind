# Sprint 5 Harness Highlight And Selection Evidence

**Date**: 2026-06-18  
**Target**: `prototypes/sprint5-combat-ui-harness`  
**Scope**: Harness visual state check  
**Result**: PASS

---

## What Was Verified

- Left fixture menu selected state updates with the current fixture.
- Current fixture row displays a visible `▶` marker.
- Current fixture row displays blue selected-state background.
- Action submenu displays visible selected / focus / hover markers.
- Selected action displays `▶`.
- Keyboard focus and mouse hover remain visible as separate states.
- Godot runtime started without errors.

---

## Evidence

- Right/action submenu highlight screenshot: `production/qa/evidence/media/sprint5-harness-highlight-check-2026-06-18-godot.png`
- Left fixture menu highlight screenshot: `production/qa/evidence/media/sprint5-harness-left-menu-highlight-check-2026-06-18-godot.png`

Observed state in the left-menu screenshot:

- Title: `Sprint 5 Combat UI Harness — EnemyCurrentQiQiao / 敌方当前巧气机`
- Left fixture menu: `EnemyCurrentQiQiao / 敌方当前巧气机` row is selected.
- Selected fixture row shows `▶`.
- Selected fixture row uses blue highlight styling.

Observed state in the action-menu screenshot:

- Action submenu selected state is visible.
- `决胜一击` shows selected styling and `▶`.
- Bottom status shows `Focus: decisive_strike` and `Hover: move:duanyue_fist`.

---

## Runtime Check

Godot MCP debug output:

```text
Godot Engine v4.6.3.stable.mono.official.7d41c59c4
errors: []
```

---

## Limitation

This evidence only verifies the harness visual selection states. It does not count as a full Production -> Polish playtest report.

Next playtest evidence should use the `DecisionLoop...` fixtures and be recorded as targeted combat decision-loop feedback.
