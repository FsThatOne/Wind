# Quick Design Spec: 任务 HUD 与任务页面分工

**Type**: Tweak / Addition  
**System**: 朦胧化 UI、主线叙事 / 章节推进  
**GDD Reference**: `design/gdd/blurred-ui.md`, `design/gdd/main-narrative.md`  
**Date**: 2026-07-04

## Change Summary

自由探索左侧任务 HUD 只显示当前任务标题和必要的短进度，不显示任务上下文说明。任务说明保留在任务数据中，后续任务页面在右侧详情区展示。

## Motivation

当前左侧 HUD 同时显示“任务要做什么”和“为什么要做”，信息量偏大，压住探索画面。参考《逸剑风云决》的任务页结构，任务上下文应放到专门页面中，让 HUD 保持轻量，任务页承担阅读和回顾。

## Design Delta

Current implementation says:

`TrackedQuestObjective.Reason` 会直接拼到左侧 HUD 的任务标题下方。

This spec changes that to:

1. 左侧 HUD 仅显示任务分类和任务标题，例如 `主线：去雾门前听师姐催你取酒`。
2. 多行任务进度可以留在 `Text` 内，例如跑腿子任务 `[ ] / [x]`，因为这是行动进度，不是上下文说明。
3. `Reason` 继续由 `GameFlow.TrackObjective` 保存，作为未来任务页面右侧详情区的数据来源。
4. 正式任务页面采用左右分栏：左侧为可折叠任务列表，右侧为选中任务详情。

## New Rules / Values

- 任务 HUD 不展示 `Reason`。
- 任务页待设计结构：
  - 左侧第一栏：任务类别，可折叠 `主线`、`支线`。
  - 左侧第二栏：当前类别下的任务列表。
  - 右侧：选中任务的标题、上下文说明、当前目标、已完成子任务。
- 未实现任务页前，不在探索 HUD 临时塞回说明文字。

## Affected Systems

| System | Impact | Action Required |
|---|---|---|
| 任务 HUD | 减少屏幕左侧文字密度 | 立即修改 Godot HUD 渲染 |
| 任务页面 | 新增 UI 设计待办 | 记录 Polish backlog |
| 主线叙事 | `Reason` 仍作为上下文文案保留 | 后续任务页读取 |

## Acceptance Criteria

- [ ] 屏幕左侧任务 HUD 不显示 `TrackedQuestObjective.Reason`。
- [ ] 跑腿等行动进度仍可在 HUD 看到。
- [ ] `Reason` 数据仍被记录，未来任务页可复用。
- [ ] Polish backlog 记录任务页面设计待办。

## GDD Update Required?

No. 这是当前 Godot HUD 与未来任务页面的分工修正；正式任务页面开工时再补完整 UX spec。
