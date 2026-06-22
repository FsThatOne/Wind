# VS Scope Spike — ADR-0020 全循环 Vertical Slice 重建

> **Date**: 2026-06-23 (Sprint 7 Day 1, ahead of schedule — 实际产出 2026-06-22)
> **Story**: `production/sprints/sprint-7.md` §Must Have `S7-VS-Scope-Spike`
> **Drivers**: gate `production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md` §C-VS / §C-PLAYTEST；Sprint 6 retro Action #1；qa-signoff-sprint-6 §Conditions §1
> **Owner**: Cursor agent (代行 architect + designer + producer 角色)；待 user 复核
> **Status**: **Draft — Pending User Sign-off**
> **Verdict 推荐**: **Option B · Lite Xingqi VS**

---

## 1. Spike 目标

在 Sprint 7 第一天回答三个问题：

1. **combat-system epic（10/10 done）是否兼容 ADR-0020 + Xingqi 行气战棋设计？复用率多少？**
2. **第一章·江南 VS 的场景结构与剧情节拍是什么？**
3. **5-10 分钟无引导可玩性目标具体指什么？哪些是 in-scope / out-of-scope？**

---

## 2. GDD vs Code Gap Analysis（核心发现）

### 2.1 现状

| 层 | 当前状态 | Last Update |
|---|---|---|
| `design/gdd/combat-system.md` | **Xingqi 行气战棋**（行气条 + 棋盘 + 移动 + 朝向 + 观气取位破绽决胜） | 2026-06-16 |
| `src/FengZhi.Foundation/Combat/` 代码 | **回合制基础框架**（`BattlePhase`: Initializing → RoundStart → IntentReveal → PlayerDecision → Resolution → RoundEnd → BattleOver） | cb-001..010 done 时（多在 2026-06-10..16 之间） |
| `src/` 中 "Xingqi" / "行气" / "xingqi" token 数 | **0** | — |
| `src/` 中棋盘 / facing / grid 代码 | **0** | — |

**结论**：combat-system epic 标记 `Complete` **不代表代码已对齐 Xingqi GDD**。code 实现的是 mechanism-neutral 的回合制框架（命名通用，不含 Burst/Read 也不含 Xingqi），但缺少 Xingqi 特定的 **行气条积累、棋盘移动、朝向、侧背击** 等核心机制。

### 2.2 模块级复用率评估

| Xingqi GDD 要素 | Code 现状 | 复用率 | VS 处理 |
|---|---|---|---|
| 回合阶段状态机（观气/出招/结算/破绽/决胜循环） | cb-001 `BattlePhase` 已有等价 phase（IntentReveal/PlayerDecision/Resolution） | 80% | **复用** — 语义层重命名 phase（IntentReveal → 观气；PlayerDecision → 出招选择；Resolution → 结算+破绽+决胜） |
| 气血/内息/破绽资源 | cb-002 `BattleCombatant` 完整 | 100% | **复用** |
| 伤害结算管线 F1-F4 | cb-003 `DamageResolutionPipeline` 完整 | 90% | **复用** — VS 不实现侧/背击加成 |
| 破绽 + 一击决胜 | cb-004 `StaggerService` 完整 | 70% | **复用** — VS 决胜条件按现有"破绽 ≥ 阈值"触发，不实现"破绽大开窗口"精确判定 |
| 行动注册表 | cb-005 `ActionRegistry`（出招/调息/反制/道具/顿悟） | 90% | **复用** — VS 不新增"移动"行动类型 |
| 反制 | cb-006 `CounterService`（独立反制按钮） | ⚠️ **30%** | **降级复用** — Xingqi GDD 说不再有独立反制按钮，反制变 passive。VS **保留** 现有反制按钮（与 cu-005 UI 一致），但 Sprint 8+ 需要 ADR + GDD 决策是否真的撤销反制按钮 |
| 意图洞察 | cb-007 `IntentInsightSystem` | 80% | **复用** — UI 文案改为"显示当前内功气机"而非"下一招" |
| 战斗事件总线 | cb-008 `BattleEventBus` | 100% | **复用** |
| 多人战斗 + 协同破绽 | cb-009 完整 | 100% | **复用** — VS 用 1v1 不需要协同 |
| 战斗入口与配置 | cb-010 `BattleFacade` | 100% | **复用** |
| **行气条积累系统**（xingqi_gain / xingqi_threshold / 出手队列排序） | **未实现** | **0%** | **OUT OF SCOPE** — VS 用固定回合制，不实现行气条 |
| **棋盘 + 移动 + 朝向 + 侧背击 + LOS** | **未实现** | **0%** | **OUT OF SCOPE** — VS 用 1v1 抽象位置（"靠近 / 拉开"两态），不实现棋盘 |
| **决胜大开窗口**（broken-pose-window） | 部分（cb-004 破绽阈值已有） | 50% | **降级实现** — 沿用 cb-004 阈值，不实现精确窗口 |

### 2.3 整体复用率

**Foundation 层加权复用率 ≈ 75%**（按 GDD 章节权重计算：核心循环 + 资源 + 伤害 + 事件总线占 GDD 70%+，全部 ≥90% 复用；行气条 + 棋盘缺失占 GDD 20-30%，0% 复用）。

---

## 3. VS Scope Decision: **Option B · Lite Xingqi VS**

### 3.1 决策

Sprint 7 VS 采用 **Lite Xingqi**：用现有 combat-system 回合制框架包装"观气 → 出招 → 破绽 → 决胜"循环，**不实现行气条与棋盘**。这是经过权衡的简化，目的是让 Sprint 7 在 8 工作日内交付可玩 VS，进入 prove-or-pivot 决策点。

### 3.2 备选对比

| 选项 | 工作量 | 完成度 | 风险 |
|---|---|---|---|
| **A · Full Xingqi VS** | 需新增 cb-011 行气条 + cb-012 棋盘 + cb-013 决胜大开 — 估 3+ sprint | 100% Xingqi GDD 实现 | 高 — VS 决策推迟 2-3 sprint；项目陷入"先实现完整 Xingqi 再 prove" |
| **B · Lite Xingqi VS（选定）** | 0 个新 cb-* story，复用现有 cb-001..010 + 简化抽象 | 75% Xingqi GDD（缺行气条/棋盘） | 中 — VS 不是完整 Xingqi；playtest 可能反馈"没行气感"。但**5-10 分钟无引导可玩性 + 观气/破绽/决胜 mood 可以验证** |
| C · Architecture-review first | 多 0.5d ADR 工作 | — | 低 — 但若 ADR 决策仍指向 Lite，浪费 0.5d；若指向 Full，回到 A |

### 3.3 选 B 的理由

1. **prove-or-pivot 决策点早达成**：Sprint 7 是 Production 阶段首个 prove-or-pivot；尽早进入决策比"完整 Xingqi"更重要
2. **75% 复用率足够**：Xingqi 的"观气取位破绽决胜" mood 不强依赖行气条与棋盘（这两者更接近 Pacing 与 Tactical Depth 维度，可在 Sprint 8+ 增量）
3. **playtest 反馈作为 Sprint 8 输入**：若 playtest 玩家说"没行气感"，那 Sprint 8 必加 cb-011 行气条；若玩家不在意，Sprint 8 可优先开 Feature epic
4. **VS 不是 production-ready**：VS 的目的是验证可玩性方向，不是上线产品；Lite 在 VS 阶段完全够用

---

## 4. VS Scene Skeleton（江南·第一章）

### 4.1 场景结构

```
[StartCave entry 复用 - 1 frame intro]
            ↓ (转场/dialog)
[江南 explore scene · jiangnan_riverside.tscn]
   ├── 主角可在场景内移动（沿用 ADR-0021 ICharacterAnimator 8 方向 sprite）
   ├── scene-markup-tool 标注 1-2 个可交互点（NPC / 物品 / 触发点）
   ├── 至少 1 个 NPC 触发对话（沿用 dialogue-system epic done）
   └── 1 个剧情触发点 → 进入战斗
            ↓ (战斗触发事件)
[战斗 scene · battle_jiangnan_bandit.tscn]
   ├── 1v1：主角 vs 1 个江湖小贼（用 enemy-ai epic done）
   ├── UI 沿用 cu-001..008（combat-ui EPIC done）
   ├── 观气 phase：显示小贼当前内功气机（IntentReveal phase，文案"读出对方刚劲外露"）
   ├── 出招 phase：玩家选 1 招（沿用 cu-004 招式面板）
   ├── 结算 + 破绽 + 决胜（沿用 cb-003/004 + cu-006）
   └── 胜利 / 失败 → outcome scene
            ↓ (战斗结束事件)
[outcome scene · battle_outcome_jiangnan.tscn]
   ├── 心境双轴位移（沿用 mindset-dual-axis epic done）
   ├── 朦胧化战后面板（沿用 blurred-ui EPIC Ready；可能需要 partial 实现）
   └── 1 段 dialog 收尾 → 回到 explore scene
            ↓
[返回 jiangnan_riverside.tscn，状态更新]
```

### 4.2 剧情节拍（最简版）

1. **入场**（30s）：主角在江南河畔，画面引导"前方似有人影"
2. **探索**（1-2 分钟）：玩家可移动 + 与河边 NPC（钓鱼老者）对话获得线索（dialogue-system）
3. **遭遇**（30s）：剧情触发点 = 小贼劫道，进入战斗
4. **战斗**（2-4 分钟）：1 场 1v1，观气 → 出招 → 破绽 → 决胜，无引导自学
5. **战后**（30s）：心境位移（如选择"放过" → 心境向"仁"轴位移；选择"重伤" → 向"狠"轴位移）+ 朦胧化反馈
6. **返回**（30s）：回 explore scene，NPC 反应改变（叙事 hook，为 Sprint 8 准备）

**总时长目标**：5-10 分钟（含玩家自学时间）

### 4.3 复用的已 done epic

| Epic | 用途 | 必要程度 |
|---|---|---|
| character-data | 主角 + 小贼属性 | 必 |
| npc-state | 钓鱼老者 NPC | 必 |
| scene-management | 三场景切换 | 必 |
| time-system | 自然日（可选） | 可省 |
| combat-system | 1v1 1 场战斗 | 必（lite 复用） |
| martial-arts-system | 主角带 2 招 + 小贼带 2 招 | 必（最小 fixture） |
| enemy-ai | 小贼 AI（基础） | 必 |
| dialogue-system | 钓鱼老者 + 战后 dialog | 必 |
| mindset-dual-axis | 战后心境位移 1 次 | 必 |
| main-narrative | 江南章 stub | 可省（VS 单 scene 不需章节系统） |
| item-system | 不用 | 可省 |
| save-system | 不存档 | 可省（VS 内不存档） |
| combat-ui | 全部 cu-001..008 | 必 |
| blurred-ui | 战后面板（Ready，未 done） | 必 — Sprint 7 需要 partial 实现 1 个面板 |

**结论**：13 done epic 中 **9 个必用** + 1 个 Ready epic (`blurred-ui`) 需 partial 实现。

---

## 5. 5-10 分钟无引导可玩性目标

### 5.1 In-Scope

- [x] 玩家不需要 tutorial 就能理解：移动 + 对话 + 战斗触发
- [x] 战斗中：玩家不需要 tutorial 就能理解 观气 → 选招 → 结算 → 破绽累积 → 决胜
- [x] 战斗中：玩家通过 UI 反馈（cu-001..008）能感知刚柔巧克制
- [x] 战后：玩家能理解心境选择 → 朦胧化反馈
- [x] 全程 60 fps（StartCave 已通过该基线）
- [x] 全程 0 S1 / S2 bug

### 5.2 Out of Scope（明确不做）

- ❌ 行气条 / xingqi gauge
- ❌ 棋盘 / 格子移动 / 朝向 / 侧背击 / LOS
- ❌ 多人战斗（VS 用 1v1）
- ❌ 反制按钮的 passive 转化（Sprint 8 ADR 决策后再做）
- ❌ 章节系统 / 多场景 explore
- ❌ 存档 / 读档
- ❌ 物品 / 顿悟（VS 内不触发）
- ❌ 全部 Feature epic（romance / exploration-insight / epiphany-breakthrough / misunderstanding / living-jianghu / party-management）
- ❌ 音频（audio-system Ready，VS 不实现）
- ❌ Cutscene（cutscene-system Ready，VS 不实现）
- ❌ 完整 hud.md 重写（gate C9，Sprint 8）
- ❌ Steam Deck 手柄验证（cu-008-Gamepad-HW-Verify，Sprint 8）

---

## 6. 风险表（更新自 Sprint 7 plan §Risks）

| Risk | Probability | Impact | Mitigation (post-spike) |
|---|---|---|---|
| ~~combat-system 需要 partial 重写~~ | ~~High~~ | ~~High~~ | **Resolved by spike** — 选 Option B Lite，**0 个新 cb-* story**，combat 复用 75% |
| Lite VS playtest 反馈"没行气感" | Medium | Medium | 接受为 Sprint 8 输入；不是 Sprint 7 的 fail 信号 |
| `blurred-ui` epic Ready 但 unstarted，VS 需 partial 实现 | High | Medium | Sprint 7 中 partial 实现 1 个战后面板（不展开 epic 全部 stories） |
| 1v1 战斗复用 enemy-ai 但小贼 fixture 未配 | Medium | Low | Sprint 7 配 1 个最小 fixture（江湖小贼，刚劲外露，2 招） |
| dialogue-system 在战后转场触发链路未验证 | Medium | Medium | Sprint 7 中 dry-run dialogue 转场 event |
| ADR-0021 Animator port 在江南 explore scene 上未实机跑过 | High | Low | S7-Day1-Smoke-Startup 顺带验证；若失败 fallback 到 StartCave 基线 |
| 估算偏差（spike 后 S7-VS-Combat-Loop 可减为 1.5d） | Low | — | 见 §7 |

---

## 7. Sprint 7 Plan Adjustments（建议）

根据 spike 结论调整 Sprint 7 Must-Have 估算：

| Story | 原估 | 新估 | 理由 |
|---|---|---|---|
| S7-VS-Scope-Spike | 1.0d | **0.5d (实际)** | 复用率分析提前完成，省 0.5d |
| S7-VS-Combat-Loop | 2.0d | **1.5d** | 复用率 75% + 0 个新 cb-* story → 主要工作是 fixture 配置 + UI 文案语义调整 + 转场 wiring |
| S7-VS-Outcome-Feedback | 1.0d | **1.25d** | 含 blurred-ui partial 实现（1 个战后面板），略加 |
| 其他 Must | 不变 | 不变 | — |

**新 Total Must-Have**: 7.0d → **6.25d**，节省 0.75d 进 buffer。

---

## 8. Linked Artifacts

- `design/gdd/combat-system.md`（Xingqi 行气战棋 GDD）
- `production/epics/combat-system/EPIC.md`（cb-001..010 done 列表）
- `src/FengZhi.Foundation/Combat/BattleState.cs`（BattlePhase 状态机）
- `docs/architecture/adr-0020-pure-2d-wuxia-rendering-direction.md`
- `docs/architecture/adr-0011-combat-ui-animation.md`（cu-006 集成基线）
- `docs/architecture/adr-0021-character-animation-port.md`（ICharacterAnimator port）
- `production/gate-checks/gate-tech-setup-to-pre-production-2026-06-22.md` §C-VS / §C-PLAYTEST
- `production/sprints/sprint-7.md`（Sprint 7 plan）
- `production/qa/qa-signoff-sprint-6-2026-06-22.md`（Sprint 7 Conditions §1）
- `production/retrospectives/retro-sprint-6-2026-06-22.md`（Sprint 7 Action #1）

---

## 9. Sign-off

| 角色 | 姓名 | 日期 | 签字 |
|---|---|---|---|
| Architect | TBD | 2026-06-22 | __pending__ |
| Designer | TBD | 2026-06-22 | __pending__ |
| Producer | TBD | 2026-06-22 | __pending__ |
| User（最终决策） | — | 2026-06-22 | __pending__（Option B 选定但需正式 sign-off） |

---

## 10. Next Steps（顺序）

1. **User sign-off** on Option B + 本 spike doc → 加 sign-off
2. 更新 `production/sprints/sprint-7.md` §Must Have 估算（按 §7 表）+ 标记 `S7-VS-Scope-Spike` status: done
3. 更新 `production/sprint-status.yaml` `S7-VS-Scope-Spike` status: done + actual_hours
4. （Sprint 7 Day 1 仍待 user 手工执行）`S7-Day1-Smoke-Startup` — 填写 `production/qa/evidence/s7-day1-smoke-2026-06-23.md`
5. 之后开始 `/dev-story S7-VS-Foundation-Scene`（场景骨架，复用 ADR-0021 + scene-markup-tool）
6. `/qa-plan sprint 7` —— 在 S7-VS-Foundation-Scene 之前或同步进行，定义 VS 的 test 期望
