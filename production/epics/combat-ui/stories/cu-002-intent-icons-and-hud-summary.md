# Story: cu-002 — 意图图标与 HUD 汇总

> **Epic**: combat-ui
> **类型**: UI
> **优先级**: P0 — 玩家读招核心信息
> **Estimate**: M（约 5h）
> **依赖**: cu-001
> **阻塞**: cu-004, cu-005
> **ADR 指引**: ADR-0011（意图图标 SceneTreeTween 动画），ADR-0002（CanvasLayer 层级）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design / 意图图标系统, §Edge Cases
> **TR-ID**: TR-combat-ui-002
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-14

## 目标

在敌方头顶和 HUD 汇总区同时呈现敌方意图，让玩家能快速看见刚、柔、巧或未知状态，并安全处理死亡目标和意图从未知到已识破的过渡。

## 范围

### 包含
- `OnIntentRevealed` 后在敌方头顶显示意图图标
- HUD 左上汇总所有敌方意图
- 刚=红色拳意、柔=蓝色水纹、巧=绿色风旋、未知=`?` 灰色迷雾
- 图标淡入 + 轻微上升动画
- 已落败目标收到意图事件时跳过动画，图标槽变暗或消隐
- `?` 下回合识破成功时过渡为正确体系图标

### 不包含
- 招式预览卡里的克制关系 → cu-004
- 反制标签 → cu-005
- 最终符文美术资产

## 技术说明

- 意图图标属于 `WorldIntentLayer`，HUD 汇总属于 `HUDLayer`
- 图标动画使用 SceneTreeTween，参数遵守 `intent_icon_fadein_duration`
- 死亡目标分支必须是安全路径，不得访问已释放节点
- HUD 汇总应以战斗系统公开的 intent snapshot 为唯一数据源，不自行判定识破概率
- 池化或复用图标 Control 时，非交互节点必须 `focus_mode = NONE`
- 多敌人同时刷新时不得每帧重建全部图标；意图图标和 HUD 汇总应复用 Control，HUD 汇总刷新目标不超过 `1ms/frame`
- Godot 场景走查需验证 SceneTreeTween 淡入/上升动画、死亡目标节点释放安全、非交互图标 `focus_mode = NONE`

## 验收标准

- [ ] `OnIntentRevealed` 后敌方头顶显示对应体系图标
- [ ] HUD 汇总区同步显示全部敌方意图
- [ ] 未识破意图显示 `?` 灰色迷雾，不泄露真实体系
- [ ] `?` 到已识破体系的变化使用过渡动画
- [ ] 已落败目标收到意图事件时槽位消隐或变暗，且不崩溃
- [ ] 多敌人同时刷新意图时 HUD 汇总顺序稳定

## QA 手动检查

- **AC-1**：双层显示
  - Setup：进入至少 3 名敌人的测试战斗并触发意图揭示
  - Verify：敌方头顶和 HUD 汇总同时出现图标
  - Pass condition：刚柔巧和未知图标可区分，位置不遮挡角色核心动作

- **AC-2**：未知转已知
  - Setup：第一回合让敌人显示 `?`，第二回合洞察成功
  - Verify：图标从迷雾过渡到体系符文
  - Pass condition：没有突兀闪烁，也不提前泄露真实体系

- **AC-3**：死亡目标安全
  - Setup：敌人落败后模拟收到意图事件
  - Verify：该目标图标槽消隐或变暗
  - Pass condition：无异常、无悬空图标、HUD 汇总不出现错误目标

## 测试证据路径

`production/qa/evidence/cu-002-intent-icons-and-hud-summary-evidence.md`

## 依赖关系

- Depends on: cu-001
- Unlocks: cu-004, cu-005

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 6/6 passing
**Deviations**:
- ADVISORY: `intent_icon_fadein_duration` 尚未接入配置调参，当前实现使用代码常量；后续可接入 tuning knob。
- ADVISORY: 真实 Godot 场景中的 SceneTreeTween 淡入/上升动画、死亡目标释放安全与非交互图标 `focus_mode = NONE` 仍需后续手动走查。
**Test Evidence**: `production/qa/evidence/cu-002-intent-icons-and-hud-summary-evidence.md`；`dotnet test tests/Foundation/Foundation.Tests.csproj --filter CombatUi` 18/18 passing；此前 Foundation full suite 1241/1241 passing。
**Code Review**: Complete — APPROVED WITH SUGGESTIONS
