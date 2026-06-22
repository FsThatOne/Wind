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

func forward_canvas_gui_input(_event: InputEvent) -> bool:
	return false

func forward_canvas_draw_over_viewport(_viewport_control: Control) -> void:
	pass

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
