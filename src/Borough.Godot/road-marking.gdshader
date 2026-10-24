// Road paint. An instance whose colour alpha is below one half is a dashed line, 3 m of paint in every 9 m.
shader_type spatial;
render_mode cull_back;

varying float along;
varying vec4 paint;

void vertex() {
	along = (VERTEX.z + 0.5) * length(MODEL_MATRIX[2].xyz);
	paint = COLOR;
}

void fragment() {
	if (paint.a < 0.5 && mod(along, 9.0) > 3.0) {
		discard;
	}
	ALBEDO = paint.rgb;
	ROUGHNESS = 0.8;
}
