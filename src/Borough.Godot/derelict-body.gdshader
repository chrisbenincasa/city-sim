shader_type spatial;

// An overlay pass that greys an abandoned family body toward Main.Derelict, lit like the body.

// sRGB. Godot linearises an instance uniform's Color.
instance uniform vec4 derelict = vec4(0.0);

void fragment() {
	ALBEDO = derelict.rgb;
	ALPHA = derelict.a;
	ROUGHNESS = 0.95;
}
