# Sprint 7 — 2026-06-23 to 2026-07-06

> **Sprint Goal**: **ADR-0020 全循环 Vertical Slice 重建** — 第一章·江南 explore→combat→outcome 完整循环，验证"纯 2D 武侠 + 行气战棋（观/动）"在 Godot 4.7-stable 工作树可玩；同步在新 VS 上录制 cu-004/005/006/008 共 4 份 Visual evidence。
>
> **本 sprint 是 Production 阶段首个 prove-or-pivot 决策点**（参见 §Verdict Matrix）。

## Capacity

- Total: 10 working days (2 weeks)
- Buffer (20%): 2 days reserved for unplanned work（含 spike unknown）
- Available: 8 days

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|---|---|---|---|---|---|
| S7-VS-Scope-Spike | VS 范围决策 spike：(a) 第一章·江南场景结构与剧情节拍；(b) 判定 combat-system epic（done）是否兼容行气战棋（观/动）或需要 partial 重写；(c) 输出 VS scope document + 风险表 | Architect / Designer / Producer | 1.0d | — | `docs/superpowers/specs/2026-06-23-vs-scope-spike.md` 含：场景骨架图、combat 复用率评估（≥%/ 重写 plan）、5-10 分钟可玩性目标、out-of-scope 列表；Day 1 完成 |
| S7-Day1-Smoke-Startup | qa-tester Day 1 手工实机启动 StartCave / 主战斗 scene 确认（cleanup commit 后基线确认） | QA | 0.25d | — | `production/qa/evidence/s7-day1-smoke-2026-06-23.md` 含截图 + 操作记录；记入 active.md；gate Condition §2 / W2 闭环 |
| S7-VS-Foundation-Scene | VS 主场景骨架（Godot 4.7 + scene-markup-tool addon）：1 个江南探索场景 + 1 个战斗场景 + 转场逻辑 | Gameplay-Programmer / Tools-Programmer | 2.0d | S7-VS-Scope-Spike done | StartCave 类 entry → 江南 explore scene → trigger → 战斗 scene → outcome scene 闭环；scene-markup-tool 标注可交互元素；scenes 在 4.7 编辑器无 import error |
| S7-VS-Combat-Loop | 行气战棋（观/动两阶段）在 VS 内跑通 1 场完整战斗，复用 combat-system / martial-arts-system / enemy-ai 已 done epic（或按 spike 输出做必要 partial 重写） | Gameplay-Programmer | 2.0d | S7-VS-Scope-Spike done, S7-VS-Foundation-Scene done | 1 场 1v1 战斗在 VS 战斗 scene 中可完整进入 → 观 → 动 → 一击决胜或正常结算 → 退出；Foundation 1367 测试无回归 |
| S7-VS-Outcome-Feedback | 战后心境双轴位移 + 朦胧化战后面板（mindset-dual-axis + blurred-ui 集成）in VS | Gameplay-Programmer / UI-Programmer | 1.0d | S7-VS-Combat-Loop done | 战斗结束触发 mindset 位移事件 → 朦胧化战后面板渲染 → 返回 explore scene；事件链路在 active.md 留 trace |
| cu-visual-evidence | 4 份 visual evidence 在 VS 上录制（cu-004 招式面板 / cu-005 反制+决胜提示 / cu-006 一击决胜 / cu-008 双焦点）；4 份 evidence MD 段 `(Sprint 7 carryover — Pending)` → `Visual Captured` + designer 与 qa-lead 双签 | Designer / QA / Dev | 0.75d | S7-VS-Combat-Loop done | 每份 evidence MD ≥1 段录屏 + ≥3 张关键截图；素材挂 `production/qa/evidence/media/`；4 份 status 升级为 `Visual Captured`；gate C-VS / C-PLAYTEST §Condition 1 闭环 |

**Total Must-Have**: 7.0d

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|---|---|---|---|---|---|
| S7-VS-Playtest-Session | 1 次新玩家试玩 session（≥30 分钟，含 5-10 分钟无引导 + 反馈访谈），产出 `production/qa/playtest-vs-2026-07-XX.md` | QA / Designer / Producer | 0.5d | cu-visual-evidence done | 1 名外部 / 半外部玩家完成试玩；记录"是否在 3-10 分钟内理解循环"; gate §C-PLAYTEST 闭环 |

**Total Should-Have**: 0.5d

### Nice to Have

本 sprint **单一焦点 VS**。retro / gate 中优先级 concerns 全部 **deferred** 到 Sprint 8：

- ❌ S8-Build-Warnings-Zero（4 warnings 清理 + CI -warnaserror）— retro #5 / W3
- ❌ S8-Estimate-Calibration（sprint-plan skill 注释加二极规则）— retro #3
- ❌ S8-Cleanup-Preflight-Checklist（cleanup commit 规范）— retro #6
- ❌ S8-Architecture-Md-Refresh（gate C5 docs/architecture/architecture.md §12 + 解 Draft 状态）
- ❌ S8-Control-Manifest-Refresh（gate C6 ADR-0019/0020 加入 control-manifest）
- ❌ S8-Hud-Md-Semantic-Rewrite（gate C9 hud.md 全文重写对齐 Xingqi 模型）
- ❌ S8-Accessibility-Slowmotion-Redesign（gate C10 "Burst+Read slow mode" 替代方案）
- ❌ S8-Combat-System-Md-Path-Fix（gate C11 design/gdd/systems/combat-system.md 路径修正）
- ❌ cu-008-Gamepad-HW-Verify（Steam Deck 借测/购入决策）— retro #4

---

## Carryover from Previous Sprint (Sprint 6)

| Task | Reason | New Estimate |
|---|---|---|
| cu-visual-evidence | harness 删除导致 fixture 录屏路径断；改在新 VS 上录制（决策 commit `9b1b9b2`） | 0.75d（拉入 Must） |
| cu-008-Gamepad-HW-Verify | Steam Deck 硬件未到 | backlog → Sprint 8 显式决策 |
| S6-Next-Presentation-Cut | 等 VS 验证完再 cut | backlog |

## Carryover to Next Sprint (Sprint 8)

预期：

- 若 Sprint 7 Verdict = **PROVE** → Sprint 8 启动 Feature/Presentation 9 个 Ready epic 中的 1-2 个（romance-system / exploration-insight / blurred-ui / audio-system 等），同时清理上述 9 个 deferred 条目（hardening + gate concerns）
- 若 Sprint 7 Verdict = **PROVE WITH PIVOT** → Sprint 8 不开新 epic，先做设计层 ADR + brainstorm
- 若 Sprint 7 Verdict = **PIVOT** → Sprint 8 stop 开发，全员 brainstorm + 重大 ADR 决策（可能撤回 ADR-0020 或 combat 模型）

---

## Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| combat-system epic（done）是基于 Burst+Read，行气战棋需要 partial 重写 | **High** | High | Day 1 S7-VS-Scope-Spike 量化复用率；若 < 50% 需 spike 报告中拆出新 story 并申请 scope 调整（/scope-check combat-system） |
| VS spike 范围失控（1 章扩到多章 / 多 loop） | Medium | High | 严格限：1 江南场景 + 1 战斗场景 + 1 outcome；超出立即 /scope-check |
| VS 不可玩（prove-or-pivot 决策点） | Medium | Critical | Playtest 失败 → Sprint 8 不开新 epic，先 brainstorm + ADR pivot |
| Visual evidence 录制要现场设计 capture flow（harness 已删） | Medium | Medium | scene-markup-tool 加入 capture overlay；cu-006 7-phase 验证用 Godot 内 debug overlay 不依赖 harness |
| 4.7 Engine.TimeScale / Tween / Camera / InputMap 在 VS 战斗 scene 集成回归 | Low | High | cu-006 Foundation 8/8 仍是自动化基线；VS 集成依赖 ADR-0011 §实机集成 + Filter any-allowed 语义 |
| 估算偏差（VS 是首次 spike，无 baseline） | **High** | Medium | 7d must + 2d buffer = 9/10d 实际使用；保留 1d 应急；Day 1 spike 完成后立即校准 |
| Sprint 6 retro #3（doc-only ≤2h default）未在本 sprint 校准 | Low | Low | S7-VS-Scope-Spike 是真 spike，不适用此规则；deferred 项归 Sprint 8 |

## Dependencies on External Factors

- 外部 playtest 玩家招募（half-external，可同事 / 朋友）
- Sprint 7 Day 1 即刻执行 S7-VS-Scope-Spike（不能拖到 Day 3+，否则 must-have 不可能完成）
- combat-system epic 实际复用率 — 需 Day 1 spike 现场判定

---

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] VS 在 Godot 4.7-stable 中可完整跑通：江南 explore → 1 场战斗（观/动）→ 心境位移 → 朦胧化反馈 → 回 explore，全程 5-10 分钟无引导
- [ ] cu-004/005/006/008 共 4 份 Visual evidence 升级为 `Visual Captured` + designer/qa-lead 双签
- [ ] Foundation 1367 测试 + 新 VS 测试无回归
- [ ] `/smoke-check sprint 7` PASS or PASS WITH WARNINGS
- [ ] `/team-qa sprint 7` APPROVED or APPROVED WITH CONDITIONS
- [ ] Day 1 manual smoke 实机启动确认完成（gate Condition §2 / W2 闭环）
- [ ] Playtest report 含"是否 5-10 分钟内理解 + 体验完整循环"答复
- [ ] No S1 / S2 bugs open
- [ ] Sprint 7 Sprint Goal Verdict: PROVE / PROVE WITH PIVOT / PIVOT 明确写入 retrospective

---

## Verdict Matrix（VS 是 prove-or-pivot 决策点）

| 触发条件 | Verdict | Sprint 8 下一步 |
|---|---|---|
| VS 完整跑通 + playtest 玩家理解循环 + 0 S1/S2 bug | **PROVE** | 启动 Feature/Presentation 9 个 Ready epic 中的 1-2 个 |
| VS 完整跑通但 playtest 玩家不理解 / 不享受 | **PROVE WITH PIVOT** | brainstorm + 设计层 ADR；不立刻开新 epic |
| VS 无法跑通（核心 spike 失败） | **PIVOT** | stop 开发，全员 brainstorm + 重大 ADR 决策 |

---

## Process Notes

- **review-mode**: `lean`（沿用，director gates 仅 PHASE-GATE 触发）
- **commit message**: 沿用 Sprint 6 引入的 conventional commits + scope（`feat: ...(TR-...)` / `chore(sprint-7): ...` / `docs(retrospective): ...`）
- **estimate 校准**: 本 sprint 不适用 Sprint 6 retro #3 (doc-only ≤2h)，因为本 sprint 唯一 spike 是真 engine-integration spike
- **gate-check 触发**: 若 Day 1 spike 输出 "combat 复用率 < 50%" 或 "VS scope > 10d"，立即触发 `/architecture-review` + `/scope-check`，不要硬推
- **关联 gate**: `production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md` §C-VS / §C-PLAYTEST 是本 sprint 主要驱动
- **active.md trace**: 每个 Must-Have story 完成后必须在 active.md 留 `Session Extract` 段，含 effort hours + key decisions

## Linked Artifacts

- `production/retrospectives/retro-sprint-6-2026-06-22.md` — Sprint 7 Action Items 来源
- `production/qa/qa-signoff-sprint-6-2026-06-22.md` — Sprint 7 Conditions §1/§2/§3 来源
- `production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md` — §C-VS / §C-PLAYTEST 驱动
- `docs/architecture/adr-0020-pure-2d-wuxia-rendering-direction.md` — 美术 + 渲染方向
- `docs/architecture/adr-0011-combat-ui-animation.md` — cu-006 集成基线
- `docs/architecture/adr-0021-character-animation-port.md` — ICharacterAnimator port（解耦动画系统）

---

> **Scope check**: 本 sprint 单一焦点 VS。如 Day 1 spike 后发现需要新增 story，请运行 `/scope-check` 评估范围。
>
> **QA Plan 状态**: ⚠️ 本 sprint 暂无 QA plan。建议在 S7-VS-Scope-Spike 完成后立即运行 `/qa-plan sprint 7`（≤ Day 1）。Production → Polish gate 要求 QA sign-off。
