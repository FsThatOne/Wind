extends Node

# SPIKE-1 — Engine.TimeScale lerp/hold/restore under Godot 4.7-stable.
# Verifies cu-006-godot-integration AC-1 / AC-6 prerequisite:
#   1.0 -> 0.2 (300ms wall-clock) -> hold 1.0s -> 1.0 (300ms wall-clock)
#   final value must equal 1.0 ±0.001
# 4.7 risk: HDR pipeline + Glow ordering changes interfering with TimeScale propagation.

func run_verify() -> Dictionary:
	Engine.time_scale = 1.0
	var start_ms := Time.get_ticks_msec()
	# Phase 1 — slow_in: 0 -> 300ms wall-clock, lerp 1.0 -> 0.2
	while Time.get_ticks_msec() - start_ms < 300:
		var p1: float = float(Time.get_ticks_msec() - start_ms) / 300.0
		Engine.time_scale = lerp(1.0, 0.2, p1)
		await get_tree().create_timer(0.016, false, true, true).timeout
	var slow_in_end: float = Engine.time_scale
	# Phase 2 — hold at 0.2 for 1.0s wall-clock
	while Time.get_ticks_msec() - start_ms < 1300:
		await get_tree().create_timer(0.016, false, true, true).timeout
	var hold_end: float = Engine.time_scale
	# Phase 3 — slow_out: 1300ms -> 1600ms wall-clock, lerp 0.2 -> 1.0
	while Time.get_ticks_msec() - start_ms < 1600:
		var p3: float = float(Time.get_ticks_msec() - start_ms - 1300) / 300.0
		Engine.time_scale = lerp(0.2, 1.0, p3)
		await get_tree().create_timer(0.016, false, true, true).timeout
	Engine.time_scale = 1.0
	var final_ts: float = Engine.time_scale
	var ok: bool = absf(final_ts - 1.0) < 0.001
	return {
		"id": "SPIKE-1 timescale",
		"verdict": "PASS" if ok else "FAIL",
		"details": {
			"slow_in_end_scale": slow_in_end,
			"hold_end_scale": hold_end,
			"final_scale": final_ts,
		}
	}
