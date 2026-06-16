# Smoke Test: Critical Paths

**用途**：QA 交付前在 15 分钟内运行这 10-15 项检查。
**入口**：`/smoke-check`（读取本文件）
**维护**：每个核心系统实现后，新增对应条目。

## Core Stability（始终运行）

1. 游戏可启动至主菜单且无崩溃
2. 主菜单可成功开启新游戏 / 新会话
3. 主菜单对所有输入响应正常（键鼠 + 手柄），无卡顿

## Core Mechanic（每个 Sprint 更新）

<!-- 每个 Sprint 实现核心机制后，将主要操作回路加入此处 -->
<!-- 示例：4. 战斗系统：玩家可发起攻击，目标接受伤害并触发受击动画 -->

4. [核心机制 —— 待第一个核心系统实现后填入]

## Data Integrity

5. 存档完成无错误（待 SaveSystem 实现后启用）
6. 读档可正确恢复状态（待 SaveSystem 实现后启用）

## Performance

7. 目标硬件（PC + Steam Deck）上无明显帧率下降（60fps 目标）
8. 5 分钟连续游玩内无内存增长（待核心循环实现后启用）

## UI / Input

9. 所有 UI 支持键鼠与手柄完整导航，无 hover-only 元素（参见 ADR-0002）
10. Steam Deck 屏幕分辨率下中文文字渲染清晰

## Audio

11. BGM 与 SFX 均可正常触发，无明显爆音 / 静音

---

> **签字**：QA Lead 在每次 smoke-check 通过后追加 `## Run YYYYMMDD` 段并签字。
> **失败处理**：任一项失败即视为 build 不可交付 QA，需先修复。
