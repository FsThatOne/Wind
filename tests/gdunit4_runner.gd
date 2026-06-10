# GdUnit4 测试 Runner —— 由 CI 与 /smoke-check 调用
# 用法：godot --headless --script tests/gdunit4_runner.gd
extends SceneTree

func _init() -> void:
	var runner := load("res://addons/gdunit4/GdUnitRunner.gd")
	if runner == null:
		push_error("GdUnit4 未找到。请通过 AssetLib 安装至 addons/gdunit4/。")
		quit(1)
		return
	var instance = runner.new()
	instance.run_tests()
	quit(0)
