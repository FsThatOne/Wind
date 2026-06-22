# Exploration Debt Triage — 2026-06-22

> **Action**: Sprint 6 `EI-Debt-Triage` (Sprint 4 retro Action #3 carryover)
> **Owner**: Dev / QA (lean review)
> **Scope**: `docs/tech-debt-register.md` 中 7 条 Exploration Insight 边界契约债（2026-06-14 登记）
> **Goal**: 每条债标 priority + decision + trigger，并圈定下一 Insight Presentation epic 启动前必须先还的 boundary-contract 项

---

## Priority / Decision Vocabulary

| 字段 | 取值 | 含义 |
|------|------|------|
| priority | **P1** | 阻塞下一 Insight Presentation epic 启动 |
| | **P2** | Presentation 启动可暂时绕过，但同 epic 关账前必须还 |
| | **P3** | 非阻塞，纯长期治理 |
| decision | **Pay down** | 在 trigger 触发时主动偿还 |
| | **Defer** | 暂不偿还，trigger 触发再评估 |
| | **Verify-then-close** | 验证已被其他 story 偿还，本次只做关账 |
| | **Accept (doc-only)** | 接受现状，仅作文档保鲜 |

---

## Triage 结果

| # | 来源 Story | 简述 | Priority | Decision | Trigger |
|---|-----------|------|----------|----------|---------|
| 1 | ei-002 | `ProximityDetector.Detect` 每次分配结果列表 | **P3** | Defer | 接入 `_PhysicsProcess` 热路径前 |
| 2 | ei-002 | `IInsightConditionEvaluator` 仅绑测试替身 | **P2** | Defer | `living-jianghu-layer` epic 启动 / 首次 Feature 系统共用 evaluator |
| 3 | ei-003 | `Detect()` legacy 路径绕过 `Tick()` 的 linger 计时 | **P1** | **Pay down** | **Insight Presentation epic 启动前** |
| 4 | ei-003 | `Ignored` 节点离开半径时 `InsightCueHiddenEvent` 可能重发 | **P2** | **Pay down** | **Insight Presentation epic 启动前** |
| 5 | ei-003 | Pending trigger queue 卸载场景清理依赖出队跳过 | **P3** | Verify-then-close | 本次 triage（已被 ei-005 实质偿还） |
| 6 | ei-004 | `DiscoveryDispatcher` reward 失败时 monologue request 无 cancel/commit 闭环 | **P1** | **Pay down** | **Narrative/Monologue Presentation adapter 接入前** |
| 7 | ei-005 | Story 正文缺 readiness 段落（Estimate / Out of Scope / Engine Notes …） | **P3** | Accept (doc-only) | 下次 ei-005 文档保鲜 / retro Action #2 落地时 |

---

## 必须先还的 Boundary-Contract 项（Insight Presentation epic 启动前 gate）

retro Action #3 要求"选哪条必须在 Presentation integration 前还掉"。结论：**两条 P1 + 一条 P2**：

### 还款项 #1 — ei-003 detect path 统一（P1）
- **风险**：双 API（`Detect()` 立即返回 vs. `Tick()` 计时）会让 Presentation adapter 看到的 cue 时序在两条路径下行为不同；UI linger / 视觉过渡会漂移。
- **偿还方案**：在 `ProximityDetector` 内部统一为单一入口；要么 `Detect()` 也走 `_detectedElapsedSeconds` 注册，要么把 `Detect()` 标 `[Obsolete]` 并在调用方迁移到 `Tick()`。
- **影响面**：`src/FengZhi.Foundation/Exploration/ProximityDetector.cs` + 相关测试。
- **Estimate**: 0.5 ~ 0.75 day（含回归测试）。

### 还款项 #2 — ei-004 monologue request lifecycle（P1）
- **风险**：reward commit 被拒绝时 monologue request 已发出，Presentation 演出后无 cancel/commit 信号，会出现"已演出但状态未推进"的孤立态。
- **偿还方案**（任选其一，需在 ADR 层敲定）：
  - (a) 在 Foundation `DiscoveryDispatcher` 中失败路径同步发布 `MonologueRequestCanceledEvent`；
  - (b) 把 monologue request 改为 `Pending → Commit/Cancel` 两阶段事件（更贴近 cu-006 BattleEventBus 模式）。
- **影响面**：`src/FengZhi.Foundation/Exploration/DiscoveryDispatcher.cs` + Foundation 事件契约。可能需要一条新 ADR 或在 ADR-0014 补章节。
- **Estimate**: 0.75 ~ 1 day（含 ADR 更新）。

### 还款项 #3 — ei-003 hide event dedup（P2，建议同期还）
- **风险**：多个 Presentation 订阅方（HUD cue / world icon / 音频）若各自实现 dedup，会出现实现漂移。
- **偿还方案**：在 `InsightDetectionService` 发布层加一层 `(nodeId, lastVisibleState)` dedup guard，确保单条 hide event。
- **理由放 P2**：Presentation adapter 单一时可绕过；多订阅方落地前必须收紧。建议与 #1 一同还，避免两次回归测试。
- **Estimate**: 0.25 day。

**合计**：≈1.5 ~ 2 day，建议在 `S6-Next-Presentation-Cut`（拆下一 Presentation epic）确定为 Insight 后，作为该 epic 的 Sprint 0 还款 sprint 处理。

---

## 不阻塞 Presentation 的项

- **#1 ei-002 allocation**：当前 active node ≤ 6，未上 `_PhysicsProcess`，等真正进热路径再评估。
- **#2 ei-002 evaluator binding**：Insight Presentation 只消费 evaluator 输出，不依赖真实绑定。等 `living-jianghu-layer` 启动再做。
- **#5 ei-003 pending queue cleanup**：ei-005 关账时已实质偿还（[AC-3](file:///Users/bytedance/my-game/production/epics/exploration-insight/stories/ei-005-save-scene-lock-recovery.md#L25)）。本次 triage 视为 Closed。
- **#7 ei-005 文档段落**：被 Sprint 4 retro Action #2（readiness 模板强制）覆盖。

---

## Follow-up Action Items

| # | Action | 触发点 | Owner |
|---|--------|--------|-------|
| F1 | 在 `S6-Next-Presentation-Cut` 拆 Insight Presentation 之前，先排一个 0.5 day 的还款 spike：合并 ei-003 detect 路径 + 加 hide dedup guard | Sprint 6 末 / Sprint 7 初 | Dev |
| F2 | ei-004 monologue lifecycle 还款 — 与 Narrative/Monologue 系统的 Foundation 接口对齐；如果该接口尚未冻结，先纳入 Insight Presentation 拆分时的 ADR 输入 | Insight Presentation epic 拆分时 | Dev / Architect |
| F3 | 下次 register 整理（Sprint 6 关账或 Sprint 7 启动）时，把 #5 条标 Closed 并加 `Settled by ei-005` 注脚 | Sprint 6 关账 | Producer |

---

## 状态同步

- `docs/tech-debt-register.md` 中 7 条 EI 条目已 inline 追加 Triage 段。
- Sprint 4 retro Action #3 / Sprint 5 retro Action #3 在本 triage 完成后可标 **Closed (output: this note)**。
- Sprint 6 `EI-Debt-Triage` 任务已在本 note 完成；`production/sprint-status.yaml` 切到 done。
