@tool
extends EditorPlugin

const DockScript: GDScript = preload("res://addons/scene_markup_tool/scene_markup_dock.gd")

var _dock: Control = null

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

func _handles(object: Object) -> bool:
	return object is CanvasItem

func _forward_canvas_gui_input(event: InputEvent) -> bool:
	if _dock == null:
		return false
	return _dock.forward_canvas_gui_input(event)

func _forward_canvas_draw_over_viewport(viewport_control: Control) -> void:
	if _dock != null:
		_dock.forward_canvas_draw_over_viewport(viewport_control)
