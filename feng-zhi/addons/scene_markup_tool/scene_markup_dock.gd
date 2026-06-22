@tool
extends VBoxContainer

const Builder: GDScript = preload("res://addons/scene_markup_tool/scene_markup_builder.gd")

enum MarkupMode {
	COLLISION,
	WALKABLE,
	OCCLUDER,
}

const Z_PRESETS: Dictionary = {
	"前景低": 15,
	"前景中": 20,
	"前景高": 30,
}

var _editor_interface: EditorInterface = null
var _undo_redo: EditorUndoRedoManager = null
var _plugin: EditorPlugin = null
var _background: Sprite2D = null
var _mode: MarkupMode = MarkupMode.COLLISION
var _current_points: PackedVector2Array = PackedVector2Array()
var _show_helpers: bool = true
var _z_preset: String = "前景中"

var _background_label: Label = null
var _status_label: Label = null
var _mode_options: OptionButton = null
var _z_options: OptionButton = null
var _helper_check: CheckBox = null

func setup(editor_interface: EditorInterface, undo_redo: EditorUndoRedoManager, plugin: EditorPlugin) -> void:
	_editor_interface = editor_interface
	_undo_redo = undo_redo
	_plugin = plugin
	_build_ui()
	_set_status("选择 Background 后点击“设为背景”。")

func forward_canvas_gui_input(event: InputEvent) -> bool:
	if not visible:
		return false

	if event is InputEventMouseButton and event.pressed:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton
		if mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_current_points.append(_get_canvas_mouse_position(mouse_event.position))
			_set_status("已添加 %d 个点。" % _current_points.size())
			_request_overlay_update()
			return true
		if mouse_event.button_index == MOUSE_BUTTON_RIGHT:
			return _complete_polygon()

	if event is InputEventKey and event.pressed and not event.echo:
		var key_event: InputEventKey = event as InputEventKey
		if key_event.keycode == KEY_ENTER or key_event.keycode == KEY_KP_ENTER:
			return _complete_polygon()
		if key_event.keycode == KEY_ESCAPE:
			_current_points.clear()
			_set_status("已取消当前圈选。")
			_request_overlay_update()
			return true

	return false

func forward_canvas_draw_over_viewport(viewport_control: Control) -> void:
	if not _show_helpers:
		return

	var transform: Transform2D = _editor_interface.get_editor_viewport_2d().get_screen_transform()
	_draw_existing_markup(viewport_control, transform)
	_draw_current_polygon(viewport_control, transform)

func _build_ui() -> void:
	var title: Label = Label.new()
	title.text = "场景标注工具"
	add_child(title)

	var set_background_button: Button = Button.new()
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
	for preset_name: String in Z_PRESETS.keys():
		_z_options.add_item(preset_name)
	_z_options.select(1)
	_z_options.item_selected.connect(_on_z_selected)
	add_child(_z_options)

	_helper_check = CheckBox.new()
	_helper_check.text = "显示标注辅助线"
	_helper_check.button_pressed = true
	_helper_check.toggled.connect(_on_helper_toggled)
	add_child(_helper_check)

	var hint: Label = Label.new()
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	hint.text = "左键逐点圈选，右键或回车闭合，Esc 取消当前圈选。"
	add_child(hint)

	_status_label = Label.new()
	_status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	add_child(_status_label)

func _on_set_background_pressed() -> void:
	var selection: Array[Node] = _editor_interface.get_selection().get_selected_nodes()
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

func _get_canvas_mouse_position(viewport_position: Vector2) -> Vector2:
	var transform: Transform2D = _editor_interface.get_editor_viewport_2d().get_screen_transform()
	return transform.affine_inverse() * viewport_position

func _draw_current_polygon(viewport_control: Control, transform: Transform2D) -> void:
	if _current_points.is_empty():
		return

	var color: Color = _mode_color()
	var index: int = 0
	while index < _current_points.size():
		var screen_point: Vector2 = transform * _current_points[index]
		viewport_control.draw_circle(screen_point, 4.0, color)
		if index > 0:
			var previous: Vector2 = transform * _current_points[index - 1]
			viewport_control.draw_line(previous, screen_point, color, 2.0)
		index += 1

	if _current_points.size() >= 3:
		var last_point: Vector2 = transform * _current_points[_current_points.size() - 1]
		var first_point: Vector2 = transform * _current_points[0]
		viewport_control.draw_line(last_point, first_point, color.darkened(0.25), 1.0)

func _draw_existing_markup(viewport_control: Control, transform: Transform2D) -> void:
	var scene_root: Node = _get_scene_root()
	if scene_root == null:
		return

	_draw_collision_helpers(viewport_control, transform, scene_root)
	_draw_walkable_helpers(viewport_control, transform, scene_root)
	_draw_occluder_helpers(viewport_control, transform, scene_root)

func _draw_collision_helpers(viewport_control: Control, transform: Transform2D, scene_root: Node) -> void:
	var layer: Node = scene_root.get_node_or_null("SceneMarkupRoot/Collisions")
	if layer == null:
		return

	for body: Node in layer.get_children():
		var polygon: Node = body.get_node_or_null("CollisionPolygon2D")
		if polygon is CollisionPolygon2D:
			_draw_polygon_lines(viewport_control, transform, (polygon as CollisionPolygon2D).polygon, Color(1, 0.2, 0.2, 0.9), 1.5)

func _draw_walkable_helpers(viewport_control: Control, transform: Transform2D, scene_root: Node) -> void:
	var layer: Node = scene_root.get_node_or_null("SceneMarkupRoot/Walkable")
	if layer == null:
		return

	for region: Node in layer.get_children():
		if region is NavigationRegion2D and (region as NavigationRegion2D).navigation_polygon != null:
			_draw_polygon_lines(viewport_control, transform, (region as NavigationRegion2D).navigation_polygon.vertices, Color(0.2, 1, 0.35, 0.9), 1.5)

func _draw_occluder_helpers(viewport_control: Control, transform: Transform2D, scene_root: Node) -> void:
	var layer: Node = scene_root.get_node_or_null("SceneMarkupRoot/Occluders")
	if layer == null:
		return

	for occluder: Node in layer.get_children():
		if occluder is Polygon2D:
			_draw_polygon_lines(viewport_control, transform, (occluder as Polygon2D).polygon, Color(0.25, 0.55, 1, 0.9), 1.5)

func _draw_polygon_lines(viewport_control: Control, transform: Transform2D, points: PackedVector2Array, color: Color, width: float) -> void:
	if points.size() < 2:
		return

	var index: int = 0
	while index < points.size():
		var from_point: Vector2 = transform * points[index]
		var to_point: Vector2 = transform * points[(index + 1) % points.size()]
		viewport_control.draw_line(from_point, to_point, color, width)
		index += 1

func _mode_color() -> Color:
	match _mode:
		MarkupMode.COLLISION:
			return Color(1, 0.2, 0.2, 0.95)
		MarkupMode.WALKABLE:
			return Color(0.2, 1, 0.35, 0.95)
		MarkupMode.OCCLUDER:
			return Color(0.25, 0.55, 1, 0.95)
	return Color.WHITE

func _get_scene_root() -> Node:
	return _editor_interface.get_edited_scene_root()

func _complete_polygon() -> bool:
	if _current_points.size() < 3:
		_set_status("至少需要 3 个点才能生成区域。")
		return true

	var scene_root: Node = _get_scene_root()
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

	var points: PackedVector2Array = _current_points.duplicate()
	_current_points.clear()
	var created: Node = _create_markup_node(scene_root, points)
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
			return Builder.create_occluder(scene_root, _background, points, int(Z_PRESETS[_z_preset]))
	return null
