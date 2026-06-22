# Sprint 6 — Combat UI Hardening & Presentation Cutoff

> **Sprint Goal**: 封板 Combat UI Presentation epic — 把 cu-006 在 Godot 4.7-stable 实机集成（Engine.TimeScale + Tween Always + Camera2D + InputMap）、产出 cu-004 / cu-005 / cu-006 / cu-008 Visual evidence、补齐 Sprint 5 Definition of Done（smoke + team-qa），并完成 Sprint 5 retrospective 的 5 条 Action Items。
>
> **Duration**: 2026-06-19 ~ 2026-07-03 (2 weeks)
>
> **Prerequisite**: Sprint 5 retrospective APPROVED (`production/retrospectives/retro-sprint-5-2026-06-18.md`)；review mode = `lean` (director gates skipped)

## Capacity

- Total days: 10
- Buffer (20%): 2 days reserved（Godot 实机集成不确定性 + Visual evidence 实机环境）
- Available: 8 estimate-days
- 计划 story load: ≈3.75 estimate-days（Must Have ≈2.5d + Should Have ≈1.25d）— 余量留给 Sprint 5 收尾、debug 与可能的 spike

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S6-Commit-Workspace | commit Sprint 5 工作树（cu-006 / cu-007 / harness 扩展 / evidence MD / tech-debt-register / sprint-status.yaml / active.md），按 `/story-done` suggested commits 分批提交 | Dev | 0.25 | Sprint 5 retro Action #1 | `git status` 干净；4 条提交对应 cu-006 / cu-007 / harness / 状态文件；commit message 符合 conventional commits 模板 |
| S6-Sprint5-DoD | 运行 `/smoke-check sprint 5` + `/team-qa sprint 5`，产出 `production/qa/smoke-2026-06-19-sprint-5.md` 与 `production/qa/qa-signoff-sprint-5-*.md` | QA / Dev | 0.5 | S6-Commit-Workspace；retro Action #2 | smoke PASS / team-qa APPROVED 或 APPROVED WITH CONDITIONS；Sprint 5 DoD 中 Smoke check passed + QA sign-off report 两条真正闭合 |
| cu-006-godot-integration | cu-006 一击决胜在 Godot 4.7-stable 实机集成：`Engine.TimeScale` 接管 + `Tween.TweenProcessMode.Always` + `Camera2D` priority bus + `InputMap` block；同时暴露 `ICombatService` Facade 给 cu-001 / cu-005 调用 | godot-csharp-specialist | 0.75 | cu-006 Foundation；retro Action #3 | 7-phase choreography 在 Godot 实机走通；TimeScale 0.4 → 0.0 → 1.0 实测；CameraRequestBus 高优先级抢占；输入封锁恢复正常；ICombatService Facade `RequestDecisiveStrike` 可被外部调用并接入 BattleEventBus |
| cu-visual-evidence | **[Carryover → Sprint 7]** cu-004 招式面板 / cu-005 反制+决胜提示 / cu-006 一击决胜实机录屏 / cu-008 双焦点 — 4 份 manual evidence MD（含截图或录屏） | QA / Dev | 0.75 | cu-006-godot-integration | **2026-06-22 决策**：harness 已删除，且 Sprint 7 ADR-0020 全循环 VS 会同步产出新的 visual baseline；本项 carryover 到 Sprint 7，与 VS 重建合并出新 evidence。详见 §Carryover to Next Sprint。 |
| S6-Commit-Lint | 引入 commit message lint：禁止 `阶段性提交` / `init(test):` 等无信号 commit；推荐 conventional commits + scope；落 `.gitmessage` 模板 + manual lint script | Dev | 0.25 | retro Action #4 | `.gitmessage` 模板 + lint script 落仓；`docs/git-workflow.md`（或 CONTRIBUTING.md）记录 commit 规范；Sprint 6 首个 PR 强制走规范 |

**Must Have 小计**：≈2.5 estimate-days

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S6-Combat-UI-Epic-Close | 关闭 combat-ui EPIC：cu-001..008 全 Complete + 实机验证后，更新 `production/epics/combat-ui/EPIC.md` status: Ready → Done，更新 `production/epics/index.md` Presentation 行 | Producer / Dev | 0.5 | cu-visual-evidence | EPIC.md status: Done；index.md Presentation 行更新；`/scope-check combat-ui` 无新条目 |
| EI-Debt-Triage | Sprint 4 retro Action #3 carryover：扫描 5 条 Exploration boundary-contract tech debt，决定哪条必须在 Presentation integration 前还掉 | Dev / QA | 0.5 | retro Action #3 (Sprint 4) | `docs/tech-debt-register.md` 5 条 EI 条目各加 priority + 决策（pay-down / accept / defer）；产出 `production/notes/exploration-debt-triage-2026-06-XX.md` |
| S6-Effort-Tracking | retro Action #5：在 `/story-done` SKILL 模板中强制 hour-level estimate / actual / variance；sprint-status.yaml 增加 `estimate_hours` + `actual_hours` 字段；本 sprint 已 done 的 task 全部回填 actual hours | Producer | 0.25 | retro Action #5 | (1) `.claude/skills/story-done/SKILL.md` + `.agents/skills/story-done/SKILL.md` Phase 6/7 含 Effort 段；(2) `production/sprint-status.yaml` 每个 task 含 `estimate_hours` + `actual_hours`（done task 必须有值）；(3) `production/session-state/active.md` 含 Sprint 6 effort 对照段 |

**Should Have 小计**：≈1.25 estimate-days

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S6-Next-Presentation-Cut | 拆分下一 Presentation 系统 stories（候选：audio-system / blurred-ui，先选有 ADR 覆盖的） | game-designer | 1.0 | S6-Combat-UI-Epic-Close | 一个 Ready epic 拆出 ≥4 stories，符合 story readiness 模板 |
| cu-008-Gamepad-HW-Verify | 手柄硬件实机走查（如手柄到位） | QA | 0.5 | 手柄硬件 | hardware screenshot + `production/qa/evidence/cu-008-gamepad-hw.md` |

## Carryover to Next Sprint (Sprint 7)

| Task | Reason | New Owner | Linked Concern |
|------|--------|-----------|----------------|
| cu-visual-evidence | (1) `prototypes/sprint5-combat-ui-harness/` 已在 commit `c8d8c14` 主动删除（与 burst-read spike / 134 个旧 protagonist 资产同清理），原计划的 fixture 录屏路径失效；(2) Sprint 7 第一优先级 = ADR-0020 全循环 VS 重建（gate-tech-setup-to-pre-production-2026-06-22 §C-VS），新 VS 会同步产出 visual baseline，cu-004/005/006/008 visual evidence 应在新 VS 上录制以代表当前架构。 | QA / Dev (Sprint 7) | gate §C-VS, §C-PLAYTEST |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|-------------|
| Godot 4.7-stable 实机集成（cu-006） | Sprint 5 scope-check 主动 defer（Foundation 契约层先到位） | 0.75d (cu-006-godot-integration) |
| cu-004 / cu-005 / cu-006 / cu-008 Visual evidence | Sprint 5 自动化通过 ≠ Visual story done | 0.75d (cu-visual-evidence) |
| 工作树未 commit (Sprint 4 → Sprint 5) | Recurring failure；本 sprint 升级为 Must Have hard gate | 0.25d (S6-Commit-Workspace) |
| 体系专属动画美术资源 (gang/rou/qiao) | 不在工程 sprint scope；属美术 sprint | deferred to art sprint |
| 真实手柄硬件验证 | 无硬件 | Nice to Have，等设备到位 |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Godot 4.7-stable `Engine.TimeScale` 与 Foundation 契约不一致（特别是 `Tween.TweenProcessMode.Always` 在 TimeScale=0 行为） | Medium | High | cu-006-godot-integration 先做 spike：用 harness scene 单独验证 4 个 Godot API 行为；如不一致，回到 Foundation 调契约 |
| Visual evidence 录屏需要实机；mcp_godot 录屏路径稳定性未知 | Medium | Medium | 截图为主、录屏 fallback；evidence MD 接受 ≥3 张关键 frame screenshots |
| commit lint 引入 hook 可能破坏现有工作流 | Low | Medium | 第一周仅推 `.gitmessage` 模板 + manual lint script；hook 留待下一 sprint |
| EI-Debt-Triage 揭示出必须先还的债务，影响下一 Presentation epic 启动 | Low | Medium | 接受 — 这正是 triage 的目的；如发生则 Nice to Have S6-Next-Presentation-Cut 退后 |

## Dependencies on External Factors

- Godot 4.7-stable editor / runtime 可用（mcp_godot 已配置）
- 录屏工具（macOS QuickTime / FFmpeg）
- 手柄硬件（仅 Nice-to-Have cu-008-Gamepad-HW-Verify 依赖）

## Definition of Done for this Sprint

- [ ] 所有 Must Have tasks completed
- [ ] 所有 tasks 通过验收标准
- [ ] QA plan exists (`production/qa/qa-plan-sprint-6.md`)
- [ ] All Logic/Integration stories 通过单元/集成测试
- [ ] Smoke check passed (`/smoke-check sprint 6`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint 6`)
- [ ] **Sprint 5 Definition of Done 真正闭合**（S6-Sprint5-DoD 产出 smoke + team-qa 文档）
- [ ] No S1 or S2 bugs in delivered features
- [x] **combat-ui EPIC 8/8 stories Done + 实机验证 + EPIC.md status: Done**（2026-06-22 S6-Combat-UI-Epic-Close done；cu-visual-evidence Visual baseline carryover 到 Sprint 7，详见 §Carryover to Next Sprint）
- [ ] **工作树干净** — `git status` 无 untracked / modified residue
- [ ] Code reviewed and merged
- [ ] hour-level actual effort 写入每条 `/story-done` session log（含 estimate vs actual 对照）

## Process Notes

- **Review mode**: `lean` — director gates 仅 PHASE-GATE 触发；PR-SPRINT 跳过；code review 仍由 dev/QA 自主触发
- **scope-check 拆层化**：本 sprint 沿用 Sprint 5 cu-006 的 "Foundation 契约 + Godot 实机" 双层模式作为高 Engine Risk Visual story 的默认起手式
- **commit gate**：S6-Commit-Workspace 在 sprint 启动当天闭合；之后每个 story `/story-done` 后立即 commit，不再积压
- **effort gate**：本 sprint 起每个 `/story-done` 必须落 Effort 段（estimate / actual / variance）；sprint 关账前必须补齐所有 task 的 actual hours，否则 DoD 不闭合（接 retro Action #5）

> **Scope check**: 本 sprint 的所有 Must Have 都源自 Sprint 5 retro Action Items + combat-ui EPIC DoD；Should Have / Nice to Have 是 Presentation cutoff 后的衔接动作。如后续追加超出本表的 stories，请运行 `/scope-check combat-ui` 或 `/scope-check exploration-insight`。
