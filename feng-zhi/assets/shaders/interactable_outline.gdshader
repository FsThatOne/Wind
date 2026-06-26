// 可交互物高亮描边 shader
//
// 用法：给 Sprite2D 挂 ShaderMaterial(shader=this)，通过 set_shader_parameter("enabled", true/false) 切换。
// 原理：采样 8 邻居像素，alpha=0 但邻居有像素 → 画 outline_color。
//
// 注意事项：
//   1. 描边只能向 sprite 内部"画"（不能突破 sprite 边界）。所以原图边缘 1-2px 必须留透明缓冲，
//      否则 sprite 紧贴 sprite 时描边会被自身像素吃掉。iso 房间导出的 tile 自带 padding，没问题。
//   2. outline_width 单位是"纹理像素"。Sprite 缩放 ≠ 1 时描边宽度会跟着缩放。
//   3. enabled=false 时 shader 退化为纯采样，性能开销可忽略。

shader_type canvas_item;

uniform vec4 outline_color : source_color = vec4(1.0, 0.85, 0.3, 1.0);
uniform float outline_width : hint_range(0.0, 8.0) = 2.0;
uniform bool enabled = false;

void fragment() {
	vec4 col = texture(TEXTURE, UV);
	if (enabled && col.a < 0.1) {
		vec2 step = TEXTURE_PIXEL_SIZE * outline_width;
		float neighbor_alpha = 0.0;
		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if (x == 0 && y == 0) continue;
				vec2 offset = vec2(float(x), float(y)) * step;
				neighbor_alpha = max(neighbor_alpha, texture(TEXTURE, UV + offset).a);
			}
		}
		if (neighbor_alpha > 0.5) {
			col = outline_color;
		}
	}
	COLOR = col;
}
