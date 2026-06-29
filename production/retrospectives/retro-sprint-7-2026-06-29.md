# Retrospective: Sprint 7

**Period**: 2026-06-23 -- 2026-07-06（计划）；Must-Have 实际收尾 2026-06-29（提前 7 天）
**Generated**: 2026-06-29
**Stage**: Production
**Sprint Goal**: ADR-0020 全循环 Vertical Slice 重建 — 第一章·江南 explore→combat→outcome 完整循环，验证"纯 2D 武侠 + 行气战棋（观/动）"在 Godot 4.7-stable 工作树可玩；同步录制 cu-004/005/006/008 共 4 份 Visual evidence。

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|---------|--------|-------|
| Must-Have Stories | 13 | 13 done | 0 |
| Should-Have | 1 | 0 done (backlog) | -1 |
| Completion Rate (Must-Have) | -- | 100% | -- |
| Effort Hours (Must-Have) | 74.0h | 28.5h | -45.5h (-61.5%) |
| Emergent Tasks Added & Done | -- | 5 | -- |
| Bugs Found | -- | 1 (cu-006 assertion timing) | -- |
| Bugs Fixed | -- | 1 | -- |
| Commits | -- | 77 | -- |
| Foundation Tests | 1403 baseline | 1403+ (含 audio 新增 29) | 0 回归 |

---

## Velocity Trend

| Sprint | Planned (Must-Have) | Completed | Rate |
|--------|---------|-----------|------|
| Sprint 5 | 5 | 5 | 100% |
| Sprint 6 | 8 in-scope | 7 + 1 carryover | 87.5% |
| Sprint 7 (current) | 13 | 13 | 100% |

**Trend**: 稳定上升 — Must-Have 完成率回到 100%，且首次在单 sprint 内完成 13 个 story（含 6 个 cu-visual-evidence 子任务拆分后的全部交付），同时额外完成 5 项涌现工作。Sprint 6 的 carryover (cu-visual-evidence) 在本 sprint 彻底闭环。

---

## What Went Well

- **Sprint Goal 完全达成**：第一章·江南 explore→combat→outcome 完整循环在 Godot 4.7-stable 工作树可玩，仅用 7 天（vs 计划 14 天），提前 7 天完成全部 Must-Have。
- **cu-visual-evidence 自动化突破**：原设计为 owner 手动录屏（2h），通过 AutoEvidencePlayer 截图系统实现完全自动化（0.5h），24 张截图零 ERROR，消除人工依赖成为可复用基础设施。
- **Sprint 6 carryover 100% 闭环**：cu-visual-evidence 4 份 evidence 全部升级为 Visual Captured，Sprint 6 retro Action Item #1（ADR-0020 VS 重建）完全达成。
- **涌现工作高价值**：对话系统端到端集成 + .dlg 编译器 + explore TileMap 化 + 音频 Motif/Volume Settings 4 项涌现工作均在 sprint 时间窗内完成，无一阻塞 Must-Have 关键路径。
- **Foundation 层 0 回归**：1403 测试基线 → 新增 audio 29 测试 = 1432，全程 0 fail、0 regression。
- **ADR-0022 iso pivot 代码层完整落地**：Foundation IsoProjection + Iso4 方向系统 + CavePlayer 迁移 + main_character.tres 重建一次性完成，8dir 代码清理干净。
- **估时偏差虽大但方向一致（全部高估）**：没有任何 story 超时，说明技术风险评估虽保守但有效防止了 sprint 超载。

---

## What Went Poorly

- **估时系统性高估 61.5%**：13 个 Must-Have story 总估时 74h，实际仅需 28.5h。最严重的 S7-VS-Foundation-Scene（16h→2.5h，-84%）说明 placeholder/scaffold story 估时模型完全失效。Sprint 6 retro 已提出"doc-only ≤2h"校准，但 code story 的估时偏差更为严重，未被同步校准。
- **Playtest session 未执行**：Sprint Goal 的 prove-or-pivot 判定条件之一是"playtest 玩家理解循环"，但 S7-VS-Playtest-Session 仍停在 backlog — 说明外部依赖（玩家招募）未在 sprint 中期主动推进。
- **cu-006 assertion timing bug**：AddStagger(5) 后 controller.Start() 消耗 1 点破绽导致 assertion 误报 — 暴露了 AssertDemoState 的断言时序设计未充分考虑 runtime 状态变化。修复简单但发现成本高（需运行自动化全流程才暴露）。
- **Demo scene 文件误删**：之前 git 操作导致 `feng-zhi/scenes/vs/demo/` 下 4 个 demo scene 被删除，需要手动 git checkout 恢复 — 与 Sprint 6 retro Action Item #6（cleanup 必跑 grep）相关的同类问题。
- **Godot headless 路径陷阱**：`/usr/local/bin/godot` 软链接触发 `.NET: Assemblies not found`，必须直接调用 `/Applications/Godot_mono.app/Contents/MacOS/Godot` — 文档化不足导致每次 headless 操作都可能踩坑。

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|----------|------------|------------|
| cu-006 assertion timing (AddStagger 后 Start 消耗破绽) | ~30 min | 断言移到 AddStagger 之后、Start 之前 | 断言应在数据写入点立即验证，不要延迟到 runtime 改变状态后 |
| Demo scene 文件被 git 删除 | ~15 min | `git checkout --` 恢复 4 个 demo .tscn + 依赖 scripts | Sprint 6 retro #6 cleanup grep 规则需加入 `.tscn` 引用扫描 |
| Godot headless .NET assemblies 路径 | ~20 min | 使用完整路径 `/Applications/Godot_mono.app/Contents/MacOS/Godot` | 文档化 + tools/run-evidence-capture.sh 固化正确路径 |
| Playtest 玩家招募 | 未解 | 延至 Sprint 8 | Sprint 8 Day 1 主动发起招募 |

---

## Estimation Accuracy

| Task | Estimated | Actual | Variance | Likely Cause |
|------|-----------|--------|----------|--------------|
| S7-VS-Foundation-Scene (最高估) | 16.0h | 2.5h | **-84%** | placeholder scaffold 不需要 2 天，0.5d 即可 |
| S7-Day1-Smoke-Startup | 2.0h | 0.5h | -75% | 手动 smoke 仅需 30min |
| cu-visual-evidence-recording | 2.0h | 0.5h | -75% | 自动化取代人工录制 |
| S7-Iso-Pivot-Foundation | 8.0h | 2.5h | -69% | survey 阶段悲观估计 + AC8 N/A |
| S7-VS-Combat-Loop | 12.0h | 4.0h | -67% | dependency R2 "编排层 0%" 实际 1h 化解 |
| cu-004-vs-integration | 4.0h | 1.5h | -62% | Foundation 已 partial 实现 |
| S7-VS-Outcome-Feedback | 10.0h | 4.5h | -55% | Foundation MindsetService 已 ready |
| S7-VS-Scope-Spike | 8.0h | 4.0h | -50% | 决策快于预期 |
| cu-005-vs-integration | 3.0h | 2.0h | -33% | — |
| cu-008-vs-integration | 2.0h | 1.5h | -25% | — |
| S7-Animator-Directional-Port (最准) | 3.0h | 3.0h | **0%** | 唯一准确估时 |

**Overall estimation accuracy**: 1/13 任务 within ±20% (7.7%)。12/13 高估 ≥25%。0 正方差（无超时）。

**Analysis**: Sprint 7 的估时偏差比 Sprint 6 更严重（Sprint 6 doc-only 60-88% 高估；Sprint 7 code stories 25-84% 高估）。根因有三：
1. **Foundation 层先验积累**被低估 — 已有 MindsetService / CombatMoveSelectionPanel / DialogueRuntime 等大量可复用基础，集成比 greenfield 实现快 2-3x。
2. **placeholder/scaffold story 需要独立估时类目**（≤0.5d / 4h 上限）。
3. **spike 决策落地比"实施 spike 结论"快** — scope-spike + combat-loop 合计仅 8h（vs 20h 估计），因 spike 本身已 de-risk 了技术不确定性。

**建议校准系数**: Sprint 8 对同类 story 应用 **0.4x multiplier**（即当前估时 × 0.4 = 预期实际工时），直到积累 2 sprint 校准数据。

---

## Carryover Analysis

| Task | Original Sprint | Times Carried | Reason | Action |
|------|----------------|---------------|--------|--------|
| cu-visual-evidence | Sprint 6 → 7 | 1 | harness 删除 → Sprint 7 VS 上重录 | **CLOSED** — 本 sprint 100% 完成 |
| cu-008-Gamepad-HW-Verify | Sprint 5 → 6 → 7 → 8 | 3 | Steam Deck 硬件未到 | 继续 carry → Sprint 8（需显式购入决策） |
| S7-VS-Playtest-Session | Sprint 7 → 8 | 1 | 外部玩家未招募 | 转 Sprint 8 Must-Have |

---

## Technical Debt Status

| 指标 | 当前 | 上 Sprint (S6) | 趋势 |
|------|------|------|------|
| TODO/FIXME/HACK（全仓 .cs） | 1 | 0 | 微增 (+1, OneShotClaimRegistry.cs SaveManager 接入待办) |
| Build warnings | ≥4 (pre-existing) | 4 | 持平 — Sprint 6 retro #5 未执行 |
| EI tech debt 条目 | 7 triaged (2 P1 + 1 P2 + 4 deferred) | 同 | 持平 |
| jiangnan-iso 资产方向 | paused (ADR-0024) | — | 新增债务 — iso 资产层暂撤但方向不变 |

---

## Previous Action Items Follow-Up

| Action Item (Sprint 6 retro) | Status | Notes |
|-------------------------------|--------|-------|
| #1 ADR-0020 全循环 VS 重建 | **Done** ✅ | Sprint 7 Goal 100% 达成 |
| #2 Godot 4.7 实机手工启动确认 | **Done** ✅ | S7-Day1-Smoke-Startup done Day 1 |
| #3 doc-only task 估算口径校准 | **Partial** | Sprint 7 无纯 doc-only task 验证；code story 估时偏差更大（新问题） |
| #4 cu-008-Gamepad-HW-Verify 升级处理 | **Not Started** ❌ | 仍停 backlog，Steam Deck 购入/借测决策未做 |
| #5 Build warnings 0-warning 硬规则 | **Not Started** ❌ | 4 warnings 仍存，未清理未加 `-warnaserror` |
| #6 cleanup 必跑 grep + 跨 story 引用扫描 | **Partial** | 本 sprint demo scene 误删说明仍有盲区 |

**Follow-up closure rate**: 2/6 Done + 2/6 Partial + 2/6 Not Started = 33% 全闭环率。较 Sprint 6 的 "首次 5/5 closure" 退步，原因是 Sprint 7 全力聚焦 VS Sprint Goal，medium-priority 改进项被挤出。

---

## Action Items for Next Iteration

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|----------|----------|
| 1 | **估时校准系数制度化**: Sprint 8 起所有 story 估时附带 `confidence` 字段（high/medium/low）+ 对 Foundation 已有基础的集成 story 应用 0.4x 校准; scaffold story ≤4h 硬上限 | Producer | **High** | Sprint 8 plan 时纳入 |
| 2 | **Playtest session 补做**: Sprint 8 Day 1-3 招募 1 名半外部玩家完成 VS 试玩，产出 prove-or-pivot 终判 | Producer / QA | **High** | Sprint 8 Week 1 |
| 3 | **Build warnings 清零 + CI -warnaserror**: 清理 CS0219×2 + CS8602×2，在 CI 加硬门控防回归（Sprint 6 retro #5 二次带入） | Dev | Medium | Sprint 8 Week 1 |
| 4 | **Godot headless 路径文档化**: 在 `docs/engine-reference/godot/` 下落"macOS headless 启动注意事项" + `tools/run-evidence-capture.sh` 作为唯一入口 | Dev | Medium | Sprint 8 Day 1 |
| 5 | **cu-008-Gamepad-HW-Verify 购入决策**: Steam Deck / 通用 USB 手柄选一；不在 Sprint 8 完成则正式 descope 至 Polish 阶段（Sprint 6 retro #4 三次带入） | Producer | Medium | Sprint 8 Week 1 |

---

## Process Improvements

- **AutoEvidencePlayer 成为标准基础设施**: `tools/run-evidence-capture.sh` + `EVIDENCE_CAPTURE=1` 环境变量模式可复用于所有未来 visual evidence story（exploration-insight、romance-system 等），建议写入 QA plan skill 注释。
- **DemoSeed + inherited .tscn 模式可推广**: `feng-zhi/scenes/vs/demo/` 下 4 个 demo scene 展示了"一个 battle scene + N 个 demo override"的最小 QA 基础设施模式，Sprint 8 新 story 可直接复用。
- **估时反馈循环需要加速**: 连续 2 个 sprint 估时偏差 > 50% 说明估时模型缺乏历史数据校准。建议 Sprint 8 plan 时引入 "reference class forecasting"（类似 story 的历史 actual hours 作为基线）。

---

## Sprint Goal Verdict

**PROVE** — VS 完整循环可玩（explore→combat→outcome→mindset shift→return），cu-004/005/006/008 四份 visual evidence 全部升级为 Visual Captured，0 S1/S2 bug。

> ⚠️ 条件: Playtest session 未完成（should-have），玩家理解度验证延至 Sprint 8。若 Sprint 8 playtest 反馈"不理解/不享受"，verdict 降级为 PROVE WITH PIVOT。

**Sprint 8 下一步**: 启动 Feature/Presentation 9 个 Ready epic 中的 1-2 个，同时补做 playtest session 闭环 prove-or-pivot 终判。

---

## Summary

Sprint 7 是《风止》Production 阶段的首个 prove-or-pivot 决策点，结果为 **PROVE**：第一章江南完整循环在 7 天内（vs 计划 14 天）全部交付，4 份 cu-visual-evidence 通过 AutoEvidencePlayer 自动化完全闭环，5 项涌现工作（对话系统、编译器工具、TileMap 化、音频 Motif/Volume）同步完成。最需要改进的是估时体系 — 连续 2 sprint 系统性高估 50-60% 说明需要引入校准系数和历史基线。Sprint 8 最重要的一件事：补做 playtest session，让 prove verdict 从"技术可行"升级为"玩家可理解"。
