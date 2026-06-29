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
> 证据状态：**Visual Captured (2026-06-29)**；Foundation 自动契约层 ✅；VS 自动化截图 ✅
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

## Visual Captured (Sprint 7 — 2026-06-29)

### Metadata

| 项 | 值 |
|---|---|
| Capture date | 2026-06-29 |
| Captured by | AutoEvidencePlayer (自动化截图) |
| Engine | Godot 4.7-stable Mono |
| Build | local dev / c3b6990d |
| Platform | macOS |
| Recording tool | AutoEvidencePlayer + Godot Viewport Screenshot |
| Harness | `feng-zhi/scenes/vs/demo/battle_demo_cu004.tscn` |
| Fixture used | DemoSeed `cu-004` (6 招式 + 心法封印 + 内息不足 + 道具未配备) |

### Captured Shots

| # | 类型 | 内容 | 文件 | 状态 |
|---|---|---|---|---|
| 1 | 截图 | 招式选择面板全貌 — 6 装备招式 + 调息 + 使用道具可见 | [`cu-004_01_panel_full_view.png`](./media/cu-004_01_panel_full_view.png) | ✅ PASS |
| 2 | 截图 | 置灰 — 内息不足 | [`cu-004_02_neixi_insufficient_greyed.png`](./media/cu-004_02_neixi_insufficient_greyed.png) | ✅ PASS |
| 3 | 截图 | 置灰 — 心法封印 | [`cu-004_03_xinfa_sealed_greyed.png`](./media/cu-004_03_xinfa_sealed_greyed.png) | ✅ PASS |
| 4 | 截图 | 置灰 — 道具未配备 | [`cu-004_04_item_unavailable_greyed.png`](./media/cu-004_04_item_unavailable_greyed.png) | ✅ PASS |
| 5 | 截图 | 预览卡 — 克制关系 | [`cu-004_05_preview_card_counter.png`](./media/cu-004_05_preview_card_counter.png) | ✅ PASS |
| 6 | 截图 | 预览卡 — 被克关系 | [`cu-004_06_preview_card_countered.png`](./media/cu-004_06_preview_card_countered.png) | ✅ PASS |
| 7 | 截图 | 预览卡 — 同系关系 | [`cu-004_07_preview_card_neutral.png`](./media/cu-004_07_preview_card_neutral.png) | ✅ PASS |
| 8 | 截图 | 默认焦点在第一个可用招式 | [`cu-004_08_default_focus_first_available.png`](./media/cu-004_08_default_focus_first_available.png) | ✅ PASS |

### Capture Checklist

- [x] 截图 ≥ 3 张（实际 8 张，覆盖全部 disabled + 全部 preview 关系）
- [x] 所有截图挂在 `production/qa/evidence/media/` 下并被本文件相对引用
- [x] 文件名使用 kebab-case；分辨率 ≥ 1280×720
- [x] header `证据状态` 字段升级为 `Visual Captured`
- [ ] story 文件 `production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md` 内升级为 `Visual Captured`
- [ ] tech-debt-register 中 cu-004 Visual evidence deferred 条目改为 `Resolved`

### Sign-off

| 角色 | 姓名 | 日期 | 签字 |
|---|---|---|---|
| AutoCapture | AutoEvidencePlayer | 2026-06-29 | ✅ |
| Designer | __pending__ | __pending__ | __pending__ |
| QA Lead | __pending__ | __pending__ | __pending__ |
