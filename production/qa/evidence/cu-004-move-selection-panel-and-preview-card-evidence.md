# cu-004 招式选择面板与预览卡 — QA Evidence

> **🔴 2026-06-24 状态更新（visual evidence 待重录）**
> - 本 evidence 文档**视觉录屏部分待 VS 江南战斗场景重录**
> - 现有内容（下方 §手动结果 等）来自**旧 prototype `sprint5-combat-ui-harness`**，VS 切换后判为 stale target
> - VS 新场景 `battle_jiangnan_bandit.tscn`（Sprint 7 MVP-A）当前用 2 Button 占位代替 `CombatMoveSelectionPanel`
> - 集成 + 录制拆为 6 子 story，scope 拆解见 [`harness spec`](../../docs/superpowers/specs/2026-06-24-cu-visual-evidence-harness.md)
> - 依赖链：`cu-004-vs-integration` done → `cu-visual-evidence-harness` done → `cu-visual-evidence-recording` done → 回填本文件 §视觉证据 段
> - Foundation 契约层自动测试仍 ✅（`CombatUiMoveSelectionPanelTest` 等）

> 日期：2026-06-17
> Story：`production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md`
> TR-ID：`TR-combat-ui-004`
> 类型：UI
> 证据状态：**视觉部分待重录（VS 集成后）**；Foundation 自动契约层 ✅；旧 `fengzhi-vertical-slice` / `sprint5-combat-ui-harness` 结果已判 stale
> QA 签字：User manual QA（旧 prototype harness）

## 验证目标

- 验证招式选择面板展示当前 GDD / registry 允许的行动。
- 验证 `普通攻击` / `BasicAttack` / `basic_attack` 不再出现在当前 Sprint 5 Combat UI 行动合同中。
- 验证不可用原因、hover / focus 预览卡和基础行动可见。

## 测试目标

| 项目 | 记录 |
|------|------|
| Target | `prototypes/sprint5-combat-ui-harness` |
| Engine | Godot 4.7-stable Mono |
| Platform | macOS local dev build |
| Input | Mouse + keyboard |
| Fixture | `DefaultAvailable`, `InsufficientNeixi`, `NoCombatItem` |

## 手动结果

用户在新 harness 中手动复测 `cu-004`，结果：**PASS**。

确认点：

- 6 个装备招式可见。
- `调息` 可见。
- `使用道具` 可见。
- `普通攻击` 不再出现。
- 不可用原因可见。
- hover / focus 预览卡可见。
- 测试目标不再依赖过期 `fengzhi-vertical-slice`。

## 自动测试覆盖

- `CombatUiMoveSelectionPanelTest`
- `CombatUiDualFocusNavigationTest`
- `CombatUiCounterDecisivePromptTest`

最近验证：

- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "CombatUiMoveSelectionPanelTest|CombatUiDualFocusNavigationTest|CombatUiCounterDecisivePromptTest"` passed `49 / 49`。
- `dotnet test FengZhi.slnx` passed `1344 / 1344`。

## 结论

当前结论：**PASS VIA HARNESS**。

`cu-004` 不再阻塞 Sprint 5 Must Have close-out。旧 `BUG-0002` 可关闭为 stale target + fixed + verified。

---

## Visual Captured (Sprint 7 carryover — Pending)

> **2026-06-22 改派**：原计划 Sprint 6 在 `prototypes/sprint5-combat-ui-harness` 上录屏。harness 已于 commit `c8d8c14` 主动删除（与 burst-read spike / 134 个旧 protagonist 资产同清理）。本段 visual evidence 推迟到 Sprint 7 的 ADR-0020 全循环 VS 重建上录制，确保 evidence 代表当前架构（纯 2D 武侠 + 行气战棋）。原 Foundation Captured 段保持只读。

> 本段为 Sprint 6 `cu-visual-evidence` story 的模板占位；待 designer 录屏 / 截图后填入并切换为 Visual Captured。

### Metadata

| 项 | 值 |
|---|---|
| Capture date | TBD |
| Captured by | TBD（designer 名 / 工号） |
| Engine | Godot 4.7-stable Mono |
| Build | TBD（local dev / commit hash） |
| Platform | TBD（macOS / Windows / Linux） |
| Recording tool | TBD（macOS Screen Recording / OBS / etc.） |
| Harness | `prototypes/sprint5-combat-ui-harness` |
| Fixture used | `DefaultAvailable` / `InsufficientNeixi` / `NoCombatItem` |

### Required Shots（per qa-plan-sprint-6 §cu-visual-evidence）

| # | 类型 | 内容 | 文件 | 状态 |
|---|---|---|---|---|
| 1 | 截图 | 招式选择面板 — 6 个装备招式 + `调息` + `使用道具` 全部可见 | `production/qa/evidence/media/cu-004-move-panel-all-actions.png` | TBD |
| 2 | 截图 | 置灰原因 ① — 内息不足（fixture: `InsufficientNeixi`） | `production/qa/evidence/media/cu-004-disabled-neixi-shortage.png` | TBD |
| 3 | 截图 | 置灰原因 ② — 道具未配备（fixture: `NoCombatItem`） | `production/qa/evidence/media/cu-004-disabled-no-combat-item.png` | TBD |
| 4 | 截图 | 置灰原因 ③ — 招式冷却 / 不可用（fixture: TBD） | `production/qa/evidence/media/cu-004-disabled-other.png` | TBD |
| 5 | 截图 | 预览卡关系 — 克制（玩家招克制敌方公开招） | `production/qa/evidence/media/cu-004-preview-counter.png` | TBD |
| 6 | 截图 | 预览卡关系 — 同系（同体系招式） | `production/qa/evidence/media/cu-004-preview-same-element.png` | TBD |
| 7 | 截图 | 预览卡关系 — 被克（玩家招被敌方招克制） | `production/qa/evidence/media/cu-004-preview-countered.png` | TBD |

### Capture Checklist

- [ ] 截图 ≥ 3 张（必备：#1 + 至少 2 个 disabled + 至少 2 个 preview 关系）
- [ ] 所有截图挂在 `production/qa/evidence/media/` 下并被本文件相对引用
- [ ] 文件名使用 kebab-case；分辨率 ≥ 1280×720
- [ ] header `证据状态` 字段升级为 `Visual Captured`
- [ ] story 文件 `production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md` 内 `Foundation Captured` / `Visual evidence not yet captured` 升级为 `Visual Captured`
- [ ] tech-debt-register 中 cu-004 Visual evidence deferred 条目改为 `Resolved`

### Sign-off

| 角色 | 姓名 | 日期 | 签字 |
|---|---|---|---|
| Designer | TBD | TBD | __pending__ |
| QA Lead | TBD | TBD | __pending__ |
