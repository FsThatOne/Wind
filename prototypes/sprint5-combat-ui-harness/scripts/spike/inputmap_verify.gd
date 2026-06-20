extends Node

# SPIKE-4 — InputMap.get_actions() snapshot stable across a cinematic-equivalent
# wait window under Godot 4.7-stable. Verifies cu-006-godot-integration AC-4
# prerequisite: cinematic period must NOT mutate InputMap.
# 4.7 risk: SDL3 gamepad + Wayland touch path changes affecting InputMap state.

func run_verify() -> Dictionary:
	var baseline: Array = InputMap.get_actions()
	# Simulate cinematic window without calling add_action / erase_action
	await get_tree().create_timer(0.1, false, true, true).timeout
	var after: Array = InputMap.get_actions()
	var equal: bool = baseline.size() == after.size()
	if equal:
		for i in baseline.size():
			if baseline[i] != after[i]:
				equal = false
				break
	return {
		"id": "SPIKE-4 inputmap",
		"verdict": "PASS" if equal else "FAIL",
		"details": {
			"baseline_size": baseline.size(),
			"after_size": after.size(),
			"baseline_first_3": baseline.slice(0, mini(3, baseline.size())),
		}
	}
