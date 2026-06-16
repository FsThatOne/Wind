# GdUnit4 Sanity Test — 验证测试框架基础设施正常工作
# 此测试不依赖任何游戏代码，仅验证：
#   1. GdUnit4 可正确发现并执行测试
#   2. 断言机制正常
#   3. CI pipeline 可捕获测试结果
extends GdUnitTestSuite

func test_framework_sanity_assert_true() -> void:
	assert_bool(true).is_true()

func test_framework_sanity_assert_equal() -> void:
	assert_int(1 + 1).is_equal(2)

func test_framework_sanity_string_contains() -> void:
	assert_str("风止 Wind Stops").contains("Wind")

func test_framework_sanity_array_not_empty() -> void:
	var items := [1, 2, 3]
	assert_array(items).is_not_empty()
	assert_array(items).has_size(3)

func test_project_name_matches() -> void:
	# 验证项目配置可被正确读取
	var project_name: String = ProjectSettings.get_setting("application/config/name", "")
	assert_str(project_name).is_not_empty()
