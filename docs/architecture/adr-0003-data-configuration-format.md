# ADR-0003: Data Configuration Format

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Core / Data |
| **Knowledge Risk** | LOW — 文件 IO 和数据解析不依赖 post-cutoff API |
| **References Consulted** | `docs/engine-reference/godot/current-best-practices.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-0005 (对话系统格式 — 可能选择与本 ADR 不同的格式), 所有使用配置数据的系统 |
| **Blocks** | Sprint 1 DataRegistry 实现 |
| **Ordering Note** | 必须在任何数据表编写开始前确定 |

## Context

### Problem Statement

《风止》的 25 个系统中大部分依赖静态配置数据（角色属性模板、招式定义、敌人 AI 行为表、物品模板、场景元数据、传闻模板等）。需要决定这些配置数据的存储格式，影响开发工作流、版本控制体验、运行时加载性能和工具链选择。

### Constraints

- 大量配置表（预估 50+ 文件）：characters, martial-arts, enemies, items, dialogues, narrative, scenes, rumors, tutorials, audio
- 团队需要直接在文本编辑器中编辑配置（无专用编辑器）
- Git 版本控制 — 需要 line-by-line diff 友好
- 加载发生在游戏启动时（一次性），运行时只读
- C# (.NET 8+) 反序列化
- 数据量估计：单表 50-500 条记录，单条 5-20 字段

### Requirements

- 人类可读可编辑
- Git diff 清晰
- C# 反序列化高效
- 支持注释（配置表需要解释字段含义）
- 支持嵌套结构（如物品词条池、AI 行为树配置）
- 启动加载性能可接受（< 2s 总计）

## Decision

采用 **YAML** 作为所有静态配置数据的标准格式。

### 具体规范

| 规范项 | 值 |
|--------|---|
| 格式 | YAML 1.2 |
| 文件扩展名 | `.yaml` |
| 编码 | UTF-8 (BOM-free) |
| C# 解析库 | YamlDotNet (NuGet) |
| 存储路径 | `assets/data/{domain}/{table}.yaml` |
| 加载时机 | 游戏启动时一次性加载到 DataRegistry |
| 运行时权限 | 只读 |

### 目录结构

```
assets/data/
├── characters/
│   ├── player-template.yaml      # 主角属性模板
│   ├── companion-templates.yaml  # 同伴模板
│   └── gongli-curves.yaml        # 功力成长曲线
├── martial-arts/
│   ├── moves.yaml                # 所有招式定义
│   └── counter-matrix.yaml       # 克制关系矩阵
├── enemies/
│   ├── templates.yaml            # 敌人模板
│   ├── ai-behaviors.yaml         # AI 行为配置
│   └── loot-tables.yaml          # 掉落表
├── items/
│   ├── consumables.yaml          # 消耗品
│   ├── equipment-templates.yaml  # 装备模板 (含品级)
│   ├── key-items.yaml            # 关键物品
│   ├── recipes.yaml              # 配方
│   └── affix-pools.yaml          # 词条池 (按品级)
├── narrative/
│   ├── chapters.yaml             # 章节结构
│   ├── nodes.yaml                # 19 个叙事节点
│   └── ending-paths.yaml         # 结局收敛规则
├── scenes/
│   ├── scene-registry.yaml       # 18 场景元数据
│   └── spawn-points.yaml         # 传送点定义
├── jianghu/
│   ├── rumors.yaml               # 传闻模板
│   ├── passphrase-rotation.yaml  # 暗号轮换规则
│   └── npc-schedules.yaml        # NPC 日程表
├── tutorials/
│   └── triggers.yaml             # 教学触发条件
└── audio/
    ├── bgm-mapping.yaml          # 场景-音乐映射
    └── dynamic-rules.yaml        # 战斗动态音乐规则
```

### DataRegistry 接口

```csharp
// Shared/DataRegistry/IDataTable.cs
public interface IDataTable<T> where T : class
{
    T Get(string id);
    IReadOnlyList<T> GetAll();
    bool Has(string id);
    IReadOnlyList<T> Query(Func<T, bool> predicate);
}

// Shared/DataRegistry/DataRegistry.cs (Autoload)
public partial class DataRegistry : Node
{
    private readonly Dictionary<Type, object> _tables = new();

    public override void _Ready()
    {
        // 启动时加载所有 YAML → 反序列化 → 建索引
        LoadAll("res://assets/data/");
    }

    public IDataTable<T> GetTable<T>() where T : class
    {
        return (IDataTable<T>)_tables[typeof(T)];
    }
}

// 使用示例
var move = Services.DataRegistry.GetTable<MoveData>().Get("tai_chi_push");
var allGrade3Items = Services.DataRegistry.GetTable<EquipmentTemplate>()
    .Query(e => e.Grade == 3);
```

### YAML 编写规范

```yaml
# assets/data/martial-arts/moves.yaml
# 招式定义表 — 每个条目为一个招式的完整数据

- id: tai_chi_push
  name: 太极推手
  category: soft          # hard | soft | skill
  power_base: 12
  stamina_cost: 2
  counter_bonus:          # 克制加成
    vs_hard: 1.5
    vs_soft: 1.0
    vs_skill: 0.8
  tags: [defensive, redirect]
  description: "借力打力，以柔克刚"

- id: iron_fist
  name: 铁拳
  category: hard
  power_base: 18
  stamina_cost: 3
  counter_bonus:
    vs_hard: 1.0
    vs_soft: 0.8
    vs_skill: 1.5
  tags: [offensive, direct]
  description: "刚猛一击，势不可挡"
```

### 性能估算

| 指标 | 估算 |
|------|------|
| 文件总数 | ~30 个 YAML 文件 |
| 总数据量 | ~500KB 原始 YAML |
| YamlDotNet 解析速度 | ~50MB/s (纯 CPU) |
| 预计加载时间 | < 100ms |
| 内存占用 | ~2-5MB (反序列化后的 C# 对象) |

## Alternatives Considered

### Alternative 1: JSON

- **Description**: 使用 .json 文件，System.Text.Json 解析
- **Pros**: .NET 原生支持无额外依赖；解析速度略快；广泛工具支持
- **Cons**: 不支持注释（配置表需要字段说明）；嵌套结构可读性差（大量花括号）；尾逗号问题；引号噪音
- **Rejection Reason**: 配置表需要注释解释字段含义和取值范围，JSON 不支持注释是硬伤

### Alternative 2: Godot Resource (.tres / .res)

- **Description**: 使用 Godot 的 Resource 系统，每个配置项为一个 .tres 文件
- **Pros**: 编辑器原生支持 Inspector 编辑；与引擎深度集成；自动序列化
- **Cons**: 二进制 .res 不可读/.tres 格式冗长；Git diff 体验差；大量小文件（每条记录一个 .tres？）；不适合表格式数据（500 条招式 = 500 个 .tres？）
- **Rejection Reason**: 不适合表格式批量数据；.tres 格式冗长不适合人工编辑；Git diff 体验差

### Alternative 3: TOML

- **Description**: 使用 TOML 格式
- **Pros**: 支持注释；相对人类可读
- **Cons**: 嵌套结构语法不如 YAML 简洁（`[section.subsection]`）；C# 生态库不如 YAML 成熟；不支持 flow sequences
- **Rejection Reason**: 嵌套数据（如词条池、AI 行为树）表达能力不如 YAML

## Consequences

### Positive

- 所有配置集中管理，一致的编辑体验
- 支持注释，新开发者可直接读懂配置含义
- Git diff 清晰，PR review 配置变更无障碍
- YamlDotNet 成熟稳定（10年+ 维护历史）
- 只读设计简化并发问题

### Negative

- 引入 NuGet 依赖（YamlDotNet）
- YAML 缩进敏感 — 编辑错误可能导致解析失败
- 无 schema 验证（需自行实现或用 JSON Schema 转换）
- 不利用 Godot 的 Resource 缓存系统

### Risks

| 风险 | 缓解 |
|------|------|
| YAML 缩进错误导致运行时崩溃 | 启动时全量加载 + 快速失败 + 清晰错误信息指向行号 |
| YamlDotNet 与 .NET 8 兼容性 | 使用最新稳定版（15.x），CI 测试覆盖 |
| 大量 YAML 手工维护易出错 | 后续可选：引入 schema 校验工具或简单 Excel→YAML 转换脚本 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| item-system.md | 品级/模板/配方为配置表驱动 | `assets/data/items/` 目录下多表定义 |
| martial-arts-system.md | 招式数据、克制矩阵 | `assets/data/martial-arts/moves.yaml` + `counter-matrix.yaml` |
| enemy-ai.md | AI 行为表配置 | `assets/data/enemies/ai-behaviors.yaml` |
| map-scene-management.md | 18 场景元数据、传送点 | `assets/data/scenes/` |
| living-jianghu-layer.md | 传闻模板、暗号轮换、NPC 日程 | `assets/data/jianghu/` |
| audio-system.md | 场景-音乐映射、动态规则 | `assets/data/audio/` |
| tutorial-onboarding.md | 教学触发条件 | `assets/data/tutorials/triggers.yaml` |
| natural-day-stamina.md | 时段定义、体力恢复规则 | 内嵌于 TimeSystem 代码（数据量极小，不独立建表） |

## Performance Implications

- **CPU**: 启动时一次性解析 ~500KB YAML → < 100ms
- **Memory**: 反序列化后 ~2-5MB 常驻
- **Load Time**: 首次启动增加 < 100ms
- **Network**: N/A

## Migration Plan

首次实现，无迁移需求。

## Validation Criteria

1. 所有 YAML 文件启动时加载无错误
2. `DataRegistry.GetTable<T>().Get(id)` O(1) 查询性能
3. 新增数据表只需：定义 C# model + 放置 YAML 文件 + 注册到 DataRegistry
4. YAML 格式错误在启动时立即报告文件名+行号

## Related Decisions

- [ADR-0001](adr-0001-event-bus-architecture.md) — DataRegistry 通过 EventBus 通知加载完成
- [ADR-0005](../architecture/adr-0005-dialogue-format.md) (PENDING) — 对话数据可能使用不同格式（图结构特殊需求）
- [architecture.md](architecture.md) — Section 9 Data Architecture
