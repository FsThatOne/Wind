# Sprint 9 Retrospective

> **Sprint**: 9 — EI Epic Close + System Polish
> **Period**: 2026-06-30 ~ 2026-06-30 (Day 1 完成全部 dev stories)
> **Velocity**: 7/7 dev-owned stories done in ~1.5h actual

---

## Metrics

| Story | Est. | Actual | Variance | Type |
|-------|------|--------|----------|------|
| S9-001 EI Epic Close | 0.5h | 0.25h | -50% | Doc |
| S9-003 集成走查 | 1.0h | 0.25h | -75% | Integration |
| S9-004 cu-008 descope | 0.5h | 0.1h | -80% | Doc |
| S9-005 测试覆盖度审查 | 1.0h | 0.25h | -75% | QA |
| S9-006 Stress Test | 1.0h | 0.25h | -75% | Logic |
| S9-007 LockMode 验证 | 1.0h | 0.25h | -75% | Integration |
| S9-008 Retrospective | 0.5h | 0.25h | -50% | Doc |
| **Total** | **5.5h** | **1.6h** | **-71%** | — |

### Calibration Factor Update

| Sprint | Category | Factor Used | Actual Variance | Recommended Next |
|--------|----------|-------------|-----------------|------------------|
| 7 | Foundation Logic | 0.4x | -40% | 0.3x |
| 8 | Integration+Config | 0.4x | -60% | 0.3x |
| 9 | Doc+QA+Polish | fixed 0.5-1h | -71% | 固定 0.25h (Doc/QA) |

**建议**: Sprint 10 起对 Doc/QA/Polish 类 story 使用 **0.25h 固定估时**，Code 类保持 0.3x。

---

## What Went Well

1. **EI Epic 高质量关闭** — DoD 5/5 全过，9 条 GDD AC 100% test coverage，无残留 tech debt（除已记录的 LockGuard advisory）
2. **Stress test 一次通过** — 50 cycles rapid scene transition 无泄漏，证明 Foundation 层设计扎实
3. **GDD 一致性保持** — 增量审查无新矛盾，6-16 rerun 修复全部持久
4. **Polish backlog 制度化** — 首次建立 `production/backlog/polish-backlog.md`，结束了 cu-008 的 4-sprint carry

## What Could Improve

1. **Playtest 仍未执行** — 连续 3 sprint carry，证明"放入 sprint"不等于"会被执行"
2. **估时系统性过高** — Doc/QA 类一律高估，需要分类基准而非统一系数
3. **Nice-to-have 基本不会执行** — 每 sprint 的 nice-to-have 都被跳过，考虑取消这一分类

## Action Items

| # | Action | Owner | Deadline |
|---|--------|-------|----------|
| 1 | Playtest: Day 3 前安排试玩，否则标记 PROVE(tech-only) | User | 2026-07-02 |
| 2 | 估时校准: Doc/QA = 0.25h fixed, Code = 0.3x | Dev | Sprint 10 plan |
| 3 | 取消 nice-to-have 分类，改用 "stretch" 仅限 1 条 | Dev | Sprint 10 plan |
| 4 | cu-008 PB-001: 购入 USB 手柄决策 | User | Polish phase |

---

## Prove-or-Pivot Status

**Technical prove**: ✅ 通过
- EI 全链路 explore→detect→investigate→reward→save 集成验证通过
- 97→99 测试全部 pass
- 4 个场景内容节点端到端可玩

**Gameplay prove**: ⏸ 待 Playtest 反馈
- 核心问题："5-10 分钟内理解循环？"仍未验证
- 如果 Day 3 未执行 → 标记 PROVE(tech-only)，不阻塞 Sprint 10 启动

---

## Sprint 10 方向建议

基于当前项目状态（14 Done epics, 9 Ready epics, 序章另一 agent 在搭建）：

1. **首选**: 等序章 agent 产出后集成 + Playtest → 用真实内容验证游戏循环
2. **次选**: misunderstanding-system epic 启动（中后期核心系统，依赖少）
3. **保底**: EI 视觉 Polish + 更多场景 InsightNode 内容配置
