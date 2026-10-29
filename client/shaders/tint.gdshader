// Colours the grey skin and hair layers: black becomes a deep shade of
// base_color, mid grey becomes base_color, white becomes a warm highlight.
shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(1.0);
varying vec4 modulate;

void vertex() {
	modulate = COLOR;
}

void fragment() {
	vec4 t = texture(TEXTURE, UV);
	float v = t.r;
	vec3 b = base_color.rgb;
	vec3 dark = b * vec3(0.36, 0.32, 0.42);
	vec3 light = b + (vec3(1.0) - b) * vec3(0.45, 0.42, 0.32);
	vec3 c = v < 0.5 ? mix(dark, b, v * 2.0) : mix(b, light, (v - 0.5) * 2.0);
	COLOR = vec4(c, t.a) * modulate;
}
