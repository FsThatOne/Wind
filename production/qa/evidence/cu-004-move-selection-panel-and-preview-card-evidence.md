# cu-004 招式选择面板与预览卡 — QA Evidence

> 日期：2026-06-17
> Story：`production/epics/combat-ui/stories/cu-004-move-selection-panel-and-preview-card.md`
> TR-ID：`TR-combat-ui-004`
> 类型：UI
> 证据状态：新 harness 手测通过；旧 `fengzhi-vertical-slice` 结果已判定为 stale target
> QA 签字：User manual QA

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
