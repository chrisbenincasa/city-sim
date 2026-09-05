shader_type spatial;
render_mode cull_front, unshaded, shadows_disabled;
uniform float width = .025;
void vertex() { VERTEX += NORMAL*width; }
void fragment() { ALBEDO=vec3(.018,.028,.04); }
