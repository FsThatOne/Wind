extends Node

# SPIKE-3 — Camera2D.position_smoothing_enabled toggles correctly under
# Godot 4.7-stable. Verifies cu-006-godot-integration AC-3 prerequisite.
# 4.7 risk: Control offset transforms rework could side-effect Camera2D.

func run_verify() -> Dictionary:
	var cam := Camera2D.new()
	add_child(cam)
	cam.make_current()
	cam.position_smoothing_enabled = true
	var p1: bool = cam.position_smoothing_enabled
	cam.position_smoothing_enabled = false
	var p2: bool = cam.position_smoothing_enabled
	cam.position_smoothing_enabled = true
	var p3: bool = cam.position_smoothing_enabled
	cam.queue_free()
	var ok: bool = p1 == true and p2 == false and p3 == true
	return {
		"id": "SPIKE-3 camera2d-smoothing",
		"verdict": "PASS" if ok else "FAIL",
		"details": {
			"toggle_on_1": p1,
			"toggle_off": p2,
			"toggle_on_2": p3,
		}
	}
