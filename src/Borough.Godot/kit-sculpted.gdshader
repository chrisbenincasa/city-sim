shader_type spatial;
render_mode specular_disabled;

uniform vec4 base_color : source_color = vec4(1.0);

void fragment() {
    ALBEDO = base_color.rgb;
    ROUGHNESS = 1.0;
}

void light() {
    float facing = max(dot(NORMAL, LIGHT), 0.0);
    float band = facing < 0.18 ? 0.0 : (facing < 0.65 ? 0.48 : 1.0);
    DIFFUSE_LIGHT += band * ATTENUATION * LIGHT_COLOR / PI;
}
