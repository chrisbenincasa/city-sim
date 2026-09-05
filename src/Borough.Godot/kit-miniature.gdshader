shader_type spatial;
render_mode diffuse_lambert_wrap;
uniform vec4 base_color : source_color;
void fragment() {
    ALBEDO=base_color.rgb;
    ROUGHNESS=.94;
    SPECULAR=.18;
}
