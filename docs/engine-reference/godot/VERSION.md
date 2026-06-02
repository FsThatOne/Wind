# Godot Engine — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Godot 4.6.3 |
| **Language** | C# (.NET 8+) |
| **Release Date** | 2026-05-20 (4.6.3 maintenance), 2026-01-26 (4.6 feature release) |
| **Project Pinned** | 2026-06-02 |
| **Last Docs Verified** | 2026-06-02 |
| **LLM Knowledge Cutoff** | May 2025 |
| **Risk Level** | 🔴 **HIGH** — 4.4, 4.5, 4.6 全部在 LLM 训练数据之外 |

## Knowledge Gap Warning

LLM 的训练数据大约覆盖到 Godot **4.3**。版本 **4.4、4.5、4.6** 引入了大量
变更，模型几乎一无所知 —— 包括：

- C# bindings 的新 `[Obsolete]` 标注、enum 重命名、变换的方法签名
- Jolt Physics 成为 3D 默认（Godot 4.6）
- D3D12 成为 Windows 默认后端
- Glow 渲染顺序改变
- UI dual-focus 系统
- 大量 `[Signal]` delegate / `[Export]` 属性的新用法

**在建议任何 Godot API 之前，必须先 cross-reference 本目录的文档。**

## Maintenance Releases (since 4.6 feature release)

| 版本 | 发布日 | 内容 |
|---|---|---|
| 4.6.3 | 2026-05-20 | 稳定性与可用性 bug 修复（**当前 pin 版本**）|
| 4.6.2 | 2026-04-01 | 第二个 4.6 maintenance |
| 4.6.1 | 2026-02-16 | 第一个 4.6 maintenance |
| 4.6 | 2026-01-26 | "All about your flow" feature release |

Maintenance 版本通常不引入 API 破坏，仅为 bug fix。从 4.6 升 4.6.1/2/3 安全。

## Post-Cutoff Version Timeline

| 版本 | 发布 | 风险 | 关键主题 |
|---|---|---|---|
| 4.4 | Mid 2025 | MEDIUM | UID 系统、Manifold CSG、SceneTree 优化、`FileAccess.store_*` 返回类型改变、CSG 不再支持 non-manifold |
| 4.5 | Sep 2025 | HIGH | AccessKit 可达性、Variadic args、`@abstract`、Shader Baker、SMAA、`duplicate_deep()` |
| 4.6 | Jan 2026 | HIGH | Jolt 默认 3D 物理、D3D12 默认、Glow 重做、IK 恢复、Dual-focus UI、Modern theme |
| 4.6.1-3 | 2026 Q1-Q2 | LOW | Bug fix only |

## C# 特别说明

由于本项目用 C#，需特别注意：

- **所有 C# 类必须声明为 `partial`** —— Godot 源生成器在 build 时注入代码
- **Signal 使用 delegate**：用 `[Signal] public delegate void HealthChangedEventHandler(int newValue);`
  连接：`HealthChanged += OnHealthChanged;`（事件式，类似 C# event）
- **Export 属性**：用 `[Export] public float MoveSpeed { get; set; } = 5.0f;`
- **.NET 版本**：Godot 4.4+ 已升至 .NET 8（旧版本是 .NET 6）—— 项目应使用 .NET 8 SDK
- **C# 在 Android/iOS 导出**：实验性支持自 4.2 起；Godot 4.4 起改进，但仍不如 GDScript 稳定（**纯 PC 项目不影响**）
- **Obsolete 标注**：Godot 4.5+ 在 C# bindings 上添加 `[Obsolete]` 特性，IDE 会直接提示过时 API

## Verified Sources

- 官方文档：https://docs.godotengine.org/en/stable/
- 4.5 → 4.6 迁移：https://docs.godotengine.org/en/stable/tutorials/migrating/upgrading_to_godot_4.6.html
- 4.4 → 4.5 迁移：https://docs.godotengine.org/en/stable/tutorials/migrating/upgrading_to_godot_4.5.html
- 4.3 → 4.4 迁移：https://docs.godotengine.org/en/stable/tutorials/migrating/upgrading_to_godot_4.4.html
- 4.6 release notes：https://godotengine.org/releases/4.6/
- 4.6.3 release：https://github.com/godotengine/godot/releases/tag/4.6.3-stable
- Interactive changelog：https://godotengine.github.io/godot-interactive-changelog/

## Refresh Cadence

运行 `/setup-engine refresh` 可刷新本目录所有文档至最新状态。建议每 3 个月或在升级前刷新。
