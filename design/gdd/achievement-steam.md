# 成就 / Steam 集成 (Achievement & Steam Integration)

> **Status**: Designed
> **Author**: user + agents
> **Last Updated**: 2026-06-08
> **System #**: 24
> **Priority**: Full Vision
> **Implements Pillar**: P2 (选择有重量) — 成就作为多周目激励，让每次选择的长期后果可见

## Overview

成就/Steam 集成系统是《风止》的**外围奖励层**，负责追踪玩家在叙事、战斗、心境、感情、收集、探索六大维度的里程碑事件，并通过 Steam 平台 API 将解锁状态同步至 Steam 成就列表和统计数据。

系统包含约 **50 个成就**，分布于 7 个类别（叙事、战斗、心境、感情、收集、探索、隐藏），设计目标：
1. **记录而非引导**：成就描述不剧透——使用隐晦的武侠文学式命名（如"一剑封喉"而非"击败最终Boss"）
2. **多周目驱动**：结缘互斥（同一周目最多1人）意味着至少 4 周目才能解锁全部感情成就，呼应 game-concept 中 Achievers 玩家画像
3. **平台解耦**：游戏逻辑层通过 `IPlatformAchievement` 接口与 Steam 解耦，为未来 Switch/移动端预留扩展点

## Player Fantasy

"我走过的每一步江湖路，都被刻进了某处石壁上。"

成就系统的情感目标是**回响感**——玩家在通关后翻看成就列表时，每个已解锁的成就都能唤起一段记忆。未解锁的成就则用朦胧化描述暗示"这条路你还没走过"，激发多周目探索欲。

## Detailed Design

### Core Rules

**R1. 成就注册表**

每个成就定义为一个 `AchievementDef` 条目：

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | string | 全局唯一 ID，格式 `ACH_{类别}_{序号}`，如 `ACH_NAR_01` |
| `steam_api_name` | string | Steam 后台配置的 API Name，如 `ach_prologue_complete` |
| `name_zh` | string | 中文显示名（武侠文学式） |
| `name_en` | string | 英文显示名 |
| `desc_locked_zh` | string | 未解锁时的隐晦描述 |
| `desc_unlocked_zh` | string | 解锁后的完整描述 |
| `category` | enum | NAR / CMB / MND / ROM / COL / EXP / HID |
| `hidden` | bool | 是否在未解锁时隐藏（HID 类默认 true） |
| `type` | enum | `BOOL`（一次性）/ `COUNTER`（累计型）/ `CONDITION`（多条件联合） |
| `target_value` | int | COUNTER 类的目标值；BOOL 和 CONDITION 类为 1 |
| `conditions` | string[] | CONDITION 类的子条件列表。每个元素为一个独立布尔判据 ID（格式 `ACH_*` 引用其他成就，或 `CND_*` 引用自定义判据）。语义：**全部子条件均为 true 时成就解锁**（AND 逻辑）。自定义判据在 `condition_defs.tres` 中定义，每个 CND 条目包含事件名、阈值和比较运算符 |
| `icon_locked` | string | 未解锁图标资源路径 |
| `icon_unlocked` | string | 已解锁图标资源路径 |

注册表以 **Godot Resource 文件** (`achievements.tres`) 存储，支持编辑器内编辑。

**R2. 事件追踪层**

成就追踪采用**事件订阅模式**，不主动轮询：

| 事件源 | 事件名 | 触发时机 | 监听成就类别 |
|--------|--------|---------|------------|
| 主线叙事 | `chapter_completed(chapter_id)` | 章节结束节点执行时 | NAR |
| 主线叙事 | `ending_triggered(ending_id)` | 终幕演出脚本启动时 | NAR |
| 主线叙事 | `evidence_found(evidence_id)` | 证据 flag 写入 choice_log 时 | COL |
| 战斗系统 | `battle_won(battle_id, tags)` | 战斗胜利结算后 | CMB |
| 战斗系统 | `decisive_blow_landed(battle_id)` | 一击决胜动画播放时 | CMB |
| 心境双轴 | `mindset_zone_changed(old, new)` | 心境区域切换时 | MND |
| 心境双轴 | `morality_tier_changed(old, new)` | 善恶档位跨越时 | MND |
| 感情系统 | `milestone_reached(npc_id, milestone)` | 关系里程碑达成时 | ROM |
| 感情系统 | `comet_stat_updated(npc_id, stat, value)` | 暗号/书信/传闻/偶遇计数变更时 | ROM, COL |
| 武学系统 | `martial_art_acquired(art_id, form)` | 获得残片/拓本/完本时 | COL |
| 顿悟系统 | `epiphany_triggered(realm)` | 顿悟突破成功时 | CMB |
| 探索系统 | `location_discovered(location_id)` | 新地点发现时 | EXP |
| 队伍管理 | `companion_recruited(npc_id)` | 同伴入队时 | EXP |

追踪层在 `AchievementManager.OnEvent(event)` 中统一处理：
1. 遍历注册表中未解锁的成就
2. 对每个成就检查其 `type` 和条件是否被当前事件推进
3. 若进度达到 `target_value` → 标记解锁 → 发射 `achievement_unlocked(id)` → 同步 Steam

**R3. 解锁时机与防作弊**

- 成就解锁仅在**游戏正常流程中**触发，不支持控制台/调试模式解锁
- 解锁判定在**事件回调内同步执行**，不延迟到帧末
- 解锁状态写入存档的 `achievement_progress` 字段（存档系统已支持序列化字典）
- Steam 侧使用 `SteamUserStats.SetAchievement()` + `StoreStats()` 立即同步
- **离线解锁**：解锁时若 Steam 不可用，标记 `pending_sync = true`，下次 Steam 可用时批量同步

**R4. 多周目与跨存档规则**

- 成就进度**跨存档全局累计**——存储在 `user://achievement_global.cfg`（与存档槽分离）
- 全局进度 = 所有存档中任何一个触发过的最大值
- 示例：存档 A 中与女主 A 结缘，存档 B 中与女主 B 结缘 → 全局进度记录两者均已达成
- 新开存档不重置全局进度，但单存档内的进度独立追踪（用于成就画廊显示"本周目进度"）

**R5. 成就列表**

#### 叙事类 (NAR) — 12 个

| ID | 名称 | 条件 | 类型 | 隐藏 |
|----|------|------|------|------|
| ACH_NAR_01 | 风止 | 完成序章 | BOOL | ✗ |
| ACH_NAR_02 | 初入世间 | 完成章外章 | BOOL | ✗ |
| ACH_NAR_03 | 何为仇 | 完成第一章 | BOOL | ✗ |
| ACH_NAR_04 | 何为江湖 | 完成第二章 | BOOL | ✗ |
| ACH_NAR_05 | 何为天下 | 完成第三章 | BOOL | ✗ |
| ACH_NAR_06 | 嗔·破 | 达成"嗔"结局 | BOOL | ✓ |
| ACH_NAR_07 | 清·正 | 达成"清"结局 | BOOL | ✓ |
| ACH_NAR_08 | 空·放 | 达成"空"结局 | BOOL | ✓ |
| ACH_NAR_09 | 悲·渡 | 达成"悲"结局 | BOOL | ✓ |
| ACH_NAR_10 | 隐·种 | 达成"隐"结局 | BOOL | ✓ |
| ACH_NAR_11 | 入魔 | 达成魔道 override 结局 | BOOL | ✓ |
| ACH_NAR_12 | 五径归一 | 解锁全部 5 种心境结局（嗔/清/空/悲/隐） | CONDITION | ✗ |

#### 战斗类 (CMB) — 8 个

| ID | 名称 | 条件 | 类型 | 隐藏 |
|----|------|------|------|------|
| ACH_CMB_01 | 初窥门径 | 赢得第一场战斗 | BOOL | ✗ |
| ACH_CMB_02 | 一剑封喉 | 首次触发一击决胜 | BOOL | ✗ |
| ACH_CMB_03 | 百战成钢 | 赢得至少 10 场不同 `battle_id` 的战斗且其中至少 3 场使用克制关系获胜 | CONDITION | ✗ |
| ACH_CMB_04 | 以柔克刚 | 在克制关系中使用柔系招式击败刚系敌人 | BOOL | ✗ |
| ACH_CMB_05 | 绝境求生 | HP ≤ 10% 时赢得战斗 | BOOL | ✗ |
| ACH_CMB_06 | 置之死地 | 在顿悟窗口中选择"凝神"并存活 | BOOL | ✓ |
| ACH_CMB_07 | 连战不辍 | 完成特定叙事连战段落（剧情强制连续战斗序列） | BOOL | ✗ |
| ACH_CMB_08 | 逆天改命 | 在"不该赢的战斗"中战胜对手 | BOOL | ✓ |

#### 心境类 (MND) — 6 个

| ID | 名称 | 条件 | 类型 | 隐藏 |
|----|------|------|------|------|
| ACH_MND_01 | 执念入世 | 到达"孤剑入世"区域 | BOOL | ✓ |
| ACH_MND_02 | 执念出世 | 到达"风止尘湮"区域 | BOOL | ✓ |
| ACH_MND_03 | 释怀入世 | 到达"白衣入世"区域 | BOOL | ✓ |
| ACH_MND_04 | 释怀出世 | 到达"大隐于市"区域 | BOOL | ✓ |
| ACH_MND_05 | 侠名远播 | 善恶达到"极善"(≥+30) | BOOL | ✗ |
| ACH_MND_06 | 浪子回头 | 从"极恶"(≤-30)回升到"善·明"(≥+10) | BOOL | ✓ |

#### 感情类 (ROM) — 8 个

| ID | 名称 | 条件 | 类型 | 隐藏 |
|----|------|------|------|------|
| ACH_ROM_01 | 有缘千里 | 与任一女主达成 M_ACQUAINTED | BOOL | ✗ |
| ACH_ROM_02 | 知己 | 与任一女主达成 M_HEART | BOOL | ✗ |
| ACH_ROM_03 | 比翼 | 与女主 A 结缘 (M_BOND) | BOOL | ✗ |
| ACH_ROM_04 | 双栖 | 与女主 B 结缘 | BOOL | ✗ |
| ACH_ROM_05 | 并蒂 | 与女主 C 结缘 | BOOL | ✗ |
| ACH_ROM_06 | 连理 | 与女主 D 结缘 | BOOL | ✗ |
| ACH_ROM_07 | 情深不寿 | 触发任一诀别 (M_BREAK) | BOOL | ✓ |
| ACH_ROM_08 | 天涯海角 | 全部 4 位女主至少各达成一次 M_BOND（跨存档） | CONDITION | ✗ |

#### 收集类 (COL) — 8 个

| ID | 名称 | 条件 | 类型 | 隐藏 |
|----|------|------|------|------|
| ACH_COL_01 | 残卷 | 获得第一个武学残片 | BOOL | ✗ |
| ACH_COL_02 | 集大成 | 收集全部武学完本（总数待 martial-arts-system 内容设计确定） | CONDITION | ✗ |
| ACH_COL_03 | 真相拼图 | 收集全部 10 件证据 | CONDITION | ✗ |
| ACH_COL_04 | 鸿雁传书 | 收齐任一女主的全部书信 | BOOL | ✗ |
| ACH_COL_05 | 字字珠玑 | 收齐全部女主的全部书信（跨存档） | CONDITION | ✗ |
| ACH_COL_06 | 暗号初识 | 发现第一个暗号 | BOOL | ✗ |
| ACH_COL_07 | 暗语流通 | 发现全部暗号 | CONDITION | ✗ |
| ACH_COL_08 | 江湖百晓生 | 听闻全部传闻 | CONDITION | ✗ |

#### 探索类 (EXP) — 5 个

| ID | 名称 | 条件 | 类型 | 隐藏 |
|----|------|------|------|------|
| ACH_EXP_01 | 踏遍江山 | 发现全部 18 个场景 | CONDITION | ✗ |
| ACH_EXP_02 | 澜国行 | 完成全部 3 个核心部族内容 | CONDITION | ✗ |
| ACH_EXP_03 | 澜国通 | 完成全部 6 个部族内容（含 3 支线） | CONDITION | ✗ |
| ACH_EXP_04 | 四海为家 | 招募全部可入队角色（总数待 party-management 角色线设计确定，跨存档） | CONDITION | ✗ |
| ACH_EXP_05 | 一期一会 | 在任一呼吸期内完成所有可用互动 | BOOL | ✓ |

#### 隐藏类 (HID) — 3 个

| ID | 名称 | 条件 | 类型 | 隐藏 |
|----|------|------|------|------|
| ACH_HID_01 | 命不该绝 | 在剧情杀战斗中获胜 | BOOL | ✓ |
| ACH_HID_02 | 返璞归真 | 境界达到最高"返璞归真" | BOOL | ✓ |
| ACH_HID_03 | 风止之后 | 全成就解锁 | CONDITION | ✓ |

> **合计: 50 个成就** (12+8+6+8+8+5+3)

### States and Transitions

每个成就实例有 3 种状态：

```
[Locked] ──事件推进──▶ [InProgress]（仅 COUNTER/CONDITION 类）
                            │
                    达到 target_value ──▶ [Unlocked]
                                            │
                                     Steam 同步成功 ──▶ [Synced]

[Locked] ──事件满足──▶ [Unlocked]（BOOL 类直接跳到 Unlocked）
```

全局管理器状态：

```
[Initializing] ──读取 achievement_global.cfg──▶ [Ready]
                                                   │
              Steam 不可用 ──▶ [OfflineMode]        │
                                   │                │
                            Steam 恢复 ──▶ [SyncPending] ──批量同步──▶ [Ready]
```

### Interactions with Other Systems

| 系统 | 方向 | 接口 | 说明 |
|------|------|------|------|
| 主线叙事 (#9) | 叙事 → 成就 | `EventBus.Emit("chapter_completed", id)` | 章节完成和结局触发 |
| 战斗系统 (#2) | 战斗 → 成就 | `EventBus.Emit("battle_won", id, tags)` | 战斗胜利和一击决胜 |
| 心境双轴 (#6) | 心境 → 成就 | `EventBus.Emit("mindset_zone_changed", old, new)` | 心境区域和善恶档位变更 |
| 感情系统 (#13) | 感情 → 成就 | `EventBus.Emit("milestone_reached", npc, ms)` | 关系里程碑和彗星统计 |
| 武学系统 (#3) | 武学 → 成就 | `EventBus.Emit("martial_art_acquired", id, form)` | 武学收集 |
| 顿悟系统 (#17) | 顿悟 → 成就 | `EventBus.Emit("epiphany_triggered", realm)` | 顿悟突破 |
| 探索系统 (#19) | 探索 → 成就 | `EventBus.Emit("location_discovered", id)` | 场景发现 |
| 队伍管理 (#25) | 队伍 → 成就 | `EventBus.Emit("companion_recruited", npc)` | 同伴入队 |
| 存档系统 (#8) | 双向 | `SaveSystem.achievement_progress` | 单存档进度存储 |
| 存档系统 (#8) | 成就 → 存档 | `achievement_global.cfg` | 跨存档全局进度（独立文件） |
| 设置/选项 (#23) | 无直接交互 | — | 成就画廊从暂停菜单或标题画面独立入口进入 |

**所有交互均为单向（上游系统 → 成就系统）**，成就系统是纯消费者——不向任何上游系统回写数据，不影响游戏逻辑。

### Steam 平台集成

**R6. Steamworks API 使用**

| 功能 | API | 说明 |
|------|-----|------|
| 成就解锁 | `SteamUserStats.SetAchievement(api_name)` | 设置成就为已解锁 |
| 统计数据 | `SteamUserStats.SetStat(key, value)` | 上传 COUNTER 类进度 |
| 同步 | `SteamUserStats.StoreStats()` | 批量提交到 Steam 服务器 |
| Rich Presence | `SteamFriends.SetRichPresence(key, value)` | 显示当前章节/区域 |
| 初始化 | `SteamUserStats.RequestCurrentStats()` | 启动时拉取已有进度 |

**R7. 平台抽象接口**

```csharp
public interface IPlatformAchievement
{
    void Initialize();
    void Unlock(string achievementId);
    void SetProgress(string achievementId, int current, int target);
    int GetProgress(string achievementId);
    bool IsUnlocked(string achievementId);
    void Sync();  // 批量提交
}
```

v1.0 实现 `SteamAchievementProvider`；后续 Switch/移动端实现对应 Provider。
游戏逻辑层仅依赖 `IPlatformAchievement`，不直接调用 Steamworks API。

**R8. Rich Presence 规则**

| 状态 | Rich Presence 文本 |
|------|-------------------|
| 标题画面 | "在标题画面" |
| 章节 N 探索 | "正在探索 {area_name}" |
| 战斗中 | "正在战斗" |
| 终幕 | "即将迎来结局" |

Rich Presence 文本从 Steam 后台的本地化文件加载，支持中英文。

## Formulas

成就系统不包含数值公式。所有解锁判定均为布尔/计数/条件逻辑，不涉及概率计算或数值推导。COUNTER 类进度为简单自增 `current += 1`。

## Edge Cases

- **如果 Steam 未初始化或处于离线模式**：成就仍正常解锁并写入本地 `achievement_global.cfg`，标记 `pending_sync = true`。下次 Steam 可用时 `Sync()` 批量提交。
- **如果玩家使用存档编辑器篡改 achievement_global.cfg**：Steam 侧已解锁的成就无法撤销（Steam 特性）；本地新增的虚假解锁在下次 `RequestCurrentStats()` 时会与 Steam 服务器对账——以 Steam 服务器为准。
- **如果玩家在同一帧内触发多个成就**：`OnEvent` 逐条处理，每个解锁独立发射 toast 通知。Toast 通知队列按触发顺序排列，同时最多显示 1 条，后续延迟 2 秒依次展示。
- **如果 CONDITION 类成就的子条件分布在不同存档中**：全局进度文件记录每个子条件的最大达成值，跨存档合并后判定 CONDITION 是否满足。
- **如果玩家重装游戏但保留 Steam 账号**：Steam 侧成就保持不变。本地 `achievement_global.cfg` 丢失时，从 Steam 服务器拉取已解锁列表重建本地缓存。
- **如果隐藏成就在 Steam 后台未正确标记为 hidden**：游戏内 UI 会根据 `hidden` 字段独立控制显示——不依赖 Steam 的 hidden 标志，两者应保持一致但互不覆盖。

## Dependencies

**硬依赖**：无。成就系统可在所有上游系统缺失时启动——只是没有事件源，不会解锁任何成就。

**软依赖**：

| 依赖系统 | 方向 | 说明 |
|---------|------|------|
| 心境双轴 (#6) | 单向（心境→成就） | 心境区域/善恶档位变更事件 |
| 感情系统 (#13) | 单向（感情→成就） | 关系里程碑/彗星统计事件 |
| 主线叙事 (#9) | 单向（叙事→成就） | 章节完成/结局/证据事件 |
| 战斗系统 (#2) | 单向（战斗→成就） | 战斗胜利/一击决胜事件 |
| 武学系统 (#3) | 单向（武学→成就） | 武学获取事件 |
| 顿悟系统 (#17) | 单向（顿悟→成就） | 顿悟突破事件 |
| 探索系统 (#19) | 单向（探索→成就） | 场景发现事件 |
| 队伍管理 (#25) | 单向（队伍→成就） | 同伴入队事件 |
| 存档系统 (#8) | 双向 | 单存档进度 + 全局进度文件 |

**被依赖系统**：无——成就系统是叶子节点，不被任何其他系统依赖。

## Tuning Knobs

| 旋钮 | 默认值 | 安全范围 | 说明 |
|------|--------|---------|------|
| `toast_display_sec` | 4 | 2–8 | 成就解锁通知的显示时长（秒） |
| `toast_queue_delay_sec` | 2 | 1–4 | 多个成就连续解锁时 toast 之间的间隔 |
| `sync_retry_interval_sec` | 60 | 30–300 | Steam 离线时重试同步的间隔 |
| `rich_presence_update_sec` | 30 | 15–120 | Rich Presence 更新频率 |

## Visual/Audio Requirements

| 资源 | 规格 | 说明 |
|------|------|------|
| 成就图标（已解锁） | 64×64 px, PNG, 每个成就 1 张 | 武侠水墨风格，50 张 |
| 成就图标（未解锁） | 64×64 px, PNG, 统一灰色剪影 | 1 张通用 + HID 类用 `?` 图标 |
| 解锁音效 | WAV/OGG, ≤ 1 秒 | 短促的古琴拨弦声，不打断 BGM |
| Toast 动画 | 从屏幕右上角滑入 → 停留 → 滑出 | Tween 动画，300ms ease-out |

## UI Requirements

| 元素 | 布局 | 交互 |
|------|------|------|
| **解锁 Toast** | 屏幕右上角，半透明深色底板（alpha 0.85），左侧图标 + 右侧名称/描述 | 自动消失，不可交互 |
| **成就画廊** | 从标题画面或暂停菜单进入；按类别分 tab（全部/叙事/战斗/心境/感情/收集/探索/隐藏） | 滚动列表，每项显示图标+名称+描述+解锁日期 |
| **进度统计** | 画廊顶部横条：`已解锁 N / 50`，百分比进度条 | 纯展示 |
| **隐藏成就** | 显示 `???` + "完成特定挑战后揭晓" | 解锁后正常显示 |
| **跨存档标记** | 需要跨存档的成就旁显示小图标提示 | hover 说明"此成就可通过多个存档累计解锁" |

**朦胧化 UI 联动**：成就画廊的整体视觉风格与朦胧化 UI 保持一致——使用相同的字体、色调和纹理底板。未解锁成就的描述文字使用朦胧化 UI 的"模糊文字"效果（文字可见但略带雾化），呼应"信息不完全"的设计哲学。

## Acceptance Criteria

| ID | 验收条件 |
|----|---------|
| AC-01 | 完成序章后 `ACH_NAR_01` 解锁，Steam 成就列表同步显示已解锁 |
| AC-02 | 首次触发一击决胜后 `ACH_CMB_02` 解锁，右上角 toast 通知显示 4 秒 |
| AC-03 | 同一帧触发 2 个成就时，toast 按序排队显示，间隔 2 秒 |
| AC-04 | 在存档 A 中与女主 A 结缘 + 存档 B 中与女主 B 结缘 → `ACH_ROM_08` 进度显示 2/4 |
| AC-05 | Steam 离线时解锁成就 → 重新联网后 60 秒内成就同步到 Steam |
| AC-06 | 隐藏成就（`hidden = true` 的 ACH_NAR_06-11、ACH_MND_01-04/06、ACH_CMB_06/08、ACH_HID_01-03，共 16 个）在未解锁时：名称显示 `???`、描述显示"完成特定挑战后揭晓"、图标显示 `?` 剪影；解锁后显示实际名称、描述和图标。可通过在解锁前后分别截图比对验证 |
| AC-07 | 成就画廊包含 8 个 tab（全部 / 叙事 / 战斗 / 心境 / 感情 / 收集 / 探索 / 隐藏），切换每个 tab 后列表仅显示对应 `category` 的成就；进度条数值 = 该 tab 下已解锁数 / 该 tab 总数（"全部"tab 为 N/50） |
| AC-08 | achievement_global.cfg 删除后重启游戏，从 Steam 服务器恢复已解锁列表 |
| AC-09 | 解锁通知音效播放时不中断当前 BGM |
| AC-10 | Rich Presence 在探索场景显示当前区域名，战斗中显示"正在战斗" |
| AC-11 | `ACH_NAR_12`（五径归一）在跨存档累计解锁全部 5 种心境结局（嗔/清/空/悲/隐，即 ACH_NAR_06-10）后自动解锁 |
| AC-12 | `ACH_HID_03`（风止之后）在全部 49 个非元成就解锁后自动解锁 |

## Open Questions

1. **Steam 交易卡/徽章**：是否需要设计 Steam 交易卡（Trading Cards）？需要额外 5-15 张卡面美术。属于 Steam 商店运营层面而非游戏系统设计——建议发行阶段决定。
2. **Steam 排行榜**：是否需要战斗评分排行榜？当前设计中无竞争性评分机制，与"武侠味先于游戏味"的 P3 可能冲突。建议 v1.0 不做。
3. **Steam Workshop**：是否支持 mod/自定义内容？scope 过大，建议 post-launch 考虑。
4. **成就解锁重放**：如果玩家删除 Steam 账号上的成就（通过第三方工具），重新满足条件时是否重新解锁？当前设计为"是"——每次满足条件都会调用 `SetAchievement()`。
