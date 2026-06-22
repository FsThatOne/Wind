extends SceneTree

const Builder: GDScript = preload("res://addons/scene_markup_tool/scene_markup_builder.gd")

func _init() -> void:
	var failed: bool = false
	failed = _test_layers_and_collision() or failed
	failed = _test_walkable_navigation_polygon() or failed
	failed = _test_occluder_uv_for_scaled_uncentered_background() or failed
	failed = _test_invalid_polygon_does_not_create_nodes() or failed
	quit(1 if failed else 0)

func _test_layers_and_collision() -> bool:
	var scene_root: Node2D = Node2D.new()
	scene_root.name = "SmokeScene"
	var points: PackedVector2Array = PackedVector2Array([
		Vector2(10, 10),
		Vector2(110, 10),
		Vector2(110, 80),
		Vector2(10, 80),
	])

	var body: StaticBody2D = Builder.create_collision(scene_root, points)
	var collision_polygon: CollisionPolygon2D = body.get_node("CollisionPolygon2D") as CollisionPolygon2D
	var ok: bool = scene_root.has_node("SceneMarkupRoot/Collisions/Collision_001")
	ok = ok and collision_polygon.polygon == points
	ok = ok and body.owner == scene_root
	ok = ok and collision_polygon.owner == scene_root
	scene_root.free()
	return _report("collision generation", ok)

func _test_walkable_navigation_polygon() -> bool:
	var scene_root: Node2D = Node2D.new()
	scene_root.name = "SmokeScene"
	var points: PackedVector2Array = PackedVector2Array([
		Vector2(0, 0),
		Vector2(64, 0),
		Vector2(64, 64),
		Vector2(0, 64),
	])

	var region: NavigationRegion2D = Builder.create_walkable(scene_root, points)
	var navigation_polygon: NavigationPolygon = region.navigation_polygon
	var ok: bool = scene_root.has_node("SceneMarkupRoot/Walkable/Walkable_001")
	ok = ok and navigation_polygon != null
	ok = ok and navigation_polygon.vertices == points
	ok = ok and navigation_polygon.get_polygon_count() == 1
	scene_root.free()
	return _report("walkable generation", ok)

func _test_occluder_uv_for_scaled_uncentered_background() -> bool:
	var scene_root: Node2D = Node2D.new()
	scene_root.name = "SmokeScene"

	var image: Image = Image.create(200, 100, false, Image.FORMAT_RGBA8)
	image.fill(Color.WHITE)
	var texture: ImageTexture = ImageTexture.create_from_image(image)

	var background: Sprite2D = Sprite2D.new()
	background.name = "Background"
	background.texture = texture
	background.centered = false
	background.scale = Vector2(2, 2)
	scene_root.add_child(background)
	background.owner = scene_root

	var points: PackedVector2Array = PackedVector2Array([
		Vector2(20, 10),
		Vector2(60, 10),
		Vector2(60, 40),
		Vector2(20, 40),
	])
	var occluder: Polygon2D = Builder.create_occluder(scene_root, background, points, 20)
	var expected_uv: PackedVector2Array = PackedVector2Array([
		Vector2(10, 5),
		Vector2(30, 5),
		Vector2(30, 20),
		Vector2(10, 20),
	])
	var ok: bool = scene_root.has_node("SceneMarkupRoot/Occluders/Occluder_001")
	ok = ok and occluder.texture == texture
	ok = ok and occluder.uv == expected_uv
	ok = ok and occluder.z_index == 20
	scene_root.free()
	return _report("occluder uv generation", ok)

func _test_invalid_polygon_does_not_create_nodes() -> bool:
	var collision_scene_root: Node2D = Node2D.new()
	collision_scene_root.name = "InvalidCollisionScene"
	var points: PackedVector2Array = PackedVector2Array([
		Vector2(0, 0),
		Vector2(32, 0),
	])
	var body: StaticBody2D = Builder.create_collision(collision_scene_root, points)
	var ok: bool = body == null
	ok = ok and not collision_scene_root.has_node("SceneMarkupRoot")
	collision_scene_root.free()

	var walkable_scene_root: Node2D = Node2D.new()
	walkable_scene_root.name = "InvalidWalkableScene"
	var region: NavigationRegion2D = Builder.create_walkable(walkable_scene_root, points)
	ok = ok and region == null
	ok = ok and not walkable_scene_root.has_node("SceneMarkupRoot")
	walkable_scene_root.free()

	var occluder_scene_root: Node2D = Node2D.new()
	occluder_scene_root.name = "InvalidOccluderScene"
	var image: Image = Image.create(16, 16, false, Image.FORMAT_RGBA8)
	image.fill(Color.WHITE)
	var texture: ImageTexture = ImageTexture.create_from_image(image)
	var background: Sprite2D = Sprite2D.new()
	background.name = "Background"
	background.texture = texture
	occluder_scene_root.add_child(background)
	background.owner = occluder_scene_root
	var occluder: Polygon2D = Builder.create_occluder(occluder_scene_root, background, points, 0)
	ok = ok and occluder == null
	ok = ok and not occluder_scene_root.has_node("SceneMarkupRoot")
	occluder_scene_root.free()
	return _report("invalid polygon rejection", ok)

func _report(label: String, ok: bool) -> bool:
	if ok:
		print("PASS: %s" % label)
		return false
	push_error("FAIL: %s" % label)
	return true
