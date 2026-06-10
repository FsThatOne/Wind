---
title: HUD Design — 战斗与探索
project: 风止 (Wind Stops)
status: Draft
version: 1.0
author: UX Lead
created: 2026-06-09
last_updated: 2026-06-09
platform_targets: PC (Steam), Steam Deck
accessibility_tier: Standard (WCAG 2.1 AA)
related:
  - design/ux/interaction-patterns.md
  - design/accessibility-requirements.md
  - design/gdd/systems/combat-system.md
  - design/gdd/systems/mood-system.md
  - design/gdd/systems/misunderstanding-system.md
  - docs/architecture/adr-0011-combat-ui-animation.md
  - docs/architecture/adr-0013-cutscene-system.md
style_reference: design/art/art-bible.md § HUD Visual Language (待创建)
---

# HUD Design: 风止

> **Scope**: 本文档定义所有在玩家直接控制角色时叠加在游戏世界上的 UI 元素。
> 包括：资源条、战斗指令面板、伤害数字、状态图标、字幕、交互提示、通知 Toast、心境指示器。
> 不包括：菜单画面、暂停界面、背包、设置（这些使用独立 ux-spec）。

---

## 1. HUD Philosophy

**与屏幕信息的关系**：

风止的 HUD 是一层「水墨薄纱」——它不解释世界，而是暗示世界。默认状态下，我们用意象（墨色浓淡、气韵流动、符号隐喻）传达信息，而非数字。玩家沉浸在文学化的江湖中，HUD 的存在感应如纸上留白：你知道它在，但视线自然落在画面本身。

这并非为难玩家，而是尊重叙事节奏。当数值成为焦点，战斗变成计算；当意象成为焦点，战斗变成对弈。数值化模式是「揭示层」——对需要精确数据的玩家，一键掀开薄纱，露出底层真相。两种模式等价有效，不存在"更好"的选择。

**可比参照**：
- 《十三机兵防卫圈》（叙事态极简 → 战术态全展开）
- 《只狼》（精简但关键信息一目了然）
- 《Hades》（Contextual + 视觉反馈优先于数字）

**Visibility Principle — 默认策略：CONTEXTUAL**

信息按"当前是否影响决策"动态显现：
- **探索态**：HUD 近乎隐形（仅保留交互提示 + 心境指示器边缘微光）
- **战斗态 (Read 阶段)**：关键资源 + 阶段指示器显现，招式面板待命
- **战斗态 (Burst 阶段)**：全 HUD 激活，行动面板前置，倒计时可见
- **演出锁定**：HUD 全部隐藏（ADR-0013 LockMode.Full），仅保留字幕

**The Rule of Necessity**：

> "一个 HUD 元素获得存在权，当且仅当——移除它后，玩家在当前阶段无法做出关键决策，或必须中断心流去查找同等信息。"

此规则的推论：
- 探索时 HP 不显示（没有持续伤害源，受伤时短暂闪现即可）
- 战斗中内力始终显示（每个招式都需要内力决策）
- 心境永远以某种形式存在（它是叙事核心，但不一定是数字——可以是颜色/氛围）

**朦胧化 vs 数值化的分界**：

| 信息层 | 朦胧化模式 (默认) | 数值化模式 (辅助) |
|--------|-------------------|-------------------|
| HP | 条形（无数字），颜色从翠→枯暗示危险程度 | 条形 + "245/300" |
| 内力 | 条形 + 段位标记（如水位线） | 条形 + "80/120" |
| 气势 (Burst) | 分段填充（如墨点逐颗亮起） | 分段 + "3/5" |
| 伤害 | 敌方动作幅度 + 画面震感（大伤害=大反馈） | 飘字 "−47 ⚔" |
| 心境 | 抽象意象（颜色/粒子/角色微表情） | 双轴坐标 + 数值 |
| 状态效果 | 角色身上视觉变化（中毒=绿雾，眩晕=星花） | 图标行 + 倒计时 |
| 行动选择 | 招式名 + 简短诗意描述 | 招式名 + 伤害预估 + 命中率 |

**设计约束**：
1. 朦胧化模式必须提供足够信息完成所有难度的战斗（不能是"hard mode"）
2. 数值化模式不增加任何信息优势，仅改变呈现形式
3. 两种模式切换为即时生效（热键或设置），无需重启战斗
4. 教程在首次战斗时告知两种模式存在，不引导偏好

---

## 2. Information Architecture

| 信息类型 | Always Show | Contextual | On-Demand | Hidden (环境/角色表演) | 理由 |
|----------|:-----------:|:----------:|:---------:|:---------------------:|------|
| **HP (生命)** | | ✓ | | | 探索态无持续伤害→隐藏；战斗态始终显示；受伤时闪现 3s |
| **内力** | | ✓ | | | 战斗态始终显示（每招消耗决策）；探索态隐藏 |
| **气势 (Burst 蓄力)** | | ✓ | | | 仅战斗态 Burst 相关阶段显示 |
| **敌方 HP** | | ✓ | | | 战斗中目标锁定时显示；探索态隐藏 |
| **敌方状态效果** | | ✓ | | | 战斗中目标锁定时显示 |
| **己方状态效果** | | ✓ | | | 有 Buff/Debuff 时显示，消失后 2s 隐藏 |
| **回合阶段 (Burst/Read)** | | ✓ | | | 仅战斗态显示 |
| **行动指令面板** | | ✓ | | | 仅 Burst 阶段玩家回合显示 |
| **心境 (Mood 双轴)** | ✓ (微弱) | | | | 始终以某种形式存在（探索=边缘微光；战斗=更明确） |
| **伤害数字** | | ✓ | | | 仅数值化模式下战斗内显示 |
| **伤害反馈 (朦胧化)** | | | | ✓ | 默认通过画面震动/动作幅度传达 |
| **对话字幕** | ✓ (对话中) | | | | 对话进行时始终显示（a11y §5.1） |
| **交互提示 (Context Prompt)** | | ✓ | | | 进入交互半径时显示 |
| **小地图/指南针** | | | | | **不适用**——非开放世界，无需导航 HUD |
| **任务/目标提示** | | ✓ | | | 目标变更时短暂显示 5s，或 Tab 键查看 |
| **Toast 通知** | | ✓ | | | 事件触发时 4-6s |
| **教程提示** | | ✓ | | | 首次遭遇新机制时显示，不重复 |
| **武学招式详情** | | | ✓ | | Focus 800ms Tooltip / 角色面板查看 |
| **经验/等级进度** | | | ✓ | | 角色面板查看；升级时 Toast 通知 |
| **金钱/材料** | | | ✓ | | 背包/商店界面查看 |
| **存档/系统状态** | | | ✓ | | 暂停菜单查看 |
| **好感度/关系值** | | | | ✓ | 通过角色表演、对话态度传达；数值化模式下见闻录可查 |
| **误会进度** | | | | ✓ | 刻意隐藏精确进度（信息差设计核心） |
| **NPC 日程/位置** | | | | ✓ | 通过 NPC 实际出现位置传达 |

**分类决策原则**：

1. **Always Show**：仅心境（极微弱形式）和字幕（a11y 强制）
2. **Contextual**：战斗相关信息全部 Contextual——进入战斗显现，离开战斗消失
3. **On-Demand**：不影响即时决策的系统数据（经验、金钱、装备详情）
4. **Hidden**：叙事体验核心（好感、误会）——刻意不给玩家精确控制感，保护"朦胧化"设计意图

> **数值化模式的影响**：开启后，所有 "Hidden (环境表演)" 中的数值型信息升级为 "On-Demand"（可通过见闻录查看），但不升级为 "Always Show"。误会系统进度即使数值化模式也仅显示阶段（Stage 1/2/3），不显示精确百分比。

---

## 3. Layout Zones

### 3.1 Zone Diagram

**探索态 (Exploration)**

```
┌─────────────────────────────────────────────────────────┐
│ [Safe Zone Margin]                                       │
│  ┌───────────────────────────────────────────────────┐  │
│  │                                                   │  │
│  │  TL                                          TR   │  │
│  │  (空)                          (Toast 通知队列)   │  │
│  │                                                   │  │
│  │                                                   │  │
│  │                   游 戏 世 界                      │  │
│  │                                                   │  │
│  │                                                   │  │
│  │  BL                                          BR   │  │
│  │  心境微光                    Context Prompt        │  │
│  │                                                   │  │
│  │  ─────────────── BC (字幕区) ───────────────────  │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

**战斗态 (Combat)**

```
┌─────────────────────────────────────────────────────────┐
│ [Safe Zone Margin]                                       │
│  ┌───────────────────────────────────────────────────┐  │
│  │  TL                    TC                    TR   │  │
│  │  己方资源条       回合阶段指示器      Toast 通知   │  │
│  │  (HP/内力/气势)   (Burst|Read)                    │  │
│  │                                                   │  │
│  │               CL                    CR            │  │
│  │         敌方状态区            己方状态效果图标行    │  │
│  │       (HP+锁定框)                                 │  │
│  │                                                   │  │
│  │                   伤害数字                         │  │
│  │                (世界空间浮动)                      │  │
│  │                                                   │  │
│  │  BL                    BC                    BR   │  │
│  │  心境指示器            字幕区         行动指令面板  │  │
│  │  (增强态)              (战斗台词)     (Burst 时)   │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### 3.2 Zone Specification Table

| Zone ID | 位置锚点 | 尺寸 (1080p) | 最大元素数 | 主要元素 | 态 |
|---------|----------|-------------|-----------|---------|-----|
| TL | Top-Left | 320×120 dp | 3 条 | HP / 内力 / 气势 | Combat |
| TC | Top-Center | 240×64 dp | 1 | 回合阶段指示器 | Combat |
| TR | Top-Right | 280×200 dp | 3 条 | Toast 通知队列 | Both |
| CL | Center-Left | 240×80 dp | 1 | 敌方资源条 + 锁定框 | Combat |
| CR | Center-Right | 320×48 dp | 6 图标 | 己方状态效果图标行 | Combat |
| BL | Bottom-Left | 128×128 dp | 1 | 心境指示器 | Both |
| BC | Bottom-Center | 640×80 dp | 2 行 | 对话字幕 / 战斗台词 | Both |
| BR | Bottom-Right | 360×240 dp | 6 按钮 | 行动指令面板 | Combat (Burst) |
| World | 世界空间 | — | ≤8 | 伤害数字、Context Prompt | Both |

### 3.3 Safe Zone Margins

| 平台/模式 | 外边距 (各边) | 理由 |
|-----------|-------------|------|
| PC Fullscreen (1080p) | 48 dp | 标准 TV-safe 适配 |
| PC Windowed | 24 dp | 窗口边框已提供视觉分隔 |
| Steam Deck (720p) | 40 dp | 较小屏幕需保留边缘呼吸空间，避免与系统通知重叠 |

**附加规则**：
- 所有 Zone 定位使用 **Anchor + Margin** 方式，不使用绝对像素坐标
- 720p (Steam Deck) 下 TL 区域缩放 0.85x，BR 行动面板按钮从 6 减为 4 可见（滚动访问其余）
- Zone 之间最小间距 16 dp，防止元素视觉粘连
- 伤害数字 (World Zone) 不受 Safe Zone 约束，跟随世界空间坐标

---

## 4. HUD Element Specifications

### 4.1 Element Overview Table

| # | Element | Zone | Visibility | Data Source | Update Freq | Overlap Priority | A11y Alt (数值化) |
|---|---------|------|-----------|-------------|-------------|-----------------|-------------------|
| 1 | HP Bar | TL | Contextual (Combat) | PlayerStats.HP | Per-hit | 100 | "{current}/{max} 生命" |
| 2 | 内力 Bar | TL | Contextual (Combat) | PlayerStats.Neili | Per-action | 100 | "{current}/{max} 内力" |
| 3 | 气势 Bar | TL | Contextual (Combat Burst) | PlayerStats.Qishi | Per-turn | 100 | "{current}/{max} 气势" |
| 4 | 敌方 HP Bar | CL | Contextual (Lock-on) | EnemyStats.HP | Per-hit | 90 | "敌方 {name}: {pct}%" |
| 5 | 回合阶段指示器 | TC | Contextual (Combat) | CombatStateMachine | Phase-change | 95 | "当前阶段: {phase}" |
| 6 | 行动指令面板 | BR | Contextual (Burst+PlayerTurn) | AbilityRegistry | Turn-start | 110 | 焦点导航读出招式名 |
| 7 | 伤害数字 | World | Contextual (数值化 only) | DamageEvent | Per-event | 80 | aria-live 播报 |
| 8 | 状态效果图标 | CR | Contextual (Active buffs) | StatusEffectManager | Add/Remove/Tick | 85 | "{name} 剩余 {t}s" |
| 9 | 字幕 | BC | Always (对话中) | DialogueManager | Per-line | 120 | 同文本内容 |
| 10 | Context Prompt | World | Contextual (Range) | InteractionZone | Enter/Exit | 70 | Steam Input Glyph + 动作名 |
| 11 | Toast | TR | Contextual (Event) | EventBus | On-publish | 60 | aria-live polite |
| 12 | 心境指示器 | BL | Always (微弱) | MoodSystem | Value-change | 50 | "心境: {axis1}/{axis2}" |

> **Overlap Priority**：数值越高越不可被遮挡。字幕 (120) > 行动面板 (110) > 资源条 (100)。

---

### 4.2 Player Resource Bar (HP / 内力 / 气势)

**引用**: [Pattern: 3.5 Health/Resource Bar](interaction-patterns.md#35-healthresource-bar)

**布局**：Zone TL，纵向排列（HP → 内力 → 气势），间距 8 dp。

| 属性 | HP | 内力 | 气势 |
|------|-----|------|------|
| 尺寸 | 280×16 dp | 280×12 dp | 280×12 dp (分段) |
| 朦胧化外观 | 翠→枯渐变（无数字） | 水位线分段 | 墨点逐颗亮起 |
| 数值化外观 | 渐变 + "245/300" 右对齐 | 水位 + "80/120" | 分段 + "3/5" |
| Ghost 追赶条 | ✓ (Slow 400ms) | ✗ | ✗ |
| 危险阈值 | ≤25% 脉冲 + 颜色偏枯 | ≤1 段（不足释放最低招式） | — |
| Reduce Motion | 脉冲→静态高亮边框 | — | — |

**显隐逻辑**：
- 探索态：隐藏（受伤时淡入 3s → 淡出 Normal 200ms）
- 战斗态：Alpha 1.0 常驻
- 演出态 (LockMode.Full)：Alpha → 0 (Fast 100ms)

---

### 4.3 Enemy Resource Bar

**布局**：Zone CL，水平条 + 锁定框（目标名称居中于条上方）。

| 属性 | 规格 |
|------|------|
| 尺寸 | 200×14 dp |
| 显示条件 | 战斗中 + 目标锁定 |
| 多目标切换 | LB/RB 切换目标，条形交叉淡入 (Fast 100ms) |
| 朦胧化 | 仅条形 + 颜色暗示（满=翠，空=枯） |
| 数值化 | 条形 + "{pct}%" 右侧 |
| Boss 特化 | 条形扩展至 360×18 dp，多段血条（每段不同颜色渐变） |
| 锁定框视觉 | 四角亮线 (64×64 dp)，随目标世界坐标移动（ScreenSpace 投射） |

**敌方状态效果**：显示在 Enemy Bar 下方，最多 4 图标（溢出使用 "+N" 折叠标记）。

---

### 4.4 Turn Order / Phase Indicator (Burst / Read)

**布局**：Zone TC，水平居中。

| 状态 | 视觉表现 |
|------|---------|
| Read 阶段 | 柔和墨色标签 "观" + 水墨涟漪动画（Cinematic 600ms loop） |
| Burst 阶段 | 明亮标签 "动" + 墨点爆裂 (Fast 100ms 入场) |
| 阶段切换 | "观"→"动": 墨滴坠落过渡 (Normal 200ms)；"动"→"观": 水面归平 (Slow 400ms) |
| Burst 倒计时 | 朦胧化：圆弧收缩（无数字）；数值化：圆弧 + 秒数 "{t}s" |
| Reduce Motion | 墨点爆裂→简单颜色切换 (Instant)；涟漪→静态边框 |

**数值化模式追加**：在阶段标签下显示 "Turn {n}/{total}" 小字。

---

### 4.5 Action Command Panel

**引用**: [Pattern: 3.4 Ability/Skill Icon](interaction-patterns.md#34-abilityskill-icon)

**布局**：Zone BR，仅在 Burst 阶段 + 玩家回合时显示。

| 属性 | 规格 |
|------|------|
| 容器 | VBoxContainer，最大 6 条目可见 |
| 每条目结构 | [图标 56×56] + [招式名] + [内力消耗] |
| 基础行动 | 攻击 / 防御 / 道具 / 逃跑（固定底部 4 按钮） |
| 导航 | 方向键上下切换焦点；确认键执行 |
| 朦胧化 | 招式名 + 诗意一行描述（"寒江独钓——冻结敌方气脉"） |
| 数值化 | 招式名 + 伤害预估 + 命中率 + 内力消耗（"寒江独钓 \| ~47伤 \| 85% \| 消耗30"） |
| 不可用态 | 灰度 + 斜杠覆盖 + focus 时 Tooltip 说明原因 |
| 入场动画 | 从右侧滑入 (Normal 200ms, ease-out) |
| 退场动画 | 选定后其余项淡出 (Fast 100ms) |
| Reduce Motion | 滑入→直接出现 (Instant)；淡出→直接隐藏 |
| Focus 规则 | 面板出现时自动 grab_focus 第一个可用招式 |

---

### 4.6 Damage Numbers

**引用**: [Pattern: 3.6 Damage Number](interaction-patterns.md#36-damage-number)

**布局**：World Zone（跟随受击者世界坐标，向上飘动）。

| 属性 | 规格 |
|------|------|
| 显示条件 | 仅数值化模式 (朦胧化模式下伤害通过画面震感传达) |
| 6 类型 | 物理伤害 (白)、内力伤害 (蓝)、暴击 (黄+放大1.3x)、治疗 (绿+↑)、格挡减免 (灰+缩小)、状态伤害 (紫) |
| 生命周期 | spawn → 飘升 60dp (Normal 200ms) → 停留 300ms → 淡出 (Fast 100ms) |
| 累加合并 | 500ms 内同类型同目标合并为一次显示（数字递增动画） |
| 同屏上限 | ≤8 个（超出时最旧的立即淡出） |
| 字号 | 普通 24sp / 暴击 32sp / 弱化(格挡) 18sp |
| CanvasLayer | 91 (HUD 层)，但使用世界坐标投射 |
| Reduce Motion | 飘升→原地出现+淡出；暴击缩放→仅颜色+字号区分 |

### 4.7 Status Effect Icons

**引用**: [Pattern: 2.9 Tooltip](interaction-patterns.md#29-tooltip)

**布局**：Zone CR，水平排列（从左到右按剩余时间降序）。

| 属性 | 规格 |
|------|------|
| 图标尺寸 | 32×32 dp，间距 4 dp |
| 最大显示数 | 6 图标（溢出显示 "+N" 折叠按钮，Focus 展开完整列表） |
| 双轨编码 | 图标形状 + 边框颜色（Buff=金, Debuff=紫, 中立=灰）+ 倒计时文字 |
| Tooltip 触发 | Focus 800ms / 鼠标 Hover 500ms → 显示效果名、描述、来源、剩余时间 |
| 朦胧化差异 | 无倒计时数字，仅图标 + 边框颜色 + 消散动画暗示即将结束 |
| 数值化差异 | 图标右下角叠加倒计时 "{t}s"，精确到秒 |
| 新增动画 | 从右侧弹入 (Fast 100ms, ease-out-back) |
| 移除动画 | 缩小至 0 + 淡出 (Fast 100ms) |
| Reduce Motion | 弹入→直接出现；消散→直接移除 |
| 优先级规则 | Debuff 优先于 Buff 显示；同类按剩余时间短→长排列 |

---

### 4.8 Dialogue / Subtitle Overlay

**引用**: [Pattern: 3.1 Dialogue Box](interaction-patterns.md#31-dialogue-box)

**布局**：Zone BC，底部居中，宽度 640 dp，最大 2 行。

| 属性 | 规格 |
|------|------|
| CanvasLayer | 91 (HUD)；演出期间提升至 95 (Cutscene 层) |
| 文本类型 | NPC 对话 / 内心独白(斜体) / 旁白(居中) / 战斗台词(顶部偏移) |
| 字号 | 正文 20sp / 说话人名 16sp (Bold) |
| 背景 | 半透明黑色底板 (70% alpha)，保证对比度 ≥4.5:1 |
| 打字机效果 | visible_ratio Tween，30 字/秒，标点停顿 (逗号 100ms / 句号 200ms) |
| 跳过/快进 | 确认键：未完成→立即展开；已完成→下一句 |
| 战斗内特化 | 战斗台词显示在 TC 区域下方（不遮挡行动面板），持续 2s 自动消失 |
| 演出锁定 | LockMode.Full 期间字幕始终可见，其他 HUD 隐藏 |
| Reduce Motion | 打字机→直接全文显示 |
| a11y | focus_mode=NONE（不可聚焦），内容同步推送至 Screen Reader |

---

### 4.9 Context Action Prompt (探索态)

**引用**: [Pattern: 3.2 Context Action Prompt](interaction-patterns.md#32-context-action-prompt)

**布局**：World Zone，跟随可交互对象世界坐标（偏移 Y+48dp 居于对象上方）。

| 属性 | 规格 |
|------|------|
| 触发距离 | 1.5m（InteractionZone Area3D radius） |
| 结构 | [Steam Input Glyph 32×32] + [动作文字 "交谈" / "检查" / "拾取"] |
| 淡入 | Alpha 0→1 (Fast 100ms) |
| 淡出 | 离开范围后 Alpha 1→0 (Fast 100ms) |
| 多交互竞争 | 仅显示最近的一个；切换时交叉淡入 |
| 手柄/键鼠切换 | Glyph 实时切换（Steam Input API Action Glyph） |
| 探索态专属 | 战斗态/演出态自动隐藏 |
| Reduce Motion | 淡入→直接出现 |

---

### 4.10 Toast Notifications

**引用**: [Pattern: 2.8 Toast](interaction-patterns.md#28-toast)

**布局**：Zone TR，纵向堆叠（新 Toast 从顶部推入，旧 Toast 下移）。

| 属性 | 规格 |
|------|------|
| CanvasLayer | 93 (Toast 层) |
| 最大同屏 | 3 条（超出时最旧的立即退场） |
| 单条尺寸 | 最大 280×48 dp（自适应文本长度） |
| 结构 | [类型图标 24×24] + [文本 1 行] |
| 持续时间 | 4-6s（按文本长度计算：字数 ÷ 4 + 2s，clamp 4-6） |
| 入场 | 从右侧滑入 (Normal 200ms) |
| 退场 | 向右滑出 + 淡出 (Normal 200ms) |
| focus_mode | NONE（不可聚焦，不中断玩家操作） |
| 战斗优先级 | 战斗中仅显示高优先级 Toast（升级、Boss 触发）；低优先级延迟到战斗结束 |
| Reduce Motion | 滑入→直接出现；滑出→直接消失 |
| a11y | aria-live="polite"，Screen Reader 在下一个空闲时刻播报 |

---

### 4.11 心境指示器 (Mood Indicator)

**布局**：Zone BL，128×128 dp 固定区域。

| 属性 | 规格 |
|------|------|
| 数据来源 | MoodSystem 双轴（Axis1: 情绪强度, Axis2: 情绪正负） |
| 朦胧化 (默认) | 抽象意象——颜色渐变圆 + 微粒子流动方向暗示当前心境；探索态仅边缘微光可见 |
| 数值化 | 双轴坐标图 (迷你雷达图) + 数值标注 "{axis1}/{axis2}" |
| 探索态 | Alpha 0.3，仅边缘发光色暗示（不引人注意但可感知） |
| 战斗态 | Alpha 0.8，尺寸不变，颜色/粒子更活跃（心境影响战斗增益时闪烁） |
| 心境变化动画 | 颜色渐变 (Slow 400ms) + 粒子方向切换 |
| 误会系统关联 | 误会触发时短暂「墨色翻涌」特效 (Cinematic 600ms)，不显示具体数值 |
| Reduce Motion | 粒子→静态颜色块；闪烁→边框高亮 |
| 具体视觉形态 | **待 Art Bible 确定**（候选：水墨圆 / 双轴雷达 / 抽象粒子球）→ 参见 Q1 |

---

## 5. State Transitions

### 5.1 探索态 → 战斗态

| 步骤 | 时间 | 动作 |
|------|------|------|
| 1 | 0ms | 战斗触发信号 → 画面短暂闪白 (Instant) |
| 2 | 0–200ms | 探索 HUD 元素淡出 (Normal 200ms)：Context Prompt、心境微光 |
| 3 | 200–400ms | 战斗 HUD 元素滑入：TL 资源条从左滑入、TC 阶段指示器从顶淡入 (Normal 200ms) |
| 4 | 400–600ms | 敌方锁定框从目标位置缩放弹出 (Normal 200ms, ease-out-back) |
| 5 | 600ms | 过渡完成，HUD 进入战斗态常驻 |

**Burst 开始追加**：行动面板从右侧滑入 (Normal 200ms)，自动 grab_focus。

### 5.2 战斗态 → 演出态 (LockMode.Full)

| 步骤 | 时间 | 动作 |
|------|------|------|
| 1 | 0ms | GameStateLock 信号 → CancelStack 锁定 |
| 2 | 0–100ms | 全 HUD 元素 Alpha → 0 (Fast 100ms) |
| 3 | 100ms+ | 仅字幕层 (CanvasLayer 95) 保持可见 |
| 4 | 演出结束 | 恢复信号 → HUD 按 5.1 逻辑反向恢复 (Normal 200ms) |

### 5.3 战斗态 → 探索态 (战斗胜利/逃跑)

| 步骤 | 时间 | 动作 |
|------|------|------|
| 1 | 0ms | 结算画面（独立 Screen，非 HUD） |
| 2 | 结算关闭后 0–200ms | 战斗 HUD 整体淡出 (Normal 200ms) |
| 3 | 200–400ms | 探索 HUD 恢复：心境指示器淡入至 Alpha 0.3 (Normal 200ms) |
| 4 | 400ms | 过渡完成 |

### 5.4 Reduce Motion 替代方案

| 标准过渡 | Reduce Motion 替代 |
|----------|-------------------|
| 滑入/滑出 | 直接出现/消失 (Instant) |
| 缩放弹出 | 直接出现 (Instant) |
| 闪白 | 跳过 |
| 淡入/淡出 | 保留但缩短至 Fast 100ms |
| 墨点爆裂 | 颜色切换 (Instant) |

---

## 6. Accessibility Integration

### 6.1 数值化模式变化总览

| 元素 | 朦胧化 → 数值化 变化 |
|------|---------------------|
| HP Bar | +数字叠加层 "{current}/{max}" |
| 内力 Bar | +数字叠加层 |
| 气势 Bar | +分数叠加层 "{n}/{max}" |
| 敌方 HP | +百分比文字 |
| 伤害反馈 | 画面震感 → 飘字数字 |
| 心境指示器 | 抽象意象 → 双轴数值 |
| 状态效果 | +倒计时秒数 |
| 行动面板 | 诗意描述 → 伤害预估 + 命中率 |
| 阶段指示器 | +Turn 计数 |
| 好感/关系 | Hidden → On-Demand (见闻录可查) |

**切换动画**：所有数值叠加层以 Fast 100ms 淡入，不打断当前操作。

### 6.2 焦点管理 (战斗 HUD)

**引用**: [Pattern: 4.1 Focus Management](interaction-patterns.md#41-focus-management)

- 战斗 HUD 非 Modal——不实现 Focus Trap（玩家可自由切换系统菜单）
- Burst 阶段行动面板出现时自动 `grab_focus()`
- Read 阶段无可聚焦 HUD 元素（焦点回归游戏世界）
- Tab 键：循环聚焦 己方状态图标 → 敌方状态图标（查看 Tooltip）
- 手柄 Focus：LB/RB 切换目标；方向键操作行动面板

### 6.3 Reduce Motion 对战斗 HUD 的影响

参见 §5.4 替代方案表 + 各元素 spec 中的 "Reduce Motion" 行。

**核心原则**：Reduce Motion 不减少信息量，仅将运动转化为即时状态变化。所有 Contextual 显隐逻辑保持不变。

### 6.4 Screen Reader 集成

| 元素 | 播报策略 |
|------|---------|
| HP 变化 | 阈值播报：≤50% / ≤25% / ≤10% 各播报一次 "生命值低" |
| 阶段切换 | 即时播报 "进入观察阶段" / "进入行动阶段" |
| 行动面板 | Focus 时读出招式名 + 消耗 |
| 伤害 (数值化) | aria-live 批量播报（500ms 合并窗口） |
| Toast | aria-live="polite" |
| 状态效果 | 新增/移除时播报一次 |

---

## 7. Performance Budget

### 7.1 CanvasLayer 分配

| Layer | 用途 | 备注 |
|-------|------|------|
| 91 | HUD (战斗+探索) | 主层，所有 HUD 元素默认 |
| 92 | Modal | 非 HUD（暂停/背包等） |
| 93 | Toast | 通知浮层 |
| 94 | Loading | 加载遮罩 |
| 95 | Cutscene/字幕 | 演出锁定期间字幕 |

### 7.2 刷新频率策略

| 类型 | 策略 | 示例 |
|------|------|------|
| Event-driven | 仅数据变化时更新 | HP Bar, 状态图标, Toast |
| Per-frame (动画中) | Tween 驱动 | Ghost 追赶条, 倒计时圆弧 |
| Polling (低频) | 每 0.5s 检查 | 心境指示器颜色渐变 |

### 7.3 帧预算约束

| 指标 | 预算 | 来源 |
|------|------|------|
| UI 总 CPU 开销 | ≤2ms / frame | interaction-patterns.md §6 |
| 同屏 Tween 上限 | ≤8 个 | interaction-patterns.md §6 |
| 伤害数字同屏 | ≤8 个 | §4.6 |
| Toast 同屏 | ≤3 条 | §4.10 |
| 状态图标同屏 | ≤6 个 (己方) + ≤4 个 (敌方) | §4.7 / §4.3 |
| Draw Call 目标 | HUD 整体 ≤15 draw calls | Godot CanvasItem batching |

### 7.4 Steam Deck 特化

- 720p 渲染 → HUD 使用独立 Viewport（不受世界分辨率缩放影响）
- TL 区域缩放 0.85x（§3.3）
- 行动面板可见条目 4 → 滚动（§4.5）
- 粒子效果（心境指示器）降低粒子数 50%

---

## 8. Open Questions

| # | 问题 | 状态 | 决定 |
|---|------|------|------|
| Q1 | 心境指示器的具体视觉形态（水墨圆/双轴雷达/抽象粒子）？ | 待 Art Bible | — |
| Q2 | 敌方 Resource Bar 显示在头顶还是屏幕固定位置？ | 待原型验证 | — |
| Q3 | Burst 阶段倒计时是否显示精确秒数还是仅进度条？ | 已决定 | 朦胧化=圆弧；数值化=圆弧+秒数 (§4.4) |
| Q4 | 多敌人战斗时状态图标溢出如何处理（折叠 vs 滚动 vs 优先级裁切）？ | 已决定 | "+N" 折叠 (§4.7) |
| Q5 | 战斗内是否允许暂停菜单？（影响 HUD Focus 规则） | 待 GDD 确认 | — |
| Q6 | 心境对战斗增益的视觉反馈形式？ | 待 Combat GDD | — |
| Q7 | 720p 下行动面板裁剪为 4 条目是否影响平衡性？ | 待 QA 验证 | — |

---

## 9. Audit History

| 日期 | 版本 | 审计人 | 变更 |
|------|------|--------|------|
| 2026-06-09 | 0.1 | AI (ux-design) | 创建骨架文件 |
| 2026-06-09 | 1.0 | AI (ux-design) | 全 8 章节填充完成 (Section 1-8) |
