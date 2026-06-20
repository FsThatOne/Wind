extends Node

# SPIKE-2 — Tween process_mode = ALWAYS advances by wall-clock when
# Engine.time_scale = 0.0 under Godot 4.7-stable.
# Verifies cu-006-godot-integration AC-2 prerequisite.
# 4.7 risk: Tween internal architecture rewrite could change ALWAYS semantics.

func run_verify() -> Dictionary:
	var node := Node2D.new()
	add_child(node)
	node.position = Vector2.ZERO
	var tween := create_tween()
	tween.set_process_mode(Tween.TWEEN_PROCESS_IDLE)
	tween.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	tween.set_ignore_time_scale(true)
	tween.tween_property(node, "position:x", 100.0, 0.5)
	Engine.time_scale = 0.0
	# Wall-clock 0.6s, ignore time_scale, process even when paused
	await get_tree().create_timer(0.6, false, true, true).timeout
	var x_after: float = node.position.x
	Engine.time_scale = 1.0
	var ok: bool = x_after > 99.0
	tween.kill()
	node.queue_free()
	return {
		"id": "SPIKE-2 tween-always",
		"verdict": "PASS" if ok else "FAIL",
		"details": {
			"position_x_after_wall_0_6s": x_after,
			"expected_min": 99.0,
		}
	}
