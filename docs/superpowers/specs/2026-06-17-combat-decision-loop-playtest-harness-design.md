# Combat Decision Loop Playtest Harness 设计

## 背景

旧 `prototypes/fengzhi-vertical-slice` 已删除，因为它实现的是过期 Burst+Read 战斗系统，会误导当前行气战棋 QA 与 playtest。当前唯一可信可运行目标是 `prototypes/sprint5-combat-ui-harness`，它已经验证 `cu-004`、`cu-005`、`cu-008` 的 Combat UI 合同。

Production -> Polish gate 仍缺真实 playtest 证据。由于当前没有完整新手流程目标，本设计先补一个范围明确的最小战斗决策 playtest 模式，用于验证当前行气战棋的核心决策闭环。

## 目标

在现有 `sprint5-combat-ui-harness` 中增加一个 `CombatDecisionLoop` 模式，验证玩家是否能完成并理解以下闭环：

1. 观察敌方当前内功气机、玩家内息、破绽与可用道具。
2. 在 6 个招式、`调息`、`使用道具`、`决胜` 入口之间选择行动。
3. 通过 hover / focus 预览行动关系、限制、收益提示和不可用原因。
4. 确认行动后看到一条确定性的本回合决策摘要。

这个目标服务于“中盘系统”和“难度曲线”的早期 targeted playtest，但不替代新手体验或完整 Production playtest。

## 非目标

- 不重建完整 vertical slice。
- 不验证叙事、探索、场景切换、存档、完整新手引导。
- 不作为 Production -> Polish gate 的 3 份完整 playtest 证据直接通过条件。
- 不接入或恢复旧 `fengzhi-vertical-slice`。
- 不显示胜率、期望值、AI 下一招、最终伤害结算或完整战斗模拟。

## 玩家可见流程

新增一个 fixture 或模式入口：

```text
CombatDecisionLoop / 最小战斗决策闭环
```

页面仍保留三栏结构：

- `Fixture`：选择不同战斗状态。
- `Current Foundation Output`：当前 Foundation 输出的真实行动列表、preview、focus/hover、确认结果。
- `GDD Expected Contract`：当前行气战棋合同和 playtest 观察点。

底部 `Navigation / Intent` 扩展为 `Navigation / Decision Result`，显示：

- 当前 focus。
- 当前 hover。
- 当前输入模式。
- 最后一次确认行动。
- 本回合决策摘要。

## Fixture 范围

至少新增 3 个 playtest fixture：

| Fixture | 用途 | 必须覆盖 |
|---------|------|----------|
| `decision_loop_balanced` | 标准可读战斗状态 | 6 招式、调息、道具、敌方当前气机、普通招式克制提示 |
| `decision_loop_resource_pressure` | 内息压力 | 高消耗招式禁用、调息可用、道具可见、不可用原因清楚 |
| `decision_loop_decisive_window` | 破绽与决胜窗口 | 决胜入口出现、与克制提示分离、确认后摘要说明决胜选择 |

可选第四个 fixture：

| Fixture | 用途 | 必须覆盖 |
|---------|------|----------|
| `decision_loop_bad_matchup` | 不利气机关系 | 预览明确提示被克或中性，不诱导玩家误判为反制 |

## 决策摘要规则

确认行动后显示一条确定性摘要。摘要只解释本次选择的合同语义，不模拟完整战斗结算。

允许显示：

- 行动是否合法。
- 行动类型：招式 / 调息 / 使用道具 / 决胜。
- 目标。
- 当前敌方内功气机。
- 行动与当前气机的关系：克制 / 被克 / 中性 / 未知。
- 内息变化预期，例如 `-2 内息`、`恢复内息`、`不消耗内息`。
- 是否利用破绽或进入决胜窗口。
- 如果行动不可用，显示原因。

禁止显示：

- 胜率。
- EV / 期望值。
- 敌方下一招。
- 完整伤害结算。
- 随机结果。
- 与 GDD 不一致的 `普通攻击` 兜底。

## 数据与边界

继续使用 `Sprint5CombatUiFixture` 作为 fixture 容器。可以扩展 record，但要保持现有 `cu-004`、`cu-005`、`cu-008` fixture 行为不变。

建议新增字段：

```csharp
bool IsPlaytestLoop;
CombatDecisionLoopExpectation? DecisionExpectation;
```

`CombatDecisionLoopExpectation` 只保存 playtest 需要展示的确定性上下文：

```csharp
string EnemyId;
string EnemyDisplayName;
MoveType EnemyCurrentQi;
int PlayerFlaw;
int EnemyFlaw;
int ExpectedNeixiAfterMeditate;
IReadOnlyDictionary<string, string> ActionOutcomeSummaries;
```

如果实现时发现字段过多，应优先把 playtest-only 数据留在 harness 层，不污染 `FengZhi.Foundation` DTO。

## 组件设计

### `Sprint5CombatUiFixtures`

新增 playtest loop fixtures。现有 Sprint 5 QA fixtures 不改语义。

要求：

- 不重新引入 `BasicAttack`。
- `RevealedEnemyMoveType` 在 harness 内继续按“敌方当前内功气机”解释。
- 每个 loop fixture 都要有明确的 `ExpectedContractSummary`。

### `Sprint5CombatUiHarnessView`

扩展展示逻辑：

- fixture 为 `IsPlaytestLoop` 时，右栏显示 playtest 观察点。
- 底部显示本回合决策摘要。
- 点击或键盘确认行动后，按 `DecisionExpectation.ActionOutcomeSummaries` 生成摘要。
- 如果确认的是禁用行动，摘要必须显示不可用原因。

现有导航行为保持不变：

- 上 / 下移动 focus。
- 左 / 右切换 fixture。
- 空格 / Enter 确认 focus。
- 鼠标 hover 不抢占键盘 focus。
- 禁用行动不进入 focus 图。

### README

补充说明：

- 当前 harness 有两类用途：Sprint 5 QA fixtures 和 Combat Decision Loop playtest fixtures。
- Combat Decision Loop 是 targeted playtest，不是完整 vertical slice。
- Playtest 报告必须注明该限制。

## 测试策略

自动测试：

- `dotnet test FengZhi.slnx`
- Combat UI targeted tests 保持通过。
- 如新增纯 C# 决策摘要 helper，应增加单测覆盖：
  - 克制摘要。
  - 被克摘要。
  - 调息摘要。
  - 道具不可用摘要。
  - 决胜摘要。

手动测试：

- 启动 `prototypes/sprint5-combat-ui-harness`。
- 切换到 `CombatDecisionLoop` fixture。
- 用键盘完成一次招式确认。
- 用鼠标 hover 另一个招式，确认 hover 与 focus 并存。
- 确认 `调息`。
- 确认 `使用道具` 可用/不可用状态。
- 确认决胜 fixture 中 `决胜` 入口出现并可确认。

## Playtest 记录方式

执行 targeted playtest 时，报告必须写清：

- 测试目标：最小战斗决策 loop。
- 测试限制：不是完整新手流程，不验证叙事/探索。
- 观察重点：
  - 玩家是否理解敌方当前气机。
  - 玩家是否理解行动不可用原因。
  - 玩家是否理解调息/道具/决胜的作用。
  - 玩家是否能从预览做出选择。
  - 玩家是否误以为存在普通攻击或下一招猜拳。

建议记录到：

```text
production/playtests/playtest-2026-06-17-mid-game-systems.md
production/playtests/playtest-2026-06-17-difficulty-curve.md
```

如果只跑 harness targeted playtest，`Gate Result` 最高只能写 `CONCERNS`，不能直接写 `PASS`，除非后续有完整当前口径 build 覆盖新手流程。

## 成功标准

实现完成后应满足：

- harness 可启动。
- 新 loop fixtures 可切换。
- 玩家可完成至少一次合法招式确认、一次调息确认、一次决胜确认。
- 所有确认结果都有清楚的决策摘要。
- `普通攻击` 不出现。
- 不显示胜率、EV、敌方下一招或完整随机结算。
- `dotnet test FengZhi.slnx` 通过。

## 风险

- targeted harness 可能被误解为完整 playtest。通过 README、playtest 报告和 gate 说明显式限制。
- fixture 数据可能逐渐变成第二套规则。通过只保存展示/摘要所需确定性数据来控制范围。
- 继续使用 `RevealedEnemyMoveType` 字段名可能产生语义噪音。短期在 harness 文案中解释为兼容字段；长期应重命名 Foundation 合同。

## 下一步

1. 写实现计划。
2. 扩展 fixtures 和 view。
3. 补 README。
4. 构建并运行 tests。
5. 启动 harness，执行 targeted playtest。
