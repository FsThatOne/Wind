# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Godot 4.6.3
- **Language**: C# (.NET 8+, primary); C++ via GDExtension (native plugins only)
- **Rendering**: Compatibility renderer (GL ES 3.0) — 2D 像素项目，无 3D 需求，最快启动 + 最广 GPU 兼容（含 Steam Deck）
- **Physics**: Godot 内置 2D 物理（CharacterBody2D / Area2D 为主）

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: PC (Steam, v1.0)
- **Input Methods**: Keyboard/Mouse, Gamepad
- **Primary Input**: Keyboard/Mouse
- **Gamepad Support**: Full (Steam Deck 友好)
- **Touch Support**: None
- **Platform Notes**: 所有 UI 必须支持手柄完整导航，禁止 hover-only 交互；Steam Deck 兼容是 v1.0 目标；中文文字渲染需在 PC 与 Steam Deck 上验证清晰度

## Naming Conventions

- **Classes**: PascalCase（如 `PlayerController`）—— 必须声明为 `partial`
- **Variables**: Public 属性/字段 PascalCase（如 `MoveSpeed`、`JumpVelocity`）；Private 字段 `_camelCase`（如 `_currentHealth`、`_isGrounded`）
- **Signals/Events**: PascalCase + `EventHandler` 后缀（如 `HealthChangedEventHandler`）
- **Files**: PascalCase 匹配类名（如 `PlayerController.cs`）
- **Scenes/Prefabs**: PascalCase 匹配根节点（如 `PlayerController.tscn`）
- **Constants**: PascalCase（如 `MaxHealth`、`DefaultMoveSpeed`）

## Performance Budgets

- **Target Framerate**: 60 FPS (PC) / 30 FPS (Steam Deck guaranteed minimum)
- **Frame Budget**: 16.67ms (PC @ 60 FPS) / 33.33ms (Steam Deck @ 30 FPS)
- **Draw Calls**: ≤ 200 per frame (2D pixel; TileMapLayer + UI + particles)
- **Memory Ceiling**: 512 MB (含已加载场景 + 所有缓存纹理)
- **Concurrent UI Tweens**: ≤ 8 同屏并发（避免 GC spike）
- **Scene Load Time**: ≤ 2s (异步预加载 + 加载屏遮罩)

## Testing

- **Framework**: GdUnit4（Godot 原生测试框架，同时支持 GDScript 与 C#，与编辑器深度集成）
- **Minimum Coverage**: [TO BE CONFIGURED — 等首个系统落地后再设阈值]
- **Required Tests**: 平衡公式（功力曲线、克制系数、伤害公式）、核心叙事系统（心境双轴位移、暗号/书信触发条件、误会触发与释怀路径）、存档序列化反序列化；不强制 UI 自动化测试

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- [None configured yet — add as architectural decisions are made]

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- **YamlDotNet** (NuGet) — YAML 1.2 解析/序列化，配置数据加载 (ADR-0003)

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- **Master Architecture**: [architecture.md](../../docs/architecture/architecture.md) — 5-layer architecture (Platform/Foundation/Core/Feature/Presentation)
- ADR-001 (P0): EventBus 实现方案 — [Proposed](../../docs/architecture/adr-0001-event-bus-architecture.md)
- ADR-002 (P0): UI 框架选型 (dual-focus 适配) — [Proposed](../../docs/architecture/adr-0002-ui-framework-dual-focus.md)
- ADR-003 (P1): 数据配置格式 — [Proposed](../../docs/architecture/adr-0003-data-configuration-format.md)
- ADR-004 (P1): 存档加密方案 — [Proposed](../../docs/architecture/adr-0004-save-encryption.md)
- ADR-005 (P1): 对话系统格式 — [Proposed](../../docs/architecture/adr-0005-dialogue-format.md)
- ADR-006 (P1): 场景加载策略 — [Proposed](../../docs/architecture/adr-0006-scene-loading-strategy.md)
- ADR-007 (P1): 输入系统适配 — [Proposed](../../docs/architecture/adr-0007-input-system.md)
- ADR-008 (P2): FSM 实现方案 — [Proposed](../../docs/architecture/adr-0008-finite-state-machine.md)
- ADR-009 (P2): 动态音乐方案 — [Proposed](../../docs/architecture/adr-0009-dynamic-audio.md)
- ADR-010 (P2): TileMapLayer 使用模式 — [Proposed](../../docs/architecture/adr-0010-tilemaplayer-usage.md)

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: godot-specialist
- **Language/Code Specialist**: godot-csharp-specialist（所有 .cs 文件）
- **Shader Specialist**: godot-shader-specialist（.gdshader 文件、VisualShader 资源）
- **UI Specialist**: godot-specialist（无独立 UI 专家 —— 由 primary 覆盖全部 UI）
- **Additional Specialists**: godot-gdextension-specialist（仅 GDExtension / 原生 C++ 绑定时）
- **Routing Notes**: 架构决策、ADR 校验、跨切代码 review 调 primary。代码质量、[Signal] delegate 模式、[Export] 属性、.csproj 管理、C# Godot idiom 调 C# specialist。材质设计与 shader 代码调 shader specialist。仅在原生 C++ 插件涉及时调 GDExtension specialist。

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->
<!-- If a row says [TO BE CONFIGURED], fall back to Primary for that file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | godot-csharp-specialist |
| Shader / material files (.gdshader, VisualShader) | godot-shader-specialist |
| UI / screen files (Control nodes, CanvasLayer) | godot-specialist |
| Scene / prefab / level files (.tscn, .tres) | godot-specialist |
| Project config (.csproj, NuGet) | godot-csharp-specialist |
| Native extension / plugin files (.gdextension, C++) | godot-gdextension-specialist |
| General architecture review | godot-specialist |
