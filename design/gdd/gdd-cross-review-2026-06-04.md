# Cross-GDD Review Report

**Date**: 2026-06-04
**GDDs Reviewed**: 13 system GDDs + game-concept + systems-index
**Systems Covered**: 角色属性, 回合制战斗, 武学组合, 敌方AI, 对话系统, 心境双轴(×2版本), 战斗UI, 存档系统, 主线叙事, NPC状态管理, 自然日+体力, 地图/场景管理
**Entity Registry**: Loaded (7 formulas, 16 constants registered)

---

## Verdict: FAIL

4 个阻断性问题必须解决后，方可进入架构设计阶段。

---

## Consistency Issues (Phase 2)

### Blocking (must resolve before architecture begins)

#### 🔴 B-1: 心境系统双文件共存

**涉及 GDD**: `mindset-system.md` (旧) / `mindset-dual-axis.md` (新) / `systems-index.md`

项目中同时存在两份心境系统 GDD，且 `systems-index.md` 链接指向旧版。两版本在轴数（2→3）、值域（0-100→±50）、初始值（50,50→-5,0,0）、结局模型（5→25变体）上完全不兼容。任何下游系统（存档、对话、NPC、感情）无法确定应对接哪套接口。

**解决建议**:
1. 归档 `mindset-system.md` 至 `_deprecated/`
2. 更新 `systems-index.md` 链接为 `mindset-dual-axis.md`
3. 确认魔道结局定位（Open Question 需关闭）

---

#### 🔴 B-2: 季节天数直接矛盾

**涉及 GDD**: `natural-day-stamina.md` (第79行: 30天/季) / `map-scene-management.md` (第127行: 15天/季)

季节周期差一倍，影响：旅行节奏、限时任务天数、NPC延迟事件时机、整个游戏时间经济。

**解决建议**: 以 `natural-day-stamina.md` 的 `days_per_season=30` 为权威（时间数据SSoT owner），修正 `map-scene-management.md` 中所有 "15天/季" 的引用。

---

#### 🔴 B-3: 12时辰光照分类不一致（3个时辰归属矛盾）

**涉及 GDD**: `natural-day-stamina.md` (第43-60行) / `map-scene-management.md` (第154-172行)

| 时辰 | 现实时间 | natural-day-stamina | map-scene-management | 矛盾？ |
|------|---------|--------------------|--------------------|--------|
| 寅时 | 03:00-05:00 | 夜 | 晨 | ✅ |
| 辰时 | 07:00-09:00 | 晨 | 日 | ✅ |
| 亥时 | 21:00-23:00 | **日** | 夜 | ✅ |

注：`natural-day-stamina.md` 将亥时(21:00-23:00)归为"日"，明显为笔误。

**解决建议**: 以 `map-scene-management.md` 的分类为准（更符合现实逻辑），同步修正 `natural-day-stamina.md`。光照分类定义权归自然日系统（时间owner），地图系统订阅 `get_light_category()`。

---

#### 🔴 B-4: 时间/体力参数数据所有权冲突

**涉及 GDD**: `natural-day-stamina.md` (第70、104行 + Tuning Knobs) / `map-scene-management.md` (第233-234行 + Tuning Knobs)

`time_per_grid=0.01天/格` 和 `stamina_per_grid=0.2点/格` 在两个GDD中均有完整定义和Tuning Knobs条目。当前数值恰好一致，但未来修改一处忘记另一处必然产生不一致。

**解决建议**: 
- `time_per_grid` 和 `stamina_per_grid` 归属地图系统（"地图定义格移动的消耗"）
- 自然日系统仅暴露 `advance_time(delta)` 和 `consume_stamina(amount)` 接口
- 从 `natural-day-stamina.md` 移除重复的参数定义和Tuning Knobs

---

### Warnings

#### ⚠️ W-1: combat-system.md 武学组合依赖方向标注错误

`combat-system.md` 第308行将"武学组合"标注为"下游"，但实际数据流为武学→战斗（战斗读取已装备招式）。应修正为"上游"。

#### ⚠️ W-2: enemy-ai.md 6项接口未被 combat-system.md 接收

`enemy-ai.md`（Approved状态）明确列出需在 `combat-system.md` 中追加的6个条目（GetBattleContext, GetPlayerActionHistory, GetChargeAnnounce, 弱点+1破绽, 调息+20%防御, Type.MEDITATION枚举），当前战斗GDD中均未包含。

#### ⚠️ W-3: save-system.md 心境序列化字段与新版不一致

存档系统Overview写"5结局"（应为25变体），morality字段描述为-2~+2索引值（新版为-50~+50原始值）。

#### ⚠️ W-4: map-scene-management.md 终幕色调使用旧版结局名

终幕色调表含"剑覆苍生（魔道）"，但新版mindset-dual-axis将魔道定位列为Open Question。

#### ⚠️ W-5: main-narrative.md 结缘状态含 heroine_d（四女主 vs 项目约束三女主）

`main-narrative.md` 第598行列出 `none|heroine_a|heroine_b|heroine_c|heroine_d`，与 project_memory 约束"三位女主"矛盾。需确认女主人数。

#### ⚠️ W-6: systems-index.md 结局描述沿用旧版 "5结局"

应更新为"5基础结局 × 善恶档位"。

#### ⚠️ W-7: natural-day-stamina.md 亥时归"日"（内部笔误）

第58行亥时(21:00-23:00)归类为"日"光照。（与B-3联动）

#### ⚠️ W-8: 对话系统已对接三轴模型但旧版 mindset-system.md 仍存在

（与B-1联动，归档旧版后自动解决）

#### ⚠️ W-9: map-scene-management 声称订阅季节事件却自定义 season_days

第128行说"订阅 season_changed"，第554行 Tuning Knobs 又定义 `season_days=15`。订阅者不应有独立的季节天数参数。

---

## Game Design Issues (Phase 3)

### Blocking

#### 🔴 (与B-2/B-3相同) 季节天数+光照分类冲突导致"活江湖"支柱实现基础不可靠

NPC出没、商铺开关、场景光照变体的触发条件完全由光照分类和季节天数决定。两个系统给出不同定义，Pillar 1 的实现无法建立在不一致的基础上。

### Warnings

#### ⚠️ W-10: 呼吸期无上限 + 限时主线不显示倒计时 = 时间分配盲区

呼吸期 `duration: null`（无限制），唯一引导为7游戏日后NPC来信。限时主线不弹系统提示不显示倒计时。玩家面临支线+武学修炼+感情线+探索全部竞争同一时间池，却无法感知时间预算余量。

**建议**: 为呼吸期设温和上限（20-30天后自动推进），限时主线增加2档"加速暗示"。

#### ⚠️ W-11: 战斗回合内信息层过多

单回合内需同时处理：意图图标、6槽选招、触发条件判断、内息管理、破绽追踪、回合计数、AI tell信号、协同burst窗口 = 8层信息。

**建议**: 确认序章教学仅暴露层1-4，后续渐进引入。

#### ⚠️ W-12: 心境三轴完全隐式 + 善恶无直接反馈 = 玩家代理感风险

Pillar 2 要求"选择有重量"——但玩家完全无法感知选择对心境的影响，终幕结局可能与意图不符。

**建议**: 保持去数值化，但在关键心境选择后增加即时文学反馈。

#### ⚠️ W-13: "纯柔 build" 可能成为优势策略

柔系面对最常见的刚猛型敌人时，同时具备克制伤害加成+弱点额外破绽+内息回复——攻击/防御/资源三维优势。

**建议**: 确保柔系base_multiplier系统性低于刚/巧；中期起增加柔韧型/诡诈型敌人（克制柔）。

#### ⚠️ W-14: 批注版武学可能使 "普通+批注 > 绝学" 成为唯一最优解

`annotation_mult` 上限1.8时，普通武学 0.7×1.8=1.26 超越未批注高级招式。

**建议**: 降低上限至1.5，或确保绝学真传有批注版无法复制的独家效果。

#### ⚠️ W-15: 体力系统力竭惩罚过轻

力竭仅速度×0.5、不致死、不扣体力。呼吸期内时间免费（无限），体力系统在非限时场景下几乎无压力。

**建议**: 明确体力系统定位——"资源管理挑战"还是"叙事节奏工具"（驱动客栈休息→推进时间→触发活江湖事件）。

#### ⚠️ W-16: "未定之人"结局可能违反 Pillar 2

触发条件为"双轴绝对值均≤14"——这不是有意识的选择，更接近"回避所有选择"。

**建议**: 明确是设计目标（需高质量叙事支撑）还是兜底机制。

#### ⚠️ W-17: 结局变体膨胀（25-125种）vs 独立开发规模

5心境×5善恶=25变体（mindset定义）×5结缘=125种理论组合。对独立开发来说内容量巨大。

**建议**: 采用层级化方案——5种核心结局脚本 × 善恶仅改旁白/色调 × 结缘仅改同行者/对白。

#### ⚠️ W-18: 魔道结局定位未决

game-concept说"隐藏魔道结局"，mindset-dual-axis列为Open Question。这是架构级决策。

**建议**: 建议"极恶阈值触发独立结局"方案（morality≤-45时override心境判定）。

---

## Cross-System Scenario Issues (Phase 4)

### Scenarios Walked: 3

#### ⚠️ 场景1: 大地图移动跨越日/季节边界 + 延迟事件触发

**Systems**: 自然日+体力 → NPC状态 → 飞书信使
**Issue**: 如果玩家在驿站传送时跨越多日（如20格×0.005=0.1天），每经过一日需批量触发延迟事件。但如果某延迟事件的NPC飞书需要玩家交互（标记为"休息后待送达"），而玩家此时不在客栈——送达逻辑需要明确"非客栈场景下如何处理待交互事件"。
**建议**: 明确"驿站传送期间触发的交互事件"的处理：到达目的地后立即显示？下次进入客栈？

#### ⚠️ 场景2: 限时主线超时 + 玩家正在客栈休息

**Systems**: 主线叙事 → 自然日+体力
**Issue**: 主线超时触发"强制推进"，但自然日系统的客栈休息"不可中断"。如果休息推进时间导致超时，强制推进在休息结束后才能执行——但此时场景可能已变（如强制推进改变了地点）。
**建议**: 明确优先级：休息结算完毕后 → 检查超时 → 触发强制推进。

#### ℹ️ 场景3: 战斗后触发心境选择 + 内息耗尽

**Systems**: 战斗 → 对话 → 心境
**Issue**: 战斗不消耗时间且战后内息不自动恢复，如果战后立即进入对话心境选择，玩家可能在耗尽状态下做决定（疲惫视觉效果影响阅读体验）。但由于战斗不影响体力/时间，这是合理的——仅为体验层面的小注意点。

---

## GDDs Flagged for Revision

| GDD | Reason | Type | Priority |
|-----|--------|------|----------|
| mindset-system.md | 旧版应归档 | Consistency | Blocking |
| natural-day-stamina.md | 光照分类笔误 + 参数所有权重复 | Consistency | Blocking |
| map-scene-management.md | 季节天数矛盾 + 独立定义 season_days | Consistency | Blocking |
| systems-index.md | 心境链接旧版 + 结局描述旧版 | Consistency | Warning |
| save-system.md | 心境字段描述旧版 | Consistency | Warning |
| main-narrative.md | heroine_d 与约束矛盾 | Consistency | Warning |
| combat-system.md | 缺少enemy-ai要求的6项接口 | Consistency | Warning |

---

## Recommended Resolution Order

1. **归档 mindset-system.md**（解决 B-1 + W-6 + W-8）
2. **统一季节天数 + 修正光照分类**（解决 B-2 + B-3 + W-7 + W-9）
3. **明确参数SSoT归属**（解决 B-4）
4. **确认女主人数**（解决 W-5）
5. **关闭魔道结局Open Question**（解决 W-18 + W-4）
6. **更新 combat-system 整合 enemy-ai 接口**（解决 W-2）
7. **更新 save-system / systems-index 旧描述**（解决 W-3 + W-6）

---

*Report generated by /review-all-gdds skill*
