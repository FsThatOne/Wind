# 《风止》跨 GDD 全文档审查报告

> **审查日期**: 2026-06-05
> **审查范围**: 全部 15 个系统 GDD + systems-index + entities.yaml
> **审查工具**: review-all-gdds (Phase 1-7)
> **前次审查**: gdd-cross-review-2026-06-04.md

---

## 审查摘要

| 维度 | CRITICAL | HIGH | MEDIUM | LOW | 合计 |
|------|----------|------|--------|-----|------|
| 跨 GDD 一致性 | 4 | 6 | 5 | 3 | 18 |
| 游戏设计理论 | 3 | 7 | 6 | 2 | 18 |
| **总计** | **7** | **13** | **11** | **5** | **36** |

**总体判定: ⚠️ CONCERNS — 存在 7 项 CRITICAL 问题需在进入 Pre-Production Sprint 前解决**

---

## Part A: 跨 GDD 一致性检查 (Phase 2)

### CRITICAL 级

#### C-1. 季节天数定义直接冲突
- **文件**: `natural-day-stamina.md` vs `map-scene-management.md`
- **矛盾**: 前者 30 天/季（120 天/年），后者 15 天/季（60 天/年）—— 2× 差异
- **影响**: 时间系统全局失效，限时主线、NPC 飞书、季节环境全部不可靠
- **修复**: `natural-day-stamina.md` 为时间系统唯一所有者；`map-scene-management.md` 删除独立定义，引用时间系统常量

#### C-2. 境界体系 9 级 vs 8 级 + 名称全面冲突
- **文件**: `character-attributes.md` / `entities.yaml` vs `blurred-ui.md`
- **矛盾**:
  - 权威定义：9 级，阈值 `[25,35,50,70,90,115,140,170,200]`
  - 朦胧化 UI：8 级，阈值 `[0,20,50,100,180,300,500,800]`
  - 名称差异：「初窥门径」vs「略窥门径」，「略有小成」vs「小有所成」
- **影响**: UI 无法正确映射玩家境界，显示与实际状态脱节
- **修复**: `blurred-ui.md` CH-1 通道必须引用 `character-attributes.md` 的权威定义；若需不同映射粒度，声明为视觉翻译层并注明转换函数

#### C-3. 战斗行动选项三方不一致
- **文件**: `combat-system.md` / `item-system.md` / `combat-ui.md`
- **矛盾**:
  - combat-system：选招 / 调息 / 反制（3 种）
  - item-system：声明「战斗中第四选项：使用道具」
  - combat-ui：显示 6 招式 + 心法 + 调息 + 普通攻击（无道具、无显式反制按钮、多出普通攻击）
- **影响**: 核心玩法行动列表无法确定，实现阻塞
- **修复**: `combat-system.md` 定义完整 ActionRegistry，统一四方共识

#### C-4. romance-system 依赖编号全部错误
- **文件**: `romance-system.md` vs `systems-index.md`
- **矛盾**: 引用 `#12 NPC State, #11 Mindset, #14 Main Narrative, #15 Dialogue`，实际编号为 `#10, #6, #9, #5`
- **修复**: 更新为正确编号

---

### HIGH 级

| # | 问题 | 涉及文件 | 核心矛盾 |
|---|------|---------|---------|
| H-1 | morality 存储 -2~+2 vs 设计 -50~+50 | save-system / mindset | 精度丢失，魔道结局判定失败 |
| H-2 | enemy-ai 引用未定义战斗接口 | enemy-ai / combat-system | `GetBattleContext`、weakness +1 stagger、调息+20%防御 均未定义 |
| H-3 | save-system 标注"自然日未设计" | save-system / natural-day-stamina | 已设计但未同步 |
| H-4 | save-system 标注"感情系统未设计" | save-system / romance-system | 已设计但未同步 |
| H-5 | 季节数据双重所有权 | natural-day-stamina / map-scene | 同 C-1 根因 |
| H-6 | morality_mod 映射函数未定义 | npc-state / mindset | 态度公式无法实现 |

---

### MEDIUM 级

| # | 问题 | 涉及文件 |
|---|------|---------|
| M-1 | mindset 未声明 NPC State 为下游 | mindset / npc-state |
| M-2 | 战斗入口接口缺失 (trigger_combat) | dialogue / combat-system |
| M-3 | 战斗行动所有权不明 | combat / item / combat-ui |
| M-4 | 魔道结局判定精度丢失 | mindset / save-system |
| M-5 | combat-ui 订阅事件在战斗 GDD 中未定义 | combat-ui / combat-system |

---

### LOW 级

| # | 问题 |
|---|------|
| L-1 | blurred-ui 境界映射与 martial-arts realm_scale 偏差 |
| L-2 | item-system 验收需 combat 支持 use_item，后者无此验收点 |
| L-3 | combat-ui「普通攻击」在战斗 GDD 中无定义 |

---

## Part B: 游戏设计整体性审查 (Phase 3)

### CRITICAL 级

| # | 问题 | 类别 | 核心风险 |
|---|------|------|---------|
| G-1 | 4 条进度循环（武功/境界/情感/主线）争夺有限时辰，无优先级信号 | 进度循环竞争 | 玩家决策瘫痪 |
| G-2 | 13 个并行追踪系统，远超叙事优先定位的认知预算 | 注意力预算 | 与 Pillar 4「情感深度优先于内容广度」冲突 |
| G-3 | 无限背包 + 装备随机词缀直接违背 Pillar 3「武侠味先于游戏味」| 支柱对齐 | ARPG 模板破坏核心幻想 |

### HIGH 级

| # | 问题 | 类别 |
|---|------|------|
| G-4 | 战斗中「读取→反击」永远占优，无反制机制 | 主导策略 |
| G-5 | 驿站永远优于步行（时间减半+体力为零），步行无独占收益 | 主导策略 |
| G-6 | 体力系统约束过弱（0.2/格，100格才耗尽），几乎不构成决策 | 经济循环 |
| G-7 | 无限背包消除物品管理决策 | 经济循环 |
| G-8 | 隐藏道德轴与"每个选择有重量"支柱矛盾（不知情=无法有意义选择）| 支柱对齐 |
| G-9 | 境界/武功关系模糊，两条战力路径竞争不清 | 进度循环 |
| G-10 | 9 阶境界可能制造战力断崖 | 难度曲线 |

### MEDIUM 级

| # | 问题 | 类别 |
|---|------|------|
| G-11 | 情感线互斥设计鼓励浅尝策略 | 进度循环 |
| G-12 | 装备三维评估（品阶/品质/词缀）在叙事游戏中冗余 | 注意力预算 |
| G-13 | 委托系统若无惩罚可使玩家规避亲历 | 主导策略 |
| G-14 | 调息可能形成无限内息循环 | 经济循环 |
| G-15 | 随机遭遇 + 时间压力 = 惩罚螺旋 | 难度曲线 |
| G-16 | 战斗内使用道具破坏武侠战斗节奏 | 支柱对齐 |

---

## Part C: 跨系统场景走查 (Phase 4)

### 场景 1: 大地图旅行中触发奇遇 → 进入战斗 → 使用道具

**系统链**: Map → Natural-Day-Stamina → Combat → Item → Combat-UI

**断链点**:
1. 玩家在大地图移动触发奇遇 → 进入战斗 → 想使用道具
2. `item-system.md` 声明可用 `RegisterCombatAction("use_item")`
3. `combat-system.md` 无此 action 定义
4. `combat-ui.md` 面板无"使用道具"按钮
5. **结论**: 此流程在当前设计中不可能完成

### 场景 2: NPC 态度变化 → 影响对话 → 触发心境变化

**系统链**: Mindset → NPC-State (F1) → Dialogue → Mindset (feedback)

**断链点**:
1. 心境区域变化 → `npc-state.md` F1 公式需要 `mindset_mod`
2. `mindset_mod = f(current_zone)` 的映射函数**未定义**
3. `morality_mod` 的来源（连续值 vs 5 档）**不确定**
4. 态度变化后对话系统需要查询当前态度 → 接口已定义 ✓
5. 对话结果可能触发 `mindset_shift` → 反馈回心境系统 ✓
6. **结论**: 心境→NPC 态度方向有公式缺失，反向链路正常

### 场景 3: 主线限时任务 → 休息推进时间 → 超时强制推进

**系统链**: Main-Narrative → Natural-Day-Stamina → Map-Scene

**断链点**:
1. 主线注册限时事件到 `natural-day-stamina` 延迟事件队列 ✓
2. 玩家在客栈休息 → 时间推进 → 可能跨越超时日
3. 超时后 `main-narrative.md` 应强制推进 → 但战斗系统缺少 `battle_type` 入口接口
4. `map-scene-management.md` 对"剧情强制切场"有定义 ✓
5. **结论**: 超时检测链路完整，但若超时触发战斗（lethal 类型），战斗入口缺失

### 场景 4: 朦胧化 UI 显示当前境界

**系统链**: Character-Attributes → Blurred-UI → Player Perception

**断链点**:
1. 玩家功力=60 → `character-attributes.md` 映射为第 4 级「融会贯通」（阈值 50–70）
2. `blurred-ui.md` CH-1 通道使用自己的阈值 `[0,20,50,100,...]` → 60 落入第 3 级「小有所成」
3. **结论**: 玩家实际境界为 4 级，UI 显示为 3 级——信息误导

### 场景 5: 存档 → 读档 → 结局判定

**系统链**: Mindset → Save → Load → Ending Determination

**断链点**:
1. 玩家 morality = -35（应触发魔道结局）
2. `save-system.md` 存储为 -2（5 档制中最低档）
3. 读档后 morality = -2，但另一个存档中 morality = -26 也存为 -2
4. 结局判定系统查询 morality 值：是用连续值还是档位？若用档位，-26 和 -35 无法区分
5. **结论**: 极端情况下结局判定可能错误

---

## Part D: 需修订 GDD 清单

| GDD 文件 | 需修订项 | 优先级 |
|----------|---------|--------|
| `combat-system.md` | 补充 ActionRegistry、Event Bus、战斗入口接口、调息防御公式、普通攻击定义 | 🔴 紧急 |
| `blurred-ui.md` | CH-1 境界映射改为引用 character-attributes 权威定义 | 🔴 紧急 |
| `map-scene-management.md` | 删除独立季节天数定义(15天/季)，引用 natural-day-stamina 常量(30天/季) | 🔴 紧急 |
| `romance-system.md` | 修正依赖编号 (#12→#10, #11→#6, #14→#9, #15→#5) | 🔴 紧急 |
| `save-system.md` | morality 改存连续值；移除"未设计"陈旧标注；补充 natural-day-stamina 和 romance schema | 🟡 高优 |
| `item-system.md` | 战斗道具集成需与 combat-system ActionRegistry 对齐 | 🟡 高优 |
| `combat-ui.md` | 面板行动列表与 combat-system 统一后同步更新 | 🟡 高优 |
| `npc-state.md` | 定义 morality_mod 映射函数 | 🟡 高优 |
| `mindset-dual-axis.md` | 补充 downstream consumers 声明 | 🟢 中优 |

---

## Part E: 设计理论优先处理建议

### 第一优先（Pre-Production Gate 前解决）

1. **时间经济重设计** — 建立循环优先级信号与保护机制（G-1）
2. **系统精简** — 13→7-8 个并行系统，合并/延迟引入（G-2）
3. **物品系统武侠化重构** — 去除 ARPG 模板，装备改为叙事锚点（G-3）
4. **战斗反击主导策略破解** — 引入虚招/递增成本/匹配收益（G-4）

### 第二优先（进入 Production 前解决）

5. 体力系统重校准（G-6）
6. 驿站 vs 步行激励平衡（G-5）
7. 隐藏道德轴改为延迟可见（G-8）
8. 境界/武功关系明确化（G-9）

---

## 审查结论

本次全 GDD 审查发现 **7 项 CRITICAL**、**13 项 HIGH** 级问题。其中一致性问题集中在：
- 时间常量定义权归属（季节天数冲突）
- 战斗系统接口欠缺（多个下游系统引用未定义接口）
- 数据精度与存储格式不匹配（morality, realm）

设计理论问题集中在：
- 进度循环资源竞争无裁决机制
- 物品/装备系统与武侠核心幻想冲突
- 认知负荷超标

**建议在下一个 Sprint 优先处理 combat-system.md 的接口补全和四项数据一致性修复，作为解除架构阻塞的第一步。**

---

*报告生成完毕。下一步可选：*
*1. `/propagate-design-change` — 对 C-1~C-4 执行变更影响传播*
*2. `/design-system` — 修订 combat-system.md 补全缺失接口*
*3. `/consistency-check` — 修复后运行实体注册表一致性验证*
