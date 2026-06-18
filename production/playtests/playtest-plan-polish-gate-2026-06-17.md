# Polish Gate Playtest Plan

**Date Created**: 2026-06-17  
**Purpose**: Close the Production -> Polish gate playtest blocker without fabricating results.  
**Gate Report**: `production/gate-checks/gate-production-to-polish-2026-06-17.md`  
**Status**: Planned / Not Yet Executed

---

## Gate Requirement

Production -> Polish requires at least 3 documented playtest sessions covering:

1. New player experience.
2. Mid-game systems.
3. Difficulty curve.

The reports below are templates. They do not count as passed evidence until the session is actually run and the results sections are filled with observed data.

---

## Core Hypothesis Under Test

From `design/gdd/game-concept.md`:

> "行气战棋武学战斗 + 武学残片获取 + 朦胧化 UI + 一位女主彗星模型陪伴" can make players love the world and characters of *Wind Stops* within 3-4 hours.

Current MVP open questions to test:

- Can players feel mindset-axis movement without seeing raw numbers?
- Does misunderstanding feel emotionally sticky rather than punitive?
- Does blurred UI feel atmospheric rather than confusing?
- Does the Xingqi tactics combat loop read as "observe qi, take position, strike, open flaws"?

---

## Planned Sessions

| Session | File | Primary Question | Required Outcome |
|---------|------|------------------|------------------|
| New Player Experience | `playtest-2026-06-17-new-player-experience.md` | Can a first-time player understand goals, controls, UI language, and early combat without developer guidance? | Identify onboarding confusion and first-5-minute emotional response. |
| Mid-Game Systems | `playtest-2026-06-17-mid-game-systems.md` | Do combat, martial fragments, mindset, dialogue, and companion hooks make sense together? | Identify system comprehension gaps and feature interaction issues. |
| Difficulty Curve | `playtest-2026-06-17-difficulty-curve.md` | Is the current challenge curve readable, fair, and motivating? | Identify spikes, boredom, resource pressure issues, and boss readability problems. |

---

## Execution Rules

- Do not coach the tester unless the session is explicitly blocked.
- Record exact confusion moments and time stamps.
- Separate implementation bugs from design confusion.
- Mark every finding with severity: High / Medium / Low.
- If the tester cannot reach the target flow, record the blocker instead of forcing a pass.
- If using the harness rather than a full build, state that limitation in the report.

---

## Evidence Checklist

For each session:

- [ ] Date, build, duration, tester, platform, input method recorded.
- [ ] Session focus filled.
- [ ] First 5 minutes captured.
- [ ] Confusion points captured.
- [ ] Bugs table filled or explicitly marked "none observed".
- [ ] Top 3 priorities filled.
- [ ] Overall assessment filled.
- [ ] Result marked as PASS / CONCERNS / FAIL for gate evidence.

---

## Gate Re-Check Criteria

After all three reports are filled:

1. Re-run `/gate-check polish`.
2. If playtest reports reveal design changes, run `/propagate-design-change` on affected GDDs before claiming gate readiness.
3. If playtest reports reveal bugs, file or update bugs under `production/qa/bugs/`.
4. If reports reveal balance issues, run `/balance-check` on the affected system before tuning values.
