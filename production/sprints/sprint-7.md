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
| ~~S7-VS-Scope-Spike~~ | **Done 2026-06-22** — VS 范围决策 spike，输出 [`docs/superpowers/specs/2026-06-23-vs-scope-spike.md`](../../docs/superpowers/specs/2026-06-23-vs-scope-spike.md)。**Verdict: Option B · Lite Xingqi VS**（combat-system 复用率 75%，**0 个新 cb-* story**，sprint 节省 0.75d 进 buffer） | Architect / Designer / Producer | ~~1.0d~~ → 0.5d actual | — | spike doc 已落盘；user sign-off pending |
| S7-Day1-Smoke-Startup | qa-tester Day 1 手工实机启动 StartCave / 主战斗 scene 确认（cleanup commit 后基线确认） | QA | 0.25d | — | `production/qa/evidence/s7-day1-smoke-2026-06-23.md` 含截图 + 操作记录；记入 active.md；gate Condition §2 / W2 闭环 |
| S7-VS-Foundation-Scene | VS 主场景骨架（Godot 4.7 + scene-markup-tool addon）：1 个江南探索场景 + 1 个战斗场景 + 转场逻辑 | Gameplay-Programmer / Tools-Programmer | 2.0d | S7-VS-Scope-Spike done | StartCave 类 entry → 江南 explore scene → trigger → 战斗 scene → outcome scene 闭环；scene-markup-tool 标注可交互元素；scenes 在 4.7 编辑器无 import error |
| **S7-Iso-Pivot-Foundation** | **ADR-0022 落地**：isometric diamond 投影 + 4 斜向 Animator (Foundation 层 IsoProjection / Iso4Direction / IIso4CharacterAnimator + Adapter + Fake) + 删除旧 8 向代码 + StartCave.tscn iso 切换。详见 [story file](sprint-7-iso-pivot-foundation.md) | Gameplay-Programmer / Tools-Programmer / Designer | 1.0d | S7-VS-Foundation-Scene done | Foundation 1378 - 11 + 8 = 1375/1375 PASS；StartCave.tscn 实机 WASD 4 斜向；ADR-0021 §扩展点 1 Partially Superseded；GDD/ADR-0010/art-bible 同步；blocks S7-VS-Combat-Loop / cu-visual-evidence / S7-VS-Outcome-Feedback |
| S7-VS-Combat-Loop | **Lite Xingqi**：1 场 1v1 战斗，复用现有 BattlePhase 包装"观气→出招→破绽→决胜"语言，**0 个新 cb-* story**。主要工作 = fixture 配置 + UI 文案语义调整 + 转场 wiring | Gameplay-Programmer | ~~2.0d~~ → 1.5d | S7-VS-Foundation-Scene done **+ S7-Iso-Pivot-Foundation done** | 1 场 1v1 战斗在 VS 战斗 scene 中可完整进入 → 观气 → 出招 → 破绽 → 决胜/普通结算 → 退出；Foundation 1367 测试无回归；spike §3 Out-of-Scope 项严格不实现 |
| S7-VS-Outcome-Feedback | 战后心境双轴位移 + 朦胧化战后面板（mindset-dual-axis + blurred-ui partial 实现 1 个面板）in VS | Gameplay-Programmer / UI-Programmer | ~~1.0d~~ → 1.25d | S7-VS-Combat-Loop done | 战斗结束触发 mindset 位移事件 → 朦胧化战后面板渲染（partial 实现）→ 返回 explore scene；事件链路在 active.md 留 trace |
| ~~cu-visual-evidence~~ | **2026-06-24 拆为 6 子 story** — 原 6h 估时反映 owner 录屏环节, 实际还需 UI 集成 4 子任务 (12h)。拆分见下 6 行 + [harness spec](../../docs/superpowers/specs/2026-06-24-cu-visual-evidence-harness.md)。Q1-Q6 全选推荐 a。 | — | ~~0.75d~~ → split | — | 被 6 子 story 替代 |
| cu-004-vs-integration | 招式选择面板 + 预览卡 集成 — `CombatMoveSelectionPanel.tscn` + `MoveRow.tscn` + `MovePreviewCard.tscn` + .cs (订阅 `CombatUiEventAdapter.OnMoveSelectionPanelUpdated`) + `JiangnanBandit1v1Fixture.CreateDemoConfig_Cu004Showcase()`；替换 LightButton/HeavyButton 占位 | Gameplay-Programmer / UI-Programmer | 0.5d | S7-VS-Combat-Loop done | cu-004.md §验收标准 8 条全 ✅；自动测试 0 回归；harness spec subtask A |
| cu-005-vs-integration | 反制 + 决胜提示集成 — `CounterTag.tscn` + `DecisiveStrikeRow.tscn`；panel 接 `CombatUiMoveSelection.CounterDecisivePrompt`；`CreateDemoConfig_Cu005Showcase()` (敌方公开柔意图 + 内息 ≥3/<3 切换 + 破绽 ≥5) | UI-Programmer | 0.25d | cu-004-vs-integration done | cu-005.md §验收标准 7 条全 ✅；harness spec subtask B |
| cu-006-vs-integration | 决胜一击演出集成 — `DecisiveStrikeOverlay.tscn` + `DecisiveStrikeGodotAdapter.cs` (bind `TimeScaleEngineBridge` + `CameraRequestBusBridge` + `CombatCinematicLockInputFilter`)；`CreateDemoConfig_Cu006Showcase()` (敌方破绽 = 5) | UI-Programmer / Gameplay-Programmer | 0.4d | cu-004 + cu-005 vs-integration done | cu-006.md §验收标准全 ✅；演出 7-phase 完整 + 输入屏蔽 + TimeScale 恢复；Q3=a pause 验证由 Foundation 自动测试覆盖不重复；harness spec subtask C |
| cu-008-vs-integration | dual-focus + 手柄导航集成 — panel 加 `focus_neighbor` 循环 + `grab_focus` FocusManager 保护 + 鼠标 hover 独立视觉态；InputMap `ui_focus_next/prev/accept` 绑 WASD / 方向键 (Q4=a 键盘 D-pad 模拟 path)；`CreateDemoConfig_Cu008Showcase()` | UI-Programmer | 0.25d | cu-004-vs-integration done | cu-008.md §验收标准 8 条全 ✅；harness spec subtask D；真手柄插拔验证拆到 cu-008-Gamepad-HW-Verify nice |
| cu-visual-evidence-harness | Recording harness — `JiangnanBattleGame.cs` 加 `[Export] DemoSeed` + `feng-zhi/scenes/vs/demo/{battle_demo_cu004..008}.tscn` 4 个 (Q2=a 独立 .tscn) + `AssertDemoState()` fail-fast | Gameplay-Programmer | 0.1d | cu-004 + cu-005 + cu-006 + cu-008 vs-integration done | 4 个 demo scene 一条 godot 命令启动；state assertion 通过；harness spec subtask E |
| cu-visual-evidence-recording | Owner 实机录 4 段视觉证据 — 按 harness spec §F 的 per-cu checklist 串；4 个 .mp4 落 `production/qa/evidence/media/` + 4 个 cu-00X-evidence.md 头部「待录」→「录制完成」 | **Owner** | 0.2d | cu-visual-evidence-harness done | 4 个 ≥720p .mp4 归档；evidence.md 段更新；gate prove-or-pivot 凭证完整；harness spec subtask F |

**Total Must-Have**: ~~7.0d~~ → ~~6.25d~~ → ~~7.25d~~ → **9.25d**（2026-06-24 cu-visual-evidence 拆 6 子 story，scope +1.75d 反映 MVP-A 显式延期到 cu-visual-evidence 的 4 widget 集成；总估时 90h，按 38h ahead-of-schedule buffer 仍塞得下 — Q5=a Sprint 7 内全做）

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|---|---|---|---|---|---|
| S7-VS-Playtest-Session | 1 次新玩家试玩 session（≥30 分钟，含 5-10 分钟无引导 + 反馈访谈），产出 `production/qa/playtest-vs-2026-07-XX.md` | QA / Designer / Producer | 0.5d | cu-visual-evidence done | 1 名外部 / 半外部玩家完成试玩；记录"是否在 3-10 分钟内理解循环"; gate §C-PLAYTEST 闭环 |

**Total Should-Have**: 0.5d

### Emergent / Out-of-Plan (2026-06-24 回溯落档)

> 本 sprint 启动后涌现的工作；不在初版 plan 内，但已实际交付。落 story 用于 retro 追踪 + 实际产出量化。详细 deliverable / verdict 见 `production/sprint-status.yaml`。

| ID | Task | Owner | Est. Days | Status | Acceptance Criteria |
|---|---|---|---|---|---|
| S7-Dialogue-Chapter-00-Wiring | Foundation DialogueRuntime → Godot 集成 + chapter_00 4 段对话内容 (memory_marker / rest_spot / storage_shelf / wine_pickup) + Provider scene-agnostic 泛化 (CaveConditionValueProvider → SceneConditionValueProvider) + DialoguePanel CanvasLayer 40 (ADR-0002 合规) + 对话期间输入屏蔽 | user | 0.75d | done | 4 段 dialogue YAML 端到端跑通：打字机渲染 → 选项 → mindset_shift 事件 → quest_flag；smoke test 场景自动触发；ADR-0002 双焦合规 |
| S7-Dialogue-Compiler-Tool | `tools/dialogue_compiler.py` (.dlg 纯文本 → DialogueSchema-compliant YAML 转译器, 397 行) + `tools/README-dialogue-compiler.md` (114 行用户文档) | user (compiler) + agent (README) | 0.4d | done | 作者无需手撸 YAML, 用 .dlg 写对话即可编译；与 chapter_00 现有 4 个手写 YAML 兼容；支持 narration / inner_monologue / choice / event / condition / fallback |

**Total Emergent**: 1.15d (5.0h + 2.5h actual)

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
| ~~combat-system epic（done）是基于 Burst+Read，行气战棋需要 partial 重写~~ | ~~High~~ | ~~High~~ | **Resolved by spike (2026-06-22)** — code 是 mechanism-neutral 回合制框架，复用率 75%；选 Option B Lite Xingqi VS，**0 个新 cb-* story**。详见 [vs-scope-spike doc](../../docs/superpowers/specs/2026-06-23-vs-scope-spike.md) §2-3 |
| Lite VS playtest 反馈"没行气感"（行气条 / 棋盘 OOS） | Medium | Medium | 接受为 Sprint 8 输入；不是 Sprint 7 fail 信号；若必加，则 Sprint 8 拆 cb-011 行气条 + cb-012 棋盘 |
| `blurred-ui` epic Ready 但 unstarted，VS 需 partial 实现 1 个战后面板 | High | Medium | S7-VS-Outcome-Feedback +0.25d；不展开 blurred-ui epic 全部 stories |
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

## Progress Log

| Date | Story | Status | Actual | Verdict / Note |
|---|---|---|---|---|
| 2026-06-22 | S7-VS-Scope-Spike | done | 4.0h (vs 8h) | Option B · Lite Xingqi VS（combat 复用率 75%, 0 new cb-*）。`docs/superpowers/specs/2026-06-23-vs-scope-spike.md` |
| 2026-06-22 | S7-VS-Foundation-Scene | done | 2.5h (vs 16h) | Code-first placeholder 骨架 (3 scene + 4 script + autoload). headless 通过, owner 实机 PASS. commit 5c07433. **重大估时偏差：placeholder story 估时下次需下调到 0.5d** |
| 2026-06-22 | S7-Day1-Smoke-Startup | done | 0.5h (vs 2h) | Owner 实机 Step 1-5 全 PASS；gate W2 闭环。`production/qa/evidence/s7-day1-smoke-2026-06-23.md` |
| 2026-06-22 | S7-Animator-Directional-Port | done | 3.0h (vs 3h) | ADR-0021 §扩展点 1 八方向角色动画端口落地 + CavePlayer 迁移。Foundation 1378/1378。commit 9e3e2de |
| 2026-06-22 | ADR-0022 iso pivot (docs only) | done | — | 全项目 pivot 至 isometric diamond + 4 斜向；新增 S7-Iso-Pivot-Foundation story (ready-for-dev, 1.0d)。commit 40d81c4 |
| 2026-06-23 | S7-VS-Combat-Loop | done | 4.0h (vs 12h) | **MVP-A 紧缩范围 PASS** — 1v1 完整 explore→combat→outcome 循环 owner 实机 sign-off 09:06。Subtask 0-8 全闭环：fixture (luo_han_quan/tie_bi_heng_lan) + VsBattleLoopController + Godot scene 集成 + 双层测试 + outcome 文案切换。复用率 ~50% (vs spike 假设 75%)。CombatMoveSelectionPanel/HudPanel 集成、PhaseBanner 推 cu-visual-evidence 阶段。**重大估时偏差 -67%**：dependency-survey 暴露的 R2「编排层 0%」实际 1h 化解。commits 7aa8786/6f768a8/204ec03/bc84362。**Iso follow-up**：battle scene 当前是 ColorRect+Button 占位，S7-Iso-Pivot-Foundation 落地后于 cu-visual-evidence 阶段顺手 iso 蒙皮 (< 1h) |
| 2026-06-23 | S7-Iso-Pivot-Foundation | done (代码层) | 2.5h (vs 8h) | **PASS** — ADR-0022 iso pivot stage 2 代码落地：Foundation 新增 IsoProjection + Iso4* + 24 单测；删除 EightDirection/IDirectional/EightDirectionAdapter + 11 8dir 测试；CavePlayer 迁移到 IIso4 + WASD cart 对角映射；main_character.tres 重建 + 资产 rename main_character_iso4/ + 归档 _archive_8dir_2026-06-22/；ADR §2 sector 区间 erratum（包含性反转 + 中心移到 cart 卡式轴）。Foundation 1399/1399 PASS；feng-zhi build 0 err；headless StartCave 加载 clean。AC8 (StartCave TileMapLayer iso) 标 **N/A** — 项目无 TileMapLayer。**Owner 实机走查作为 DoD pending**。**估时偏差 -69%**。commits 7da0ba1/0836963/5a5ec05/3ae8116/5e0bcdf |
| 2026-06-23 | S7-VS-Outcome-Feedback | done (代码层) | 4.5h (vs 10h) | **PASS** — VS outcome scene 接入真实 Mindset 服务 + blurred-ui partial 1 面板：Foundation 新增 MindsetZoneLiteraryNames + MindsetOutcomeShifts (spec §3 唯一权威映射) + 4 integration tests (Spare/Defeat/ZoneCross/SnapshotContract, 4/4 PASS)；JiangnanFlowController 持 MindsetService + EventBus autoload；OutcomeGame 渲染 zone 名 + 前后独白对比 + 道义档位 + warmth → SelfModulate 染色；BlurredPanel StyleBoxFlat 半透明 + 6px 圆角。Foundation 1399 → 1403 (+4)；feng-zhi build 0 err；Godot headless 0 err。**Owner 实机走查 + 录屏 (Spare/Defeat/Zone-or-Tier 切换) DoD pending**。**估时偏差 -55%**。commits 79b9b0d/4464998/7e0200c/9be8a8b/5002aa3/86bb24a/f527e29 |
| 2026-06-24 | S7-Dialogue-Chapter-00-Wiring | done (emergent) | 5.0h (vs 6h est) | **PASS — out-of-original-plan** Foundation DialogueRuntime → Godot 集成层完成：DialogueManager + DialoguePanel 新建 + SceneConditionValueProvider (从 CaveConditionValueProvider 重命名 + 泛化) + DialoguePanel CanvasLayer 40 修复 (ADR-0002 合规) + 4 段 chapter_00 dialogue YAML (memory_marker / rest_spot / storage_shelf / wine_pickup) + CavePlayer 对话期间输入屏蔽 + DialogueSmokeTestScene 自动触发场景。端到端跑通：打字机渲染 → 选项 → mindset_shift 事件 → quest_flag。FengZhi.csproj 加 CopyLocalLockFileAssemblies 确保 YamlDotNet.dll 运行时可用。commits 255a510 / 6d53546 / 2a4618b / 1129350 |
| 2026-06-24 | S7-Dialogue-Compiler-Tool | done (emergent) | 2.5h (vs 3h est) | **PASS — out-of-original-plan** 作者层对话内容生产工具：`tools/dialogue_compiler.py` (397 行 .dlg 纯文本 → DialogueSchema-compliant YAML 转译器，支持 narration/inner_monologue/choice/event/condition/fallback/parameterized event) + `tools/README-dialogue-compiler.md` (114 行用户文档：快速开始/语法/示例)。与 chapter_00 现有 4 个手写 YAML 兼容。estimate 偏差 -17%。commits 3b1dfd2 / 48a1ce7 |

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
