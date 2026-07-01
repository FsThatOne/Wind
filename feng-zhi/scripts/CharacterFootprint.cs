using Godot;

namespace FengZhi;

public static class CharacterFootprint
{
	private const float Scale = 0.5f;

	private static readonly Vector2[] BaseIsoDiamond =
	{
		new(-64f, 0f),
		new(0f, -32f),
		new(64f, 0f),
		new(0f, 32f),
	};

	public static Vector2[] CreateCollisionPolygon()
	{
		var polygon = new Vector2[BaseIsoDiamond.Length];
		for (var i = 0; i < BaseIsoDiamond.Length; i++)
			polygon[i] = BaseIsoDiamond[i] * Scale;

		return polygon;
	}

	public static void ApplyTo(CollisionPolygon2D collision)
	{
		collision.Position = Vector2.Zero;
		collision.Polygon = CreateCollisionPolygon();
	}
}
