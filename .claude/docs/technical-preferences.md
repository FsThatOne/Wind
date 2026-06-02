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

- **Target Framerate**: [TO BE CONFIGURED]
- **Frame Budget**: [TO BE CONFIGURED]
- **Draw Calls**: [TO BE CONFIGURED]
- **Memory Ceiling**: [TO BE CONFIGURED]

## Testing

- **Framework**: GdUnit4（Godot 原生测试框架，同时支持 GDScript 与 C#，与编辑器深度集成）
- **Minimum Coverage**: [TO BE CONFIGURED — 等首个系统落地后再设阈值]
- **Required Tests**: 平衡公式（功力曲线、克制系数、伤害公式）、核心叙事系统（心境双轴位移、暗号/书信触发条件、误会触发与释怀路径）、存档序列化反序列化；不强制 UI 自动化测试

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- [None configured yet — add as architectural decisions are made]

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- [None configured yet — add as dependencies are approved]

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]

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
