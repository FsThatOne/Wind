# Godot Engine — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Godot 4.7-stable |
| **Language** | C# (.NET 8+) |
| **Release Date** | 2026-06-18 (4.7 feature release) |
| **Project Pinned** | 2026-06-20 |
| **Last Docs Verified** | 2026-06-20 (4.7 migration guide verified; module pages pending `/setup-engine refresh`) |
| **LLM Knowledge Cutoff** | May 2025 |
| **Risk Level** | 🔴 **HIGH** — 4.4, 4.5, 4.6, 4.7 全部在 LLM 训练数据之外 |

## Knowledge Gap Warning

LLM 的训练数据大约覆盖到 Godot **4.3**。版本 **4.4、4.5、4.6、4.7** 引入了大量
变更，模型几乎一无所知 —— 包括：

- C# bindings 的新 `[Obsolete]` 标注、enum 重命名、变换的方法签名
- Jolt Physics 成为 3D 默认（Godot 4.6）
- D3D12 成为 Windows 默认后端
- Glow 渲染顺序改变
- UI dual-focus 系统（Godot 4.6）
- HDR 输出、AreaLight3D 节点、Control 节点 offset transforms（Godot 4.7）
- Wayland 触控交互、Asset Store 重做（Godot 4.7）
- 大量 `[Signal]` delegate / `[Export]` 属性的新用法

**在建议任何 Godot API 之前，必须先 cross-reference 本目录的文档。**
**4.7 module pages 在 `/setup-engine refresh` 完成前仍为 STALE；遇到 4.7-specific API 时必须先查官方文档。**

## Release Timeline (since 4.6 feature release)

| 版本 | 发布日 | 内容 |
|---|---|---|
| 4.7 | 2026-06-18 | "HDR + AreaLight3D + Asset Store + Wayland 触控 + Control offset transforms" feature release（**当前 pin 版本**）|
| 4.6.3 | 2026-05-20 | 稳定性与可用性 bug 修复（previous pin）|
| 4.6.2 | 2026-04-01 | 第二个 4.6 maintenance |
| 4.6.1 | 2026-02-16 | 第一个 4.6 maintenance |
| 4.6 | 2026-01-26 | "All about your flow" feature release |

Maintenance 版本通常不引入 API 破坏，仅为 bug fix。从 4.6.x 升 4.7 是 minor 版本升级（feature release），可能引入 minor compatibility breakage。

## Post-Cutoff Version Timeline

| 版本 | 发布 | 风险 | 关键主题 |
|---|---|---|---|
| 4.4 | Mid 2025 | MEDIUM | UID 系统、Manifold CSG、SceneTree 优化、`FileAccess.store_*` 返回类型改变、CSG 不再支持 non-manifold |
| 4.5 | Sep 2025 | HIGH | AccessKit 可达性、Variadic args、`@abstract`、Shader Baker、SMAA、`duplicate_deep()` |
| 4.6 | Jan 2026 | HIGH | Jolt 默认 3D 物理、D3D12 默认、Glow 重做、IK 恢复、Dual-focus UI、Modern theme |
| 4.6.1-3 | 2026 Q1-Q2 | LOW | Bug fix only |
| 4.7 | Jun 2026 | HIGH | HDR 输出、AreaLight3D、Asset Store 重做、Wayland 触控、Control offset transforms、shader inline preview、Android 直接发布 |

## C# 特别说明

由于本项目用 C#，需特别注意：

- **所有 C# 类必须声明为 `partial`** —— Godot 源生成器在 build 时注入代码
- **Signal 使用 delegate**：用 `[Signal] public delegate void HealthChangedEventHandler(int newValue);`
  连接：`HealthChanged += OnHealthChanged;`（事件式，类似 C# event）
- **Export 属性**：用 `[Export] public float MoveSpeed { get; set; } = 5.0f;`
- **.NET 版本**：Godot 4.4+ 已升至 .NET 8（旧版本是 .NET 6）—— 项目应使用 .NET 8 SDK
- **C# 在 Android/iOS 导出**：实验性支持自 4.2 起；Godot 4.4 起改进；4.7 Android 端可直接从设备导出，但 C# 在 Android 仍不如 GDScript 稳定（**纯 PC 项目不影响**）
- **Obsolete 标注**：Godot 4.5+ 在 C# bindings 上添加 `[Obsolete]` 特性，IDE 会直接提示过时 API

## 4.6.3 → 4.7 升级注记 (2026-06-20)

本项目于 2026-06-20 从 Godot 4.6.3 升级到 Godot 4.7-stable。迁移依据官方 4.6 → 4.7 upgrade guide；当前已完成项目配置、C# SDK pin、活跃 sprint 目标与 Godot import smoke 的迁移。

项目审计结果：
- 未发现 `InputEvent.device == 0` / `InputEvent.Device == 0` 用法；暂不受 4.7 鼠标/键盘 device id 常量变更影响。
- 未发现 `RichTextLabel.add_image/update_image` 旧参数用法；虽然旧 combat concept 使用 `RichTextLabel`，但未触发 4.7 改签名 API。
- 未发现 `AudioEffectSpectrumAnalyzer.tap_back_pos`、`Animation.Length`、`RenderingServer.particles_request_process_time`、`EditorSceneFormatImporter.IMPORT_*`、`SoftBody3D`、`WorldBoundaryShape3D` 等迁移命中。
- Godot C# project `.csproj` 已从 `Godot.NET.Sdk/4.6.3` 升至 `Godot.NET.Sdk/4.7.0`。

4.7 迁移时需要重点留意：
- `InputEvent.device` 判断鼠标/键盘时使用 `InputEvent.DEVICE_ID_MOUSE` / `InputEvent.DEVICE_ID_KEYBOARD`，不要用 `0`。
- `RichTextLabel.add_image/update_image` 的 percent 参数改名/改类型；后续若做富文本插图 UI，必须按 4.7 API 写。
- `CanvasItem` 画线不再自动添加 antialias feather；依赖线条厚度观感的 UI/调试绘制需要复查。
- 若启用 `Area2D/Area3D` audio bus override，`AudioStreamPlayer.area_mask` 默认值变化可能导致 override 不生效。
- Jolt 3D 物理的 `WorldBoundaryShape3D`、`SoftBody3D` 与 `Area3D` overlap 行为有变更；当前项目未命中，但后续 3D 原型需复查。
- 动态字体 importer 的 `hinting` 默认值从 1 改为 3；像素字体或中文字体观感需要在 UI evidence 中确认。

待办：
- [ ] 运行 `/setup-engine refresh` 重灌 `docs/engine-reference/godot/modules/*` 与 `current-best-practices.md` / `breaking-changes.md` / `deprecated-apis.md`
- [ ] cu-006-godot-integration story 中 4 个 engine behavior 需在 4.7 下 spike 重验（`Tween.TweenProcessMode.Always`、`Camera2D.PositionSmoothingEnabled`、`InputMap`、`Engine.TimeScale`）
- [ ] 下次 `/architecture-review` 时检查 4.7 breaking changes 对 ADR-0002 / ADR-0011 的影响

## Verified Sources

- 官方文档：https://docs.godotengine.org/en/stable/
- 4.5 → 4.6 迁移：https://docs.godotengine.org/en/stable/tutorials/migrating/upgrading_to_godot_4.6.html
- 4.4 → 4.5 迁移：https://docs.godotengine.org/en/stable/tutorials/migrating/upgrading_to_godot_4.5.html
- 4.3 → 4.4 迁移：https://docs.godotengine.org/en/stable/tutorials/migrating/upgrading_to_godot_4.4.html
- 4.6 release notes：https://godotengine.org/releases/4.6/
- 4.6.3 release：https://github.com/godotengine/godot/releases/tag/4.6.3-stable
- 4.6 → 4.7 迁移：https://docs.godotengine.org/en/4.7/tutorials/migrating/upgrading_to_godot_4.7.html
- 4.7 release notes：https://godotengine.org/releases/4.7/ (待运行 `/setup-engine refresh` 验证完整 release notes)
- Interactive changelog：https://godotengine.github.io/godot-interactive-changelog/

## Refresh Cadence

运行 `/setup-engine refresh` 可刷新本目录所有文档至最新状态。建议每 3 个月或在升级前刷新。
**当前状态**：4.6.3 → 4.7-stable 升级后 migration guide 已审计；module pages 仍待 `/setup-engine refresh`。
