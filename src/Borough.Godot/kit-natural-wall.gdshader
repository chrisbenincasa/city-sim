shader_type spatial;
uniform vec4 base_color : source_color;
uniform sampler2D masonry_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D masonry_roughness : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;
varying vec3 world_point;
varying vec3 world_normal;
float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
void vertex() {
    world_point=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
    world_normal=normalize(mat3(MODEL_MATRIX)*NORMAL);
}
void fragment() {
    vec2 p=abs(world_normal.x)>abs(world_normal.z) ? world_point.zy : world_point.xy;
    float row=floor(p.y*12.0);
    vec2 brick=vec2(p.x*4.0+mod(row,2.0)*.5,p.y*12.0);
    vec2 edge=min(fract(brick),1.0-fract(brick));
    float joint=1.0-smoothstep(.02,.045,min(edge.x,edge.y));
    float variation=hash(floor(brick));
    ALBEDO=mix(base_color.rgb*(.89+.2*variation),base_color.rgb*.84,joint*.65);
    ROUGHNESS=texture(masonry_roughness,UV).r;
    NORMAL_MAP=texture(masonry_normal,UV).rgb;
    NORMAL_MAP_DEPTH=.8;
}
