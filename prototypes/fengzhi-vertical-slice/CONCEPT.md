---
status: reverse-documented
source: prototypes/fengzhi-vertical-slice/
date: 2026-06-10
verified-by: user-approved continuation
---

# 《风止》Vertical Slice 概念

> 本概念文档从可玩 Vertical Slice 与其报告反向整理而来。该 Slice 是验证证据，不是正式生产源码。

## 概述

《风止》Vertical Slice 验证了第一个端到端核心循环：对话铺垫 → Burst+Read 战斗 → 战后心境选择 → 朦胧化叙事反馈。它证明玩家可以在约 3-5 分钟内，于 Godot 4.6.3 + C# 技术栈上体验到游戏的核心幻想。

## 原型验证问题

新玩家能否通过完整的“开始 → 挑战 → 解决”循环，体验“无名侠客在江湖中抉择、克制、悟道”的核心幻想？这个循环是否足够清晰、可控，能够支持后续生产规划？

## 范围

- 一个可玩的 Godot 4.6.3 + C# Vertical Slice。
- 流程包含：对话 → 战斗 → 心境选择 → 朦胧化反馈。
- 使用程序化 UI 和占位表现。
- 单一受控场景，不代表正式内容生产管线。

## 已构建 / 已测试内容

- `EventBus`、`GameManager`、`CharacterData`、`MartialMove`。
- 用于 Burst+Read 战斗的 `CombatManager` 与 `EnemyAI`。
- 用于战前对话和战后选择的 `DialogueManager`。
- `DialogueUI`、`CombatUI`、`BlurredFeedbackUI`。
- 作为可玩场景根节点的 `Main.tscn`。
- 战斗、心境和反馈系统串联后的完整可玩循环。

## 关键发现

- 核心循环可以完整跑通，结论为 PROCEED。
- 约 1 天 / 4 小时完成骨架 Slice，为生产排期提供了有用速度参考。
- 显式的 `Initialize -> Bind -> Begin` 三阶段流程解决了 Godot 节点时序问题。
- 程序化 UI 可以满足 Slice，但正式 UI 应改为 `.tscn` 场景 + C# 逻辑绑定。
- 对话推进应改为全屏点击 + Space / Enter，而不是显式“继续”按钮。
- 战斗还需要回合提示、当前行动提示、转场、动画、音效、键盘 / 手柄 InputMap，才能代表正式手感。

## 验证价值

- Godot 4.6.3 + C# 可以支撑本项目核心玩法循环。
- Burst+Read 战斗、心境选择、朦胧化反馈可以连接成一个可理解的玩家体验。
- 当前系统边界足够清晰，可以进入 Foundation 与 Core 的生产规划。
- 该 Slice 可作为创建 Foundation / Core epics 和 stories 的依据。

## 已知限制

- 试玩证据目前只有 self-play；Production 门禁仍需要独立玩家试玩记录。
- Slice 的视觉与音频表现很稀疏，不能验证最终氛围或战斗冲击力。
- UI 功能可用，但结构不是正式生产结构。
- 敌人、招式和场景数据仍是硬编码 / 受控配置，需要正式的数据驱动路径。

## 带入正式设计的决定

- 正式生产代码应重新开始，不能把该原型直接升级成生产架构。
- 保留显式管理器初始化阶段作为生产架构经验。
- 核心 UI 面应在生产阶段转成 Godot 场景化 UI。
- 下一个可玩版本应优先补齐对话输入、战斗 HUD 清晰度、转场、动画和音频。
- 该 Slice 可作为 Foundation / Core 规划依据，但不能单独证明 Production 门禁已通过。

## 参考文件

- `REPORT.md`
- `project.godot`
- `scenes/Main.tscn`
- `scripts/`
