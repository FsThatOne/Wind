extends SceneTree

# 加载 pond showcase 场景到 SubViewport，渲染若干帧，保存截图。
# 不在 --headless 模式下运行（headless 禁用渲染）。
# 使用:
#   godot --path feng-zhi --script ../tools/capture_jiangnan_iso_pond_showcase.gd

const SCENE_PATH := "res://scenes/showcase/jiangnan_iso_pond_showcase.tscn"
const OUTPUT_PATH := "res://../production/qa/evidence/s7-iso-pivot-foundation/jiangnan_iso_pond_showcase.png"
const VIEWPORT_SIZE := Vector2i(1600, 900)
const BG_COLOR := Color(0.12, 0.12, 0.14, 1.0)


func _init() -> void:
	var packed: PackedScene = load(SCENE_PATH)
	if packed == null:
		push_error("Unable to load " + SCENE_PATH)
		quit(1)
		return

	var scene: Node = packed.instantiate()
	if scene == null:
		push_error("Unable to instantiate " + SCENE_PATH)
		quit(1)
		return

	var viewport := SubViewport.new()
	viewport.size = VIEWPORT_SIZE
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	viewport.transparent_bg = false
	root.add_child(viewport)

	# 纯色背景板，避免 transparent_bg=false 但仍呈现 checkerboard 的 Godot 默认行为
	var bg := ColorRect.new()
	bg.color = BG_COLOR
	bg.size = Vector2(VIEWPORT_SIZE)
	bg.position = Vector2.ZERO
	bg.z_index = -1000
	viewport.add_child(bg)

	viewport.add_child(scene)

	# 等待渲染管线稳定
	await process_frame
	await process_frame
	await process_frame

	var tex := viewport.get_texture()
	if tex == null:
		push_error("Unable to get viewport texture")
		quit(1)
		return

	var img: Image = tex.get_image()
	if img == null:
		push_error("Unable to get image from texture")
		quit(1)
		return

	var save_result := img.save_png(OUTPUT_PATH)
	if save_result != OK:
		push_error("Unable to save PNG: " + str(save_result))
		quit(1)
		return

	print("Saved screenshot: " + OUTPUT_PATH + " size=" + str(VIEWPORT_SIZE))
	quit()
