shader_type spatial;
render_mode cull_back, depth_draw_opaque;

uniform vec4 grid_color : source_color = vec4(0.2, 0.2, 0.2, 1.0);
uniform float grid_width = 0.035;

void fragment() {
    vec2 cell = min(fract(UV), 1.0 - fract(UV));
    vec2 antialias = max(fwidth(UV), vec2(0.001));
    vec2 lines = 1.0 - smoothstep(vec2(grid_width), vec2(grid_width) + antialias, cell);
    float grid = max(lines.x, lines.y);
    ALBEDO = mix(COLOR.rgb, grid_color.rgb, grid);
    ROUGHNESS = 0.82;
}
