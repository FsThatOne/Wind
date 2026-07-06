# Polish Phase Backlog

本文件收集从 Sprint 正式 descope 到 Polish phase 的工作项。

---

## Hardware Verification

| ID | Item | 来源 | 前置条件 | 优先级 |
|----|------|------|---------|--------|
| PB-001 | cu-008 手柄硬件实机走查 — Steam Deck/通用手柄验证双焦点+D-pad导航 | Sprint 5-9 carry (4 sprint) | 购入 Steam Deck 或通用 USB 手柄 | P1 |

### PB-001 验收标准
- 实际硬件连接后运行战斗招式面板
- D-pad 上下导航、A 键确认正常
- 鼠标 hover 与手柄焦点互不干扰
- 焦点不逃出面板
- 截图/录屏作为 evidence

---

## Tech Debt (from Sprint 走查)

| ID | Item | 来源 | 风险 | 优先级 |
|----|------|------|------|--------|
| PB-002 | ExplorationLockGuard 未在 Godot 层接入 — Bridge 使用 MovementFrozen 反射替代 | S9-003 集成走查 | Low (当前可工作) | P2 |
| PB-003 | 任务页面 UX 设计与实现 — 左侧主线/支线可折叠列表，右侧显示选中任务上下文说明 | 序章试玩反馈 2026-07-04 | Medium (当前 HUD 只能短追踪，缺少任务回顾页面) | P1 |

### PB-002 说明
- `ExplorationLockGuard` 在 Foundation 层通过 `LockMode` 控制 `ProximityDetector.PauseDetection()`
- Godot 层的 `InsightDetectorBridge` 未实例化 LockGuard，而是通过反射 `MovementFrozen` 内联暂停
- 当前可工作（战斗/对话均设置 MovementFrozen），但语义不统一
- 建议：在 Bridge 中接入 LockGuard 或统一到单一暂停路径

### PB-003 说明
- 左侧任务 HUD 只保留即时目标，不展示任务上下文说明
- 任务页面参考《逸剑风云决》：左侧为两栏任务导航（类别 + 任务列表），类别至少包含 `主线`、`支线`，并支持折叠/展开
- 右侧显示选中任务详情：标题、上下文说明、当前目标、已完成子任务
- 后续实现时复用 `GameFlow.TrackedQuestObjective.Text` 与 `Reason`，不要再让 HUD 承担长说明阅读
