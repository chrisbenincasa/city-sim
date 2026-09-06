shader_type spatial;
render_mode diffuse_burley;
uniform int surface_kind = 0;
uniform int roof_treatment = 2;
uniform float roof_texture_metres = 3.0;
uniform sampler2D roof_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D roof_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D roof_roughness : filter_linear_mipmap, repeat_enable;
uniform sampler2D roof_wall_brick : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D roof_wall_plaster : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
varying vec4 wall;
varying vec2 heights;
varying float base_y;
varying vec3 metres;
varying vec3 face;
void vertex() {
    vec3 size = vec3(length(MODEL_MATRIX[0].xyz), length(MODEL_MATRIX[1].xyz), length(MODEL_MATRIX[2].xyz));
    metres = VERTEX * size;
    heights = vec2(metres.y + size.y*.5, size.y*.5 - metres.y);
    base_y = MODEL_MATRIX[3].y;
    wall = INSTANCE_CUSTOM;
    face = normalize(NORMAL / max(size, vec3(0.0001)));
    if (surface_kind == 2) {
        TANGENT = abs(NORMAL.y) > 0.999 ? vec3(1,0,0) : normalize(cross(vec3(0,1,0), NORMAL));
        BINORMAL = normalize(cross(NORMAL, TANGENT));
    }
}
float tile_hash(vec2 p) { return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453); }
void fragment() {
    // PROVISIONAL construction detail, not a record of maintenance or traffic.
    vec2 p = surface_kind == 2 ? vec2(metres.x + metres.z, metres.y * 1.6) : metres.xz;
    if (surface_kind == 2 && roof_treatment > 0) {
        vec3 n = normalize(face);
        vec3 across = abs(n.y) > 0.999 ? vec3(1,0,0) : normalize(cross(vec3(0,1,0),n));
        p = vec2(dot(metres, across), dot(metres, cross(n,across)));
    }
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
    if (surface_kind == 2 && roof_treatment > 0) {
        vec2 module = vec2(0.32, 0.24);
        float row = floor(p.y / module.y);
        vec2 course = vec2(p.x + mod(row,2.0)*module.x*0.5,p.y);
        vec2 cell = floor(course / module);
        vec2 within = mod(course,module);
        float variation = tile_hash(cell);
        vec2 edge = min(within,module-within);
        vec2 joint = 1.0 - smoothstep(vec2(0.006)-pixel, vec2(0.006)+pixel,edge);
        float lip = 1.0 - smoothstep(0.006-pixel.y,0.006+pixel.y,within.y);
        detail = 1.0 + (variation-.5)*.20*visible;
        detail *= 1.0 - (joint.x*.13 + joint.y*.16 + lip*.14)*visible;
        float weather = sin(p.x*.31 + sin(p.y*.53)) * sin(p.y*.27);
        detail *= .94 + weather*.045;
        NORMAL_MAP = vec3(.5 + (variation-.5)*.04*visible, .5 + lip*.09*visible, 1.0);
        ROUGHNESS = .88 + variation*.08*visible;
    }
    if (surface_kind == 2 && roof_treatment == 2) {
        vec2 uv = vec2(p.x, -p.y) / roof_texture_metres;
        vec2 dx = dFdx(uv), dy = dFdy(uv);
        vec3 photographed = textureGrad(roof_albedo, uv, dx, dy).rgb;
        detail = mix(1.0, clamp(dot(photographed, vec3(.2126,.7152,.0722)) / 0.25906122, .4, 1.8), .7);
        vec3 relief = textureGrad(roof_normal, uv, dx, dy).rgb * 2.0 - 1.0;
        relief.y = -relief.y;
        NORMAL_MAP = normalize(mix(vec3(0,0,1),relief,.55))*.5+.5;
        ROUGHNESS = textureGrad(roof_roughness, uv, dx, dy).r;
    }
    ALBEDO = COLOR.rgb * detail;
    if (surface_kind == 2 && roof_treatment > 0) {
        // A narrow ridge cap and eaves/valley band give construction scale without giant tiles.
        float cap = 1.0 - smoothstep(.09, .17, heights.y);
        float eave = 1.0 - smoothstep(.035, .09, heights.x);
        ALBEDO *= 1.0 - cap*.15 - eave*.22;
        if (abs(face.y) < .01) {
            // Gable ends continue the wall palette; roofing belongs only on pitched surfaces.
            vec2 wall_uv = vec2(metres.x + metres.z, -(metres.y + base_y));
            float seed = mod(wall.a, 256.0) / 255.0;
            vec3 sample_colour = seed > .38 ? texture(roof_wall_plaster, wall_uv/4.0).rgb
                : texture(roof_wall_brick, wall_uv/1.125).rgb;
            float mean_light = seed > .38 ? .37427 : .089;
            float grain = clamp(dot(sample_colour, vec3(.2126,.7152,.0722)) / mean_light, .45, 1.5);
            ALBEDO = wall.rgb * mix(1.0,grain,.55);
            NORMAL_MAP = vec3(.5,.5,1);
            ROUGHNESS = .95;
        }
    }
    if (surface_kind != 2 || roof_treatment == 0) ROUGHNESS = surface_kind == 2 ? 0.82 : 0.96;
    SPECULAR = 0.25;
}
