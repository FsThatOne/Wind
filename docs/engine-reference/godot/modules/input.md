# Godot Input — Quick Reference

Last verified: 2026-02-12 | Engine: Godot 4.7-stable

> **STALE — Engine pin upgraded 4.6.3 → 4.7-stable on 2026-06-20.** This module page was last verified on 2026-02-12 against 4.6 and has NOT been refreshed for 4.7 changes. Treat all 4.5/4.6 entries below as historically accurate; verify any API decisions against live `godot-docs` for 4.7 before implementation. Pending: `/setup-engine godot 4.7` refresh sweep.

## What Changed Since ~4.3 (LLM Cutoff)

### 4.6 Changes
- **Dual-focus system**: Mouse/touch focus is now separate from keyboard/gamepad focus
  - Visual feedback differs by input method
  - Custom focus implementations may need updating
- **Select Mode keybind changed**: "Select Mode" is now `v` key; old mode renamed "Transform Mode" (`q` key)

### 4.5 Changes
- **SDL3 gamepad driver**: Gamepad handling delegated to SDL library for better cross-platform support
- **Recursive Control disable**: Single property disables mouse/focus for entire node hierarchies

### 4.3 Changes (in training data)
- **InputEventShortcut**: Dedicated event type for menu shortcuts (optional)

## Current API Patterns

### Input Actions (unchanged)
```gdscript
func _physics_process(delta: float) -> void:
    var input_dir: Vector2 = Input.get_vector(
        &"move_left", &"move_right", &"move_forward", &"move_back"
    )
    if Input.is_action_just_pressed(&"jump"):
        jump()
```

### Input Events (unchanged)
```gdscript
func _unhandled_input(event: InputEvent) -> void:
    if event is InputEventMouseButton:
        if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
            handle_click(event.position)
    elif event is InputEventKey:
        if event.keycode == KEY_ESCAPE and event.pressed:
            toggle_pause()
```

### Focus Management (4.6 — CHANGED)
```gdscript
# Mouse/touch and keyboard/gamepad focus are now SEPARATE
# Visual styles may differ depending on which input method is active
# If you have custom focus drawing, test with both input methods

# Standard approach still works:
func _ready() -> void:
    %StartButton.grab_focus()  # Keyboard/gamepad focus

# But be aware: mouse hover focus != keyboard focus in 4.6
```

### Gamepad (4.5+ — SDL3 backend)
```gdscript
# API unchanged, but SDL3 provides:
# - Better device detection across platforms
# - Improved rumble support
# - More consistent button mapping

func _input(event: InputEvent) -> void:
    if event is InputEventJoypadButton:
        if event.button_index == JOY_BUTTON_A and event.pressed:
            confirm_selection()
```

## Common Mistakes
- Not testing both mouse and keyboard focus paths (dual-focus in 4.6)
- Assuming `grab_focus()` affects mouse focus (it only affects keyboard/gamepad in 4.6)
- Using string literals instead of `StringName` (`&"action"`) for action names in hot paths
