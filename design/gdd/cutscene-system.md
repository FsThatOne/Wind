# CG / 演出系统 (Cutscene System)

> **System**: #20 CG / 演出
> **Status**: Designed
> **Priority**: Alpha
> **Depends On**: #9 主线叙事, #2 回合制战斗
> **Author**: user + agents
> **Created**: 2026-06-07

---

## Overview

CG/演出系统是《风止》的**全屏演出序列播放器**——它接收来自其他系统（主线叙事、战斗、顿悟、感情、心境等）的演出请求，按照预定义的脚本序列播放画面、动画、音效和文字，然后将控制权交还调用方。

**它是一个纯播放器，不是内容定义者**。具体演出的触发条件由各来源系统定义（如 combat-system 定义何时触发一击决胜演出，main-narrative 定义何时播放章节过渡），演出的具体内容（画面、镜头、时序）通过**演出脚本**（CutsceneScript）配置。本系统只负责：读取脚本 → 按时序执行每个步骤 → 管理演出期间的游戏状态锁定 → 处理跳过 → 通知调用方演出完成。

从玩家视角，演出系统是"把武侠小说名场面搬上像素屏幕"的核心手段——一击决胜时的慢动作特写、章节过渡时的水墨题字、终幕结局的插画叙述、顿悟时的内心世界闪回。**每一段演出都是玩家情绪的放大器**：紧张的战斗因一击决胜演出而爆发、伤感的离别因诀别演出而铭刻、绝处逢生的顿悟因演出而成为玩家口耳相传的高光时刻。

核心设计原则：
- **演出是叙事节拍的可视化**：每段演出对应一个叙事情感拐点，不为炫技而演出
- **可跳过但不可忽视**：玩家可跳过任何演出，但跳过后系统确保所有 gameplay 副作用（flag 设置、状态变更、物品发放）仍然正确执行
- **武侠美学统一**：所有演出共享水墨像素美学基调，通过演出类型区分表现层次（全屏 CG 插画 → 全屏像素演出 → 半屏局部演出 → 内嵌微演出）

## Player Fantasy

**"那一刻，世界为我这一招停了下来。"**

你在第三回合终于看穿了对手的套路——他的每一次刚系进攻都是为了逼你后退，而你一直在用柔系化解。当破绽累积到顶点，你选择了"断水寒光"。画面骤然减速，镜头从全景拉到特写，你的尺身划过一道弧光，对手的表情从自信变为错愕——然后画面定格，一声金属碰撞的余韵拉长，墨色晕开，收招。

**这不只是"一个动画在播放"**——这是武侠小说里那种"一招之间胜负已分"的文学美学的视觉翻译。同样的感受出现在每一个情感转折点：章节过渡时水墨题字缓缓展开，你感受到旅程的阶段感；顿悟时主角闭目凝神，内心风暴化为笔墨飞溅；结缘时两人的像素身影被月光剪出轮廓，画面安静得只剩风声和心跳。

**情感锚点**：
- **"武侠名场面"的即时反应**——一击决胜、顿悟突破、结缘诀别，这些时刻的演出让玩家发出"好燃"/"好美"/"好痛"的情绪
- **"翻到精彩章节"的仪式感**——章节过渡的水墨题字、终幕结局的插画叙述，让玩家感受到"故事进入新篇章"的分量
- **"回忆里的高光"**——通关后回忆游戏，玩家脑中浮现的不是数值面板，而是那些演出画面

**参考体验**：
- 《仙剑奇侠传四》—— 关键剧情的全屏 CG 插画配合 BGM 切换，每一帧都是壁纸
- 《Octopath Traveler》—— 像素美术中的"特写演出"，用有限像素创造电影感
- 《Hollow Knight》—— 极简动画中的"仪式感"瞬间（梦之钉、王之魂获取）

## Detailed Design

### Core Rules

**1. 演出类型分级**

| 层级 | 名称 | 画面形式 | 典型时长 | 可跳过 | 锁定范围 | 用例 |
|------|------|---------|---------|--------|---------|------|
| **Tier 1** | 全屏 CG 插画 | 静态/半动态手绘插画 + 文字叙述 | 10-60s | 是 | 全局（输入+系统+存档） | 终幕结局演出、关键剧情 CG |
| **Tier 2** | 全屏像素演出 | 像素角色动画 + 镜头运动 + 特效层 | 3-15s | 是 | 全局 | 一击决胜、章节过渡、顿悟、结缘/诀别、境界突破、Boss 阶段转换 |
| **Tier 3** | 半屏局部演出 | 画面局部变化（色调/滤镜/特写框） | 1-5s | 否（自动结束） | 部分（禁止输入，不禁止系统） | 转念事件、绝境爆发"狂化"氛围、破境提示 |
| **Tier 4** | 内嵌微演出 | 场景内原位动画，不切换画面 | 0.5-3s | 否（自动结束） | 无锁定 | 信使送信、客栈休息过场、洞察发现卷轴展开 |

> **设计原则**：Tier 越高，情感分量越大，生产成本越高。对应关系是"叙事拐点的重要程度 = 演出层级"。

**2. 演出脚本（CutsceneScript）**

```
CutsceneScript {
    id: string                   // 唯一标识（如 "ch1_transition", "decisive_gang"）
    tier: int                    // 演出层级（1-4）
    skippable: bool              // 是否可跳过（Tier 1-2 默认 true，Tier 3-4 默认 false）
    steps: CutsceneStep[]        // 有序步骤序列
    on_complete: GameplayEffect[] // 演出完成后执行的 gameplay 副作用
    bgm_override: string?        // 演出期间覆盖 BGM（null = 不改变）
    lock_mode: LockMode          // 锁定模式（FULL / PARTIAL / NONE）
}
```

**3. 演出步骤类型（CutsceneStep）**

每个步骤是一个原子操作，按顺序执行：

| 步骤类型 | 参数 | 说明 |
|---------|------|------|
| `SHOW_IMAGE` | image_id, position, transition(fade/slide/dissolve), duration | 显示 CG 插画或背景图 |
| `SHOW_TEXT` | text, style(narration/dialogue/title), position, duration | 显示文字（旁白/对话/章节题字） |
| `PLAY_ANIMATION` | anim_id, target(character/vfx/camera), loop | 播放像素动画或特效 |
| `CAMERA_MOVE` | target_position, zoom, duration, easing | 镜头运动（平移/缩放/跟踪） |
| `SLOW_MOTION` | time_scale(0.1-1.0), duration | 慢动作（战斗演出用） |
| `SCREEN_EFFECT` | effect_type(fade/flash/shake/ink_spread), params, duration | 全屏效果（黑屏/闪白/震动/水墨扩散） |
| `PLAY_SFX` | sfx_id, volume | 播放音效 |
| `PLAY_BGM` | bgm_id, fade_in_ms | 切换 BGM |
| `WAIT` | duration | 等待指定时间 |
| `WAIT_INPUT` | prompt_text? | 等待玩家按键继续（可选提示文字） |
| `PARALLEL` | steps: CutsceneStep[] | 并行执行多个步骤（如动画+音效同时播放） |

**4. 播放流程**

```
调用方发起请求 play_cutscene(script_id, context?)
  → 系统加载 CutsceneScript
  → 根据 lock_mode 设置游戏状态锁定
  → 按顺序执行 steps（PARALLEL 步骤内部并行）
  → 每个步骤完成后推进到下一步
  → 所有步骤完成后：
    → 执行 on_complete 的所有 GameplayEffect
    → 解除游戏状态锁定
    → 发送 cutscene_completed(script_id) 事件通知调用方
```

**5. 跳过机制**

- **Tier 1-2**（skippable = true）：玩家长按确认键 1.0 秒触发跳过
  - 跳过时显示"跳过中..."进度条（防误触）
  - 跳过后：立即中断所有视觉/音频步骤，**但完整执行 `on_complete` 的所有 GameplayEffect**
  - 跳过后的画面恢复：播放快速淡出黑屏（0.3s）→ 恢复游戏画面
- **Tier 3-4**（skippable = false）：不可跳过，时长短（≤5s），自动播完
- **首次观看保护**：可选标记 `first_view_unskippable = true`——同一存档首次观看时不可跳过，重读/二周目可跳过

**6. 演出串联**

多段演出可串联播放（如顿悟 + 境界突破）：

```
play_cutscene_chain([script_id_1, script_id_2, ...], transition_gap_ms)
```

- 演出之间插入 `transition_gap_ms`（默认 500ms）的短暂黑屏过渡
- 串联中每段演出独立可跳过（跳过当前段 → 进入下一段，不跳过整个链）
- 每段演出的 `on_complete` 在该段结束/跳过后立即执行

**7. 游戏状态锁定**

| LockMode | 禁止操作 | 允许操作 | 适用层级 |
|----------|---------|---------|---------|
| `FULL` | 玩家输入、系统 tick（自然日推进/体力消耗）、存档、飞书送达 | 跳过操作 | Tier 1-2 |
| `PARTIAL` | 玩家移动/交互输入 | 系统 tick、存档 | Tier 3 |
| `NONE` | 无 | 全部 | Tier 4 |

### States and Transitions

```
IDLE → LOADING → PLAYING → COMPLETED
                   ↓
                SKIPPING → COMPLETING_EFFECTS → COMPLETED
```

| 状态 | 含义 |
|------|------|
| **IDLE** | 无演出播放中 |
| **LOADING** | 演出资源加载中（预加载大型 CG 素材） |
| **PLAYING** | 演出正在播放，执行当前 step |
| **SKIPPING** | 玩家触发跳过，中断视觉步骤 |
| **COMPLETING_EFFECTS** | 跳过后执行 on_complete 的 GameplayEffect |
| **COMPLETED** | 演出完成，锁定已解除，事件已发送 |

### Interactions with Other Systems

| 系统 | 方向 | 接口 |
|------|------|------|
| **主线叙事** | 叙事 → 演出 | `play_cutscene("ch1_transition")` 章节过渡、`play_cutscene("death_screen")` 死亡结局、`play_cutscene("ending_*")` 终幕结局 |
| **回合制战斗** | 战斗 → 演出 | `play_cutscene("decisive_[gang/rou/qiao]")` 一击决胜演出 |
| **敌方 AI** | AI → 演出 | `play_cutscene("boss_phase_*")` Boss 阶段转换、`play_cutscene("berserk_*")` 绝境爆发 |
| **顿悟突破** | 顿悟 → 演出 | `play_cutscene_chain(["epiphany_*", "breakthrough_*"])` 顿悟+境界突破串联 |
| **角色属性** | 属性 → 演出 | `play_cutscene("breakthrough_rank_*")` 境界突破单独触发时 |
| **感情系统** | 感情 → 演出 | `play_cutscene("bond_*")` 结缘、`play_cutscene("farewell_*")` 诀别、`play_cutscene("encounter_*")` 偶遇 |
| **心境双轴** | 心境 → 演出 | `play_cutscene("turning_point_*")` 转念事件 |
| **探索/洞察** | 探索 → 演出 | `play_cutscene("discovery_scroll")` 卷轴展开发现演出 |
| **NPC 状态** | NPC → 演出 | `play_cutscene("messenger_arrive")` 信使送信 |
| **自然日+体力** | 日历 → 演出 | `play_cutscene("inn_rest")` 客栈休息过场 |
| **朦胧化 UI** | UI → 演出 | `play_cutscene("force_break_*")` 决裂演出 |
| **存档系统** | 双向 | 演出播放中禁止存档（`FULL` 锁定）；`cutscene_completed` 后可触发自动存档 |
| **音乐/音效** | 演出 → 音频 | 演出脚本内的 `PLAY_SFX`/`PLAY_BGM` 步骤通过音频系统执行 |

## Formulas

CG/演出系统是纯播放器，不涉及 gameplay 数值计算。仅有时序相关的公式：

### F1. 跳过判定

```
skip_triggered = hold_duration >= skip_hold_threshold AND script.skippable == true
```

- `hold_duration`: float, 玩家持续按住确认键的时长（秒）
- `skip_hold_threshold`: float, 默认 1.0s（见 Tuning Knobs）

### F2. 演出总时长估算（供内容设计参考）

```
estimated_duration = Σ step.duration + Σ wait.duration + Σ wait_input.avg_response
```

- 非精确公式——仅用于内容设计时估算演出长度
- `wait_input.avg_response` 按 2.0s 估算

> 本系统无 gameplay 数值公式（伤害/属性/概率均由调用方系统处理）。

## Edge Cases

| ID | 场景 | 处理 |
|----|------|------|
| E1 | 演出播放中玩家 Alt+Tab 切出游戏 | 暂停演出计时器，回到游戏时从暂停点继续（不重放、不跳过） |
| E2 | 两个系统同时请求播放演出 | 排队机制：后到的请求进入待播放队列，当前演出完成后按 FIFO 播放。不支持同时播放两段 Tier 1-2 演出 |
| E3 | 演出播放中触发战斗（如 Boss 阶段转换演出后立即进入下一阶段） | 演出 `on_complete` 可包含 `resume_combat` 效果，演出完成后自动回到战斗状态 |
| E4 | 跳过包含 `WAIT_INPUT` 步骤的演出 | 所有 `WAIT_INPUT` 步骤视为已确认，直接跳到下一步 |
| E5 | 演出脚本引用的素材缺失（开发期） | 播放占位画面（纯色+脚本 ID 文字），不崩溃，日志输出警告 |
| E6 | Tier 4 微演出与 Tier 2 演出同时触发 | Tier 2 优先，Tier 4 进入队列；或丢弃（若微演出的叙事意义在 Tier 2 完成后已过时） |
| E7 | 串联演出中途存档加载 | 演出状态不持久化——加载存档后回到 IDLE 状态，跳过正在播放的演出。`on_complete` 在存档点已执行的不重复执行 |
| E8 | `first_view_unskippable` 标记的演出，玩家二周目用新存档 | 二周目新存档 = 新的 `cutscene_viewed` 记录，首次观看仍不可跳过。只有同一存档重读才可跳过 |

---

## Dependencies

| 依赖系统 | 依赖类型 | 说明 |
|---------|---------|------|
| **#9 主线叙事** | 硬依赖 | 章节过渡、死亡结局、终幕结局的演出脚本触发 |
| **#2 回合制战斗** | 硬依赖 | 一击决胜演出的战斗上下文（招式类型、角色位置） |
| **#21 音乐/音效** | 软依赖 | 演出脚本中的 `PLAY_SFX`/`PLAY_BGM` 步骤需要音频系统执行 |
| **#12 地图/场景管理** | 软依赖 | 场景内 Tier 4 微演出需要场景坐标和摄像机控制 |
| **#8 存档系统** | 软依赖 | `cutscene_viewed` 记录持久化（用于 `first_view_unskippable`）；FULL 锁定期间阻止存档 |

---

## Tuning Knobs

| 参数 | 默认值 | 范围 | 说明 |
|------|--------|------|------|
| `skip_hold_threshold` | 1.0s | 0.5-2.0 | 长按跳过所需时长 |
| `skip_fadeout_duration` | 0.3s | 0.1-0.5 | 跳过后黑屏淡出时长 |
| `chain_transition_gap` | 500ms | 200-1000 | 串联演出间的黑屏过渡时长 |
| `loading_timeout` | 3.0s | 1.0-5.0 | 素材加载超时（超时后用占位画面） |
| `tier4_max_duration` | 3.0s | 1.0-5.0 | Tier 4 微演出最大时长限制 |
| `tier3_max_duration` | 5.0s | 2.0-8.0 | Tier 3 局部演出最大时长限制 |

---

## Visual/Audio Requirements

### 视觉需求

| 资产类别 | 预估数量 | 规格 |
|---------|---------|------|
| **终幕结局 CG 插画** | 5 张基础 × 5 变体 = 25 张 | 全屏分辨率（待定），静态/半动态 |
| **章节过渡画面** | 4 套（序章→一章、一→二、二→三、三→终幕） | 水墨晕染 + 章节名题字动画 |
| **死亡结局画面** | 1 套统一 | 水墨散开 + 画面褪色 |
| **一击决胜特写** | 3 套（刚/柔/巧） | 像素角色特写动画 + 慢动作 |
| **境界突破** | 8 段 | 各境界不同的全屏演出（character-attributes.md 已定义） |
| **顿悟演出** | 凝神版 + 冥想版 | epiphany-breakthrough.md 已定义视觉规格 |
| **结缘/诀别/偶遇** | 每女主 1 套结缘 + 1 套诀别 = 6+随机偶遇 | romance-system.md 已定义 |
| **转念事件** | 通用 1 套 | 水墨晕散/聚合（mindset-dual-axis.md 已定义） |
| **信使送信** | 通用 1 套（可选角色特性信使变体） | npc-state.md 已定义 |
| **客栈休息过场** | 通用 1 套 | 渐暗→时间文字→渐亮（natural-day-stamina.md 已定义） |
| **发现卷轴展开** | 通用 1 套 | exploration-insight.md 已定义 |

### 音效需求

| 触发点 | 描述 |
|--------|------|
| 章节题字展开 | 毛笔落纸声 + 纸张展开声 |
| 死亡结局 | 低沉弦乐渐弱 + 水墨散开声 |
| 一击决胜 | 武器碰撞余韵拉长 + 力量感爆发 |
| 跳过演出 | 轻微"嗖"声（反馈操作完成） |

> 其余演出音效在各来源系统 GDD 中已定义，本系统按脚本配置播放。

---

## UI Requirements

- **跳过进度条**：屏幕右下角，细长进度条，长按时填充，松开时立即消失
- **"按住跳过"提示**：Tier 1-2 演出开始 2 秒后，右下角淡入小字提示（可在设置中关闭）
- **串联演出序号**：不显示——玩家不需要知道"这是第 2/3 段演出"
- **无 HUD**：所有演出播放期间隐藏全部 HUD 元素

---

## Acceptance Criteria

| AC | 描述 |
|----|------|
| AC1 | **GIVEN** 战斗中触发一击决胜，**WHEN** `play_cutscene("decisive_gang")` 被调用，**THEN** 画面切入全屏像素演出（慢动作+特写），演出完成后返回战斗画面 |
| AC2 | **GIVEN** Tier 2 演出播放中，**WHEN** 玩家长按确认键 ≥1.0 秒，**THEN** 演出被跳过，`on_complete` 的所有 GameplayEffect 正确执行 |
| AC3 | **GIVEN** Tier 2 演出播放中，**WHEN** 玩家短按确认键（<1.0 秒），**THEN** 演出不被跳过 |
| AC4 | **GIVEN** `first_view_unskippable = true` 的演出在同一存档首次播放，**WHEN** 玩家尝试跳过，**THEN** 跳过无效 |
| AC5 | **GIVEN** 顿悟+境界突破串联，**WHEN** `play_cutscene_chain` 被调用，**THEN** 两段演出依次播放，间隔 500ms 黑屏过渡，各段的 `on_complete` 分别在对应段结束后执行 |
| AC6 | **GIVEN** 两个系统同时请求 Tier 2 演出，**WHEN** 第一段正在播放，**THEN** 第二段进入队列，第一段完成后自动播放 |
| AC7 | **GIVEN** 演出播放中（FULL 锁定），**WHEN** 玩家尝试存档，**THEN** 存档被阻止 |
| AC8 | **GIVEN** Tier 4 微演出播放中，**WHEN** 玩家继续移动，**THEN** 移动不被阻止（NONE 锁定） |
| AC9 | **GIVEN** 演出脚本引用的 CG 素材缺失，**WHEN** 演出加载时，**THEN** 显示占位画面而非崩溃 |

---

## Open Questions

| OQ | 问题 | 影响 |
|----|------|------|
| OQ1 | 是否需要"演出回放"功能（在菜单中重看已观看过的 CG）？ | UI 设计 + 存档系统扩展 |
| OQ2 | 终幕结局 25 套变体是否全部需要独立 CG 插画，还是可以用"基础画面+文字变体"降低成本？ | 美术生产量 |
| OQ3 | 战斗内演出（一击决胜、顿悟）是否需要根据当前战场环境调整背景？ | 演出资源复杂度 |
