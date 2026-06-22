# Scene Markup Tool Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Godot editor plugin that lets a level author draw polygon regions over a full background image and generate native collision, walkable, and foreground occlusion nodes.

**Architecture:** The plugin is a GDScript `EditorPlugin` with a dock UI and a small builder helper. Runtime data is pure Godot nodes under `SceneMarkupRoot`, so gameplay scenes do not depend on editor scripts.

**Tech Stack:** Godot 4.7-stable, GDScript editor plugin, generated `StaticBody2D`, `CollisionPolygon2D`, `NavigationRegion2D`, `NavigationPolygon`, and `Polygon2D`.

## Global Constraints

- Target project is `feng-zhi`.
- Engine is Godot 4.7-stable.
- Plugin implementation uses GDScript.
- Existing gameplay code remains C# and is not rewritten for this plugin.
- Generated runtime content must be native Godot nodes saved in `.tscn`.
- First version supports point-by-point polygon drawing only.
- First version supports collision, walkable, and occluder modes.
- First version does not export JSON, YAML, or `.tres` annotation data.
- First version does not implement freehand drawing, auto edge detection, boolean polygon operations, runtime hot editing, or automatic image cutting.
- Do not commit unless the user explicitly asks for a commit.
- Do not modify `StartCave.tscn` with sample markup unless the user explicitly approves saving scene markings.

---

## File Structure

- Create `feng-zhi/addons/scene_markup_tool/plugin.cfg`  
  Godot plugin manifest.

- Create `feng-zhi/addons/scene_markup_tool/scene_markup_plugin.gd`  
  EditorPlugin entry point. Adds/removes dock, forwards 2D viewport input and overlay drawing, exposes `EditorInterface` and `EditorUndoRedoManager` to the dock.

- Create `feng-zhi/addons/scene_markup_tool/scene_markup_dock.gd`  
  Dock UI, plugin state, background selection, mode selection, point collection, preview status, and calls into the builder.

- Create `feng-zhi/addons/scene_markup_tool/scene_markup_builder.gd`  
  Focused helper for finding/creating `SceneMarkupRoot`, generating typed nodes, setting owners for scene saving, assigning collision layers, building `NavigationPolygon`, and calculating occluder UVs.

- Create `feng-zhi/addons/scene_markup_tool/tests/scene_markup_builder_smoke_test.gd`  
  Headless smoke test script for builder functions that can run without the editor UI.

- Modify `feng-zhi/project.godot`  
  Add the plugin to `[editor_plugins]` only after the plugin loads cleanly.

---

### Task 1: Builder Helper And Smoke Test Harness

**Files:**
- Create: `feng-zhi/addons/scene_markup_tool/scene_markup_builder.gd`
- Create: `feng-zhi/addons/scene_markup_tool/tests/scene_markup_builder_smoke_test.gd`

**Interfaces:**
- Produces: `SceneMarkupBuilder.ensure_markup_root(scene_root: Node) -> Node2D`
- Produces: `SceneMarkupBuilder.ensure_layer(root: Node2D, layer_name: String) -> Node2D`
- Produces: `SceneMarkupBuilder.create_collision(scene_root: Node, points: PackedVector2Array) -> StaticBody2D`
- Produces: `SceneMarkupBuilder.create_walkable(scene_root: Node, points: PackedVector2Array) -> NavigationRegion2D`
- Produces: `SceneMarkupBuilder.create_occluder(scene_root: Node, background: Sprite2D, points: PackedVector2Array, z_index: int) -> Polygon2D`
- Produces: `SceneMarkupBuilder.calculate_background_uv(background: Sprite2D, points: PackedVector2Array) -> PackedVector2Array`
- Consumes: Godot native node/resource APIs only.

- [ ] **Step 1: Write the builder helper with native node generation**

Create `feng-zhi/addons/scene_markup_tool/scene_markup_builder.gd`:

```gdscript
@tool
class_name SceneMarkupBuilder
extends RefCounted

const ROOT_NAME := "SceneMarkupRoot"
const COLLISIONS_NAME := "Collisions"
const WALKABLE_NAME := "Walkable"
const OCCLUDERS_NAME := "Occluders"

static func ensure_markup_root(scene_root: Node) -> Node2D:
	var existing := scene_root.get_node_or_null(ROOT_NAME)
	if existing is Node2D:
		return existing

	var root := Node2D.new()
	root.name = ROOT_NAME
	scene_root.add_child(root)
	root.owner = scene_root
	return root

static func ensure_layer(root: Node2D, layer_name: String) -> Node2D:
	var existing := root.get_node_or_null(layer_name)
	if existing is Node2D:
		return existing

	var layer := Node2D.new()
	layer.name = layer_name
	root.add_child(layer)
	layer.owner = root.owner
	return layer

static func create_collision(scene_root: Node, points: PackedVector2Array) -> StaticBody2D:
	_require_polygon(points)
	var markup_root := ensure_markup_root(scene_root)
	var layer := ensure_layer(markup_root, COLLISIONS_NAME)

	var body := StaticBody2D.new()
	body.name = _next_name(layer, "Collision")
	body.collision_layer = 1
	body.collision_mask = 1
	layer.add_child(body)
	body.owner = scene_root

	var polygon := CollisionPolygon2D.new()
	polygon.name = "CollisionPolygon2D"
	polygon.polygon = points
	body.add_child(polygon)
	polygon.owner = scene_root
	return body

static func create_walkable(scene_root: Node, points: PackedVector2Array) -> NavigationRegion2D:
	_require_polygon(points)
	var markup_root := ensure_markup_root(scene_root)
	var layer := ensure_layer(markup_root, WALKABLE_NAME)

	var region := NavigationRegion2D.new()
	region.name = _next_name(layer, "Walkable")
	layer.add_child(region)
	region.owner = scene_root

	var navigation_polygon := NavigationPolygon.new()
	navigation_polygon.vertices = points

	var indices := PackedInt32Array()
	for index in points.size():
		indices.append(index)
	navigation_polygon.add_polygon(indices)

	region.navigation_polygon = navigation_polygon
	return region

static func create_occluder(scene_root: Node, background: Sprite2D, points: PackedVector2Array, z_index: int) -> Polygon2D:
	_require_polygon(points)
	if background.texture == null:
		push_error("Background Sprite2D has no texture.")
		return null

	var markup_root := ensure_markup_root(scene_root)
	var layer := ensure_layer(markup_root, OCCLUDERS_NAME)

	var occluder := Polygon2D.new()
	occluder.name = _next_name(layer, "Occluder")
	occluder.polygon = points
	occluder.texture = background.texture
	occluder.uv = calculate_background_uv(background, points)
	occluder.z_index = z_index
	occluder.antialiased = true
	layer.add_child(occluder)
	occluder.owner = scene_root
	return occluder

static func calculate_background_uv(background: Sprite2D, points: PackedVector2Array) -> PackedVector2Array:
	var uv := PackedVector2Array()
	for point in points:
		var local_point := background.to_local(point)
		if background.centered and background.texture != null:
			local_point += background.texture.get_size() * 0.5
		uv.append(local_point)
	return uv

static func _require_polygon(points: PackedVector2Array) -> void:
	if points.size() < 3:
		push_error("Scene markup polygons need at least 3 points.")

static func _next_name(parent: Node, prefix: String) -> String:
	var index := 1
	while true:
		var candidate := "%s_%03d" % [prefix, index]
		if parent.get_node_or_null(candidate) == null:
			return candidate
		index += 1
```

- [ ] **Step 2: Write the smoke test script**

Create `feng-zhi/addons/scene_markup_tool/tests/scene_markup_builder_smoke_test.gd`:

```gdscript
extends SceneTree

const Builder := preload("res://addons/scene_markup_tool/scene_markup_builder.gd")

func _init() -> void:
	var failed := false
	failed = _test_layers_and_collision() or failed
	failed = _test_walkable_navigation_polygon() or failed
	failed = _test_occluder_uv_for_scaled_uncentered_background() or failed
	quit(1 if failed else 0)

func _test_layers_and_collision() -> bool:
	var scene_root := Node2D.new()
	scene_root.name = "SmokeScene"
	var points := PackedVector2Array([
		Vector2(10, 10),
		Vector2(110, 10),
		Vector2(110, 80),
		Vector2(10, 80),
	])

	var body := Builder.create_collision(scene_root, points)
	var collision_polygon := body.get_node("CollisionPolygon2D") as CollisionPolygon2D
	var ok := scene_root.has_node("SceneMarkupRoot/Collisions/Collision_001")
	ok = ok and collision_polygon.polygon == points
	ok = ok and body.owner == scene_root
	ok = ok and collision_polygon.owner == scene_root
	scene_root.free()
	return _report("collision generation", ok)

func _test_walkable_navigation_polygon() -> bool:
	var scene_root := Node2D.new()
	scene_root.name = "SmokeScene"
	var points := PackedVector2Array([
		Vector2(0, 0),
		Vector2(64, 0),
		Vector2(64, 64),
		Vector2(0, 64),
	])

	var region := Builder.create_walkable(scene_root, points)
	var navigation_polygon := region.navigation_polygon
	var ok := scene_root.has_node("SceneMarkupRoot/Walkable/Walkable_001")
	ok = ok and navigation_polygon != null
	ok = ok and navigation_polygon.vertices == points
	ok = ok and navigation_polygon.get_polygon_count() == 1
	scene_root.free()
	return _report("walkable generation", ok)

func _test_occluder_uv_for_scaled_uncentered_background() -> bool:
	var scene_root := Node2D.new()
	scene_root.name = "SmokeScene"

	var image := Image.create(200, 100, false, Image.FORMAT_RGBA8)
	image.fill(Color.WHITE)
	var texture := ImageTexture.create_from_image(image)

	var background := Sprite2D.new()
	background.name = "Background"
	background.texture = texture
	background.centered = false
	background.scale = Vector2(2, 2)
	scene_root.add_child(background)
	background.owner = scene_root

	var points := PackedVector2Array([
		Vector2(20, 10),
		Vector2(60, 10),
		Vector2(60, 40),
		Vector2(20, 40),
	])
	var occluder := Builder.create_occluder(scene_root, background, points, 20)
	var expected_uv := PackedVector2Array([
		Vector2(10, 5),
		Vector2(30, 5),
		Vector2(30, 20),
		Vector2(10, 20),
	])
	var ok := scene_root.has_node("SceneMarkupRoot/Occluders/Occluder_001")
	ok = ok and occluder.texture == texture
	ok = ok and occluder.uv == expected_uv
	ok = ok and occluder.z_index == 20
	scene_root.free()
	return _report("occluder uv generation", ok)

func _report(label: String, ok: bool) -> bool:
	if ok:
		print("PASS: %s" % label)
		return false
	push_error("FAIL: %s" % label)
	return true
```

- [ ] **Step 3: Run the smoke test and verify it passes**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path feng-zhi --script res://addons/scene_markup_tool/tests/scene_markup_builder_smoke_test.gd
```

Expected:

```text
PASS: collision generation
PASS: walkable generation
PASS: occluder uv generation
```

---

### Task 2: Editor Plugin Shell And Dock Skeleton

**Files:**
- Create: `feng-zhi/addons/scene_markup_tool/plugin.cfg`
- Create: `feng-zhi/addons/scene_markup_tool/scene_markup_plugin.gd`
- Create: `feng-zhi/addons/scene_markup_tool/scene_markup_dock.gd`

**Interfaces:**
- Consumes: `SceneMarkupBuilder` from Task 1.
- Produces: `SceneMarkupDock.setup(editor_interface: EditorInterface, undo_redo: EditorUndoRedoManager, plugin: EditorPlugin) -> void`
- Produces: `SceneMarkupDock.forward_canvas_gui_input(event: InputEvent) -> bool`
- Produces: `SceneMarkupDock.forward_canvas_draw_over_viewport(viewport_control: Control) -> void`

- [ ] **Step 1: Add plugin manifest**

Create `feng-zhi/addons/scene_markup_tool/plugin.cfg`:

```ini
[plugin]

name="Scene Markup Tool"
description="Draw collision, walkable, and foreground occlusion polygons over full-background 2D scenes."
author="Codex Game Studios"
version="0.1.0"
script="res://addons/scene_markup_tool/scene_markup_plugin.gd"
```

- [ ] **Step 2: Add the EditorPlugin entry point**

Create `feng-zhi/addons/scene_markup_tool/scene_markup_plugin.gd`:

```gdscript
@tool
extends EditorPlugin

const DockScript := preload("res://addons/scene_markup_tool/scene_markup_dock.gd")

var _dock: Control

func _enter_tree() -> void:
	_dock = DockScript.new()
	_dock.name = "Scene Markup"
	_dock.setup(get_editor_interface(), get_undo_redo(), self)
	add_control_to_dock(DOCK_SLOT_RIGHT_UL, _dock)

func _exit_tree() -> void:
	if _dock != null:
		remove_control_from_docks(_dock)
		_dock.queue_free()
		_dock = null

func _get_plugin_name() -> String:
	return "Scene Markup Tool"

func _forward_canvas_gui_input(event: InputEvent) -> bool:
	if _dock == null:
		return false
	return _dock.forward_canvas_gui_input(event)

func _forward_canvas_draw_over_viewport(viewport_control: Control) -> void:
	if _dock != null:
		_dock.forward_canvas_draw_over_viewport(viewport_control)
```

- [ ] **Step 3: Add the dock skeleton**

Create `feng-zhi/addons/scene_markup_tool/scene_markup_dock.gd`:

```gdscript
@tool
extends VBoxContainer

const Builder := preload("res://addons/scene_markup_tool/scene_markup_builder.gd")

enum MarkupMode {
	COLLISION,
	WALKABLE,
	OCCLUDER,
}

const Z_PRESETS := {
	"前景低": 15,
	"前景中": 20,
	"前景高": 30,
}

var _editor_interface: EditorInterface
var _undo_redo: EditorUndoRedoManager
var _plugin: EditorPlugin
var _background: Sprite2D
var _mode := MarkupMode.COLLISION
var _current_points := PackedVector2Array()
var _show_helpers := true
var _z_preset := "前景中"

var _background_label: Label
var _status_label: Label
var _mode_options: OptionButton
var _z_options: OptionButton
var _helper_check: CheckBox

func setup(editor_interface: EditorInterface, undo_redo: EditorUndoRedoManager, plugin: EditorPlugin) -> void:
	_editor_interface = editor_interface
	_undo_redo = undo_redo
	_plugin = plugin
	_build_ui()
	_set_status("选择 Background 后点击“设为背景”。")

func forward_canvas_gui_input(_event: InputEvent) -> bool:
	return false

func forward_canvas_draw_over_viewport(_viewport_control: Control) -> void:
	pass

func _build_ui() -> void:
	var title := Label.new()
	title.text = "场景标注工具"
	add_child(title)

	var set_background_button := Button.new()
	set_background_button.text = "设为背景"
	set_background_button.pressed.connect(_on_set_background_pressed)
	add_child(set_background_button)

	_background_label = Label.new()
	_background_label.text = "背景：未设置"
	add_child(_background_label)

	_mode_options = OptionButton.new()
	_mode_options.add_item("碰撞区", MarkupMode.COLLISION)
	_mode_options.add_item("可行走区", MarkupMode.WALKABLE)
	_mode_options.add_item("遮挡区", MarkupMode.OCCLUDER)
	_mode_options.item_selected.connect(_on_mode_selected)
	add_child(_mode_options)

	_z_options = OptionButton.new()
	for preset_name in Z_PRESETS.keys():
		_z_options.add_item(preset_name)
	_z_options.select(1)
	_z_options.item_selected.connect(_on_z_selected)
	add_child(_z_options)

	_helper_check = CheckBox.new()
	_helper_check.text = "显示标注辅助线"
	_helper_check.button_pressed = true
	_helper_check.toggled.connect(_on_helper_toggled)
	add_child(_helper_check)

	var hint := Label.new()
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	hint.text = "左键逐点圈选，右键或回车闭合，Esc 取消当前圈选。"
	add_child(hint)

	_status_label = Label.new()
	_status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	add_child(_status_label)

func _on_set_background_pressed() -> void:
	var selection := _editor_interface.get_selection().get_selected_nodes()
	if selection.size() != 1 or not selection[0] is Sprite2D:
		_set_status("请选择一个 Sprite2D 作为背景。")
		return
	_background = selection[0]
	_background_label.text = "背景：%s" % _background.name
	_set_status("背景已设置，可以开始圈选。")

func _on_mode_selected(index: int) -> void:
	_mode = _mode_options.get_item_id(index)
	_current_points.clear()
	_request_overlay_update()

func _on_z_selected(index: int) -> void:
	_z_preset = _z_options.get_item_text(index)

func _on_helper_toggled(enabled: bool) -> void:
	_show_helpers = enabled
	_request_overlay_update()

func _set_status(message: String) -> void:
	if _status_label != null:
		_status_label.text = message

func _request_overlay_update() -> void:
	if _plugin != null:
		_plugin.update_overlays()
```

- [ ] **Step 4: Verify project parses the plugin files**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path feng-zhi --editor --quit
```

Expected:

```text
Godot Engine v4.7.stable.mono.official...
```

The command should exit with code `0` and no GDScript parse errors mentioning `scene_markup_tool`.

---

### Task 3: Point Collection And Viewport Preview

**Files:**
- Modify: `feng-zhi/addons/scene_markup_tool/scene_markup_dock.gd`

**Interfaces:**
- Consumes: `SceneMarkupDock.forward_canvas_gui_input(event: InputEvent) -> bool`
- Consumes: `SceneMarkupDock.forward_canvas_draw_over_viewport(viewport_control: Control) -> void`
- Produces: left-click point collection, right-click or Enter polygon completion, Escape cancellation.

- [ ] **Step 1: Add viewport input handling**

Replace the placeholder `forward_canvas_gui_input` in `scene_markup_dock.gd` with:

```gdscript
func forward_canvas_gui_input(event: InputEvent) -> bool:
	if not visible:
		return false

	if event is InputEventMouseButton and event.pressed:
		var mouse_event := event as InputEventMouseButton
		if mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_current_points.append(_get_canvas_mouse_position(mouse_event.position))
			_set_status("已添加 %d 个点。" % _current_points.size())
			_request_overlay_update()
			return true
		if mouse_event.button_index == MOUSE_BUTTON_RIGHT:
			return _complete_polygon()

	if event is InputEventKey and event.pressed and not event.echo:
		var key_event := event as InputEventKey
		if key_event.keycode == KEY_ENTER or key_event.keycode == KEY_KP_ENTER:
			return _complete_polygon()
		if key_event.keycode == KEY_ESCAPE:
			_current_points.clear()
			_set_status("已取消当前圈选。")
			_request_overlay_update()
			return true

	return false

func _get_canvas_mouse_position(viewport_position: Vector2) -> Vector2:
	var transform := _editor_interface.get_editor_viewport_2d().get_screen_transform()
	return transform.affine_inverse() * viewport_position
```

- [ ] **Step 2: Add preview overlay drawing**

Replace the placeholder `forward_canvas_draw_over_viewport` in `scene_markup_dock.gd` with:

```gdscript
func forward_canvas_draw_over_viewport(viewport_control: Control) -> void:
	if not _show_helpers:
		return

	var transform := _editor_interface.get_editor_viewport_2d().get_screen_transform()
	_draw_existing_markup(viewport_control, transform)
	_draw_current_polygon(viewport_control, transform)

func _draw_current_polygon(viewport_control: Control, transform: Transform2D) -> void:
	if _current_points.is_empty():
		return

	var color := _mode_color()
	for index in _current_points.size():
		var screen_point := transform * _current_points[index]
		viewport_control.draw_circle(screen_point, 4.0, color)
		if index > 0:
			var previous := transform * _current_points[index - 1]
			viewport_control.draw_line(previous, screen_point, color, 2.0)
	if _current_points.size() >= 3:
		viewport_control.draw_line(transform * _current_points[-1], transform * _current_points[0], color.darkened(0.25), 1.0)

func _draw_existing_markup(viewport_control: Control, transform: Transform2D) -> void:
	var scene_root := _get_scene_root()
	if scene_root == null:
		return

	_draw_collision_helpers(viewport_control, transform, scene_root)
	_draw_walkable_helpers(viewport_control, transform, scene_root)
	_draw_occluder_helpers(viewport_control, transform, scene_root)

func _draw_collision_helpers(viewport_control: Control, transform: Transform2D, scene_root: Node) -> void:
	var layer := scene_root.get_node_or_null("SceneMarkupRoot/Collisions")
	if layer == null:
		return
	for body in layer.get_children():
		var polygon := body.get_node_or_null("CollisionPolygon2D")
		if polygon is CollisionPolygon2D:
			_draw_polygon_lines(viewport_control, transform, polygon.polygon, Color(1, 0.2, 0.2, 0.9), 1.5)

func _draw_walkable_helpers(viewport_control: Control, transform: Transform2D, scene_root: Node) -> void:
	var layer := scene_root.get_node_or_null("SceneMarkupRoot/Walkable")
	if layer == null:
		return
	for region in layer.get_children():
		if region is NavigationRegion2D and region.navigation_polygon != null:
			_draw_polygon_lines(viewport_control, transform, region.navigation_polygon.vertices, Color(0.2, 1, 0.35, 0.9), 1.5)

func _draw_occluder_helpers(viewport_control: Control, transform: Transform2D, scene_root: Node) -> void:
	var layer := scene_root.get_node_or_null("SceneMarkupRoot/Occluders")
	if layer == null:
		return
	for occluder in layer.get_children():
		if occluder is Polygon2D:
			_draw_polygon_lines(viewport_control, transform, occluder.polygon, Color(0.25, 0.55, 1, 0.9), 1.5)

func _draw_polygon_lines(viewport_control: Control, transform: Transform2D, points: PackedVector2Array, color: Color, width: float) -> void:
	if points.size() < 2:
		return
	for index in points.size():
		var from_point := transform * points[index]
		var to_point := transform * points[(index + 1) % points.size()]
		viewport_control.draw_line(from_point, to_point, color, width)

func _mode_color() -> Color:
	match _mode:
		MarkupMode.COLLISION:
			return Color(1, 0.2, 0.2, 0.95)
		MarkupMode.WALKABLE:
			return Color(0.2, 1, 0.35, 0.95)
		MarkupMode.OCCLUDER:
			return Color(0.25, 0.55, 1, 0.95)
	return Color.WHITE
```

- [ ] **Step 3: Add scene root and completion stubs**

Add these helper methods to `scene_markup_dock.gd`:

```gdscript
func _get_scene_root() -> Node:
	var edited_scene := _editor_interface.get_edited_scene_root()
	return edited_scene

func _complete_polygon() -> bool:
	if _current_points.size() < 3:
		_set_status("至少需要 3 个点才能生成区域。")
		return true

	_set_status("圈选已闭合，下一任务会接入节点生成。")
	_current_points.clear()
	_request_overlay_update()
	return true
```

- [ ] **Step 4: Verify no parse errors after input and overlay code**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path feng-zhi --editor --quit
```

Expected: exit code `0`, no GDScript parse errors.

---

### Task 4: Generate Nodes From Completed Polygons

**Files:**
- Modify: `feng-zhi/addons/scene_markup_tool/scene_markup_dock.gd`
- Modify: `feng-zhi/addons/scene_markup_tool/tests/scene_markup_builder_smoke_test.gd`

**Interfaces:**
- Consumes: `Builder.create_collision`
- Consumes: `Builder.create_walkable`
- Consumes: `Builder.create_occluder`
- Produces: completed polygons create saved scene nodes under `SceneMarkupRoot`.

- [ ] **Step 1: Replace completion stub with generation logic**

Replace `_complete_polygon` in `scene_markup_dock.gd` with:

```gdscript
func _complete_polygon() -> bool:
	if _current_points.size() < 3:
		_set_status("至少需要 3 个点才能生成区域。")
		return true

	var scene_root := _get_scene_root()
	if scene_root == null:
		_set_status("当前没有打开可编辑场景。")
		return true

	if _mode == MarkupMode.OCCLUDER:
		if _background == null:
			_set_status("遮挡区需要先设置背景 Sprite2D。")
			return true
		if _background.texture == null:
			_set_status("背景没有纹理，不能生成遮挡区。")
			return true

	var points := _current_points.duplicate()
	_current_points.clear()
	var created := _create_markup_node(scene_root, points)
	if created != null:
		_editor_interface.mark_scene_as_unsaved()
		_set_status("已生成：%s" % created.name)
	_request_overlay_update()
	return true

func _create_markup_node(scene_root: Node, points: PackedVector2Array) -> Node:
	match _mode:
		MarkupMode.COLLISION:
			return Builder.create_collision(scene_root, points)
		MarkupMode.WALKABLE:
			return Builder.create_walkable(scene_root, points)
		MarkupMode.OCCLUDER:
			return Builder.create_occluder(scene_root, _background, points, Z_PRESETS[_z_preset])
	return null
```

- [ ] **Step 2: Add a smoke assertion for repeated names**

Append this test call in `_init` before `quit(...)`:

```gdscript
	failed = _test_incrementing_names() or failed
```

Add this test function:

```gdscript
func _test_incrementing_names() -> bool:
	var scene_root := Node2D.new()
	scene_root.name = "SmokeScene"
	var points := PackedVector2Array([
		Vector2(0, 0),
		Vector2(32, 0),
		Vector2(32, 32),
		Vector2(0, 32),
	])

	var first := Builder.create_collision(scene_root, points)
	var second := Builder.create_collision(scene_root, points)
	var ok := first.name == "Collision_001"
	ok = ok and second.name == "Collision_002"
	scene_root.free()
	return _report("incrementing names", ok)
```

- [ ] **Step 3: Run builder smoke tests**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path feng-zhi --script res://addons/scene_markup_tool/tests/scene_markup_builder_smoke_test.gd
```

Expected:

```text
PASS: collision generation
PASS: walkable generation
PASS: occluder uv generation
PASS: incrementing names
```

- [ ] **Step 4: Verify editor startup**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path feng-zhi --editor --quit
```

Expected: exit code `0`, no GDScript parse errors.

---

### Task 5: Enable Plugin And Validate Editor Workflow

**Files:**
- Modify: `feng-zhi/project.godot`

**Interfaces:**
- Consumes: Godot plugin manifest from Task 2.
- Produces: plugin enabled for the project.

- [ ] **Step 1: Add enabled plugin entry**

Add this section to `feng-zhi/project.godot` if it is not already present:

```ini
[editor_plugins]

enabled=PackedStringArray("res://addons/scene_markup_tool/plugin.cfg")
```

If `[editor_plugins]` already exists, only add `"res://addons/scene_markup_tool/plugin.cfg"` to its `enabled` array.

- [ ] **Step 2: Run editor startup check**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path feng-zhi --editor --quit
```

Expected: exit code `0`, no errors mentioning `Scene Markup Tool`, `scene_markup_plugin.gd`, `scene_markup_dock.gd`, or `scene_markup_builder.gd`.

- [ ] **Step 3: Manually verify dock appears**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --path feng-zhi
```

Manual check:

- Open `StartCave.tscn`.
- Confirm a right-side dock named `Scene Markup` appears.
- Select `Background`.
- Click `设为背景`.
- Confirm the dock shows `背景：Background`.

Stop the editor after checking. Do not save `StartCave.tscn` in this task.

---

### Task 6: Manual Creation Checks In StartCave

**Files:**
- No planned file writes unless the user approves saving the scene after inspection.

**Interfaces:**
- Consumes: enabled plugin.
- Produces: manual evidence that each mode creates the expected node type.

- [ ] **Step 1: Open the scene in Godot**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --path feng-zhi
```

Manual check:

- Open `StartCave.tscn`.
- Select `Background`.
- Click `设为背景`.

- [ ] **Step 2: Check collision generation**

Manual steps:

- Choose `碰撞区`.
- Left-click four points around a small wall test area.
- Press Enter.
- Confirm the scene tree contains `SceneMarkupRoot/Collisions/Collision_001/CollisionPolygon2D`.
- Select `CollisionPolygon2D` and confirm its polygon has four points.

- [ ] **Step 3: Check walkable generation**

Manual steps:

- Choose `可行走区`.
- Left-click four points around a small floor test area.
- Press Enter.
- Confirm the scene tree contains `SceneMarkupRoot/Walkable/Walkable_001`.
- Select `Walkable_001` and confirm `navigation_polygon` is assigned.

- [ ] **Step 4: Check occluder generation**

Manual steps:

- Choose `遮挡区`.
- Select `前景中`.
- Left-click four points over a visible foreground object.
- Press Enter.
- Confirm the scene tree contains `SceneMarkupRoot/Occluders/Occluder_001`.
- Select `Occluder_001` and confirm:
  - `texture` is `res://起始山洞.png`.
  - `z_index` is `20`.
  - `polygon` and `uv` have the same point count.

- [ ] **Step 5: Check helper visibility**

Manual steps:

- Turn off `显示标注辅助线`.
- Confirm red, green, and blue helper outlines disappear in the editor viewport.
- Turn it back on.
- Confirm helper outlines reappear.

- [ ] **Step 6: Close without saving unless requested**

Manual step:

- If this task was only for plugin validation, close the scene and choose not to save.
- If the user explicitly asked to keep the sample markings, save `StartCave.tscn`.

---

### Task 7: Runtime Collision And Occlusion Verification

**Files:**
- Optional modify: `feng-zhi/StartCave.tscn`, only if the user approves saving real scene markup.

**Interfaces:**
- Consumes: generated native nodes.
- Produces: evidence that generated collision and occlusion work during play.

- [ ] **Step 1: Ask before saving real markup**

Ask the user:

```text
是否允许我把正式的碰撞/可行走/遮挡标注保存进 /Users/bytedance/my-game/feng-zhi/StartCave.tscn？
```

Proceed with saved runtime verification only if the user approves.

- [ ] **Step 2: Create a small collision and occluder sample**

Manual steps after approval:

- In `StartCave.tscn`, create one collision polygon along an obvious wall edge.
- Create one occluder polygon on an obvious foreground object.
- Save `StartCave.tscn`.

- [ ] **Step 3: Run the scene**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --path feng-zhi
```

Manual play check:

- Press play.
- Move the player into the collision test area.
- Confirm `MoveAndSlide()` prevents walking through the marked wall.
- Move the player behind the occluder test area.
- Confirm the foreground `Polygon2D` visually covers the player.

- [ ] **Step 4: Run C# build after scene/plugin changes**

Run:

```bash
dotnet build feng-zhi/FengZhi.csproj
```

Expected:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Warnings unrelated to this plugin should be reported with exact count and first warning code if they appear.

---

### Task 8: Final Review And Handoff

**Files:**
- Review: `feng-zhi/addons/scene_markup_tool/plugin.cfg`
- Review: `feng-zhi/addons/scene_markup_tool/scene_markup_plugin.gd`
- Review: `feng-zhi/addons/scene_markup_tool/scene_markup_dock.gd`
- Review: `feng-zhi/addons/scene_markup_tool/scene_markup_builder.gd`
- Review: `feng-zhi/addons/scene_markup_tool/tests/scene_markup_builder_smoke_test.gd`
- Review: `feng-zhi/project.godot`
- Optional review: `feng-zhi/StartCave.tscn`

**Interfaces:**
- Consumes: all previous tasks.
- Produces: a verified plugin ready for user playtesting.

- [ ] **Step 1: Run placeholder scan**

Run:

```bash
rg -n "TB""D|TO""DO|FIX""ME|待""定|占""位" feng-zhi/addons/scene_markup_tool feng-zhi/project.godot
```

Expected: no matches.

- [ ] **Step 2: Run builder smoke test**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path feng-zhi --script res://addons/scene_markup_tool/tests/scene_markup_builder_smoke_test.gd
```

Expected:

```text
PASS: collision generation
PASS: walkable generation
PASS: occluder uv generation
PASS: incrementing names
```

- [ ] **Step 3: Run editor startup check**

Run:

```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path feng-zhi --editor --quit
```

Expected: exit code `0`, no plugin parse errors.

- [ ] **Step 4: Run C# build**

Run:

```bash
dotnet build feng-zhi/FengZhi.csproj
```

Expected:

```text
Build succeeded.
```

- [ ] **Step 5: Report changed files and verification**

Run:

```bash
git status --short feng-zhi/addons/scene_markup_tool feng-zhi/project.godot feng-zhi/StartCave.tscn
```

Report:

- Whether `StartCave.tscn` was modified.
- Whether plugin smoke tests passed.
- Whether editor startup passed.
- Whether `dotnet build` passed.
- That no commit was created unless the user explicitly requested one.

---

## Self-Review

- Spec coverage: collision, walkable, occluder generation, point-by-point drawing, background selection, helper outlines, native-node runtime boundary, and Godot 4.7 verification are all covered by Tasks 1-8.
- Scope control: JSON export, freehand drawing, automatic edge detection, boolean operations, runtime hot editing, and automatic image cutting remain out of scope.
- Type consistency: builder method names in Tasks 1 and 4 match dock usage.
- Godot API references checked: `EditorPlugin._forward_canvas_gui_input`, `EditorPlugin._forward_canvas_draw_over_viewport`, `Polygon2D.texture/uv/polygon`, and `NavigationPolygon.vertices/add_polygon` are the APIs this plan relies on.
