shader_type spatial;
render_mode diffuse_burley;
uniform sampler2D terrain : source_color, filter_linear, repeat_disable;
varying vec2 ground_metres;
void vertex() {
    ground_metres = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xz;
}
float grain_at(vec2 at) {
    return fract(sin(dot(at, vec2(12.9898, 78.233))) * 43758.5453);
}
void fragment() {
    // PROVISIONAL material grain only: no wear, fertility, or other simulation signal.
    float footprint = max(length(dFdx(ground_metres)), length(dFdy(ground_metres)));
    float visible = 1.0 - smoothstep(0.15, 1.2, footprint);
    vec2 at = floor(ground_metres);
    vec2 blend = fract(ground_metres);
    blend = blend * blend * (3.0 - 2.0 * blend);
    float grain = mix(mix(grain_at(at), grain_at(at + vec2(1.0, 0.0)), blend.x),
        mix(grain_at(at + vec2(0.0, 1.0)), grain_at(at + vec2(1.0)), blend.x), blend.y);
    ALBEDO = texture(terrain, UV).rgb * (1.0 + (grain - 0.5) * 0.09 * visible);
    ROUGHNESS = 0.98;
    SPECULAR = 0.2;
}
