# Sprint 8 — 2026-06-30 to 2026-07-13

> **Sprint Goal**: **Exploration-Insight Presentation 层集成** — 偿还 P1/P2 tech debt 后将洞察系统接入 Godot 场景（水墨提示 + 追查交互 + chapter_00 内容配置），让"探索发现"在实机可体验；补做 VS Playtest 闭合 Sprint 7 prove-or-pivot 终判。
>
> **校准说明**: Sprint 7 估时偏差 -61.5%，本 sprint 对"Foundation 已有基础的集成 story"应用 **0.4x 校准系数**（Est.Days 列为保守上限，Cal.Actual 列为校准后预期）。

## Capacity

- Total: 10 working days (2 weeks)
- Buffer (20%): 2 days reserved for unplanned work
- Available: 8 days
- Calibrated throughput: ~4-5 actual days for Must-Have (基于 Sprint 7 velocity)

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Est. Days | Cal. Actual | Dependencies | Acceptance Criteria |
|---|---|---|---|---|---|---|
| S8-EI-Debt-Paydown | P1 detect path 统一 (`ProximityDetector` 单一入口) + P1 monologue lifecycle 两阶段事件 + P2 hide event dedup guard | Dev | 2.0d | ~0.8d | — | ei-003 `Detect()` 废弃或统一到 `Tick()` 路径；monologue request 走 Pending→Commit/Cancel；hide event 全局 dedup；Foundation tests 0 回归 |
| S8-EI-Presentation-Adapter | ProximityDetector Godot `_PhysicsProcess` adapter + 水墨晕染视觉提示 cue 渲染 (InsightCueOverlay scene) | Dev | 2.0d | ~1.0d | EI-Debt done | `_PhysicsProcess` 每帧调 detector.Tick(delta)；InsightCueShownEvent → 水墨粒子/shader 提示出现在 node position；InsightCueHiddenEvent → 提示消失；headless 0 err |
| S8-EI-Presentation-Interaction | 玩家追查交互输入 + InnerMonologue 显示 (复用 DialoguePanel) + discovery reward UI 反馈 | Dev | 1.5d | ~0.75d | EI-Adapter done | interact 键 → DiscoveryDispatcher.OnPlayerInvestigate → monologue 打字机显示 → quest_flag 设置确认；CodePhrase 学会 → toast 提示 |
| S8-EI-Scene-Content | chapter_00 场景 3-4 个 InsightNode 配置 (cave: 松动砖墙 / 旧信 / 酒痕 / 药壶) + 对应 narrative_context | Dev/Designer | 1.0d | ~0.5d | EI-Interaction done | 实机探索 cave 时可发现 ≥2 个洞察节点；threshold 分级（低/中/高）有差异体验；one_time 节点存档后不重复 |
| S8-Playtest-Session | 1 名半外部玩家试玩 VS 完整循环 (explore→combat→outcome) ≥30min + 反馈报告 | Producer/QA | 0.5d | 0.5d | — | 报告含"是否 5-10 分钟内理解循环"答复；Sprint 7 prove-or-pivot 终判写入 retrospective |
| S8-Build-Warnings-Zero | 清理 CS0219×2 (BossPhaseSystem.cs) + CS8602×2 + CI 加 `-warnaserror` 防回归 | Dev | 0.25d | 0.25d | — | `dotnet build` 0 warning；CI yaml 含 `-warnaserror` flag |
| S8-CI-Godot-Version | CI tests.yml godot 4.6.3 → 4.7-stable (gate C1 一行修复) | Dev | 0.1d | 0.1d | — | CI 使用 Godot 4.7-stable；workflow pass |
| S8-Audio-Epic-Close | 标记 audio-system epic Done + systems-index status 更新 | Dev | 0.1d | 0.1d | — | EPIC.md Status: Done；systems-index audio-system 行标 Done |

**Total Must-Have**: 7.45d est → **~4.0d calibrated actual**

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|---|---|---|---|---|---|
| S8-Architecture-Md-Refresh | architecture.md §12 ADR Roadmap 更新 + Draft→Accepted (gate C5) | Dev | 0.5d | — | §12 包含 ADR-0019~0024；文档状态 Accepted |
| S8-Control-Manifest-Refresh | ADR-0019/0020 规则写入 control-manifest.md (gate C6) | Dev | 0.25d | — | control-manifest 含 ADR-0020 渲染方向约束 |
| S8-Estimate-Calibration | 0.4x 校准规则 + reference class forecasting 写入 sprint-plan skill 注释 | Producer | 0.1d | — | sprint-plan skill 模板含 calibration 段 |

**Total Should-Have**: 0.85d

### Nice to Have

| ID | Task | Status | Notes |
|---|---|---|---|
| cu-008-Gamepad-HW-Verify | 手柄硬件实机走查 | blocked | Steam Deck/通用手柄购入决策待定 |
| S8-Hud-Md-Semantic-Rewrite | hud.md 全文重写对齐 Xingqi 模型 (gate C9) | deferred | 规模较大，可能需独立 sprint |
| S8-Cleanup-Preflight-Checklist | cleanup commit 规范文档 | deferred | retro Action Item |

---

## Carryover from Previous Sprint (Sprint 7)

| Task | Reason | New Estimate |
|------|--------|-------------|
| S7-VS-Playtest-Session → S8-Playtest-Session | 外部玩家未招募 | 0.5d (Must-Have) |
| cu-008-Gamepad-HW-Verify | Steam Deck 硬件未到 (第 3 次 carry) | 0.5d (Nice-to-Have, blocked) |
| Build warnings zero (Sprint 6 retro #5) | Sprint 7 未执行 | 0.25d (Must-Have) |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Playtest 外部玩家招募失败 | Medium | Medium | Day 1 发起招募；备选内部同事 |
| EI detect path 统一可能引起 ei-002/003 测试连锁修改 | Medium | Low | 已有 Foundation 测试覆盖，可快速定位回归 |
| Monologue lifecycle ADR 扩展需跨系统对齐 | Low | Medium | 沿用 cu-006 BattleEventBus 的 Pending→Commit 模式，不新增 ADR |
| 水墨视觉提示 shader 调试耗时超预期 | Medium | Low | 首版用 Particle2D + modulate 替代 shader；shader 留 Sprint 9 Polish |
| 估时校准 0.4x 可能对债务偿还型 story 不适用 | Low | Low | debt paydown 类任务保持原估时不压缩 |

## Dependencies on External Factors

- 半外部玩家招募（同事/朋友），需 Day 1 发起邀约
- CI Godot 4.7-stable action 可用性（GitHub Actions 市场已有）

---

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] EI Presentation 在实机可体验：cave 场景中探索时出现水墨提示 + 可追查
- [ ] Playtest 报告含 Sprint 7 prove-or-pivot 终判
- [ ] Foundation tests 0 regression（baseline 1432+）
- [ ] `dotnet build` 0 warning
- [ ] CI workflow 使用 Godot 4.7-stable
- [ ] `/smoke-check sprint` PASS or PASS WITH WARNINGS
- [ ] No S1 / S2 bugs open
- [ ] Audio-system epic 标记 Done

---

## Process Notes

- **review-mode**: `lean`（沿用）
- **估时校准**: 本 sprint 首次正式应用 0.4x 校准系数；retrospective 时回看偏差以调整系数
- **EI Presentation stories 拆分**: S8-EI-Debt-Paydown → S8-EI-Presentation-Adapter → S8-EI-Presentation-Interaction → S8-EI-Scene-Content 为串行依赖链
- **commit message**: 沿用 conventional commits + scope (`feat(exploration): ...` / `fix(ei): ...`)
- **关联 retro action items**: Sprint 7 retro #1 (估时校准) + #2 (Playtest) + #3 (Build warnings) + #4 (headless docs) + #5 (Gamepad 决策) 均在本 sprint 计划中体现

## Linked Artifacts

- `production/retrospectives/retro-sprint-7-2026-06-29.md` — Sprint 8 Action Items 来源
- `production/notes/exploration-debt-triage-2026-06-22.md` — EI debt paydown 参考
- `production/epics/exploration-insight/EPIC.md` — ei-001~005 Foundation Complete
- `production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md` — C1/C5/C6 驱动
- `docs/architecture/adr-0018-exploration-insight.md` — EI Presentation 架构约束

---

> **Scope check**: 本 sprint 单一焦点 EI Presentation + playtest。如发现 EI Presentation 需要新增超出计划的 story，请运行 `/scope-check exploration-insight` 评估范围。
