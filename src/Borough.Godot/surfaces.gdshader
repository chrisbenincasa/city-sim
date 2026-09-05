shader_type spatial;
render_mode diffuse_burley;
uniform int surface_kind = 0;
varying vec3 metres;
varying vec3 face;
void vertex() {
    vec3 size = vec3(length(MODEL_MATRIX[0].xyz), length(MODEL_MATRIX[1].xyz), length(MODEL_MATRIX[2].xyz));
    metres = VERTEX * size;
    face = NORMAL;
}
void fragment() {
    // PROVISIONAL construction detail, not a record of maintenance or traffic.
    vec2 p = surface_kind == 2 ? vec2(metres.x + metres.z, metres.y * 1.6) : metres.xz;
    vec2 pixel = fwidth(p) + vec2(0.0001);
    float visible = 1.0 - smoothstep(0.12, 0.75, max(pixel.x, pixel.y));
    float detail = 1.0;
    if (surface_kind != 0) {
        vec2 module = surface_kind == 2 ? vec2(0.48, 0.30) : vec2(0.8, 0.6);
        float row = floor(p.y / module.y);
        vec2 q = vec2(p.x + mod(row, 2.0) * module.x * 0.5, p.y);
        vec2 edge = min(mod(q, module), module - mod(q, module));
        vec2 seams = 1.0 - smoothstep(vec2(0.018) - pixel, vec2(0.018) + pixel, edge);
        detail -= max(seams.x, seams.y) * 0.22 * visible;
    } else {
        detail += sin(p.x * 19.0) * sin(p.y * 17.0) * 0.025 * visible;
    }
    ALBEDO = COLOR.rgb * detail;
    ROUGHNESS = surface_kind == 2 ? 0.82 : 0.96;
    SPECULAR = 0.25;
}
