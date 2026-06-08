# Romance System (Comet Model) — GDD

> **Status**: Designed

## A. Overview

| Field | Value |
|-------|-------|
| System ID | #13 |
| System Name | Romance System (Comet Model) |
| Priority | Vertical Slice |
| Layer | Feature |
| Dependencies | NPC State (#10), Mindset Dual-Axis (#6), Main Narrative (#9), Dialogue System (#5) |
| Dependents | Blurred UI (#14), Misunderstanding System (#18), Achievement System (#24) |

### Purpose

管理主角与四位女主（A/B/C/D）之间的情感关系。采用"彗星模型"——女主拥有独立旅程，与主角的关系是阶段性轨迹交叉而非长期陪伴。

### Core Architecture

双层解耦结构：

- **态度档位**（8 级枚举，可双向波动）：反映 NPC 当前对主角的即时情绪温度，驱动对话语气与即时反应。数据存储于 NPC State 系统。
- **关系里程碑**（bool flags，不可逆）：记录叙事事实（如"共渡险境"、"交托心事"），为态度设定安全下限（地板机制），防止普通误会导致逻辑跳脱。数据存储于 NPC State 系统。

### Key Rules

- **结缘互斥**：同一周目最多结缘一人（`bonded_heroine ∈ {None, A, B, C, D}`）
- **地板机制**：已达成里程碑约束态度下限，普通负面事件不可将态度打至地板以下
- **门槛机制**：态度需达到阈值才可触发新里程碑
- **诀别覆写**：极端剧情可 `force_break(npc_id)` 无视地板，设为不可修复对立状态
- **魔道 override**：`morality ≤ -30` 时触发第 6 结局，不走结缘逻辑
- **心境兼容性**：终幕时 `is_zone_compatible(npc_id, zone)` 决定"同行"或"道别"变体

### Player Fantasy

"得之我幸、失之我命"——不是攻略游戏，而是在各自旅途中因缘际会、渐生情愫，最终或同行或别离的江湖爱情。

---

## B. Detailed Design

### B1. 通用关系里程碑（Milestone Template）

所有女主共享以下 6 个里程碑定义，具体触发条件因人而异（由配置数据指定）：

| ID | 里程碑名称 | 含义 | 态度地板 | 解锁门槛（态度 ≥） |
|----|-----------|------|---------|-------------------|
| `M_ACQUAINTED` | 相识 | 不再是陌路人，有过正式交谈 | 萍水相逢 (0) | — (自动) |
| `M_TRUST` | 信任建立 | 经历过某事后主动选择信任对方 | 以礼相待 (+1) | 以礼相待 (+1) |
| `M_CRISIS` | 共渡险境 | 在生死或高压情境中互相依靠 | 推心置腹 (+2) | 以礼相待 (+1) |
| `M_HEART` | 交托心事 | 一方主动向另一方坦露隐秘 | 推心置腹 (+2) | 推心置腹 (+2) |
| `M_BOND` | 结缘 | 双方在关键时刻做出明确承诺 | 生死相托 (+3) | 推心置腹 (+2) |
| `M_BREAK` | 诀别 | 不可修复的决裂 | 拔剑相向 (-4) | — (force) |

**规则：**
- 里程碑按叙事节点触发，不可跳级（必须先有 `M_TRUST` 才能触发 `M_CRISIS`）
- `M_BREAK` 是诀别覆写，由 `force_break(npc_id)` 直接设置，无视当前态度和已有里程碑
- `M_BOND` 的触发还需满足 `bonded_heroine == None`（结缘互斥检查）

### B2. 态度档位与里程碑协作

```
on attitude_change(npc_id, delta, source):
    new_attitude = current_attitude + delta
    floor = get_milestone_floor(npc_id)

    if source.is_force_break:
        # 诀别覆写：无视地板
        set_milestone(npc_id, M_BREAK)
        set_attitude(npc_id, SWORD_DRAWN)  # 拔剑相向
        return

    if new_attitude < floor:
        new_attitude = floor  # 地板钳位，多余负面量静默吞掉

    set_attitude(npc_id, new_attitude)
```

**地板映射表：**

| 最高已达成里程碑 | 态度地板 |
|----------------|---------|
| 无 | 无限制 |
| M_ACQUAINTED | 萍水相逢 (0) |
| M_TRUST | 以礼相待 (+1) |
| M_CRISIS | 推心置腹 (+2) |
| M_HEART | 推心置腹 (+2) |
| M_BOND | 生死相托 (+3) |
| M_BREAK | 拔剑相向 (-4)（覆写） |

### B3. 彗星存在感机制

女主离队/不在场期间，通过以下四种方式维持情感连接：

| 机制 | 类型 | 触发方式 | 频率 | 效果 |
|------|------|---------|------|------|
| **暗号** | 主动发现 | 玩家在场景中发现女主留下的暗号标记（环境叙事） | 每章 1-2 次 | 触发主角内心独白，微量态度 +0/+1 |
| **书信** | 主动联系 | 女主通过飞书系统（NPC State #10）发送来信 | 自然日延迟触发 | 传递情报/心事/牵挂，可能附带选择 |
| **传闻** | 被动线索 | 在客栈/NPC对话中听到女主行踪或相关事件 | 区域进入时概率触发 | 更新女主独立旅程阶段，丰富世界感 |
| **偶遇** | 被动重逢 | 特定叙事节点或呼吸期中安排短暂碰面 | 章节设计预埋 | 触发短对话，可能推进里程碑 |

**设计约束：**
- 暗号不可错过（场景存在即可发现）；书信有未读提醒；传闻可错过但不影响主线
- 偶遇是设计师预埋的剧情节点，不由系统随机生成
- 彗星存在感不直接改变态度数值（暗号除外），而是通过丰富"她一直在"的感知，为后续里程碑解锁提供叙事铺垫

### B4. 结缘系统

**前置条件：**
1. `milestone[npc_id].M_HEART == true`（已交托心事）
2. `attitude[npc_id] >= 推心置腹 (+2)`（态度门槛）
3. `bonded_heroine == None`（尚未结缘他人）
4. 到达专属结缘剧情节点（主线或支线预埋）

**触发流程：**
```
on enter_bond_node(npc_id):
    if not milestone[npc_id].M_HEART:
        skip_bond_dialogue()  # 条件不足，走普通对话
        return
    if attitude[npc_id] < INTIMATE:  # 推心置腹
        skip_bond_dialogue()
        return
    if bonded_heroine != None:
        play_variant("already_bonded")  # 已结缘，播放"道歉/遗憾"变体
        return

    # 进入结缘抉择演出
    present_bond_choice(npc_id)
    # 玩家选择"承诺"后：
    # set_milestone(npc_id, M_BOND)
    # set_bonded_heroine(npc_id)
```

**结缘后效果：**
- 态度地板提升至 `生死相托 (+3)`
- 终幕结局判定时该女主参与"同行者"演出变体
- 解锁该女主的最终章节专属对话和互动
- 其余女主的结缘节点自动锁定（呈现"时机已过"或"各有归处"叙事）

### B5. 结缘与结局的交互

```
get_ending_variant():
    zone = get_mindset_zone()          # 心境双轴 → 5方向之一
    morality = get_morality_tier()

    if morality <= -30:
        return ENDING_6_DEMONIC        # 魔道结局 override

    script = ending_scripts[zone]      # 核心脚本（5种）

    if bonded_heroine != None:
        heroine = bonded_heroine
        if is_zone_compatible(heroine, zone):
            variant = "companion"      # 同行变体
        else:
            variant = "farewell"       # 道别变体
    else:
        variant = "solo"               # 独行变体

    return (script, variant, morality_narrator_tone)
```

**心境兼容性定义（待填充具体区域）：**

| 女主 | 兼容心境方向 | 不兼容时表现 |
|------|------------|-------------|
| A（沉渊阁养女） | 待定 | "我终究还是暗面的人，你已走向光明" |
| B（靖川案后人） | 待定 | "你的路太高远，我只想为故人讨个说法" |
| C（官员之女） | 待定 | "你选了江湖，我必须回到庙堂" |
| D（青狼部族长女） | 待定 | "草原才是我的归处，你的江湖留不住我" |

### B6. 四位女主彗星轨迹概览

| 女主 | 初遇时机 | 彗星特征 | 离场原因 | 存在感侧重 |
|------|---------|---------|---------|-----------|
| A | 第一章中后期 | 夜间出没，行踪隐秘 | 暗面任务，不可控 | 暗号 + 偶遇 |
| B | 第一章末 / 第二章初 | 底层视角，质朴直接 | 追查旧案，四处奔走 | 传闻 + 书信 |
| C | 第二章 | 身份敏感，公私两难 | 官场调动，身不由己 | 书信 + 传闻 |
| D | 第二章中后期 | 异域气质，来去如风 | 部族事务，季节性迁徙 | 偶遇 + 暗号 |

### B7. 数据结构

```csharp
// 存储于 NPC State 系统，感情系统负责写入规则
public class RomanceData
{
    // 通用里程碑 flags
    public bool Acquainted;    // M_ACQUAINTED
    public bool TrustBuilt;    // M_TRUST
    public bool CrisisShared;  // M_CRISIS
    public bool HeartOpened;   // M_HEART
    public bool Bonded;        // M_BOND
    public bool Broken;        // M_BREAK

    // 彗星存在感记录
    public int SignsDiscovered;     // 已发现暗号数
    public int LettersReceived;    // 已收书信数
    public int RumorsHeard;        // 已听传闻数
    public int EncountersHad;      // 已发生偶遇数
}

// 全局单例
public enum BondedHeroine { None, A, B, C, D }
public BondedHeroine CurrentBond = BondedHeroine.None;
```

---

## C. Formulas & Logic

### C1. 态度地板计算

```
get_milestone_floor(npc_id) -> int:
    if milestone[npc_id].Broken:
        return -4  # M_BREAK 覆写
    if milestone[npc_id].Bonded:
        return +3
    if milestone[npc_id].HeartOpened:
        return +2
    if milestone[npc_id].CrisisShared:
        return +2
    if milestone[npc_id].TrustBuilt:
        return +1
    if milestone[npc_id].Acquainted:
        return 0
    return -4  # 无里程碑，无限制（态度枚举最低为 -4）
```

### C2. 里程碑解锁检查

```
can_unlock_milestone(npc_id, milestone_id) -> bool:
    match milestone_id:
        M_ACQUAINTED:
            return true  # 自动
        M_TRUST:
            return milestone[npc_id].Acquainted
                   AND attitude[npc_id] >= +1
        M_CRISIS:
            return milestone[npc_id].TrustBuilt
                   AND attitude[npc_id] >= +1
        M_HEART:
            return milestone[npc_id].CrisisShared
                   AND attitude[npc_id] >= +2
        M_BOND:
            return milestone[npc_id].HeartOpened
                   AND attitude[npc_id] >= +2
                   AND bonded_heroine == None
        M_BREAK:
            return true  # force_break 不检查前置
```

### C3. 结缘互斥判定

```
try_bond(npc_id) -> BondResult:
    if bonded_heroine != None:
        return ALREADY_BONDED
    if not can_unlock_milestone(npc_id, M_BOND):
        return CONDITIONS_NOT_MET
    # 进入玩家选择
    choice = present_bond_choice(npc_id)
    if choice == ACCEPT:
        set_milestone(npc_id, M_BOND)
        bonded_heroine = npc_id
        lock_other_bond_nodes()
        return BONDED
    else:
        return DECLINED
```

### C4. 结局变体选取

```
ending_variant = f"{mindset_zone}_{bond_status}_{compatibility}"

# 完整枚举：
# 5 zones × 3 variants (companion/farewell/solo) = 15 种演出组合
# + 魔道结局 (1) = 16 种总变体

# 旁白语气由 morality_tier 独立控制（不影响结局分支，只影响文本色调）
```

### C5. 彗星存在感触发概率（传闻）

```
rumor_trigger_chance(npc_id, region) -> float:
    base = 0.3  # 30% 基础概率
    if npc_journey_stage[npc_id].region == region:
        base += 0.4  # 女主当前在同一区域
    if days_since_last_contact[npc_id] > 7:
        base += 0.2  # 长时间未联系，传闻概率提升
    return clamp(base, 0.0, 0.8)  # 上限 80%
```

---

## D. Edge Cases

| # | 场景 | 处理方式 |
|---|------|---------|
| D1 | 里程碑解锁后立刻遭遇严重误会，态度急跌 | 地板钳位生效，态度不低于里程碑地板；日志记录被吞掉的 delta 供调试 |
| D2 | 玩家在同一呼吸期内满足多位女主的结缘条件 | 结缘互斥检查：先触发的结缘节点优先，后续节点自动走"时机已过"变体 |
| D3 | 玩家拒绝结缘选择（DECLINED）后态度是否保留 | 保留当前态度和里程碑，`M_BOND` 不设置；该女主的结缘节点后续不再触发（一次性选择） |
| D4 | 魔道结局 override 时已结缘的女主 | 第 6 结局不使用结缘逻辑，但结缘状态仍保存在存档中（影响旁白提及） |
| D5 | `M_BREAK`（诀别）后是否可恢复 | 不可恢复。`M_BREAK` 是终态，该 NPC 后续不再参与感情系统判定 |
| D6 | 同一 NPC 同时触发 `force_break` 和正面里程碑 | `force_break` 优先级最高，覆写一切现有里程碑和态度 |
| D7 | 女主死亡时的感情数据处理 | NPC 生命状态为"死亡"时冻结感情数据，不再触发彗星存在感；结局判定时"bonded but dead"走独立悲剧变体 |
| D8 | 存档加载后彗星存在感计时器 | `days_since_last_contact` 从存档读取，续算而非重置 |
| D9 | 新周目继承 | 感情数据不继承（全部重置），保证每个周目独立体验 |
| D10 | 传闻触发概率叠加到上限 | `clamp(0.0, 0.8)` 确保永远有 20% 概率不触发，避免信息过载 |

---

## E. Dependencies & Interfaces

### 依赖（本系统读取）

| 系统 | 接口 | 用途 |
|------|------|------|
| NPC State (#10) | `get_attitude(npc_id)`, `set_attitude()`, `npc_state_change()` | 读写态度档位和里程碑数据 |
| NPC State (#10) | `get_npc_journey_stage(npc_id)` | 查询女主当前独立旅程阶段（用于传闻概率） |
| NPC State (#10) | 飞书系统 | 彗星书信的投递载体 |
| Mindset Dual-Axis (#6) | `get_mindset_zone()`, `is_zone_compatible(npc_id, zone)` | 结局变体判定 |
| Mindset Dual-Axis (#6) | `get_morality_tier()` | 魔道结局 override 检查 |
| Main Narrative (#9) | 章节节点、结缘节点标记 | 里程碑和结缘的触发时机 |
| Dialogue System (#5) | 对话选择事件 | 态度变化来源之一 |

### 提供（本系统暴露）

| 接口 | 消费者 | 用途 |
|------|--------|------|
| `get_bonded_heroine() -> BondedHeroine` | Main Narrative, 结局系统 | 结局变体选取 |
| `get_milestone_flags(npc_id) -> RomanceData` | 对话系统, 误会系统 | 条件对话分支 |
| `get_milestone_floor(npc_id) -> int` | NPC State | 态度变化时的地板检查 |
| `force_break(npc_id)` | 主线剧情, 误会系统 | 极端决裂触发 |
| `try_bond(npc_id) -> BondResult` | Main Narrative | 结缘流程入口 |
| `get_comet_stats(npc_id) -> CometStats` | Achievement System | 统计暗号/书信/传闻/偶遇数量 |

---

## F. Tuning Knobs

| 参数 | 默认值 | 范围 | 作用 |
|------|--------|------|------|
| `rumor_base_chance` | 0.3 | 0.1–0.5 | 传闻基础触发概率 |
| `rumor_same_region_bonus` | 0.4 | 0.2–0.5 | 女主同区域时传闻概率加成 |
| `rumor_absence_bonus` | 0.2 | 0.1–0.3 | 长时间未联系时传闻概率加成 |
| `rumor_absence_threshold_days` | 7 | 5–14 | 触发 absence bonus 的天数阈值 |
| `rumor_cap` | 0.8 | 0.6–0.9 | 传闻触发概率上限 |
| `sign_attitude_bonus` | 0 或 1 | 0–1 | 发现暗号时态度变化量（0 = 纯叙事） |
| `bond_attitude_threshold` | +2 | +1 至 +3 | 结缘所需最低态度档位 |
| `signs_per_chapter` | 1–2 | 0–3 | 每章预埋暗号数量上限 |

---

## G. Visual & Audio

### 视觉

| 元素 | 描述 |
|------|------|
| 暗号标记 | 场景中的环境像素细节（刻痕、布条、特定花色），需鲜明到玩家路过可注意到 |
| 结缘演出 | 全屏特写 + 对话演出（类似 CG 但用像素美术），配合慢动作和光效 |
| 诀别演出 | 画面饱和度下降 + 屏幕边缘暗角加深，NPC 像素精灵转身离去动画 |
| 偶遇过场 | 短暂黑幕过渡 → 近景双人构图（像素），3-5 句对话后自动结束 |
| 传闻 UI | 客栈 NPC 对话气泡中以不同颜色高亮女主相关关键词 |

### 音频

| 元素 | 描述 |
|------|------|
| 暗号发现 | 轻微环境音效 + 短乐句（弦乐泛音/风铃），每位女主可用不同音色区分 |
| 书信到达 | 飞书信使音效（海东青鸣叫 / 特性信使对应音效） |
| 结缘主题 | 每位女主有专属短旋律 motif，在结缘演出中完整展开 |
| 诀别主题 | 低沉弦乐 + 风声渐强 → 静默（呼应"风止"主题） |
| 偶遇背景 | 场景 BGM 渐弱 → 女主 motif 轻声进入 → 偶遇结束后恢复原 BGM |

---

## H. UI Requirements

### 玩家可见信息

感情系统**不暴露数值面板**（不显示好感度数字或进度条）。玩家通过以下间接方式感知关系状态：

| 渠道 | 表现 |
|------|------|
| 对话语气 | 态度档位直接影响 NPC 对话文本选取（由对话系统消费态度值） |
| 飞书内容 | 书信措辞/称呼随态度变化（亲密 vs 疏远） |
| 人物图鉴 | 在"人物志"界面以**文学化描述**（而非数值）展示当前关系概况，如"彼此已推心置腹" |
| 回忆录 | 已达成里程碑以"回忆片段"形式解锁在图鉴中（带缩略图和一句话描述） |

### 不需要的 UI

- 无好感度数字/进度条
- 无"攻略进度百分比"
- 无结缘状态的直接文字提示（通过剧情演出传达）

---

## I. Acceptance Criteria

| # | 验收条件 | 验证方式 |
|---|---------|---------|
| AC1 | 态度变化被里程碑地板正确钳位 | 单元测试：设置 M_TRUST 后尝试将态度降至 0 以下，断言结果 = +1 |
| AC2 | `force_break()` 无视地板，态度设为 -4 | 单元测试：任意里程碑状态下调用 force_break，断言态度 = -4 且 M_BREAK = true |
| AC3 | 结缘互斥：已结缘后第二位女主节点走"时机已过"变体 | 集成测试：bond(A) → enter_bond_node(B) → 断言播放 already_bonded |
| AC4 | 里程碑不可跳级 | 单元测试：未设 M_TRUST 时尝试解锁 M_CRISIS，断言失败 |
| AC5 | 魔道结局 override 不受 bonded_heroine 影响 | 集成测试：morality = -30 + bonded = A → 断言返回 ENDING_6_DEMONIC |
| AC6 | 传闻触发概率正确计算且不超上限 | 单元测试：所有加成叠满时断言 ≤ 0.8 |
| AC7 | 存档读取后 days_since_last_contact 续算 | 存档测试：保存/加载后断言计时器正确恢复 |
| AC8 | 人物图鉴正确显示文学化关系描述 | 手动验收：检查各态度档位对应的文案输出 |
| AC9 | 拒绝结缘后节点不再重复触发 | 集成测试：decline(A) → 再次进入同节点 → 断言 skip_bond_dialogue |

---

## J. Open Questions

| # | 问题 | 影响 | 决策时限 |
|---|------|------|---------|
| OQ1 | 每位女主的心境兼容区域具体是哪些？ | 结局变体的 companion/farewell 分流 | 需在角色详细设计阶段确定 |
| OQ2 | 暗号发现是否给予态度 +1？还是纯叙事（+0）？ | 影响态度曲线和地板触发节奏 | Tuning 阶段可调 |
| OQ3 | "bonded but dead"悲剧变体需要几种（按死亡时机分？按女主分？） | 结局内容量 | 叙事设计阶段 |
| OQ4 | 是否需要"准结缘"状态（满足条件但尚未到达结缘节点）的 UI 暗示？ | 玩家引导 vs 悬念保持 | UI 设计阶段 |
| OQ5 | 女主 D（青狼部族长女）的彗星存在感是否需要特殊处理（异域/语言差异）？ | 暗号和书信的叙事可信度 | 角色详设阶段 |
