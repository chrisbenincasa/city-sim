shader_type spatial;
render_mode specular_disabled;
uniform vec4 base_color : source_color;
void fragment() { ALBEDO=base_color.rgb; ROUGHNESS=1.0; }
void light() {
    float facing=dot(NORMAL,LIGHT);
    float shade=facing>.6 ? 1.0 : facing>.12 ? .56 : .06;
    DIFFUSE_LIGHT += shade*ATTENUATION*LIGHT_COLOR/PI;
}
