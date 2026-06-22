# S7-VS-Combat-Loop · MVP-A Dev Story

> **Sprint**: 7 (must-have)
> **Estimate**: 1.5d (12h) — MVP-A 紧缩范围
> **Owner**: TBD（gameplay-programmer）
> **Status**: 草稿 — 等待 owner sign-off 后启动
> **来源决策**:
> - `docs/superpowers/specs/2026-06-23-vs-scope-spike.md` Option B · Lite Xingqi VS
> - [Combat-Loop deps survey](30823ca6-1990-4a2c-8c2e-0b8f86824f3e) — 2026-06-22 22:45 暴露集成层 0% 的盲点
> - 2026-06-22 22:55 owner 决策选 MVP-A（紧缩范围）over reestimate / split / spike
>
> **依赖**:
> - `S7-VS-Foundation-Scene` ✅ done (commit 5c07433)
> - `S7-Day1-Smoke-Startup` ✅ done (commit dfc5c2d)
> - `feng-zhi/FengZhi.csproj` 已含 ProjectReference 到 `src/FengZhi.Foundation/`

---

## 1 · 目标（What & Why）

在 `feng-zhi/scenes/vs/battle_jiangnan_bandit.tscn` 中接入真实战斗循环，让玩家能够：

1. 进入战斗 → 看到 1v1 对战面板（主角 vs 江湖小贼）
2. 每回合选 1 招（沿用 `CombatMoveSelectionPanel`）
3. 看到结算（伤害、HP 变化）
4. 直到一方 HP 归零 → `BattleEndEvent` 自动触发 `JiangnanFlowController.GoToOutcome()`
5. 在战后 outcome scene 看到自己的胜/负结果文本

**Why MVP-A**：先证明全循环可玩，prove-or-pivot 信号最早出现；决胜演出 / 反制 / 手柄导航后置到 `cu-visual-evidence` 子任务。

---

## 2 · 范围（In / Out）

### 2.1 包含 (In Scope)

- **新建** `feng-zhi/scripts/vs/JiangnanBanditFixture.cs` — `BattleConfig` + 主角 `CharacterLoadout` (2 招) + 小贼 `CharacterLoadout` (2 招) + `ScriptedAI` (Fierce personality)
- **新建** `feng-zhi/scripts/vs/VsBattleLoopController.cs` — 事件驱动战斗 runtime（包 `BattleInstance.AdvancePhase` + 发布 `BattleEventBus` 事件 + 收集 `CombatUiMoveSelectionIntent` → `BattleAction` 转换 + 等待玩家输入）
- **新建** `feng-zhi/scripts/vs/VsCombatUiBinder.cs` — `CombatUiEventAdapter.GetSnapshot()` → `CombatHudPanel.ApplyIntentSummary` / `ApplyResourceSnapshot` 的 `_Process` 绑定器
- **改造** `feng-zhi/scripts/vs/JiangnanBattleGame.cs` — 移除 Win/Lose 占位按钮；构建 `BattleFacade` + bus + `CombatUiRoot.EnterBattle(bus)`；挂载 `CombatMoveSelectionPanel` + `CombatHudPanel`；订阅 `BattleEndEvent` → 调 `flow.GoToOutcome(result)`
- **改造** `feng-zhi/scenes/vs/battle_jiangnan_bandit.tscn` — 删 `PlaceholderPanel`；占位 ColorRect 背景保留；CanvasLayer 下挂 `CombatUiRoot` 与 `CombatMoveSelectionPanel` 节点（程序化创建即可）
- **扩展** `feng-zhi/scripts/vs/JiangnanFlowController.cs` — `GoToOutcome` 增加 `BattleResult` 参数；新增 `LastBattleResult` 属性
- **改造** `feng-zhi/scripts/vs/JiangnanOutcomeGame.cs` — 读 `LastBattleResult`，区分胜/负展示文案
- **新建** `feng-zhi/scripts/vs/LiteXingqiPhaseBanner.cs` — 简单的 Phase → 中文文案映射横幅（观气 / 出招 / 结算 / 战斗结束）
- **新建** 集成测试 `tests/integration/vs/vs_combat_loop_smoke_test.cs` — 纯 NUnit 跑通 `BattleFacade` + `BattleEventBus` + 简化 view binder 的 minimal 集成

### 2.2 不包含 (Out of Scope — 推到 cu-visual-evidence 或 Sprint 8)

- 决胜演出（`CombatAnimationDirector` 慢动作 / 推镜）→ cu-visual-evidence 子任务（属 cu-006 evidence 范围）
- 反制提示（Counter prompt 完整链路）→ cu-visual-evidence 子任务（属 cu-005 evidence 范围）
- 手柄导航（cu-008 双焦点）→ cu-visual-evidence carryover 范围
- 协同 cue / 多目标切换 / 道具
- 行气条 / 棋盘（与 spike Option B Out-of-Scope 一致）
- 江湖小贼的剧情对话（spike §4.1：仅"对话老者"提示战斗）

---

## 3 · 范围内的 Fixture 数据

> **Subtask 0a 完成（2026-06-22 23:25）**：grep `assets/data/martial-arts/moves.yaml` 确认 registry 中实际有的 basic 招：`luo_han_quan` / `tie_bi_heng_lan` / `lian_xu_xi` / `qing_dian_feng`。选定主角与小贼都用 `luo_han_quan` + `tie_bi_heng_lan`（对称刚体系，避免引入克制矩阵复杂度）。

### 3.1 主角 (Player)

| 字段 | 值 | 备注 |
|---|---|---|
| ActorId | `player_protagonist` | |
| DisplayName | `风止·主角` | 对齐 `assets/data/characters/player.yaml` |
| MaxHp | 100 | 同上（base_hp=100） |
| InitialQi (neixi) | 20 | 同上（base_neixi=20） |
| MaxQi | 40 | VS 内放大 2x，保证 2-3 回合可循环出招 |
| StaggerThreshold | 5 | 同上 |
| 招式 1 | `luo_han_quan`（罗汉拳·轻击·刚 / neixi 2 / mult 0.70 / flaw_expose） | 来自 `moves.yaml` |
| 招式 2 | `tie_bi_heng_lan`（铁臂横拦·重击·刚 / neixi 4 / mult 1.15 / stagger_bonus） | 同上 |
| Personality | N/A（玩家手控） | |

### 3.2 江湖小贼 (Enemy)

| 字段 | 值 | 备注 |
|---|---|---|
| ActorId | `enemy_jiangnan_bandit` | |
| DisplayName | `江湖小贼` | |
| MaxHp | 60 | 主角 60%，确保 2-3 回合可结束 |
| InitialQi (neixi) | 15 | |
| MaxQi | 25 | |
| StaggerThreshold | 5 | |
| 招式 1 | `luo_han_quan` | 与主角共享，对称简化 |
| 招式 2 | `tie_bi_heng_lan` | 同上 |
| Personality | `PersonalityTemplate.Fierce` | 刚劲外露（spike §4.1 文案） |
| AI | `ScriptedAI`（回合 1 = luo_han_quan, 回合 2 = tie_bi_heng_lan, 回合 ≥3 循环；neixi 不足时退化到 basic_attack） | |

### 3.3 BattleConfig

```csharp
new BattleConfig {
    BattleType = "vs_jiangnan_lite",
    PlayerParty = new[] { protagonistConfig },
    EnemyGroup = new[] { banditConfig },
    MaxRounds = 8,  // 上限保护，理论 2-3 回合解决
}
```

### 3.4 招式 id 校验 (Subtask 0a 结论)

- ✅ `luo_han_quan` 存在于 `assets/data/martial-arts/moves.yaml` L35
- ✅ `tie_bi_heng_lan` 存在于 `assets/data/martial-arts/moves.yaml` L25
- ⚠️ **Pre-existing 数据债**（与本 story 无关，但要标注）：`assets/data/characters/player.yaml` 引用的 `duan_shui_han_guang` / `tie_shan_kao` 在 moves.yaml 中**不存在**。本 fixture 绕开此问题，直接用 moves.yaml 中存在的招。Sprint 8 应补一个 player.yaml 数据修复任务。

---

## 4 · 子任务（0.25d / 2h 粒度）

| # | 子任务 | 估时 | 依赖 | 验收 |
|---|---|---|---|---|
| 0a | **Ground move id**：grep `MartialArts` registry，确认 fixture 用的 move id 真实存在；如果不存在，回填到本 spec | 0.25h | — | spec 里的 move id 全部对应到 registry |
| 0b | **Spike-style 小验证**：写 5-line 测试调用 `BattleFacade.InitiateBattle(fixture).AdvancePhase()` 直到 BattleOver，确认 1v1 fixture 数据合理（不卡死、不无限循环、回合数 2-5） | 0.25h | 0a | 测试 PASS，回合数 ≤ 5 |
| 1 | **JiangnanBanditFixture.cs**：实现 §3 的 BattleConfig + 两个 CharacterLoadout + ScriptedAI 配置 | 1h | 0a | 静态 fixture，无运行时副作用；单元测试覆盖 InitiateBattle 不报错 |
| 2 | **VsBattleLoopController.cs**：事件驱动战斗 runtime — `Start()` 初始化 BattleInstance 与 bus；`AdvancePhase` + 在各 phase 发布对应 BattleEventBus 事件；PlayerDecision 等待 `SubmitPlayerIntent(intent)`；BattleOver → 发 `BattleEndEvent` | 3h | 1 | NUnit 测试：跑 ScriptedAI vs ScriptedAI（双方都自动出招）走通完整战斗，bus 收到至少 1 个 RoundStart / IntentRevealed / BattleEnd 事件 |
| 3 | **VsCombatUiBinder.cs**：`_Process` 中 `RefreshIfDirty` adapter snapshot；snapshot → 调 `CombatHudPanel.ApplyIntentSummary/ApplyResourceSnapshot` + `CombatMoveSelectionPanel.ApplySnapshot` | 2h | 2 | binder 单测：mock snapshot 后 panel 字段被正确 apply |
| 4 | **JiangnanBattleGame.cs 改造**：组装 BattleFacade + bus + LoopController + UiRoot + binder + MoveSelectionPanel；订阅 `BattleEndEvent` → `flow.GoToOutcome(result)`；从 MoveSelectionPanel `IntentConfirmed` → `LoopController.SubmitPlayerIntent` | 2h | 2,3 | 在编辑器手动 Play 能跑通至少 1 回合（玩家选招 → 看到结算） |
| 5 | **FlowController + OutcomeGame 改造**：`GoToOutcome(BattleResult)` 增参；OutcomeGame 读 LastBattleResult 区分胜/负标题文案 | 1h | 4 | Outcome scene 在胜利与失败两种情况下文案不同 |
| 6 | **LiteXingqiPhaseBanner.cs + scene 节点**：监听 phase 事件，顶部短暂显示「观气」/「出招」/「结算」/「战斗结束」横幅 1.5s | 1h | 4 | 编辑器 Play 中能看到 phase 横幅 |
| 7 | **集成测试 vs_combat_loop_smoke_test.cs**：NUnit 跑通 fixture + LoopController + 模拟玩家 intent（脚本化），断言 BattleEndEvent.Result ∈ {Victory, Defeat} | 1.5h | 1,2 | 测试 PASS；Foundation 1367 测试无回归 |
| 8 | **实机 smoke + sprint-status 更新 + 回写 progress log** | 1h | 4,5,6,7 | 编辑器 Play 跑通 explore → battle (实战, 非占位) → outcome 全链路 |

**合计**：12h（1.5d，恰好对齐 sprint plan estimate）

**Buffer 策略**：如果 Subtask 2 或 3 实际超过 1.5x，**stop** 并触发 prove-or-pivot：要么砍 LiteXingqiPhaseBanner（Subtask 6），要么把测试 (Subtask 7) 推到 cu-visual-evidence。

---

## 5 · 集成层架构图

```
JiangnanBattleGame._Ready
  │
  ├─ new BattleEventBus() ──────────────┐
  ├─ JiangnanBanditFixture.Create() ───┤  shared bus
  ├─ new BattleFacade().InitiateBattle ─┤
  ├─ new VsBattleLoopController(instance, bus, scriptedEnemyAI)
  │      │
  │      └─ Tick: AdvancePhase + Publish events
  │
  ├─ new CombatUiRoot() ─────────────────┐
  │      └─ EnterBattle(bus) ←───────────┤  订阅 bus
  │           └─ CombatUiEventAdapter ────┤
  │                                      │
  ├─ new VsCombatUiBinder(adapter, panels) ─┐
  │      └─ _Process: snapshot → panels ──┤
  │                                       │
  ├─ new CombatMoveSelectionPanel ────────┤  挂到 UiLayer
  │      └─ IntentConfirmed → loop.SubmitPlayerIntent
  │
  ├─ new CombatHudPanel ──────────────────┤  挂到 UiLayer
  │
  ├─ new LiteXingqiPhaseBanner(bus) ──────┤  顶部 phase 横幅
  │
  └─ bus.Subscribe<BattleEndEvent>(e =>
       flow.GoToOutcome(e.Result))
```

---

## 6 · 验收标准 (Acceptance Criteria)

- [ ] **AC-1 · 战斗循环可玩**：编辑器 Play `battle_jiangnan_bandit.tscn`，玩家能看到主角 + 小贼的 HP/Qi；每回合选 1 招（轻击/重击）；3-5 回合内分出胜负
- [ ] **AC-2 · 转场闭环**：战斗结束自动触发 `JiangnanFlowController.GoToOutcome(result)`，跳转到 outcome scene，标题文案根据胜/负切换
- [ ] **AC-3 · 全链路在 jiangnan_riverside 起点可走通**：从 `jiangnan_riverside.tscn` 起 → 点「前往战斗」→ 完整打一场 → outcome 选择心境 → 回 explore 看到 NPC 反应切换（沿用 Foundation-Scene 已验证的链路）
- [ ] **AC-4 · 文案语义**：phase 横幅按 IntentReveal → 观气，PlayerDecision → 出招，Resolution → 结算，BattleOver → 战斗结束 显示
- [ ] **AC-5 · 测试无回归**：`dotnet test FengZhi.slnx` 仍 PASS；新增的 `vs_combat_loop_smoke_test.cs` 也 PASS
- [ ] **AC-6 · 编辑器干净**：`dotnet build feng-zhi/FengZhi.csproj` 0 warn / 0 err；Godot 编辑器 Output 在 Play 期间无 ERROR（WARNING 可接受）
- [ ] **AC-7 · Out-of-scope 严格不实现**：不接入决胜演出 / 反制完整链路 / 手柄导航；这些显式留在 `cu-visual-evidence` 子任务

---

## 7 · 已知风险与 fallback

| # | 风险 | 概率 | 影响 | Fallback |
|---|---|---|---|---|
| R1 | move id `quick_strike` / `heavy_strike` 不在 MartialArts registry | 中 | 阻塞 fixture | Subtask 0a 验证；不存在则用 registry 中真实存在的 2 招 |
| R2 | VsBattleLoopController 设计比 Subtask 2 估算复杂（intent 协议、AI 桥接） | 高 | 超期 2-4h | **stop & re-scope**：跳过 LoopController，直接用 `BattleFacade.RunFullBattle` 全自动模式 + 在 PlayerDecision 阶段插入 `playerDecisions` 队列；玩家"选招"退化为「2 个按钮硬编码到 fixture」（仍然可玩，spike 评判可达） |
| R3 | `CombatMoveSelectionPanel` 节点挂载需要 Godot scene tree 上下文，纯 NUnit 测不出来 | 中 | 集成测试 (Subtask 7) 失效 | 集成测试退化为仅测 LoopController + bus + binder，不测真实 panel；panel 验证靠 AC-1 手动 |
| R4 | `CombatHudPanel.ApplyIntentSummary` 与 adapter snapshot 形态不匹配 | 中 | UI 显示乱 | Subtask 3 加 contract test：snapshot DTO 字段 vs panel API 字段对齐 |
| R5 | 1.5d 仍乐观，到 Day 4 末未 done | 中 | Sprint 7 滑坡 | Day 4 末 stop & retro：未完成的 panel UI 推到 cu-visual-evidence，能跑通全链路即 done (AC-1/2/3/5/6 必达，AC-4/7 弹性) |

---

## 8 · prove-or-pivot 检查点

| 时机 | 信号 | 行动 |
|---|---|---|
| Subtask 0a 完成 | move id 缺失 + registry 也找不到合适替代 | **pivot**：暂停 Combat-Loop，转去补 `MartialArts.populate-vs-moves` mini-spike (0.5d) |
| Subtask 2 实际 > 5h | LoopController 比预估复杂 2x | **降级 R2 fallback**：用 RunFullBattle 模式，玩家选招走 playerDecisions 队列 |
| Subtask 4 完成时编辑器 Play 不能跑通 1 回合 | 集成层有未知 bug | **stop & investigate**：开 BUG-XXX 单独修；不要继续叠加 5/6/7 |
| Subtask 7 测试无法写成功 | UI 强依赖 Godot scene tree | **降级 R3 fallback**：测试只覆盖 fixture + LoopController + bus |
| Day 4 末未 done | sprint 滑坡 | **retro**：保留已完成的 AC，未完成的推到 cu-visual-evidence 或 Sprint 8 |

---

## 9 · 完成后链接

- `production/sprint-status.yaml` 中 `S7-VS-Combat-Loop` status backlog → done
- `production/sprints/sprint-7.md` Progress Log 新增一行
- `production/session-state/active.md` 新增 Session Extract
- `feng-zhi/scenes/vs/battle_jiangnan_bandit.tscn` placeholder UI 移除
- 提交策略：建议拆 2-3 个 commit（fixture / loop+binder / scene integration），方便回滚

---

## 10 · Owner Sign-off 与开放问题决议

**Owner 决策（2026-06-22 23:20）**：全部按推荐方案。

| # | 问题 | 决议 |
|---|---|---|
| 1 | fixture 持久化形式 | **C# 静态常量**（B1 · 与 spike Out-of-Scope §5 一致，data-driven 留给 Sprint 8+） |
| 2 | phase 横幅文案 | **三字版「观气 / 出招 / 结算 / 战斗结束」**（C1 · 对齐 BattlePhase 三段状态机） |
| 3 | spec 是否立即 commit | **立即 commit**（D1 · prove-or-pivot 留痕，retro 可引用） |
| 4 | R1 (move id 缺失) | A1 · grep registry 找现有招 → 已完成 §3.4 |

scope 锁定为 MVP-A 紧缩范围，正式可执行。
