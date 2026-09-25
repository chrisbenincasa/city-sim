shader_type spatial;

// An overlay pass that greys an abandoned family body toward Main.Derelict, lit like the body.
// The instance COLOR carries the linear grey and, in alpha, its share.

void fragment() {
	ALBEDO = COLOR.rgb;
	ALPHA = COLOR.a;
	ROUGHNESS = 0.95;
}
