# Sprint 5 Combat UI Harness 设计

## 目标

新建一个干净、可运行的 Sprint 5 Combat UI QA 目标，因为 `prototypes/fengzhi-vertical-slice` 已经过期，并且仍携带旧战斗模型假设。

这个 harness 用于验证 `cu-004`、`cu-005`、`cu-008`，但不依赖旧 Boss 战流程。它必须同时展示当前 Foundation 的真实输出，以及最新 GDD 的期望合同，让 QA 能区分“实现当前行为”和“设计合同漂移”。

## 当前上下文

- `src/FengZhi.Foundation/CombatUi/CombatUiMoveSelection.cs` 已包含当前招式选择 Presenter、DTO、预览卡、基础行动和导航控制器。
- `tests/integration/combat-ui/*` 已在纯 Foundation 层覆盖 `cu-004`、`cu-005`、`cu-008`。
- 当前 Foundation 的 `cu-005` 合同仍使用 `RevealedEnemyMoveType` 和“公开意图 / 招式类型”语义。
- 最新 GDD 要求反制 / 识破提示基于“敌方当前内功气机 / 当前内功属性”，而不是敌方下一招或招式属性。
- 新 harness 必须显式暴露这类合同差异，不能把旧语义伪装成通过。

## 已批准方案

采用“双层对照 harness”：

- **Current Foundation Output**：左侧面板渲染真实 `CombatUiMoveSelectionPresenter` 和 `CombatUiNavigationController` 快照。
- **GDD Expected Contract**：右侧面板渲染同一 fixture 在最新行气战棋 GDD 下的期望 UI 合同。
- **Contract Drift Banner**：如果当前输出仍表现为旧 `RevealedEnemyMoveType` / 招式类型反制语义，或仍输出最新 registry 不包含的 `普通攻击` 行动，则显示 `CONTRACT DRIFT`。

## 建议位置

- 工程：`prototypes/sprint5-combat-ui-harness`
- 场景：`scenes/Sprint5CombatUiHarness.tscn`
- 入口脚本：`scripts/Sprint5CombatUiHarness.cs`
- 视图脚本：`scripts/ui/Sprint5CombatUiHarnessView.cs`
- 测试数据脚本：`scripts/testdata/Sprint5CombatUiFixtures.cs`
- 可选说明文档：`README.md`

## 范围

### 包含

- 渲染 Sprint 5 招式选择面板状态。
- 渲染 GDD 当前允许的行气行动入口：6 个已装备招式、`调息`、`使用道具`、决胜入口，以及后续同属 `combat_action_types` 的轻功、切换内功等入口。
- 不把 `普通攻击` 作为 GDD 期望项；如果当前 Foundation 输出 `BasicAttack` / `basic_attack`，harness 必须标记为合同漂移。
- 渲染不可用原因，例如 `内息不足`、`心法封印中`、`无可用战斗道具`。
- 渲染 hover / focus 预览卡文本；预览卡可显示 GDD 要求的预计伤害区间、预计破绽变化和合法范围，但不得显示胜率、期望值或最终结算输出。
- 渲染决胜一击资格与可聚焦状态。
- 将鼠标 hover 与键盘 / 手柄 focus 显示为彼此独立的状态。
- 渲染 `cu-005` 的 GDD 期望敌方当前内功气机状态。
- 清晰标记当前实现与 GDD 期望之间的语义漂移。

### 不包含

- 完整战斗模拟。
- 伤害、治疗、破绽、内息等结算。
- 旧 Boss 战流程迁移。
- Enemy AI 行为。
- 存档 / 读档集成。
- 最终美术或演出抛光。

## Fixture

### `DefaultAvailable`

验证 `cu-004` 的默认形态。

- 6 个已装备招式。
- `调息`、`使用道具`、决胜入口。
- 当前目标存在。
- 道具数量大于 0。
- 期望结果：所有当前 GDD 核心行动可见且可聚焦；如果出现 `普通攻击`，标记为 `CONTRACT DRIFT`。

### `InsufficientNeixi`

验证招式禁用原因。

- 一个或多个招式消耗内息高于玩家当前内息。
- 期望结果：禁用招式显示 `差 X 内息`。

### `NoCombatItem`

验证道具行动禁用状态。

- 战斗道具数量为 0。
- 期望结果：`使用道具` 可见但禁用，并显示 `无可用战斗道具`。

### `EnemyCurrentQiGang`

验证新的 `cu-005` GDD 合同。

- 敌方当前内功气机为 `Gang`。
- 期望结果：GDD 面板描述的是针对敌方当前气机的反制 / 识破关系，而不是敌方下一招类型。
- 如果 Foundation 输出只能表达 `RevealedEnemyMoveType`，则显示 `CONTRACT DRIFT`。

### `EnemyCurrentQiRou`

验证气机状态切换。

- 敌方当前内功气机为 `Rou`。
- 期望结果：GDD 面板根据当前气机切换关系文本。

### `StaggerAndDecisive`

验证决胜提示状态。

- 目标处于可决胜的破绽暴露状态。
- 期望结果：决胜条目出现、可聚焦，并且与反制 / 识破提示分离。

### `DualFocus`

验证 `cu-008`。

- 键盘 / 手柄 focus 位于一个行动。
- 鼠标 hover 位于另一个行动。
- 期望结果：focus 与 hover 同时显示，禁用行动不可聚焦。

## UI 布局

场景是一个单页诊断 QA 页面：

- 顶部：fixture 选择器、输入提示、当前 verdict。
- 左列：Current Foundation Output。
- 右列：GDD Expected Contract。
- 底部：导航快照、当前 focus、当前 hover、确认意图、合同漂移标记。

UI 可使用普通 Godot `Control` / `VBoxContainer` / `HBoxContainer` / `Label` / `Button` 控件实现，不需要最终游戏美术风格。

## 数据流

1. 用户选择 fixture。
2. Harness 构造 `BattlePanelDisplayData` 和 `CombatUiMoveSelectionContext`。
3. Harness 打开 `CombatUiMoveSelectionPresenter`。
4. Harness 将快照应用到 `CombatUiNavigationController`。
5. 左列渲染真实 Foundation 快照。
6. 右列渲染 fixture 定义的 GDD 期望。
7. 漂移检测比较语义期望，尤其是 `cu-005`。
8. Hover、focus 移动和确认行动会更新页面快照。

## 成功标准

- Harness 可独立于 `fengzhi-vertical-slice` 运行。
- QA 可在一个场景中验证 `cu-004`、`cu-005`、`cu-008`。
- `cu-004` 的当前 GDD 行动入口、不可用原因和预览卡行为可见。
- `cu-008` 的双焦点和导航行为可见。
- 如果 Foundation 仍使用旧招式类型反制语义，`cu-005` 的语义漂移必须可见。
- 如果 Foundation 仍输出 `普通攻击` 行动，`cu-004` 的 registry 漂移必须可见。
- Harness 不新增战斗结算逻辑。

## 风险

- `cu-005` 可能需要先修改 Foundation 合同，才能真正按最新 GDD 通过。
- 双层 harness 能暴露漂移，但不会自动修复漂移。
- 如果 harness 没有正确引用 Foundation，而是复制 DTO 行为，可能会产生虚假的验证结果。

## 推荐实现顺序

1. 创建新的 Godot 工程壳。
2. 引用或复制最小 project 设置，使其能编译并使用 `FengZhi.Foundation`。
3. 添加 fixture。
4. 添加诊断场景和视图。
5. 将 hover / focus / navigation 控件接到当前 Presenter 与 Navigation Controller。
6. 添加 `cu-005` 漂移检测。
7. 运行 `dotnet test FengZhi.slnx`。
8. 使用 Godot MCP 启动 harness 并执行 targeted manual QA。

## 审批记录

用户已于 2026-06-16 批准“双层对照”方案。实现阶段仍需要明确批准后才能写入 harness 文件。
