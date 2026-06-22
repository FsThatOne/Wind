@tool
class_name SceneMarkupBuilder
extends RefCounted

const ROOT_NAME: String = "SceneMarkupRoot"
const COLLISIONS_NAME: String = "Collisions"
const WALKABLE_NAME: String = "Walkable"
const OCCLUDERS_NAME: String = "Occluders"

static func ensure_markup_root(scene_root: Node) -> Node2D:
	var existing: Node = scene_root.get_node_or_null(ROOT_NAME)
	if existing is Node2D:
		return existing as Node2D

	var root: Node2D = Node2D.new()
	root.name = ROOT_NAME
	scene_root.add_child(root)
	root.owner = scene_root
	return root

static func ensure_layer(root: Node2D, layer_name: String) -> Node2D:
	var existing: Node = root.get_node_or_null(layer_name)
	if existing is Node2D:
		return existing as Node2D

	var layer: Node2D = Node2D.new()
	layer.name = layer_name
	root.add_child(layer)
	layer.owner = root.owner
	return layer

static func create_collision(scene_root: Node, points: PackedVector2Array) -> StaticBody2D:
	if not _require_polygon(points):
		return null

	var markup_root: Node2D = ensure_markup_root(scene_root)
	var layer: Node2D = ensure_layer(markup_root, COLLISIONS_NAME)

	var body: StaticBody2D = StaticBody2D.new()
	body.name = _next_name(layer, "Collision")
	body.collision_layer = 1
	body.collision_mask = 1
	layer.add_child(body)
	body.owner = scene_root

	var polygon: CollisionPolygon2D = CollisionPolygon2D.new()
	polygon.name = "CollisionPolygon2D"
	polygon.polygon = points
	body.add_child(polygon)
	polygon.owner = scene_root
	return body

static func create_walkable(scene_root: Node, points: PackedVector2Array) -> NavigationRegion2D:
	if not _require_polygon(points):
		return null

	var markup_root: Node2D = ensure_markup_root(scene_root)
	var layer: Node2D = ensure_layer(markup_root, WALKABLE_NAME)

	var region: NavigationRegion2D = NavigationRegion2D.new()
	region.name = _next_name(layer, "Walkable")
	layer.add_child(region)
	region.owner = scene_root

	var navigation_polygon: NavigationPolygon = NavigationPolygon.new()
	navigation_polygon.vertices = points

	var indices: PackedInt32Array = PackedInt32Array()
	var index: int = 0
	while index < points.size():
		indices.append(index)
		index += 1
	navigation_polygon.add_polygon(indices)

	region.navigation_polygon = navigation_polygon
	return region

static func create_occluder(scene_root: Node, background: Sprite2D, points: PackedVector2Array, z_index: int) -> Polygon2D:
	if not _require_polygon(points):
		return null

	if background.texture == null:
		push_error("Background Sprite2D has no texture.")
		return null

	var markup_root: Node2D = ensure_markup_root(scene_root)
	var layer: Node2D = ensure_layer(markup_root, OCCLUDERS_NAME)

	var occluder: Polygon2D = Polygon2D.new()
	occluder.name = _next_name(layer, "Occluder")
	occluder.polygon = points
	occluder.texture = background.texture
	occluder.uv = calculate_background_uv(background, points)
	occluder.z_index = z_index
	occluder.antialiased = true
	layer.add_child(occluder)
	occluder.owner = scene_root
	return occluder

static func calculate_background_uv(background: Sprite2D, points: PackedVector2Array) -> PackedVector2Array:
	var uv: PackedVector2Array = PackedVector2Array()
	var point_index: int = 0
	while point_index < points.size():
		var point: Vector2 = points[point_index]
		var local_point: Vector2 = background.to_local(point)
		if background.centered and background.texture != null:
			local_point += background.texture.get_size() * 0.5
		uv.append(local_point)
		point_index += 1
	return uv

static func _require_polygon(points: PackedVector2Array) -> bool:
	if points.size() < 3:
		push_error("Scene markup polygons need at least 3 points.")
		return false
	return true

static func _next_name(parent: Node, prefix: String) -> String:
	var index: int = 1
	while true:
		var candidate: String = "%s_%03d" % [prefix, index]
		if parent.get_node_or_null(candidate) == null:
			return candidate
		index += 1
	return "%s_%03d" % [prefix, index]
