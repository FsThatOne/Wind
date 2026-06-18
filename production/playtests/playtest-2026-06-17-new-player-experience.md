# Playtest Report — New Player Experience

**Status**: Cancelled — stale target deleted  
**Gate Coverage**: Production -> Polish playtest requirement  
**Related Plan**: `production/playtests/playtest-plan-polish-gate-2026-06-17.md`

---

## Session Info

- **Date**: 2026-06-17
- **Build**: `prototypes/fengzhi-vertical-slice` local Godot debug run
- **Duration**: Cancelled before valid playtest
- **Tester**: User
- **Platform**: macOS local dev build
- **Input Method**: KB+M
- **Session Type**: First-time player
- **Observer**: Agent

## Test Target Limitation

This session was originally started with `prototypes/fengzhi-vertical-slice` because it was the only runnable full-loop Godot target. The target was then deleted on 2026-06-17 because it used the obsolete combat model and could mislead current Production -> Polish evidence.

Use this session for:

- New-player flow readability.
- Dialogue -> combat -> mindset / feedback loop comprehension.
- General UI / onboarding confusion.
- Emotional hook and pacing observations.

Do not use this session as final evidence for:

- Current Sprint 5 Combat UI contract.
- Current enemy qi / Xingqi Tactics combat correctness.
- Current `cu-004` / `cu-005` / `cu-008` behavior, which is covered by `prototypes/sprint5-combat-ui-harness`.

Startup evidence before cancellation:

- Godot 4.6.3 Mono launched without script/runtime errors.
- Debug output includes old `Boss决策: Attack 体系:Rou`, confirming stale combat semantics.

Cancellation result:

- This file does not count as one of the 3 required Production -> Polish playtest reports.
- A new current full-loop target is required before running the New Player Experience playtest.

---

## Test Focus

Validate whether a first-time player can understand the first playable flow without developer guidance:

- What the player is trying to do.
- How to move through dialogue / exploration / combat UI.
- Whether blurred UI feels atmospheric or confusing.
- Whether the first combat interaction communicates "observe qi, take position, strike".
- Whether the opening emotional hook points toward the world and characters.

---

## First Impressions (First 5 Minutes)

- **Understood the goal?** [Yes / No / Partially]
- **Understood the controls?** [Yes / No / Partially]
- **Understood UI language?** [Yes / No / Partially]
- **Emotional response**: [Engaged / Confused / Bored / Frustrated / Excited]
- **Notes**:
  - [Observation]

---

## Gameplay Flow

### What Worked Well

- [Observation]

### Pain Points

- [Issue] — Severity: [High / Medium / Low]

### Confusion Points

- [Where the player was confused and why]

### Moments Of Delight

- [What surprised or pleased the player]

---

## Bugs Encountered

| # | Description | Severity | Reproducible |
|---|-------------|----------|--------------|
| 1 | [None observed / description] | [High / Medium / Low] | [Yes / No / Unknown] |

---

## Feature-Specific Feedback

### Onboarding / Goal Clarity

- **Understood purpose?** [Yes / No / Partially]
- **Found engaging?** [Yes / No / Partially]
- **Suggestions**: [Tester suggestions]

### Blurred UI

- **Atmospheric or confusing?** [Atmospheric / Confusing / Mixed]
- **Specific confusing labels**: [List]

### First Combat Readability

- **Understood current qi?** [Yes / No / Partially]
- **Understood movement + strike?** [Yes / No / Partially]
- **Understood unavailable actions?** [Yes / No / Partially]

---

## Quantitative Data

- **Time to understand first goal**: [mm:ss]
- **Time to first successful combat action**: [mm:ss]
- **Times tester asked "what do I do?"**: [count]
- **Deaths / failures**: [count and location]
- **Features discovered vs missed**: [List]

---

## Overall Assessment

- **Would play again?** [Yes / No / Maybe]
- **Difficulty**: [Too Easy / Just Right / Too Hard]
- **Pacing**: [Too Slow / Good / Too Fast]
- **Session length preference**: [Shorter / Good / Longer]
- **Gate Result**: [PASS / CONCERNS / FAIL]

---

## Top 3 Priorities From This Session

1. [Most important finding]
2. [Second priority]
3. [Third priority]

---

## Action Routing

- **Design changes needed**: [None / list]
- **Balance adjustments**: [None / list]
- **Bug reports**: [None / list]
- **Polish items**: [None / list]
