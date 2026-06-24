# cu-visual-evidence Harness 设计 — 把"录屏 1 个 story"还原成可执行的 6 子任务图

> 类型：Dev-story spec（先于 dev-story 落产，让 cu-visual-evidence 实际 scope 显形）
> Story：`cu-visual-evidence`（Sprint 7 唯一剩 must-have）
> 日期：2026-06-24
> 作者：agent
> 状态：Draft（待 owner approve subtask graph + 排期）

---

## TL;DR

`cu-visual-evidence` 在 `sprint-status.yaml` 里看着是 **6h 的 owner 录屏任务**。
实际跑通需要先做 **UI 集成层**：当前 `feng-zhi/scenes/vs/battle_jiangnan_bandit.tscn`（MVP-A）用 **2 个简单 Button + 4 个 Label** 占位代替了 CombatUi 系列 widget，cu-004/005/006/008 demo 的 UI 元素**根本不在场景里**。

**所以 cu-visual-evidence 实际是一个 epic 级工作**，应该拆成 **6 个 subtask**：

```
cu-visual-evidence (实际工时估 12-15h, 而非 sprint-status 标的 6h)
├── A. cu-004 集成 (CombatMoveSelectionPanel.tscn + 数据绑定)        4h
├── B. cu-005 集成 (CounterDecisivePromptPanel + 状态绑定)           2h
├── C. cu-006 集成 (DecisiveStrikeAnimationDirector + Godot 适配)    3h
├── D. cu-008 集成 (dual-focus + D-pad 导航 wiring + InputMap 检查)  2h
├── E. Recording harness 落地 (deterministic seed × 4 + 1 命令启动)  1h
└── F. Owner 实机录 4 段证据 (按 harness checklist 串)               1-2h
```

**E + F 是真正的"录屏"**，A+B+C+D 是先把 UI 集成进 battle scene 才能录。

---

## Background — 为什么不是简单录屏

### MVP-A 显式声明的 scope deviation

`feng-zhi/scripts/vs/JiangnanBattleGame.cs` 头部注释（2026-06-22 23:55 写入）：

> **MVP-A 范围说明（spec §2.1 deviation）：**
> - 用 2 个简单 Button 代替 CombatMoveSelectionPanel（cu-004 集成推到 cu-visual-evidence）
> - 用 自有 Label 代替 CombatHudPanel（cu-001..003 集成同上）
> - 仍复用 BattleFacade + BattleEventBus + CombatUiEventAdapter + ResolutionService

S7-VS-Combat-Loop 完成时**明确把 CombatUi widget 集成推到 cu-visual-evidence**。所以 cu-visual-evidence 不是"录屏 done UI"，而是"集成 + 录屏"。

### Foundation 已有，feng-zhi 未集成

| 层 | 状态 | 文件 |
|---|---|---|
| Foundation CombatUi 业务逻辑 | ✅ done | `src/FengZhi.Foundation/CombatUi/{CombatUiRoot, CombatUiMoveSelection, CombatUiEventAdapter, DecisiveStrikeDirector/, GodotIntegration/}` |
| Foundation 单测 | ✅ done | `tests/integration/combat-ui/` 多套（49+ tests，cu-001..008 自动契约层全 ✅） |
| feng-zhi Godot widget | ❌ **缺失** | 应在 `feng-zhi/scenes/ui/combat/` + `feng-zhi/scripts/combat/` 下，当前为空 |
| feng-zhi battle scene 集成 | ❌ 占位 | `JiangnanBattleGame.cs` 用 2 Button + Label，未消费 Foundation CombatUi widget |

cu story 自身已 done（验收基于 Foundation 自动契约 + 旧 prototype harness 手测）。**cu-visual-evidence 录的是「新 VS 场景的视觉呈现」**，需要 feng-zhi 层先把 widget 集成进 VS。

---

## Subtask 拆解

### A. cu-004 集成 · 招式选择面板 · 4h

**目标**：在 `battle_jiangnan_bandit.tscn` 把 `LightButton + HeavyButton` 替换为完整的 `CombatMoveSelectionPanel.tscn`。

**Foundation 端**：消费 `CombatUiMoveSelection` 模块 + `CombatUiEventAdapter`（已 ready）。

**Godot 端新建**：
- `feng-zhi/scenes/ui/combat/CombatMoveSelectionPanel.tscn`
  - 8 行 `MoveRow.tscn`（招式名 / 体系角标 / 内息消耗 / 触发条件图标 / 一行摘要 / 置灰原因 label）
  - 1 行预览卡 `MovePreviewCard.tscn`（关系标签 + 反制提示，**绝对不显示胜率/期望值/最终结算数字**）
- `feng-zhi/scripts/combat/CombatMoveSelectionPanel.cs`
  - 订阅 `CombatUiEventAdapter.OnMoveSelectionPanelUpdated`
  - 8 招式 = 6 装备招式 + 调息 + 使用道具（**不含**普通攻击）
  - 心法专属招式渲染"心法"角标
  - hover/focus → 更新 PreviewCard

**fixture 扩展**（用于 demo seed）：
- `JiangnanBandit1v1Fixture.CreateDemoConfig_Cu004Showcase()` — 6 个不同招式 + 1 个心法专属 + 1 个内息不足 + 1 个心法封印 + 1 个无道具

**验收点（来自 cu-004.md）**：
- [ ] 招式面板展示 6 装备 + 调息 + 使用道具，**不展示**普通攻击
- [ ] 心法专属角标
- [ ] 置灰原因可见（内息不足 / 心法封印 / 无道具）
- [ ] hover/focus 预览卡显示体系关系 + 反制提示
- [ ] 预览卡**不含**胜率 / 期望值 / 最终结算输出
- [ ] 默认聚焦第一个可用招式
- [ ] 资源刷新事件交互不中断

---

### B. cu-005 集成 · 反制 + 决胜提示 · 2h

**目标**：在 `CombatMoveSelectionPanel` 上叠加反制 / 决胜行高亮 + 标签。

**Foundation 端**：消费 `CombatUiMoveSelection.CounterDecisivePrompt`（已 ready）。

**Godot 端新建**：
- `feng-zhi/scenes/ui/combat/CounterTag.tscn` — 单招式行的"反制"标签（启用 / 置灰 + "内息不足"）
- `feng-zhi/scenes/ui/combat/DecisiveStrikeRow.tscn` — 面板顶部插入的"决胜一击"高亮行（破绽 ≥5 时出现）

**fixture 扩展**：
- `JiangnanBandit1v1Fixture.CreateDemoConfig_Cu005Showcase()` — 敌方公开柔意图 + 玩家有刚系招式 + 内息 ≥3（启用反制） / 内息 <3（置灰反制）；敌方破绽 ≥5（显示决胜行）

**验收点（来自 cu-005.md）**：
- [ ] 克制 + 公开意图 + 内息 ≥3 → 显示"反制"标签
- [ ] 内息 <3 → 反制标签置灰 + "内息不足"
- [ ] 提交反制 → `isCounter=true`
- [ ] 破绽 ≥5 → 顶部"决胜一击"高亮行
- [ ] 切目标刷新决胜提示
- [ ] 面板期间内息变化即时刷新

---

### C. cu-006 集成 · 决胜一击演出 · 3h

**目标**：在 battle scene 接入 `DecisiveStrikeDirector` 完整 7-phase 演出。

**Foundation 端**：消费 `CombatUi/DecisiveStrikeDirector/DecisiveStrikeSequence`（已 ready）+ `GodotIntegration/{TimeScaleEngineBridge, CameraRequestBusBridge, CombatCinematicLockInputFilter}`（已 ready）。

**Godot 端新建**：
- `feng-zhi/scenes/ui/combat/DecisiveStrikeOverlay.tscn` — 慢动作覆盖层（dim background + 中央动画 placeholder）
- `feng-zhi/scripts/combat/DecisiveStrikeGodotAdapter.cs`
  - bind `TimeScaleEngineBridge` 到 `Engine.TimeScale`（演出期 1.0 → 0.2）
  - bind `CameraRequestBusBridge` 到 battle scene `Camera2D`（推镜 + 锁目标 + 禁用 smoothing）
  - bind `CombatCinematicLockInputFilter` 到 InputMap（演出期屏蔽确认/取消/方向）

**fixture 扩展**：
- `JiangnanBandit1v1Fixture.CreateDemoConfig_Cu006Showcase()` — 敌方破绽 = 5（直接可触发决胜）

**验收点（来自 cu-006.md）**：
- [ ] 慢动作 / 推镜 / 动画 / 伤害浮字 / 恢复依序发生
- [ ] 演出中按确认 / 取消 / 方向键**无效**
- [ ] 演出中模拟暂停 → TimeScale 优先级生效，释放后继续
- [ ] 最终 TimeScale = 1.0，Camera 回默认跟随
- [ ] 伤害浮字 = max-size deep-gold

---

### D. cu-008 集成 · 双焦点 + 手柄 D-pad 导航 · 2h

**目标**：让 `CombatMoveSelectionPanel` 满足 Godot 4.7-stable dual-focus 行为。

**Foundation 端**：消费 `CombatUiMoveSelection.NavigationGraph`（已 ready）。

**Godot 端**：
- `feng-zhi/scripts/combat/CombatMoveSelectionPanel.cs` 加：
  - `focus_neighbor_top/bottom` 循环配置（最后一项 → 第一项）
  - `grab_focus()` 受 FocusManager 生命周期保护
  - 鼠标 hover 独立视觉状态（不抢手柄 focus）
  - 输入模式切换时不丢失 focus
- `feng-zhi/scenes/ui/combat/CombatMoveSelectionPanel.tscn` 检查：
  - 所有可交互 Control 设 `focus_mode = All`
  - 反制 / 决胜行也参与 `focus_neighbor`
- InputMap 检查：D-pad up/down / 确认 / 取消 全部 mapped

**验收点（来自 cu-008.md）**：
- [ ] D-pad 上下导航所有可交互行动
- [ ] 最后/最前循环
- [ ] 焦点不逃出招式面板
- [ ] A/确认提交聚焦行动
- [ ] 反制/决胜行可手柄触达
- [ ] 鼠标 hover + 手柄 focus 共存视觉不冲突
- [ ] 输入模式切换焦点高亮规则正确

---

### E. Recording harness 落地 · 1h

**目标**：让 owner 一条命令进 demo state，不必走 explore → battle → 玩到正确状态。

#### E.1 Demo Mode 入参

`JiangnanBattleGame.cs` 新增编辑器导出参数：

```csharp
[Export]
public string DemoSeed { get; set; } = "";  // "" / "cu-004" / "cu-005" / "cu-006" / "cu-008"
```

在 `_Ready()` 开头：

```csharp
var config = string.IsNullOrEmpty(DemoSeed)
    ? JiangnanBandit1v1Fixture.CreateBattleConfig()
    : DemoSeed switch
    {
        "cu-004" => JiangnanBandit1v1Fixture.CreateDemoConfig_Cu004Showcase(),
        "cu-005" => JiangnanBandit1v1Fixture.CreateDemoConfig_Cu005Showcase(),
        "cu-006" => JiangnanBandit1v1Fixture.CreateDemoConfig_Cu006Showcase(),
        "cu-008" => JiangnanBandit1v1Fixture.CreateDemoConfig_Cu008Showcase(),  // = cu-004 + 强制 gamepad-only 模式
        _ => throw new ArgumentException($"unknown DemoSeed: {DemoSeed}")
    };
```

#### E.2 Demo Scene Variants

4 个 demo scene 文件（每个继承自 `battle_jiangnan_bandit.tscn`，仅覆盖 `DemoSeed`）：

```
feng-zhi/scenes/vs/demo/
├── battle_demo_cu004.tscn   (DemoSeed = "cu-004")
├── battle_demo_cu005.tscn   (DemoSeed = "cu-005")
├── battle_demo_cu006.tscn   (DemoSeed = "cu-006")
└── battle_demo_cu008.tscn   (DemoSeed = "cu-008")
```

#### E.3 一条命令 Launch

```bash
# cu-004 demo
godot --path feng-zhi/ feng-zhi/scenes/vs/demo/battle_demo_cu004.tscn

# cu-005 demo
godot --path feng-zhi/ feng-zhi/scenes/vs/demo/battle_demo_cu005.tscn
# (and so on)
```

或在 `project.godot` 加 4 个 run preset。

#### E.4 State assertion（防 demo 漂移）

每个 demo scene 加 `assert _Ready()` 末尾：

```csharp
#if DEBUG
AssertDemoState();
#endif

private void AssertDemoState()
{
    switch (DemoSeed)
    {
        case "cu-004":
            Debug.Assert(_panel.AvailableMoves.Count >= 6, "cu-004 需要 ≥6 招式");
            Debug.Assert(_panel.HasMethodMove, "cu-004 需要至少 1 心法专属招式");
            Debug.Assert(_panel.HasInsufficientNeixiMove, "cu-004 需要至少 1 内息不足招式");
            break;
        case "cu-005":
            Debug.Assert(_bandit.IntentPublic && _bandit.IntentSchool == School.Rou, "cu-005 需要敌方公开柔意图");
            Debug.Assert(_bandit.OpeningGap >= 5, "cu-005 需要敌方破绽 ≥5（同时演示决胜）");
            break;
        // ...
    }
}
```

state assertion 在 demo seed 漂移时**直接 fail-fast**，避免 owner 录到一半发现状态不对。

---

### F. Owner 录制 4 段证据 · 1-2h

#### F.1 录前 checklist

- [ ] 拔掉鼠标？（cu-008 keyboard-only / gamepad-only path）→ 否则用键盘焦点路径
- [ ] macOS 录屏工具（QuickTime 或 OBS）就绪
- [ ] 录屏分辨率 ≥ 1280×720（建议 1920×1080）
- [ ] 项目最新 main 拉过（避免录的是过期分支）
- [ ] 4 个 demo scene 编辑器可 import 0 err
- [ ] Foundation 测试 0 fail（基线干净）

#### F.2 录制流程（每个 cu 单独录）

**cu-004：招式选择面板与预览卡**

1. 启动 `battle_demo_cu004.tscn`
2. 录屏开始
3. 镜头停在打开的招式选择面板（默认聚焦第一个可用招式）
4. 鼠标 hover 4 个不同招式（克制 / 被克 / 中性 / 心法专属）— 每次定格 1 秒看预览卡
5. 键盘方向键移到内息不足的招式 — 定格 1 秒看置灰 + 原因
6. 键盘移到心法封印招式 — 定格看角标 + 置灰
7. 键盘移到无道具的"使用道具" — 定格看置灰 + 原因
8. 停止录屏，文件名 `cu-004-evidence-2026-06-XX.mp4`

**cu-005：反制 + 决胜提示**

1. 启动 `battle_demo_cu005.tscn`
2. 录屏开始
3. 镜头停在面板（已显示反制 + 决胜行）
4. 聚焦克制招式（内息 ≥3）— 定格 2 秒看"反制"标签
5. 模拟内息消耗（按"调息"或场景里设的快捷键回到内息 <3）— 定格看"反制"置灰 + "内息不足"
6. 切换目标（如果有多目标）— 定格看决胜提示刷新
7. 顶部"决胜一击"高亮行 — 定格 2 秒
8. 停止录屏，文件名 `cu-005-evidence-2026-06-XX.mp4`

**cu-006：决胜一击演出**

1. 启动 `battle_demo_cu006.tscn`（敌方破绽 = 5，可直接触发决胜）
2. 录屏开始
3. 聚焦决胜一击行
4. 按确认键触发演出
5. **完整录完 7-phase**（慢动作 → 推镜 → 动画 → 伤害浮字 → 恢复）
6. 演出期间按方向键 / 取消键 — 验证输入屏蔽
7. （optional）演出中按系统暂停（在另一终端 `kill -STOP $godot_pid && sleep 2 && kill -CONT $godot_pid`）验证 TimeScale 恢复
8. 演出结束后 TimeScale = 1.0 + Camera 回默认 — 定格 2 秒
9. 停止录屏，文件名 `cu-006-evidence-2026-06-XX.mp4`

**cu-008：双焦点 + 手柄导航**

1. 准备：拔鼠标 OR 用手柄；macOS 可用键盘 D-pad 模拟（W/A/S/D + 回车 = 确认）
2. 启动 `battle_demo_cu008.tscn`
3. 录屏开始
4. 镜头停在打开的面板（默认焦点在第一个可用招式）
5. D-pad / 方向键持续向下 — 验证导航 + 循环（最后 → 第一）
6. D-pad 上 — 验证反方向循环
7. 确认键提交 — 验证提交聚焦行动
8. D-pad 到反制行 + 决胜行 — 验证可触达
9. （如有外接鼠标）hover + D-pad — 验证 dual-focus 共存
10. 切换输入模式（拔/插手柄 OR 模拟）— 验证焦点高亮规则
11. 停止录屏，文件名 `cu-008-evidence-2026-06-XX.mp4`

#### F.3 录后归档

4 个 `.mp4` 落 `production/qa/evidence/media/`，对应 `cu-00X-evidence.md` 文件的 `## 录制环境 / 视频路径` 字段更新（已有 placeholder TBD）。

每个 evidence.md 加段：
```markdown
## 视觉证据（VS 江南战斗场景 demo seed）

- 录制日期：2026-06-XX
- demo scene：`feng-zhi/scenes/vs/demo/battle_demo_cuXXX.tscn`
- 视频：`production/qa/evidence/media/cu-XXX-evidence-2026-06-XX.mp4`
- 验收：见上方 AC 清单（全 ✅）
```

---

## 推荐工作顺序 + 估时

| 顺序 | 子任务 | 工时 | 依赖 | Agent / Owner |
|---|---|---|---|---|
| 1 | A · cu-004 集成 | 4h | Foundation CombatUi（已 ready） | Agent |
| 2 | B · cu-005 集成 | 2h | A（共用 panel） | Agent |
| 3 | D · cu-008 集成 | 2h | A（共用 panel + focus） | Agent |
| 4 | C · cu-006 集成 | 3h | A（决胜行触发） | Agent |
| 5 | E · Recording harness | 1h | A B C D 全 done | Agent |
| 6 | F · Owner 录 4 段 | 1-2h | E done | **Owner** |

**关键路径**：A → (B || D) → C → E → F，最长 12h。

**为什么 A 是瓶颈**：cu-004 的 `CombatMoveSelectionPanel` 是 B/C/D 共同消费的核心 widget，必须先落。

---

## Sprint 7 排期建议

| 选项 | 内容 | 优劣 |
|---|---|---|
| **(推荐) Sprint 7 内全部完成** | Sprint 7 ahead of schedule 38h，12h 工时塞得下；瓶颈 = owner 录制窗口 | + Sprint 7 gate prove-or-pivot 凭证完整 / − 占用 12 天剩余 sprint 工时 |
| Sprint 7 完成 A+B+C+D+E（集成 + harness），录制延到 Sprint 8 头 | Agent 这边 sprint 内完成，Owner 等空档录 | + Agent 利用率高 / − Sprint 7 gate 凭证不完整 |
| 拆 must-have 等级：A 必做（含 cu-004 评分），B/C/D 降 should | 把决胜演出 / 反制提示 / 手柄 之一推 Sprint 8 | + 收口更快 / − 决胜演出留尾 |

---

## Sprint-status.yaml 同步建议

当前 `cu-visual-evidence` story 标 `estimate_hours: 6.0`，与本 spec 拆解的 12-15h 偏差 **+100%**。建议：

**选项 a（最干净）**：把 `cu-visual-evidence` 拆为 6 个独立 story：
- `cu-004-vs-integration` (4h, must-have)
- `cu-005-vs-integration` (2h, must-have)
- `cu-006-vs-integration` (3h, must-have)
- `cu-008-vs-integration` (2h, should-have)
- `cu-visual-evidence-harness` (1h, must-have)
- `cu-visual-evidence-recording` (1-2h, must-have, owner)

**选项 b（轻量）**：保留 `cu-visual-evidence` 单 story，但把 `estimate_hours` 从 6 改为 14，note 字段加 subtask 列表引用本 spec。

**推荐 选项 a** — 6 个子 story 都有清晰交付物 + 估时，retro 时偏差分析清晰；harness 文件 / 集成代码 / 录屏视频 三类交付物各归各的 story。

---

## Future-proofing

本 harness 模式可复用于：
- 任何未来 cu-009+ 视觉证据
- 探索系统视觉证据（exploration-insight）
- 心境系统视觉证据（mindset-dual-axis 后续 stage）

`feng-zhi/scenes/vs/demo/` 目录 + `DemoSeed` 导出参数模式 = 通用 "deterministic demo seed" 范式，下次复用直接套。

---

## Validation Criteria

1. ⏳ Owner approve subtask graph + 排期选项（a / b / c）
2. ⏳ A done：cu-004 AC 全 ✅ + 自动测试 0 回归
3. ⏳ B done：cu-005 AC 全 ✅
4. ⏳ C done：cu-006 AC 全 ✅ + 演出无卡死
5. ⏳ D done：cu-008 AC 全 ✅ + dual-focus 实机走查
6. ⏳ E done：4 个 demo scene 可一条命令启动 + state assertion 通过
7. ⏳ F done：4 个 `.mp4` 落 `media/` + 4 个 evidence.md 视频路径更新 + Sprint 7 gate prove-or-pivot 凭证完整

---

## Open Questions（owner 待定）

- **Q1**：subtask 拆解走选项 a 还是 b？（推荐 a）
- **Q2**：4 个 demo scene 是 standalone .tscn 还是 1 个 scene + 4 个 export var preset？（推荐 4 个 .tscn 简单粗暴）
- **Q3**：cu-006 演出期间手动暂停验证用什么方式？（kill -STOP 进程 / 系统层暂停 / 不验证此点）
- **Q4**：cu-008 是否真接外接手柄录制（推荐），还是仅键盘 D-pad 模拟？（取决于手头有无手柄）
- **Q5**：Sprint 7 排期建议选 1 / 2 / 3？（推荐 1，Sprint 7 ahead 38h 塞得下）
- **Q6**：本 spec 是否要同步落 `production/qa/evidence/cu-00X-evidence.md` 头部"待录"提示？（推荐落，让 owner 看 evidence.md 时知道 cu-visual-evidence 在路上）
