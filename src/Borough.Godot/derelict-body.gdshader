shader_type spatial;

// An overlay pass that greys an abandoned family body toward Main.Derelict, lit like the body.
// INSTANCE_CUSTOM carries the linear grey and, in alpha, its share.

varying vec4 derelict;

void vertex() {
	derelict = INSTANCE_CUSTOM;
}

void fragment() {
	ALBEDO = derelict.rgb;
	ALPHA = derelict.a;
	ROUGHNESS = 0.95;
}
