# Godot — Current Best Practices

Last verified: 2026-06-02 | Engine: Godot 4.7-stable

> **STALE — Engine pin upgraded 4.6.3 → 4.7-stable on 2026-06-20.** This page was last verified on 2026-06-02 against 4.6.3 and has NOT been refreshed for 4.7 changes. All 4.5/4.6 entries below remain historically accurate; verify any 4.7-specific API decisions against live `godot-docs` before implementation. Pending: `/setup-engine godot 4.7` refresh sweep.

Practices that are **new or changed** since the model's training data (~4.3).
This supplements (not replaces) the agent's built-in knowledge.

## C# in Godot 4.6 (PROJECT LANGUAGE)

> 本项目使用 C# (.NET 8+) 作为主语言。以下规范在 4.4-4.6 期间引入或强化。

### 类与节点
- **所有节点脚本类必须声明为 `partial`**：Godot 源生成器在 build 时注入代码
  ```csharp
  using Godot;

  public partial class PlayerController : CharacterBody2D
  {
      [Export] public float MoveSpeed { get; set; } = 200f;
  }
  ```

### Signals（信号）
- **使用 `[Signal] delegate` 声明**，连接走 C# event 语法（**4.5+ 推荐**）
  ```csharp
  [Signal] public delegate void HealthChangedEventHandler(int newValue, int oldValue);

  // 触发
  EmitSignal(SignalName.HealthChanged, 80, 100);

  // 订阅（type-safe）
  enemy.HealthChanged += OnEnemyHealthChanged;
  ```
- **禁止**用 string-based `Connect("signal_name", callable)` —— 已被替代且不 type-safe

### Exports（导出到 Inspector）
- **属性导出**：用 `{ get; set; }` 而非 public field（更符合 C# 惯例）
  ```csharp
  [Export] public float JumpVelocity { get; set; } = -400f;
  [Export] public PackedScene EnemyScene { get; set; }
  [Export(PropertyHint.Range, "0,100,1")] public int MaxHealth { get; set; } = 100;
  ```

### Resource Duplication（4.5+）
- 嵌套资源用 `DuplicateDeep()` 而非 `Duplicate(true)`
  ```csharp
  var copy = original.DuplicateDeep(); // 显式深拷贝
  ```

### Translation Strings 自动提取（4.6）
- C# 字符串字面量可被自动提取到翻译表 —— 配合 `Tr()` 调用
  ```csharp
  label.Text = Tr("OBJECTIVE_FIND_MASTER"); // 自动加入 .pot
  ```

### .NET 8 & async
- 使用标准 .NET 8 `async/await` —— Godot 4.4+ 完整支持
- 帧同步等待用 `await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);`

### Obsolete 警告（4.5+）
- IDE 会显示 `[Obsolete("...")]` 警告 —— **不要忽略**，按提示替换 API

### 注意：C# 在 mobile 导出
- 当前本项目纯 PC，不受影响
- 如未来移植 mobile：C# 在 Android/iOS 是实验性的，不如 GDScript 稳定

---

## GDScript (4.5+)

- **Variadic arguments**: Functions can accept arbitrary parameter counts
  ```gdscript
  func log_values(prefix: String, values: Variant...) -> void:
      for v in values:
          print(prefix, ": ", v)
  ```

- **Abstract classes and methods**: Use `@abstract` to enforce inheritance
  ```gdscript
  @abstract
  class_name BaseEnemy extends CharacterBody3D

  @abstract
  func get_attack_pattern() -> Array[Attack]:
      pass  # Subclasses MUST override
  ```

- **Script backtracing**: Detailed call stacks available even in Release builds

## Physics (4.6)

- **Jolt Physics is the default 3D engine** for new projects
  - Better determinism and stability than GodotPhysics3D
  - Some HingeJoint3D properties (`damp`) only work with GodotPhysics
  - Switch: Project Settings → Physics → 3D → Physics Engine
  - 2D physics unchanged (still Godot Physics 2D)

## Rendering (4.6)

- **D3D12 is the default backend on Windows** (was Vulkan) — for better driver compatibility
- **Glow now processes before tonemapping** with screen blending mode — existing glow setups may look different
- **SSR overhauled** — significant improvement in realism, stability, and performance
- **AgX tonemapper** — new white point and contrast controls

## Rendering (4.5)

- **Shader Baker**: Pre-compile shaders to eliminate startup hitching
- **SMAA 1x**: New AA option — sharper than FXAA, cheaper than TAA
- **Stencil buffer**: Available for advanced masking/portal effects
- **Bent normal maps**: Directional occlusion in normal map textures
- **Specular occlusion**: Ambient occlusion now affects reflections

## Accessibility (4.5+)

- **Screen reader support**: Control nodes integrate with accessibility tools via AccessKit
- **Live translation preview**: Test GUI layouts in different languages directly in-editor
- **FoldableContainer**: New accordion-style UI node for collapsible sections
- **Recursive Control disable**: Disable mouse/focus interactions for entire node hierarchies with a single property

## Animation (4.5+)

- **BoneConstraint3D**: Bind bones to other bones with modifiers
  - AimModifier3D, CopyTransformModifier3D, ConvertTransformModifier3D

## Animation (4.6)

- **IK system fully restored**: Complete inverse kinematics reintroduced for 3D
  - Available modifiers: CCDIK, FABRIK, Jacobian IK, Spline IK, TwoBoneIK
  - Applied via `SkeletonModifier3D` nodes

## Resources (4.5+)

- **`duplicate_deep()`**: Explicit deep duplication for nested resource trees
  - Old `duplicate()` behavior retained for backward compatibility
  - Use `duplicate_deep()` when you need per-instance copies of nested resources

## Navigation (4.5+)

- **Dedicated 2D navigation server**: No longer proxied through 3D NavigationServer
  - Reduces export binary size for 2D-only games

## UI (4.6)

- **Dual-focus system**: Mouse/touch focus is now separate from keyboard/gamepad focus
  - Visual feedback differs depending on input method
  - Consider this when designing custom focus behavior

## Editor Workflow (4.6)

- Flexible dock drag-and-drop with blue outline preview (including bottom panel)
- Most panels support floating windows (except Debugger)
- New keyboard shortcuts: Alt+O (Output), Alt+S (Shader)
- Export variable auto-generation: drag resource from FileSystem into script editor
- Live preview in Quick Open dialog when "Live Preview" enabled
- New "Select Mode" (v key) prevents accidental transforms; old mode renamed "Transform Mode" (q key)

## Tooling

- **ripgrep has no `gdscript` type**: `*.gd` is registered under `gap` (GAP programming language).
  `rg --type gdscript` is a hard error — the search never executes.
  Always use `rg --glob "*.gd"` (shell) or `glob: "*.gd"` (Grep tool) to filter GDScript files.

## Platform (4.5+)

- **visionOS export**: First new platform since open-sourcing (windowed app mode)
- **SDL3 gamepad driver**: Better cross-platform gamepad support
- **Android**: Edge-to-edge display, camera feed access, 16KB page support (Android 15+)
- **Linux**: Wayland subwindow support for multi-window capability
