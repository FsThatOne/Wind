extends Node

# Godot 4.7-stable API spike launcher
#
# Sequentially executes the 4 verifiers required to unblock
# cu-006-godot-integration BUILD phase:
#   SPIKE-1 Engine.TimeScale lerp/hold/restore       (AC-1 / AC-6)
#   SPIKE-2 Tween process_mode ALWAYS @ time_scale=0 (AC-2)
#   SPIKE-3 Camera2D.position_smoothing_enabled      (AC-3)
#   SPIKE-4 InputMap.get_actions stability           (AC-4)
#
# Each verifier returns Dictionary {id, verdict, details}.
# Launcher prints per-verifier results and an OVERALL verdict, then quits.

const VERIFIERS := [
	preload("res://scripts/spike/timescale_verify.gd"),
	preload("res://scripts/spike/tween_always_verify.gd"),
	preload("res://scripts/spike/camera2d_smoothing_verify.gd"),
	preload("res://scripts/spike/inputmap_verify.gd"),
]

func _ready() -> void:
	print("=== Godot 4.7-stable API spike launcher ===")
	print("Engine version: ", Engine.get_version_info())
	var summary: Array[Dictionary] = []
	for script in VERIFIERS:
		var node: Node = script.new()
		add_child(node)
		var verdict: Dictionary = await node.run_verify()
		summary.append(verdict)
		print("[", verdict.get("id", "?"), "] verdict=", verdict.get("verdict", "?"),
				" details=", verdict.get("details", {}))
		node.queue_free()
	print("=== SPIKE SUMMARY ===")
	var all_pass: bool = true
	for v in summary:
		print("  ", v.get("id", "?"), ": ", v.get("verdict", "?"))
		if v.get("verdict", "FAIL") != "PASS":
			all_pass = false
	print("=== OVERALL: ", "PASS" if all_pass else "FAIL", " ===")
	get_tree().quit()
