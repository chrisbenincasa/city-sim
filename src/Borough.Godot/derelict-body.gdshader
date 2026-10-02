shader_type spatial;

// An overlay pass that greys an abandoned family body toward Main.Derelict, lit like the body.
// INSTANCE_CUSTOM carries the linear grey and, in alpha, its share.

#include "body-fade.gdshaderinc"

// The overlay takes the same side of the fade and the same depth bias as the body under it, so it
// greys the pixels that body keeps and sits flush with them.

varying vec4 derelict;

void vertex() {
	derelict = INSTANCE_CUSTOM;
	fade_in = body_fade_weight(MODEL_MATRIX[3].xyz, MAIN_CAM_INV_VIEW_MATRIX[3].xyz);
	POSITION = body_fade_position(VERTEX, MODELVIEW_MATRIX, PROJECTION_MATRIX);
}

void fragment() {
	if (body_fade_discards(fade_in, FRAGCOORD.xy)) {
		discard;
	}

	ALBEDO = derelict.rgb;
	ALPHA = derelict.a;
	ROUGHNESS = 0.95;
}
