# 行气战棋可玩薄片设计

> **日期**: 2026-06-30  
> **状态**: Draft for owner review  
> **目标场景**: `feng-zhi/scenes/vs/tactics_battle.tscn`  
> **目标**: 将现有江南战斗演示升级为可操作的等距菱形格战棋薄片  
> **Owner decision**: 先做战棋可玩薄片，再接入师兄临别传承教学
> **2026-07-01 owner layout decision**: 招式面板底部居中横排；当前角色信息左下；选中单位信息右下；hover 招式显示信息，点击招式进入出招意图并显示范围。

## 1. 设计对照

本 spec 推进前已对照以下约束：

- `design/gdd/combat-system.md`: 当前战斗模型是行气战棋，核心循环为“观气 -> 取位 -> 出招 -> 破绽 -> 决胜”。角色行气满后执行一次“移动 + 出手”。
- `design/gdd/combat-ui.md`: 战斗 UI 的职责是让玩家看见棋盘，必须展示移动格、攻击范围、路径、气机关系、身位收益、预计破绽变化。
- `design/gdd/map-scene-management.md`: 探索与战斗统一使用 128x64 等距菱形地块；地块、足迹、交互和阻挡体积不得退回矩形。
- `docs/architecture/adr-0020-pure-2d-wuxia-rendering-direction.md`: 当前路线是纯 2D 武侠战棋；不采用伪 2.5D、HD-2D、动态光照或复杂后处理。
- `docs/architecture/adr-0022-isometric-projection-and-iso4-animator.md`: 战斗与探索共享等距菱形投影、4 斜向角色朝向、`IsoProjection`、y-sort 与 128x64 tile 规格。
- `docs/architecture/control-manifest.md`: 战棋可读性优先级为移动格、阵营/朝向、遮挡关系、招式 VFX、氛围。
- `production/qa/evidence/pc-006-senior-brother-misunderstanding-and-farewell-evidence.md`: 序章目前只有战斗教学入口 key，真实 combat tutorial config 尚未接入。

## 2. 核心原则

第一版薄片只验证“玩家是否真的在战棋中做决策”。它必须让玩家完成一次完整的取位与出招：

```text
行气推进
  -> 当前行动者亮起
  -> 显示可移动格
  -> 玩家选择移动格或原地
  -> 角色逐格移动并更新朝向
  -> 打开出手面板
  -> 玩家选择招式和目标
  -> 显示攻击范围、气机关系、正侧背收益、破绽变化
  -> 结算伤害与破绽
  -> 回到行气推进或战斗结束
```

探索与战斗的空间语言必须统一：

- 同一套 `IsoProjection` 与 128x64 菱形 tile。
- 同一套角色占格原则：每个角色/动物占 1 格，脚底体积遵循主角统一足迹。
- 同一套高亮语义：可达格、不可达格、攻击范围、当前目标、危险/决胜窗口使用不同颜色。
- 同一套朝向语义：NE/SE/SW/NW，结算中的正面/侧面/背面必须能被玩家看懂。

## 3. 范围

### 3.1 包含

- 使用现有 `tactics_battle.tscn` 作为薄片场景。
- 棋盘规模使用 5x5，沿用当前战棋薄片与 ADR-0022 已验证的薄片尺寸，便于快速调试。
- 主角 1 人 vs 江湖小贼 1 人。
- 使用 `XingqiBattleLoopController` 的行气模式，不再把默认路径停留在旧的面板回合演示。
- 接入 `BoardVisualizer` 显示移动范围、攻击范围、目标格和当前光标格。
- 接入 `GridCursorController` 选择移动格。
- 接入 `CombatantMover` 做逐格移动，移动完成后更新角色逻辑位置与视觉位置。
- 移动后打开 `CombatMoveSelectionBinder` 的招式选择面板。
- 招式面板固定在屏幕底部正中，招式横向排开。
- 当前行动角色简要信息固定显示在屏幕左下角。
- 当前选中/悬停的其他单位信息显示在屏幕右下角。
- 出招前提供最小目标预览：气机克制关系、正侧背标签、预计破绽变化。
- 破绽达到阈值时显示决胜一击入口；决胜演出继续使用现有轻量集成，不扩展成完整美术演出。
- 战斗能够自然结束并跳转 outcome。

### 3.2 不包含

- 不接入序章师兄教学剧情。
- 不做多人队伍、同伴上阵、围势完整表现。
- 不做道具、切换内功、主动轻功的完整菜单。
- 不做正式战斗地图资产重设。
- 不做 Boss、阶段转换、复杂 AI。
- 不做完整战斗教学 UI，只保留可测的战斗操作。
- 不改 GDD/ADR 的战斗规则，只补现有实现的 Presentation/Integration 缺口。

## 4. 当前实现基础

已有可复用组件：

- `src/FengZhi.Foundation/Combat/Runtime/XingqiBattleLoopController.cs`
  - 已支持行气脉冲、行动队列、玩家移动阶段、玩家出手阶段、AI 行动、战斗结束。
- `src/FengZhi.Foundation/Combat/Board/BattleGrid.cs`
  - 已有棋盘、占位、寻路与移动服务基础。
- `feng-zhi/scripts/combat/BoardVisualizer.cs`
  - 已能在 `HighlightLayer` 上显示可达格，但需要扩展高亮类型。
- `feng-zhi/scripts/combat/GridCursorController.cs`
  - 已能在合法格集合内移动光标并确认格子。
- `feng-zhi/scripts/combat/CombatantMover.cs`
  - 已能按路径 tween 移动 sprite。
- `feng-zhi/scripts/vs/TacticsBattleGame.cs`
  - 已有 `UseXingqiMode`、`InitXingqiMode`、`MovementRangeCalculatedEvent` 订阅、行气 HUD 和面板绑定雏形。

主要缺口：

- `tactics_battle.tscn` 没有完整的 `BoardVisualizer` / `GridCursorController` / `HighlightLayer` 节点装配。
- 默认 `UseXingqiMode` 没有成为主路径。
- 移动阶段目前没有玩家可见、可点选、可确认的棋盘操作闭环。
- 攻击范围和目标预览还没有成为棋盘层信息。
- 正侧背与气机关系更多停留在底层规则，玩家看不见。

## 5. 组件设计

### 5.1 BattleBoardPresentation

在 `TacticsBattleGame` 内先做轻量编排，不新建大框架。需要绑定：

- `BoardVisualizer`: 管理棋盘高亮。
- `GridCursorController`: 管理移动格选择。
- `CombatantMover`: 管理角色视觉移动。
- `XingqiBattleLoopController`: 作为战斗状态权威。

第一版可直接在 `TacticsBattleGame` 中编排，等薄片稳定后再抽 `BattleBoardPresentation` 独立类。

### 5.2 BoardVisualizer 扩展

从“只显示可达格”扩展为四类高亮：

| 类型 | 用途 | 颜色语义 |
|---|---|---|
| MoveReachable | 可移动格 | 青绿色半透明 |
| Cursor | 当前光标格 | 白/金描边 |
| AttackRange | 当前招式攻击范围 | 淡红或橙色半透明 |
| TargetPreview | 当前目标格 | 金色或深红描边 |

高亮必须使用 128x64 等距菱形，不使用矩形 Control 覆盖。

### 5.3 GridCursorController 扩展

第一版支持键盘/手柄方向输入，鼠标点击可后置。

- `Enable(validCells, startPos)` 时显示光标。
- 方向键只在合法格之间移动。
- `ui_accept` 确认目标格。
- `ui_cancel` 可回到原地或关闭移动选择，但不退出战斗。

### 5.4 移动阶段

当 `MovementRangeCalculatedEvent` 到达：

1. `BoardVisualizer.ShowMoveReachable(cells)`。
2. `GridCursorController.Enable(cells, actor.Position)`。
3. 状态文案显示“取位”。
4. 玩家确认后调用 `XingqiBattleLoopController.SubmitPlayerMovement(target)`。
5. 根据返回路径驱动 `CombatantMover.MoveAlongPath(path)`。
6. 移动完成后清除移动高亮，进入出手阶段。

若玩家选择原地，仍然进入出手阶段。

### 5.5 出手阶段

移动阶段完成后：

1. 打开 `CombatMoveSelectionBinder`。
2. 招式面板在屏幕底部正中横向排开。
3. 默认聚焦第一个可用招式。
4. 鼠标 hover 或键盘/手柄 focus 到某个招式时，显示该招式信息，但不提交行动。
5. 点击招式或按确认键时，视为“打算出招”，棋盘层显示该招式释放范围。
6. 如果范围内有合法目标，默认指向最近或当前敌人。
7. 目标预览显示：
   - 招式气质 vs 目标当前气机。
   - 正面/侧面/背面标签。
   - 预计破绽变化。
   - 是否会打开决胜窗口。
8. 再次确认合法目标后提交 `SubmitXingqiPlayerAction(action)`。

第一版目标固定为唯一敌人，不做目标切换；范围与预览仍必须显示。

### 5.6 行气与行动队列

第一版保留现有两条行气条，但要补足行动状态：

- 当前行动者高亮。
- 状态条显示“观气 / 取位 / 出招 / 结算 / 战斗结束”。
- 行气 ready 时单位头顶或角色脚下出现高亮。

行动队列完整 UI 可以后置，但日志和 HUD 必须能确认谁在行动。

### 5.7 HUD 布局

薄片采用三块固定 HUD，避免信息散落：

| 区域 | 位置 | 内容 | 显示时机 |
|---|---|---|---|
| 当前角色简要信息 | 左下角 | 当前行动角色姓名、气血、内息、破绽、当前气机、行气状态 | 战斗中常驻；当前行动者变化时刷新 |
| 招式面板 | 底部正中 | 可用招式横向排开；每个招式显示名称、气质图标、内息消耗、可用/不可用状态 | 只在出手阶段显示 |
| 选中单位信息 | 右下角 | 当前选中/悬停单位姓名、阵营、气血、内息、破绽、当前气机、正/侧/背关系 | 玩家选中其他单位、hover 目标格或进入出招意图时显示 |

交互规则：

- 鼠标 hover 招式：更新招式信息与预览卡，不显示释放范围，不提交行动。
- 键盘/手柄 focus 招式：与鼠标 hover 同等处理，保证非鼠标输入也能看到招式信息。
- 点击招式或确认聚焦招式：进入“打算出招”状态，棋盘展示释放范围和可命中目标。
- 在“打算出招”状态选中其他单位：右下角单位信息切换为该单位，并同步显示该招式对该单位的气机关系、正/侧/背标签和破绽变化方向。
- 点击空白或取消：退出“打算出招”状态，清除攻击范围，回到招式面板浏览。
- 三块 HUD 不得遮挡当前行动者、目标单位、可移动格和攻击范围核心区域；必要时底部 HUD 半透明，但文字必须可读。

## 6. 数据流

```text
XingqiBattleLoopController.Start
  -> XingqiAdvancedEvent
  -> ActorTurnStartedEvent
  -> MovementRangeCalculatedEvent
  -> BoardVisualizer + GridCursorController
  -> CellConfirmed
  -> SubmitPlayerMovement
  -> CombatantMover
  -> CombatMoveSelectionBinder.OpenForPlayerDecision
  -> move hover/focus changes
  -> bottom-center move info preview
  -> move click/confirm
  -> BoardVisualizer.ShowAttackRange
  -> unit select/hover
  -> bottom-right unit info + target preview
  -> SubmitXingqiPlayerAction
  -> Damage/Stagger/Neixi events
  -> HUD refresh
  -> BattleEndEvent or next pulse
```

权威边界：

- 棋盘合法性、占位、移动范围由 Foundation Combat 决定。
- UI 只展示和收集输入，不自行判定最终伤害。
- 预览数值如果 Foundation 暂未提供 DTO，第一版只显示关系标签和破绽方向，不显示精确伤害。

## 7. 验收标准

- **AC-1 空间统一**: 战斗棋盘使用 128x64 等距菱形；角色占 1 格；移动与高亮和地砖重叠准确。
- **AC-2 行气主路径**: 进入战斗后走 `XingqiBattleLoopController` 主路径，而不是旧 `VsBattleLoopController` 面板回合路径。
- **AC-3 移动阶段可玩**: 玩家行动时显示可移动格，能用方向键选择格子并确认移动或原地停留。
- **AC-4 出手阶段可玩**: 移动后打开招式面板，玩家可选择招式并攻击合法目标。
- **AC-5 战术反馈可读**: hover/focus 招式时显示招式信息；点击/确认招式后显示攻击范围、目标格、气机关系、正/侧/背标签、破绽变化方向。
- **AC-6 视觉不遮挡**: 高亮、角色、HUD 不互相遮挡关键操作对象；战棋可读性优先级符合 control manifest。
- **AC-7 完整闭环**: 战斗能打到胜利或失败并跳转 outcome。
- **AC-8 测试与 smoke**: `dotnet build feng-zhi/FengZhi.csproj` 通过；Foundation 相关战斗测试通过；Godot headless 能启动战斗场景；手动跑通一场薄片。
- **AC-9 HUD 布局**: 招式面板位于底部正中横向排开；当前角色简要信息在左下角；选中其他单位时其信息显示在右下角。

## 8. 推荐实施顺序

1. **场景装配**
   - 给 `tactics_battle.tscn` 补 `BoardVisualizer`、`HighlightLayer`、`GridCursorController` 节点。
   - 默认启用 `UseXingqiMode = true`，DemoSeed 使用新的 `tactics-slice`。

2. **移动闭环**
   - 在 `MovementRangeCalculatedEvent` 中开启棋盘光标。
   - 确认格后调用 `SubmitPlayerMovement`。
   - 将角色 sprite 移动到目标格并更新朝向。

3. **出手闭环**
   - 移动完成后打开招式面板。
   - 按底部正中横向布局展示招式。
   - hover/focus 招式显示招式信息；点击/确认招式后进入出招意图并展示范围。
   - 提交招式后结算并刷新 HUD。

4. **范围与预览**
   - BoardVisualizer 增加攻击范围和目标预览。
   - 左下角补当前角色简要信息；右下角补选中单位信息。
   - HUD 增加气机关系、身位、破绽变化文本。

5. **结束与证据**
   - Godot smoke + 手动走查。
   - 补测试和 QA evidence。

## 9. 后续接入师兄教学

薄片通过后再接序章，不提前混入：

- 触发点: `senior_brother_mis_resolved` 且 `prologue_joint_burial_completed` 后。
- 教学叙事: 师兄临别传承，不是生死决斗，不要求打败师兄。
- 教学三段:
  1. `tut_combat_basic`: 移动取位与出招。
  2. `tut_combat_qi_counter`: 当前气机与刚/柔/巧克制。
  3. `tut_combat_decisive`: 破绽大开与决胜一击。
- 教学失败不 Game Over，由师兄台词兜底并重试。

## 10. 风险与降级

| 风险 | 影响 | 降级策略 |
|---|---|---|
| TileMapLayer 高亮 tileset 不足 | 高亮不可读 | 第一版用 `Polygon2D` 运行时画菱形高亮，不阻塞玩法 |
| 目标预览 DTO 不完整 | 预览精度不足 | 先显示关系标签和破绽方向，不显示精确伤害 |
| 鼠标拾取误差 | 操作不稳 | 第一版只承诺键盘/手柄格子选择 |
| 旧 VS demo seed 与新主路径冲突 | 场景状态混乱 | 保留 cu-004/cu-005/cu-006/cu-008 demo scene，主 scene 默认 `tactics-slice` |
| 动画移动与逻辑位置不同步 | 角色错位 | 逻辑位置先更新，视觉 tween 只负责表现；动画期间锁输入 |

## 11. 完成定义

这次薄片完成时，玩家应能用一句话描述战斗：

> “我等到自己行气满，走到侧面，用克制气机的招式打出破绽，然后继续推进到决胜。”

如果玩家只能说“我在菜单里选了一招”，则薄片未完成。
