# S7-VS-Outcome-Feedback Dev Story

> **Sprint**: 7 (must-have)
> **Estimate**: 1.25d (10h) — 含 blurred-ui partial 1 个面板
> **Owner**: TBD（gameplay-programmer + ui-programmer）
> **Status**: 草稿 — 等待 owner sign-off 后启动
> **来源决策**:
> - `docs/superpowers/specs/2026-06-23-vs-scope-spike.md` §7 re-estimate 表
> - `production/sprints/sprint-7.md` Must-Have 表
> - 2026-06-23 09:06 owner 选定方案：Morality ±5 + Resolve -2 联动；blurred-ui 用 `modulate` 色调 + 半透明 + 文学化 Label（不上 ShaderMaterial）
>
> **依赖**:
> - `S7-VS-Combat-Loop` ✅ done (commit 55749fe)
> - `S7-Iso-Pivot-Foundation` ✅ done 代码层 (commit ac6b346) — outcome 视觉 follow-up 不在本 story 范围
> - Mindset epic ✅ Complete (7/7 stories) — runtime `MindsetService` + `MindsetPresentationService` + `MindsetShiftedEvent` 已 ready
> - `feng-zhi/FengZhi.csproj` 已含 ProjectReference 到 `src/FengZhi.Foundation/`

---

## 1 · 目标（What & Why）

让玩家在 `feng-zhi/scenes/vs/battle_outcome_jiangnan.tscn` 战后选择「放过」/「重伤」时：

1. 触发**真实**的 `MindsetService.ApplyShift` 调用（不再是 placeholder 文本）
2. 心境位移广播 `MindsetShiftedEvent` 经 `EventBus`
3. 朦胧化战后面板呈现：
   - 半透明背景 + Mindset zone 文学化区域名（如「中庸 · 心如平湖」）
   - `MindsetPresentationService.GetResolveDescription` / `GetWorldlyDescription` 输出的文学独白
   - 位移前 → 位移后的内心独白对比（"旧恨如铁……" → "心头霜雪渐化……"）
   - 色调用 `MindsetVisualParams.Warmth` 调 `modulate`（暖色偏「仁」、冷色偏「狠」）
4. 「继续」按钮回 explore scene（已有，保留）

**Why this story**：
- Sprint 7 VS 闭环的最后一块 must-have 关键拼图（剩 cu-visual-evidence 是 carryover 录制）
- Mindset epic 已 Complete 但 VS 链路从未真正接入过；这是首次"真接线"，给 Sprint 8 后续 epic 集成铺路
- blurred-ui partial 1 个面板验证「数据→文学」翻译链路在 Godot Control 树里能跑通
- 给 cu-visual-evidence 录制提供完整的"战后心境位移"画面

---

## 2 · 范围（In / Out）

### 2.1 包含 (In Scope)

- **改造** `feng-zhi/scripts/vs/JiangnanFlowController.cs`：
  - 持有 `MindsetService` + `IEventBus` 实例（由 Autoload 在 `_Ready` 创建，全 VS 共享单例）
  - 暴露 `IEventBus` getter（供 OutcomeGame 订阅 `MindsetShiftedEvent`）
  - `RecordMindsetChoice(MindsetChoice)` 改为：
    - `Spare` → `ApplyShifts([Morality +5, Resolve +2])`（释怀 / 放下）
    - `Defeat` → `ApplyShifts([Morality -5, Resolve -2])`（执念 / 加深）
    - 同时记录 pre-shift state 快照（给 UI 展示用）
  - 新增 `MindsetSnapshot LastMindsetShiftSnapshot { get; }` — 含 oldState + newState + 应用 deltas
- **改造** `feng-zhi/scripts/vs/JiangnanOutcomeGame.cs`：
  - 不再调 `RecordMindsetChoice` 单参数版；改调 `flow.ApplyOutcomeChoice(choice)` 触发真实位移
  - 监听 `EventBus.Subscribe<MindsetShiftedEvent>` 收集本帧位移（用于 UI 提示）
  - 根据 `LastMindsetShiftSnapshot` 在 BlurredPanel 渲染：
    - 当前 zone 名（中文文学化映射；本 story 内建 `MindsetZoneLiteraryNames` 静态表）
    - `Old GetResolveDescription` → arrow → `New GetResolveDescription`
    - 同上 Worldly
    - Morality tier 名（不展示数值；用 `MindsetPresentationService` 已有的"恶/中庸/善"映射）
  - 朦胧化视觉：BlurredPanel `modulate = Color(...)` 基于 `GetVisualParams(newZone).Warmth`
- **新建** `feng-zhi/scripts/vs/MindsetZoneLiteraryNames.cs`：
  - 静态类，9 区域 + 5 善恶 tier 中文映射
  - 单一职责，零依赖（纯 lookup），便于 Sprint 8 blurred-ui epic 收编
- **改造** `feng-zhi/scenes/vs/battle_outcome_jiangnan.tscn`：
  - BlurredPanel 内容重排：
    - `ZoneNameLabel`（顶部，大字号）
    - `ResolveBeforeLabel` → `ResolveAfterLabel`（带 → 分隔）
    - `WorldlyBeforeLabel` → `WorldlyAfterLabel`
    - `MoralityTierLabel`（底部小字）
    - `ContinueButton`（保留）
  - BlurredPanel 增加 `theme_override_styles/panel` StyleBoxFlat 半透明背景
  - 删除 OverlayLabel 占位
- **新建** `tests/integration/vs/JiangnanFlowMindsetIntegrationTests.cs`：
  - Foundation 层纯 XUnit（不依赖 Godot），构造 `MindsetService` + `EventBus` 模拟 OutcomeFlow 调用链路
  - 4 tests：
    1. `Spare` 调用 → Morality +5 + Resolve +2 写入 state + 发 `MindsetShiftedEvent x2`
    2. `Defeat` 调用 → Morality -5 + Resolve -2 + 发 `MindsetShiftedEvent x2`
    3. 跨越 zone 边界时发 `MindsetZoneChangedEvent`（fixture 初始 resolve=-13 + Spare +2 → 跨过 -15 边界）
    4. Snapshot 含 old/new state + deltas（UI 可用契约）

### 2.2 不包含 (Out of Scope — 推到 Sprint 8 或独立 story)

- **shader-based 高斯模糊**：Sprint 7 用 `modulate` 色调 + 半透明 Panel 已满足 blurred-ui partial；ShaderMaterial 推到 blurred-ui epic story
- **场景级色调偏移**（GDD blurred-ui Rule 3）：本 story 只在 outcome panel 内做 modulate；explore 场景的 mindset zone 染色推到 sm-004（已 done 的 color-tone-calculator）+ 后续集成 story
- **存档持久化**（ms-007 已 done 但本 story 不接 save runtime）：本 sprint 是单会话内存 only
- **NPC 反应联动**（深渊轴对 NPC attitude 影响）：超出 VS 范围
- **回到 explore 后的 zone 渐变**：blurred-ui Rule 4 的 `pending_reveals` 队列推到 Sprint 8
- **战斗胜/负叠加位移**（GDD §来源 B）：本 story 只处理玩家在 outcome scene 的二次选择位移，不在 BattleEndEvent 时自动位移（避免与玩家的"放过/重伤"叠加）
- **音效 / 动画**：留给 audio-system / polish epic

---

## 3 · Mindset 位移数值

按 owner 2026-06-23 决策（GDD §"关键位移" ±5-10 范围内、+2 联动属"日常位移"上限）：

| 玩家选择 | 心境位移 | 哲学释义 |
|---|---|---|
| **放过 (Spare)** | Morality **+5** | "以仁慈处置"=主线节点的关键善行 |
| | Resolve **+2** | "放下"=对师门血仇的执念松动一寸 |
| **重伤 (Defeat)** | Morality **-5** | "重伤平民"=触碰恶轴关键阈值 |
| | Resolve **-2** | "杀气未消"=执念加深 |

**为何 Resolve 是 ±2 而非 ±5**：Spare/Defeat 是"对小贼一次性处置"，与"师门血仇主线对决"不同重量级。±2 属"日常位移"上限，符合 GDD §来源 A 表。

**Worldly 不动**：江南章节属"入世练手"前段，单次小贼遭遇不足以影响"入世↔出世"哲学倾向。

---

## 4 · 朦胧化视觉规约

### 4.1 BlurredPanel `modulate` 色调表

基于 `MindsetPresentationService.GetVisualParams(newZone).Warmth`（已有 API）：

| Warmth | 来源 zone | Color (modulate) | 文学映射 |
|---|---|---|---|
| -0.35 | GuJianRuShi | `Color(0.7, 0.8, 1.0, 1)` 冷青 | 「孤剑入世」|
| -0.3 | ZhiNianWeiDing / FengZhiChenYan | `Color(0.8, 0.85, 1.0, 1)` 微冷 | 「执念未定 / 风止沉烟」|
| 0.0 | RuShiWeiDing / ZhongYong / ChuShiWeiDing | `Color(1.0, 1.0, 1.0, 1)` 中性 | 「未定 / 中庸」|
| 0.3 | ShiHuaiWeiDing | `Color(1.0, 0.92, 0.82, 1)` 微暖 | 「释怀未定」|
| 0.35 | BaiYiRuShi / DaYinYuShi | `Color(1.0, 0.85, 0.7, 1)` 暖橙 | 「白衣入世 / 大隐于市」|

**实现**：把 `Warmth ∈ [-0.35, 0.35]` 线性 lerp 到 RGB 三通道偏移。本 story 内做 `WarmthToModulate(float)` 工具函数。

### 4.2 BlurredPanel StyleBoxFlat

```
bg_color = Color(0.08, 0.08, 0.12, 0.85)
border_color = Color(0.4, 0.4, 0.5, 0.5)
border_width_* = 1
corner_radius_* = 6
```

半透明深色 + 细灰边 = "朦胧"基线视觉；色调由 modulate 调节。

### 4.3 文学独白排版

```
[ZoneNameLabel]      中庸 · 心如平湖（24pt，Center）
                     ────────────
[ResolveBeforeLabel] 旧恨如铁，尚未松手。
[ResolveArrowLabel]            ▼
[ResolveAfterLabel]  心头霜雪渐化，往事已有归处。

[WorldlyBeforeLabel] 去留之间，尚未择路。
[WorldlyArrowLabel]            ▼
[WorldlyAfterLabel]  去留之间，尚未择路。  （未变则保持原文，前后同字）

[MoralityTierLabel]  道义：常人 → 善
[ContinueButton]     继续 — 江南雨歇，路还要走
```

字号建议：Zone 24pt / Before-After 16pt / Tier 12pt。`autowrap_mode = 3` 全开。

---

## 5 · Acceptance Criteria

| # | AC | Status |
|---|---|---|
| AC1 | dev-story spec 文件落档（即本文件） | [ ] |
| AC2 | `JiangnanFlowController` 内集成 `MindsetService` + `EventBus` autoload 单例；`ApplyOutcomeChoice` 调用 `ApplyShifts` 真实位移 | [ ] |
| AC3 | `JiangnanOutcomeGame` 监听 `MindsetShiftedEvent` + 读 `LastMindsetShiftSnapshot`；BlurredPanel 渲染朦胧化文学化展示（zone 名 + 前后独白对比 + tier） | [ ] |
| AC4 | BlurredPanel 视觉升级：StyleBoxFlat 半透明 + `modulate` 色调由 zone Warmth 驱动；删除 "[朦胧化面板 占位]" Label | [ ] |
| AC5 | `MindsetZoneLiteraryNames` 静态映射：9 zone + 5 morality tier 中文文学名 | [ ] |
| AC6 | `JiangnanFlowMindsetIntegrationTests` 4 tests pass（Spare / Defeat / Zone 跨越 / Snapshot 契约） | [ ] |
| AC7 | feng-zhi 项目 `dotnet build` 无新增 warning；Godot `--headless --import` 0 error；StartCave → explore → battle → outcome → continue 实机闭环可走通 | [ ] |
| AC8 | `production/session-state/active.md` Session Extract + `sprint-7.md` Progress Log + `sprint-status.yaml` status → done + dev-story spec Final Status 节 | [ ] |

---

## 6 · Subtask 拆分 + 估时

按 AC 顺序 + 依赖排：

| # | Subtask | AC | 估时 | 依赖 |
|---|---|---|---|---|
| 1 | 本 spec 草稿（即本文件） | AC1 | 0.5h | — |
| 2 | `MindsetZoneLiteraryNames` 静态映射 + 单测（可选）| AC5 | 0.5h | 1 |
| 3 | `JiangnanFlowController` 集成 MindsetService + EventBus + Snapshot | AC2 | 2.0h | 2 |
| 4 | `JiangnanOutcomeGame` 接入 + 文学化渲染 | AC3 | 2.0h | 3 |
| 5 | BlurredPanel 视觉升级（modulate + StyleBoxFlat） | AC4 | 2.0h | 4 |
| 6 | `JiangnanFlowMindsetIntegrationTests` 4 tests | AC6 | 1.5h | 3 |
| 7 | 实机 smoke + Godot headless import + build 检查 | AC7 | 1.0h | 5,6 |
| 8 | Closeout 文档（active.md / sprint-7.md / sprint-status / spec Final Status） | AC8 | 0.5h | 7 |

**Total**: 10.0h（与 estimate 1.25d / 10h cap 持平）。

---

## 7 · Definition of Done

- [ ] AC1-8 全部 ✅
- [ ] `dotnet test --filter "FullyQualifiedName~JiangnanFlowMindset"` 4/4 PASS
- [ ] Foundation 1399 + 4 = 1403/1403 PASS 无回归
- [ ] feng-zhi build 0 warn 0 err
- [ ] Godot --headless --import 0 err
- [ ] 实机走查：从 StartCave 进入 → 江南 explore → 触发战斗 → 战斗结束 → outcome scene 选「放过」或「重伤」 → 看到朦胧化面板（zone 名 + 文学化前后描述 + tier）→ continue 回 explore；至少录制一份 30s 录屏 + 3 张关键截图（放过/重伤/zone 切换）放 `production/qa/evidence/s7-vs-outcome-feedback/`
- [ ] 留 trace：`production/session-state/active.md` 含 Outcome-Feedback 完成 extract（commits + AC 进度 + 决策回顾 + 估时偏差）

---

## 8 · Linked Artifacts

- `design/gdd/mindset-dual-axis.md` §来源 A / §来源 B（位移规则）
- `design/gdd/blurred-ui.md` §Rule 1-4（朦胧化分治）
- `production/epics/mindset-dual-axis/EPIC.md` (Complete 7/7)
- `production/epics/blurred-ui/EPIC.md` (Ready, stories 未拆)
- `src/FengZhi.Foundation/Mindset/MindsetService.cs`
- `src/FengZhi.Foundation/Mindset/MindsetPresentationService.cs`
- `src/FengZhi.Foundation/Mindset/MindsetEvents.cs`
- `src/FengZhi.Foundation/Events/EventBus.cs`
- `docs/superpowers/specs/2026-06-23-vs-scope-spike.md` §7
- `production/sprints/sprint-7.md` Must-Have 表

---

## 9 · 风险 / 验证项

| Risk | Prob | Impact | Mitigation |
|---|---|---|---|
| Godot Autoload C# 静态对象生命周期 vs scene transition（`MindsetService` 单例跨 scene 状态丢失）| Low | High | `JiangnanFlowController` 已 Autoload，service 实例存这里。验证：AC7 实机走 explore→battle→outcome 多次循环看 state 是否累积 |
| `modulate` 色调与现有 ColorRect 背景叠加视觉脏 | Medium | Low | AC4 实施时单独测；必要时把 ColorRect Background 也按 modulate 协调 |
| `MindsetShiftedEvent` 在 single-frame 内多次发布（Morality + Resolve）UI 收到顺序问题 | Low | Low | UI 只读 Snapshot（已计算好 final state），不依赖事件序 |
| 单测覆盖率超过 lite-VS 期望（spike §7 不强求） | Low | Low | 4 tests 是最小集；可在 AC6 实施时砍到 2-3 个 |

---

## 10 · Final Status — (待 owner sign-off 后完成)

填写：commit 列表 / 估时偏差 / 实际 AC 完成 / 暴露问题 / 解锁项 / 下一步。
