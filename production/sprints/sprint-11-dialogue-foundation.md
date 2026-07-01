# Sprint 11: Dialogue System Foundation

> **Goal**: 对话系统 Foundation 核心层实现 — 数据模型、状态机、条件引擎、事件管道、选择路由
> **Start**: 2026-07-01
> **End**: 2026-07-07
> **Calibration**: Code 0.3x / Doc+QA 0.25h fixed

## Must Have (ds-001 ~ ds-005)

| ID | Story | Estimate | Owner |
|----|-------|----------|-------|
| ds-001 | Data Model & Sequence Graph | 0.3d | dev |
| ds-002 | State Machine & Lifecycle | 0.3d | dev |
| ds-003 | Condition Evaluation Engine | 0.3d | dev |
| ds-004 | Event Pipeline & Batch Dispatch | 0.15d | dev |
| ds-005 | Choice System & Routing | 0.3d | dev |

## Should Have (ds-006 ~ ds-009)

| ID | Story | Estimate | Owner |
|----|-------|----------|-------|
| ds-006 | Insight Mechanism (In-Dialogue) | 0.15d | dev |
| ds-007 | Code Phrase System | 0.15d | dev |
| ds-008 | Letter System | 0.15d | dev |
| ds-009 | Sequence Routing & Re-talk | 0.15d | dev |

## Notes

- Presentation 层 (ds-010~012) 留到 Sprint 12，需要 Godot UI 框架和 UX spec
- 对话数据格式使用 YAML（与现有对话数据目录一致）
- Foundation 层 9 个 story 预计实际 1.6 天（基于 Sprint 10 校准系数）
