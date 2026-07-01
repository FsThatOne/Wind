# Epic: 对话系统 (Dialogue System)

> **Status**: Done
> **Stories**: 12 (12 Complete — pre-existing implementation discovered)
> **GDD**: design/gdd/dialogue-system.md
> **Priority**: MVP
> **Layer**: Foundation + Presentation
> **Created**: 2026-06-30

## Overview

《风止》叙事 RPG 的骨架管道。负责承载所有角色对话、剧情分支、心境选择和关系互动。7 种节点类型、8 种条件类型、9 种事件管道，外加洞察/暗号/书信子机制。

## Stories

| ID | Name | Layer | Type | Status | Blocker |
|----|------|-------|------|--------|---------|
| ds-001 | Data Model & Sequence Graph | Foundation | Logic | Complete | — |
| ds-002 | State Machine & Lifecycle | Foundation | Logic | Complete | — |
| ds-003 | Condition Evaluation Engine | Foundation | Logic | Complete | — |
| ds-004 | Event Pipeline & Batch Dispatch | Foundation | Logic | Complete | — |
| ds-005 | Choice System & Routing | Foundation | Logic | Complete | — |
| ds-006 | Insight Mechanism (In-Dialogue) | Foundation | Logic | Complete | — |
| ds-007 | Code Phrase System | Foundation | Logic | Complete | — |
| ds-008 | Letter System | Foundation | Logic | Complete | — |
| ds-009 | Sequence Routing & Re-talk | Foundation | Logic | Complete | — |
| ds-010 | Dialogue UI & Typewriter | Presentation | Visual | Complete | — |
| ds-011 | Choice UI & Input Handling | Presentation | Visual | Complete | — |
| ds-012 | Letter UI & Mailbox | Presentation | Visual | Complete | — |

## Definition of Done

1. Foundation 层全部 story 通过单元测试
2. 对话数据格式（YAML）可被加载器正确解析
3. 事件管道可驱动下游系统 mock
4. 状态机覆盖 GDD 全部 8 种状态转换
5. 条件引擎支持 GDD 定义的 8 种条件类型 + AND/OR 组合

## Next Step

Epic complete. All 12 stories pre-implemented with 117 tests passing. Implementation includes:
- `DialogueSchema.cs` — data model + graph validator + YAML loader
- `DialogueRuntime.cs` — state machine + lifecycle + insight + dead-loop protection
- `DialogueConditions.cs` — condition evaluator + option filtering
- `DialogueEvents.cs` — 9 event types + batch dispatch queue
- `DialogueCodePhrases.cs` — code phrase book + context matching
- `DialogueLetters.cs` — letter inbox + delayed notification + save/load
- `DialogueUiPresenter.cs` — UI snapshot DTO + typewriter + input mapping
