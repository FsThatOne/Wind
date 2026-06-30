# Hook: post-sprint-retrospective

## Trigger

Manual trigger at the end of each sprint (typically invoked by the producer
agent or the human developer).

## Purpose

Automatically generates a retrospective starting point by analyzing the sprint
data: what was planned vs completed, velocity changes, bug trends, and common
blockers. This is not a git hook but a workflow hook invoked through the
`producer` agent.

## Implementation

This is a workflow hook, not a git hook. It is invoked by running:

```
@producer Generate sprint retrospective for Sprint [N]
```

The producer agent should:

1. **Read the sprint plan** from `production/sprints/sprint-[N].md`
2. **Calculate metrics**:
   - Tasks planned vs completed
   - Story points planned vs completed (if used)
   - Carryover items from previous sprint
   - New tasks added mid-sprint
   - Average task completion time
3. **Analyze patterns**:
   - Most common blockers
   - Which agent/area had the most incomplete work
   - Which estimates were most inaccurate
4. **Generate the retrospective**:

```markdown
# Sprint [N] Retrospective

## Metrics
| Metric | Value |
|--------|-------|
| Tasks Planned | [N] |
| Tasks Completed | [N] |
| Completion Rate | [X%] |
| Carryover from Previous | [N] |
| New Tasks Added | [N] |
| Bugs Found | [N] |
| Bugs Fixed | [N] |

## Velocity Trend
[Sprint N-2]: [X] | [Sprint N-1]: [Y] | [Sprint N]: [Z]
Trend: [Improving / Stable / Declining]

## What Went Well
- [Automatically detected: tasks completed ahead of estimate]
- [Facilitator adds team observations]

## What Went Poorly
- [Automatically detected: tasks that were carried over or cut]
- [Automatically detected: areas with significant estimate overruns]
- [Facilitator adds team observations]

## Blockers
| Blocker | Frequency | Resolution Time | Prevention |
|---------|-----------|----------------|-----------|

## Action Items for Next Sprint
| # | Action | Owner | Priority |
|---|--------|-------|----------|

## Estimation Accuracy
| Area | Avg Planned | Avg Actual | Accuracy | Calibration Factor |
|------|------------|-----------|----------|-------------------|

<!-- 估时校准追踪 (Sprint 7 retro Action #1):
  - 记录本 sprint 实际校准系数: actual_total / estimated_total
  - 与上 sprint 对比趋势
  - 当连续 2 sprint |variance| < 30% 时可将 0.4x 调回 0.6x
  - 分类追踪: Foundation 集成 / Scaffold / Doc-only / Greenfield
  - 任何 story 超时（正方差 > 20%）单独分析根因
-->

### Calibration History
| Sprint | Factor Used | Actual Factor | Trend |
|--------|-----------|---------------|-------|
| S7 | 1.0x (未校准) | 0.38x | 首次发现 |
| S8 | 0.4x | [填入] | [填入] |
```

5. **Save** to `production/sprints/sprint-[N]-retro.md`
