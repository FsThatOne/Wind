# 音乐 / 音效系统 (Audio System)

> **Status**: Designed
> **Author**: user + game-designer, audio-director, sound-designer
> **Last Updated**: 2026-06-07
> **Implements Pillar**: 沉浸叙事 / 武侠氛围

## Overview

音乐/音效系统是《风止》的情绪基底层，通过**三轨音频架构**（BGM 层 · 环境音层 · 音效层）为全部 13 个下游系统提供音频服务。

**架构层面**：系统管理三条独立音频轨道的混音、淡入淡出与优先级仲裁。BGM 层负责场景音乐与自适应战斗音乐；环境音层负责天气、时辰、地形等持续性音景；音效层负责战斗打击、UI 反馈、叙事触发等一次性音频事件。三轨可独立调节音量，由统一的音频总线（Master Bus）汇总输出。

**玩家体验层面**：玩家感知到的是"江南烟雨有烟雨的声音，塞北风雪有塞北的声音"——每个场景的音乐与环境音共同构建地域氛围。战斗中音乐随局势（优势/劣势/绝境/一击决胜）动态转变，强化"一读定生死"的紧张感。关键叙事节点（灭门、重逢、结局）拥有专属配乐，作为情绪记忆锚点。

核心设计原则：**Music-heavy**（以古琴/笛/箫/二胡/琵琶等中国传统乐器为主，现代电子元素作为点缀和张力补充）+ **Adaptive**（根据场景、战斗状态、叙事进程动态切换音乐段落，而非简单循环播放）。

## Player Fantasy

> *"我闭上眼睛，光靠声音就知道自己在哪——江南的雨落在青石板上、塞北的风卷过枯草、戈壁的沙砾打在斗篷上。每当古琴响起，我就知道一场改变命运的对话要来了；鼓点骤急，那是一击决胜的瞬间。这不是'背景音乐'，这是我的江湖在呼吸。"*

**情绪锚点**：
- **地域归属感**：每个区域有独属的音乐主题和环境音景，玩家仅凭听觉就能辨识地域——正如《仙剑奇侠传》系列通过配乐刻印每座城镇的记忆。
- **战斗紧张感**：音乐跟随战斗局势实时变化（蓄力→爆发→绝境→决胜），而非固定循环；灵感参考《空之轨迹》的分层战斗 BGM。
- **叙事仪式感**：关键剧情节点（灭门夜、师兄重逢、五结局）拥有一次性演奏的专属曲目，成为玩家记忆中的"那首曲子"。
- **武侠留白**：并非所有时刻都有音乐——独行荒野时只有风声和脚步，刻意的"静"也是音效设计的一部分，呼应风止山庄的"止"字哲学。

**Pillar 对齐**：服务于"沉浸叙事"支柱——音频是叙事的无字表达层，补完文字和画面无法传递的情绪维度。

## Detailed Design

### Core Rules

**R1 — 三轨音频架构**

系统维护三条并行音频轨道，各轨独立混音：

| 轨道 | 用途 | 并发 | 循环 |
|------|------|------|------|
| **BGM 轨** | 场景音乐、战斗音乐、演出音乐 | 1 首（含 crossfade 过渡时短暂 2 首） | 是（无限循环或按脚本播放） |
| **Ambient 轨** | 地形基底音、天气叠加、时辰叠加 | 最多 3 层叠加（地形 + 天气 + 时辰） | 是（无缝循环） |
| **SFX 轨** | 战斗打击、UI 反馈、叙事触发、信使等 | 最多 8 路同时播放 | 否（一次性触发） |

三轨汇入 **Master Bus**，玩家可在设置中独立调节 BGM / Ambient / SFX / Master 四档音量。

**R2 — BGM 管理**

1. 任意时刻只有一首 BGM 处于主播放状态。
2. 切换 BGM 时使用 crossfade：旧曲 `fade_out_ms` 淡出，新曲 `fade_in_ms` 淡入，两段可重叠。默认 `fade_out_ms = 1500`，`fade_in_ms = 800`。
3. **Override 栈**：演出系统 / 战斗系统可临时 override BGM。override 结束后自动 restore 到栈中上一首。栈深度最大 3 层（Exploration → Combat → Cutscene）。
4. 场景切换时，若新场景 BGM 与当前 BGM 相同，不重新播放（无缝续播）。
5. 特殊标记 `bgm_id = "SILENCE"` 表示刻意无音乐（如灭门开头场景），此时淡出当前 BGM 但不播放新曲。

**R3 — 自适应战斗音乐**

战斗 BGM 由多个水平分层段落（horizontal segments）组成，根据战斗局势动态切换：

| 战斗局势 | 音乐段落 | 触发条件 |
|---------|---------|---------|
| 准备 (Prep) | 低张力前奏 | 进入战斗、选择招式阶段 |
| 交锋 (Clash) | 中等张力主旋律 | Burst 阶段开始 |
| 优势 (Advantage) | 旋律高扬 + 节奏加快 | 连续 2 回合命中克制 |
| 劣势 (Disadvantage) | 低沉压抑 + 节奏放慢 | HP < 30% 或被连续克制 |
| 绝境 (Desperation) | 极简音色 + 心跳节奏 | HP < 15% 且 stamina < 20% |
| 决胜 (Finisher) | 全频爆发 + 完整主题 | 一击决胜触发 |

- 段落切换在小节线（bar boundary）处执行，避免中途切断旋律。
- Boss 战使用独立 BGM 资源（不共享普通战斗段落），但遵循相同的 6 段结构。
- 战斗结束后 `fade_out_ms = 2000` 淡出战斗 BGM，restore 场景 BGM。

**R4 — 环境音分层**

Ambient 轨同时叠加最多 3 层循环音：

| 层级 | 内容 | 来源系统 |
|------|------|---------|
| 地形基底 | 河流水声 / 竹林风声 / 市集人声 / 荒漠风声 | 地图/场景管理 |
| 天气叠加 | 雨声（小/大）/ 风声 / 雪声 / 雷声 | 地图/场景管理 |
| 时辰叠加 | 晨鸟 / 午蝉 / 昏鸦 / 夜虫 | 自然日+体力 |

- 各层独立淡入淡出，默认 `ambient_fade_ms = 2000`。
- 场景切换时地形基底必换，天气/时辰层若无变化则保持。
- 季节变化不单独触发音效，通过环境音层渐变过渡。

**R5 — SFX 优先级与并发**

当 SFX 并发达到上限（8 路）时，按优先级淘汰最低优先级的正在播放 SFX：

| 优先级 | SFX 类型 | 可被淘汰 |
|--------|---------|---------|
| P0 (最高) | 叙事关键音效（灭门、结局、顿悟触发） | 否 |
| P1 | 战斗核心音效（一击决胜、克制命中） | 否 |
| P2 | 战斗常规音效（普通命中、回合标记） | 是 |
| P3 | UI/系统音效（选择确认、书信展开） | 是 |
| P4 (最低) | 环境 one-shot（脚步、远处鸟叫） | 是 |

- 同一 SFX 在 `sfx_cooldown_ms`（默认 50ms）内不重复触发，防止叠音。
- 同一来源的 SFX 最多同时 2 路（如连续两击命中可叠加，但第 3 击等前一个结束）。

**R6 — 音乐留白**

以下场景设计为**刻意无 BGM**，仅保留环境音和关键 SFX：
- 灭门场景开头（main-narrative 要求"极度安静→渐入悲伤主旋律"）
- 独行荒野探索段落（Player Fantasy"武侠留白"）
- 重大选择后的短暂静默（2-3 秒，让玩家消化）
- 诀别演出尾声（弦乐渐弱→风声→静默）

留白由 `bgm_id = "SILENCE"` 触发，Ambient 轨正常运行。

**R7 — 演出音频接管**

CG/演出系统通过 `PLAY_BGM` 和 `PLAY_SFX` 步骤直接控制音频轨道：
- `PLAY_BGM` 将新 BGM 压入 override 栈，演出结束自动 restore。
- `PLAY_SFX` 使用 P0 优先级，不可被淘汰。
- 演出被跳过时，所有音频立即中断（`fade_out_ms = 200`），BGM 栈回退到演出前状态。
- 演出期间的 `PARALLEL` 步骤允许多个 SFX 同时触发（不受 SFX cooldown 限制）。

### States and Transitions

系统在以下 6 种音频状态间切换：

| 状态 | BGM 行为 | Ambient 行为 | SFX 行为 |
|------|---------|-------------|---------|
| **EXPLORATION** | 场景 BGM 播放 | 全部 3 层活跃 | 正常优先级仲裁 |
| **COMBAT** | 战斗自适应 BGM override | 淡出至 20% 音量 | 战斗 SFX 优先 |
| **CUTSCENE** | 演出脚本接管 | 淡出至 0%（除非脚本保留） | 仅演出脚本触发 |
| **DIALOGUE** | BGM 继续（降至 60% 音量） | 继续（降至 40% 音量） | 仅对话 SFX（极少量） |
| **MENU** | BGM 继续（降至 40% 音量） | 继续（降至 30% 音量） | 仅 UI SFX |
| **SILENCE** | 无 BGM | 正常 | 正常 |

**状态转换规则**：

| 来源 → 目标 | 触发条件 | BGM 过渡 | Ambient 过渡 |
|------------|---------|---------|-------------|
| EXPLORATION → COMBAT | 战斗开始 | crossfade 1500ms | 淡出至 20% / 1000ms |
| COMBAT → EXPLORATION | 战斗结束 | crossfade 2000ms | 恢复至 100% / 1500ms |
| * → CUTSCENE | play_cutscene() | 由演出脚本控制 | 淡出 800ms |
| CUTSCENE → 前状态 | 演出结束 | restore 栈 / 1000ms | 恢复 / 1000ms |
| * → DIALOGUE | 对话开始 | 降音量 / 500ms | 降音量 / 500ms |
| DIALOGUE → 前状态 | 对话结束 | 恢复音量 / 500ms | 恢复音量 / 500ms |
| * → MENU | 打开菜单 | 降音量 / 300ms | 降音量 / 300ms |
| * → SILENCE | bgm_id = "SILENCE" | 淡出 / 2000ms | 不变 |

### Interactions with Other Systems

| # | 系统 | 方向 | 接口 | 说明 |
|---|------|------|------|------|
| 2 | 回合制战斗 | ← 接收 | `battle_state_changed(state)` | 接收战斗局势变化，驱动自适应 BGM 段落切换 |
| 2 | 回合制战斗 | ← 接收 | `combat_sfx(sfx_id, priority)` | 播放克制命中、一击决胜等战斗音效 |
| 7 | 战斗 UI | ← 接收 | `ui_sfx(sfx_id)` | 播放招式选择、破绽爆满等 UI 音效 |
| 5 | 对话系统 | ← 接收 | `dialogue_state(started/ended)` | 进入/退出 DIALOGUE 状态，调整音量 |
| 5 | 对话系统 | ← 接收 | `dialogue_sfx(sfx_id)` | 选择确认、洞察暗示、书信展开等极少量 SFX |
| 9 | 主线叙事 | ← 接收 | `narrative_bgm(bgm_id, fade_in_ms)` | 章节/结局专属 BGM 切换 |
| 12 | 地图/场景管理 | ← 接收 | `scene_audio_config(bgm_id, terrain_ambient, weather_ambient)` | 场景切换时设定 BGM 和环境音 |
| 11 | 自然日+体力 | ← 接收 | `time_period_changed(period)` | 时辰变化，切换时辰环境音层 |
| 11 | 自然日+体力 | ← 接收 | `stamina_sfx(sfx_id)` | 力竭循环音 / 恢复音 |
| 20 | CG/演出 | ← 接收 | `cutscene_play_bgm(bgm_id, fade_in_ms)` / `cutscene_play_sfx(sfx_id, volume)` | 演出脚本直接控制音频 |
| 10 | NPC 状态管理 | ← 接收 | `messenger_sfx(messenger_type, action)` | 信使系统音效（飞来/落地/展信） |
| 13 | 感情系统 | ← 接收 | `romance_motif(character_id, action)` | 女主 motif 进入/退出、偶遇 BGM 渐弱渐强 |
| 14 | 朦胧化 UI | ← 接收 | `zone_shift_ambient(from_zone, to_zone)` | 心境区域切换时环境音调性微调 |
| 6 | 心境双轴 | ← 接收 | `mindset_sfx(event_type)` | 转念事件 SFX |
| 1 | 角色属性 | ← 接收 | `breakthrough_sfx(realm_level)` | 境界突破专属音效（8 段递进） |
| 17 | 顿悟突破 | ← 接收 | `epiphany_audio(phase)` | 顿悟触发（钟声+降音量）/ 突破成功（三段音效）/ 稳妥（短音效） |
| 16 | 活江湖层 | ← 接收 | `world_pulse()` | 世界脉搏提示音 |
| 18 | 误会系统 | ← 接收 | `misunderstanding_sfx(event_type)` | 关系变化暗示 / 窗口紧迫 / 澄清 / 定型音效 |
| 19 | 探索/洞察 | ← 接收 | `insight_sfx(event_type)` | 洞察提示 / 追查成功 / 重要线索发现 |
| 3 | 武学组合 | ← 接收 | `martial_sfx(event_type)` | 残卷合成 / 修炼完成 / 真传获得音效 |
| 8 | 存档系统 | ← 接收 | `save_failed_sfx()` | 存档失败纸张皱起音效（仅失败时） |
| 15 | 物品/道具 | ← 接收 | `item_sfx(event_type)` | 拾取/使用/丢弃音效（待物品 GDD 确认） |
| 23 | 设置/选项 | ↔ 双向 | `volume_changed(track, value)` | 玩家调整音量，音频系统响应 |

> 音频系统是纯**被动服务方**——所有 21 个上游系统向音频系统发送播放请求，音频系统不主动查询任何系统状态。唯一例外是设置系统（双向：音频系统读取/写入音量配置）。

## Formulas

**F1 — Crossfade 音量曲线**

BGM 切换时，旧曲和新曲的音量随时间按等功率曲线（Equal-power crossfade）变化：

`volume_old(t) = cos(t / fade_out_ms × π/2)`
`volume_new(t) = sin(t / fade_in_ms × π/2)`

| 变量 | 类型 | 范围 | 说明 |
|------|------|------|------|
| t | int | 0 – max(fade_out_ms, fade_in_ms) | 过渡已经过的毫秒数 |
| fade_out_ms | int | 200 – 3000 | 旧曲淡出时长 |
| fade_in_ms | int | 200 – 3000 | 新曲淡入时长 |

**输出范围**：0.0 – 1.0（乘以轨道音量系数后输出到 Master Bus）
**示例**：fade_out_ms = 1500, t = 750 → volume_old = cos(0.5 × π/2) ≈ 0.707（-3dB 中点）

**F2 — 状态音量衰减**

进入 DIALOGUE / MENU / COMBAT 状态时，非主导轨道按目标系数衰减：

`effective_volume(track) = base_volume(track) × state_attenuation(track, state)`

| 变量 | 类型 | 范围 | 说明 |
|------|------|------|------|
| base_volume | float | 0.0 – 1.0 | 玩家在设置中配置的轨道音量 |
| state_attenuation | float | 0.0 – 1.0 | 当前状态对该轨道的衰减系数 |

state_attenuation 默认值：

| 状态 | BGM 衰减 | Ambient 衰减 | SFX 衰减 |
|------|---------|-------------|---------|
| EXPLORATION | 1.0 | 1.0 | 1.0 |
| COMBAT | 1.0 | 0.2 | 1.0 |
| CUTSCENE | 由脚本控制 | 0.0 | 由脚本控制 |
| DIALOGUE | 0.6 | 0.4 | 1.0 |
| MENU | 0.4 | 0.3 | 1.0 |
| SILENCE | 0.0 | 1.0 | 1.0 |

**F3 — 自适应战斗段落切换阈值**

`combat_segment = evaluate_combat_state(hp_ratio, stamina_ratio, advantage_streak, finisher_triggered)`

| 变量 | 类型 | 范围 | 说明 |
|------|------|------|------|
| hp_ratio | float | 0.0 – 1.0 | 当前 HP / 最大 HP |
| stamina_ratio | float | 0.0 – 1.0 | 当前内息 / 最大内息 |
| advantage_streak | int | 0 – N | 连续克制命中回合数 |
| finisher_triggered | bool | — | 是否触发一击决胜 |

判定逻辑（按优先级从高到低）：
1. `finisher_triggered == true` → **Finisher**
2. `hp_ratio < 0.15 AND stamina_ratio < 0.20` → **Desperation**
3. `hp_ratio < 0.30 OR 被连续克制 >= 2` → **Disadvantage**
4. `advantage_streak >= 2` → **Advantage**
5. Burst 阶段进行中 → **Clash**
6. 否则 → **Prep**

**输出**：6 种段落 enum 之一。切换仅在当前小节结束时生效。

## Edge Cases

- **E1 — Override 栈溢出**：如果 BGM override 栈已达 3 层（Exploration → Combat → Cutscene），新的 override 请求将替换栈顶而非再压入。确保栈深度永远 <= 3。

- **E2 — 极短场景过渡**：如果玩家在 BGM crossfade 未完成时触发新场景切换（如快速穿过过渡区域），立即中断当前 crossfade，以新场景 BGM 为目标重新开始 fade_in。不会出现"三曲叠播"。

- **E3 — 战斗中触发演出**：Combat 状态下 play_cutscene() 被调用时，战斗 BGM 入栈保留，演出 BGM 压入栈顶。演出结束后 restore 到战斗 BGM 的当前段落（非从头播放）。

- **E4 — 演出跳过时的音频清理**：玩家跳过演出后，200ms 内淡出所有演出音频，BGM 栈回退，Ambient 恢复。不允许出现"跳过后突然安静 2 秒再恢复"的空隙——restore 立即执行。

- **E5 — 全局静音**：玩家在设置中将 Master 音量设为 0 时，所有音频计算正常运行（状态机仍在切换、crossfade 仍在计时），但最终输出为 0。恢复音量后从当前正确状态继续播放，无需重新初始化。

- **E6 — SFX 洪水**：如果在单帧内收到 > 8 条 SFX 请求（如连续多个系统同时触发），按优先级排序，仅播放前 8 条中优先级最高的。P0 和 P1 类 SFX 如果超过 8 路也必须播放（临时突破上限），确保叙事关键音效不丢失。

- **E7 — 相同 BGM 重复请求**：如果 `scene_audio_config` 请求播放的 BGM 与当前正在播放的 BGM 相同（bgm_id 匹配），跳过 crossfade，保持当前播放位置不变。

- **E8 — 环境音层缺失**：如果场景配置中某个 Ambient 层（天气/时辰）的音频资源 ID 为空或 null，该层静默（volume = 0），其他层正常叠加。不因单层缺失而清空整个 Ambient 轨。

- **E9 — 顿悟触发时的音量覆盖**：顿悟系统要求"环境音量降低 50% 持续 1.5s"。如果此时已处于 COMBAT 状态（Ambient 已衰减至 20%），则进一步衰减至 10%（20% × 50%），而非从 100% 计算。衰减始终相对于当前有效音量。

- **E10 — 女主 motif 与场景 BGM 冲突**：偶遇触发 `romance_motif(character_id, "enter")` 时，场景 BGM 渐弱至 30%（非淡出至 0），motif 作为临时 BGM overlay 叠加播放。偶遇结束后 motif 淡出、场景 BGM 恢复 100%。motif 不进 override 栈（它是叠加层，不是替代层）。

## Dependencies

### 硬依赖（系统无法正常运作如果缺失）

| 上游系统 | 接口 | 性质 |
|---------|------|------|
| **地图/场景管理** (#12) | `scene_audio_config(bgm_id, terrain_ambient, weather_ambient)` | 场景 BGM 和地形环境音的唯一来源。缺失则音频系统无法知道"现在在哪"。 |
| **回合制战斗** (#2) | `battle_state_changed(state)` + `combat_sfx(sfx_id, priority)` | 自适应战斗音乐的驱动来源。缺失则战斗期间无法切换音乐段落、无法播放战斗音效。 |
| **设置/选项** (#23) | `volume_changed(track, value)` | 音量持久化读写。缺失则玩家无法调节音量（硬编码默认值可工作但体验不完整）。 |

### 软依赖（增强体验但缺失不影响核心运转）

| 上游系统 | 接口 | 说明 |
|---------|------|------|
| CG/演出 (#20) | `cutscene_play_bgm` / `cutscene_play_sfx` | 演出音频接管。无演出时音频系统独立运行。 |
| 自然日+体力 (#11) | `time_period_changed(period)` / `stamina_sfx` | 时辰环境音和体力音效。缺失则无昼夜音景变化。 |
| 对话系统 (#5) | `dialogue_state` / `dialogue_sfx` | 对话期间音量调节。缺失则对话时 BGM 不降音量。 |
| 主线叙事 (#9) | `narrative_bgm(bgm_id, fade_in_ms)` | 章节/结局专属 BGM。缺失则使用默认场景 BGM。 |
| NPC 状态管理 (#10) | `messenger_sfx(messenger_type, action)` | 信使音效。缺失则信使到达无声。 |
| 感情系统 (#13) | `romance_motif(character_id, action)` | 女主 motif 叠加。缺失则偶遇无专属旋律。 |
| 朦胧化 UI (#14) | `zone_shift_ambient(from_zone, to_zone)` | 心境区域环境音微调。缺失则无感知。 |
| 心境双轴 (#6) | `mindset_sfx(event_type)` | 转念 SFX。缺失则无音效反馈。 |
| 角色属性 (#1) | `breakthrough_sfx(realm_level)` | 境界突破 SFX。缺失则突破无声。 |
| 顿悟突破 (#17) | `epiphany_audio(phase)` | 顿悟触发/成功音效。缺失则顿悟无声。 |
| 活江湖层 (#16) | `world_pulse()` | 世界脉搏提示音。缺失则无感知。 |
| 误会系统 (#18) | `misunderstanding_sfx(event_type)` | 误会状态音效。缺失则无感知。 |
| 探索/洞察 (#19) | `insight_sfx(event_type)` | 洞察提示 SFX。缺失则无音效线索。 |
| 武学组合 (#3) | `martial_sfx(event_type)` | 残卷/修炼 SFX。缺失则无声。 |
| 战斗 UI (#7) | `ui_sfx(sfx_id)` | 战斗 UI 音效。缺失则战斗 UI 无声。 |
| 存档系统 (#8) | `save_failed_sfx()` | 存档失败 SFX。缺失则失败无声。 |
| 物品/道具 (#15) | `item_sfx(event_type)` | 物品操作 SFX。缺失则无声。 |

### 下游被依赖

无。音频系统是叶子节点——没有其他系统依赖音频系统的输出来运行。

## Tuning Knobs

| 参数 | 默认值 | 安全范围 | 过高风险 | 过低风险 |
|------|--------|---------|---------|---------|
| `bgm_fade_out_ms` | 1500 | 200 – 3000 | 过渡太慢，玩家感觉卡顿 | 过渡太突兀，音乐像被切断 |
| `bgm_fade_in_ms` | 800 | 200 – 3000 | 新曲进入太慢，空档期太长 | 新曲突然出现，听觉冲击 |
| `ambient_fade_ms` | 2000 | 500 – 5000 | 环境音切换太缓慢 | 环境音突然跳变，不自然 |
| `sfx_max_concurrent` | 8 | 4 – 16 | CPU 负担增加，可能卡顿 | 高密度战斗时音效丢失 |
| `sfx_cooldown_ms` | 50 | 0 – 200 | 快速连击时音效被吞掉 | 相同音效叠加产生刺耳共振 |
| `sfx_same_source_max` | 2 | 1 – 4 | 同一来源音效叠加过多 | 快速事件只有一个声音，缺乏连续感 |
| `combat_bgm_restore_fade_ms` | 2000 | 500 – 4000 | 战斗结束后过渡太慢 | 过渡太快，缺乏战后余韵 |
| `dialogue_bgm_attenuation` | 0.6 | 0.3 – 0.8 | BGM 太响盖过对话氛围 | BGM 太弱丢失场景感 |
| `dialogue_ambient_attenuation` | 0.4 | 0.1 – 0.6 | 环境音干扰阅读节奏 | 环境音消失感觉进入"真空" |
| `combat_ambient_attenuation` | 0.2 | 0.0 – 0.4 | 战斗中环境音干扰打击感 | 完全静音后战斗结束恢复跳感太大 |
| `motif_bgm_duck_ratio` | 0.3 | 0.1 – 0.5 | 场景 BGM 太响盖过 motif | 场景 BGM 消失感觉断裂 |
| `override_stack_max` | 3 | 2 – 4 | 栈过深恢复时行为难以预测 | 嵌套场景（战斗中演出）无法正确恢复 |
| `silence_bgm_fade_ms` | 2000 | 1000 – 4000 | 淡入静默太慢失去冲击力 | 太快像 bug |
| `bar_boundary_tolerance_ms` | 100 | 50 – 300 | 段落切换延迟太大，状态变化感知滞后 | 切换点太精确，无法对齐小节 |

**关键交互**：
- `dialogue_bgm_attenuation` 和 `dialogue_ambient_attenuation` 需要联合调整——两者差值过大会让玩家感觉"进入另一个空间"。
- `sfx_max_concurrent` 和 `sfx_cooldown_ms` 互相制约——放宽并发就需要收紧 cooldown，否则音频总线过载。

## Visual/Audio Requirements

本系统**即是**音频系统，因此此章节定义音频资产的制作规格与风格指南：

### 音乐风格指南

| 维度 | 规格 |
|------|------|
| **主乐器** | 古琴（主旋律/留白）、竹笛（明朗/江南）、洞箫（悲伤/孤寂）、二胡（哀怨/北方）、琵琶（激烈/战斗） |
| **辅助乐器** | 古筝（装饰音）、埙（远古/洞穴）、鼓（战斗节奏）、铜磬（仪式感） |
| **现代元素** | 环境电子音垫（低频 pad）、合成器弦乐（高潮叠加）、电子鼓点（boss 战增强）——仅作为底层支撑，不成为主导 |
| **禁止** | 西洋管弦乐队编制、摇滚吉他、电子舞曲节奏、流行人声 |
| **情绪基调** | 克制→爆发循环。日常以留白和极简为主，高潮段落全频爆发后迅速回归静默 |

### 音效风格指南

| 来源 | 风格 | 示例 |
|------|------|------|
| 战斗打击 | Foley 质感 + 武器物理特性 | 刚=金属劈裂、柔=布帛旋涡、巧=利器瞬闪 |
| UI 操作 | 纸竹金属有机音 | 纸张翻动、细竹击响、绢布展卷 |
| 叙事仪式 | 乐器独奏短句 | 古琴单音拉长（一击决胜）、铜磬一声（破绽爆满） |
| 环境 | 自然录音 + 少量加工 | 真实雨声、风声、鸟鸣，不使用合成白噪音替代 |
| 信使系统 | 动物音 + 动作音 | 翅膀扑击（按鸟种区分音色）、轻落地、纸卷摩擦 |

### 技术规格

| 参数 | 值 |
|------|------|
| 采样率 | 44.1kHz（BGM）/ 44.1kHz（SFX） |
| 位深 | 16-bit（发布）/ 24-bit（源文件） |
| 格式 | OGG Vorbis（BGM/Ambient，流式加载）/ WAV（SFX，预加载） |
| BGM 平均时长 | 2-4 分钟循环 |
| SFX 平均时长 | 0.3-2.0 秒 |
| 响度标准 | -14 LUFS（BGM）/ -12 LUFS（SFX 峰值） |

### 音频资产总量估算

| 大类 | 数量 |
|------|------|
| BGM（场景/昼夜变体） | ~20 首 |
| BGM（叙事专属：序章+灭门+章节4+结局5+大地图2+boss1） | ~14 首 |
| BGM（女主 motif） | 3 首短旋律 |
| 环境音循环 | ~16 条 |
| 一次性 SFX（战斗/UI/叙事/信使/物品等） | ~100 条 |
| **总计** | ~153 条音频资产 |

## UI Requirements

音频系统自身无游戏内 HUD 展示，但通过**设置/选项**系统暴露以下 UI：

| UI 元素 | 位置 | 行为 |
|---------|------|------|
| Master 音量滑块 | 设置 → 音频 | 0-100，实时预览，持久化到存档 |
| BGM 音量滑块 | 设置 → 音频 | 0-100，实时预览 |
| 环境音音量滑块 | 设置 → 音频 | 0-100，实时预览 |
| SFX 音量滑块 | 设置 → 音频 | 0-100，实时预览 |

> 无需为音频系统单独设计 UX spec——音量控制嵌入设置系统 UI。

## Acceptance Criteria

- **AC1**: GIVEN 玩家进入新场景, WHEN 场景 BGM 与前一场景不同, THEN 旧 BGM 在 1500ms 内淡出、新 BGM 在 800ms 内淡入，过渡期间无明显音频空白或爆音。
- **AC2**: GIVEN 玩家进入战斗, WHEN battle_state 从 Prep → Clash → Advantage 依次变化, THEN 音乐段落在小节线处无缝切换，无明显卡顿或重复。
- **AC3**: GIVEN 一击决胜触发, WHEN finisher_triggered = true, THEN Finisher 段落在 bar_boundary_tolerance_ms 内开始播放，同时播放 P1 优先级音效。
- **AC4**: GIVEN 演出正在播放, WHEN 玩家长按跳过, THEN 所有演出音频在 200ms 内淡出，BGM 栈正确回退到演出前状态，无 2 秒以上静默间隙。
- **AC5**: GIVEN Master 音量 = 0, WHEN 场景切换/战斗/演出正常进行, THEN 音频状态机正常运转，恢复音量后音频从正确的当前状态播放。
- **AC6**: GIVEN 对话开始, WHEN dialogue_state("started") 触发, THEN BGM 在 500ms 内降至 60% 音量、Ambient 降至 40% 音量。对话结束后各轨恢复。
- **AC7**: GIVEN 单帧内 12 个 SFX 请求同时到达, WHEN 其中 2 个为 P0、3 个为 P1, THEN P0 和 P1 全部播放（5 个），剩余从 P2-P4 按优先级选取至 sfx_max_concurrent 上限。
- **AC8**: GIVEN 江南场景 + 小雨天气 + 午时, WHEN 三层 Ambient 同时激活, THEN 可同时听到河流水声（地形基底）+ 轻柔雨声（天气叠加）+ 蝉鸣（时辰叠加），三层独立可辨。
- **AC9**: GIVEN 女主偶遇触发, WHEN romance_motif(character_id, "enter") 到达, THEN 场景 BGM 渐弱至 30%、女主 motif 淡入播放。偶遇结束后 BGM 恢复 100%、motif 淡出。
- **AC10**: GIVEN 设置中调节 BGM 滑块, WHEN 滑块从 80 调至 30, THEN BGM 音量实时变化（无需重新加载），退出设置后保持新音量。

## Open Questions

| # | 问题 | 影响 | 所有者 | 目标解决时间 |
|---|------|------|--------|------------|
| OQ1 | 是否需要支持 Partial VO（关键台词配音）？对话系统预留了该接口，但当前定位为"纯文字 + BGM"。如启用，需额外规划 VO 录制流程和音频资产量。 | 资产量可能翻倍 | narrative-director | Architecture 阶段 |
| OQ2 | 自适应战斗音乐的"小节线对齐"实现方式？需要预制固定 BPM 的音乐段落 vs. 运行时节拍检测？前者限制音乐创作自由度但实现简单。 | 技术复杂度 + 音乐制作流程 | audio-director | Architecture 阶段 |
| OQ3 | Boss 战是否需要独立的 6 段自适应结构，还是可以用 2-3 段简化结构（只区分常规/低血/决胜）？全 6 段意味着每个 boss 需要 6 段音乐资产。 | 资产量 × boss 数量 | game-designer | GDD review 阶段 |
| OQ4 | 音频资源的内存预算？~153 条资产是否需要按场景分组流式加载，还是全部常驻内存？需配合引擎内存分析。 | 性能 + 加载时间 | engine-programmer | Architecture 阶段 |
