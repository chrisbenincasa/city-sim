shader_type spatial;

// A family body part dressed from the texture library.
//
// COLOR carries the part's sRGB paint in rgb, and 1 in alpha where the part is painted. Paint
// multiplies the photograph with its mean luminance divided out, so the paint reads true.
// Each Building shifts the texture by an offset hashed from its origin, so neighbours built from
// one mesh do not repeat in step. World-space noise at 24, 12 and 6 m scales brightness by up to
// macro_strength either way, which breaks the tile repeat across a wall.

uniform sampler2D albedo_map : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D normal_map : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D roughness_map : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float mean_luminance = 0.7;
uniform float macro_strength = 0.3;

varying vec3 world;
varying vec2 offset;

float hash31(vec3 p) {
	p = fract(p * 0.1031);
	p += dot(p, p.zyx + 31.32);
	return fract((p.x + p.y) * p.z);
}

vec2 hash22(vec2 p) {
	vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
	q += dot(q, q.yzx + 33.33);
	return fract((q.xx + q.yz) * q.zy);
}

float value_noise(vec3 p) {
	vec3 i = floor(p);
	vec3 f = p - i;
	vec3 u = f * f * (3.0 - 2.0 * f);
	return mix(
		mix(mix(hash31(i), hash31(i + vec3(1, 0, 0)), u.x),
			mix(hash31(i + vec3(0, 1, 0)), hash31(i + vec3(1, 1, 0)), u.x), u.y),
		mix(mix(hash31(i + vec3(0, 0, 1)), hash31(i + vec3(1, 0, 1)), u.x),
			mix(hash31(i + vec3(0, 1, 1)), hash31(i + vec3(1, 1, 1)), u.x), u.y),
		u.z);
}

float macro(vec3 p) {
	return (0.5 * value_noise(p / 24.0) + 0.3 * value_noise(p / 12.0 + 17.0) + 0.2 * value_noise(p / 6.0 + 41.0));
}

void vertex() {
	world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	offset = hash22(floor(MODEL_MATRIX[3].xz * 10.0 + 0.5));
}

void fragment() {
	vec2 uv = UV + offset;
	vec3 photo = texture(albedo_map, uv).rgb;
	vec3 srgb = COLOR.rgb;
	vec3 paint = mix(srgb / 12.92, pow((srgb + 0.055) / 1.055, vec3(2.4)), step(0.04045, srgb)) / mean_luminance;
	vec3 albedo = mix(photo, photo * paint, COLOR.a);
	float shade = 1.0 + macro_strength * clamp((macro(world) - 0.5) / 0.2, -1.0, 1.0);
	ALBEDO = clamp(albedo * shade, 0.0, 1.0);
	NORMAL_MAP = texture(normal_map, uv).rgb;
	ROUGHNESS = texture(roughness_map, uv).r;
}
