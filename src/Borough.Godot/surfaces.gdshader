shader_type spatial;
render_mode diffuse_burley;
// 0 carriageway, 1 footway/kerb, 2 a pitched roof, 3 a flat deck behind a parapet.
//
// 🔴 3 EXISTS BECAUSE THE PHOTOGRAPH WAS BOTH REPEATING AND WRONG. `roof_albedo` is a slate
// photograph mapped at `roof_texture_metres` = 3 m, which on a pitched roof is broken up by the
// ridge cap, the eaves band and the foreshortening -- and on a 36 x 20 m flat deck is TWELVE BY
// SEVEN copies of one image lying flat and facing the camera. ***A tiling artefact is a property
// of the surface it is on, not of the texture***, so the same map that reads on a gable reads as
// wallpaper on a deck. ⚠ AND IT WAS SLATE ON A MEMBRANE, which is the assembly research pass 03
// puts in its own table of invalid combinations. Both faults have one repair: a flat deck is not
// a roof covering at a smaller angle, it is a different covering, and it is drawn rather than
// photographed.
uniform int surface_kind = 0;

// How wide a membrane roll is, kerb to kerb across the lap, in metres.
//
// ⚠ A DRAWING FIGURE inside the band single-ply comes in, and not a specification. What it is for
// is that ***the only straight lines on a flat roof run one way***, which is the mark that says
// membrane rather than sheet, board or asphalt.
const float ROLL_METRES = 1.05;
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
    if (surface_kind >= 2) {
        TANGENT = abs(NORMAL.y) > 0.999 ? vec3(1,0,0) : normalize(cross(vec3(0,1,0), NORMAL));
        BINORMAL = normalize(cross(NORMAL, TANGENT));
    }
}
float tile_hash(vec2 p) { return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453); }

// How much of a mark of this size a pixel this wide can still resolve.
//
// 🔴 THE SHARED `visible` IS ONE THRESHOLD FOR EVERY MARK ON A SURFACE, AND THAT IS THE BUG IT
// HIDES. It fades on the pixel footprint alone, so a 3 cm weld and a 30 cm course fade together --
// and the weld goes sub-pixel while `visible` is still 1.0, which is a full-contrast line drawn
// narrower than the sample grid. ***That is not a thin line, it is a moiré***, and it is what put
// wide regular bands across a deck whose actual laps are a metre apart. A mark has to lose contrast
// as it loses width, and only the mark knows its own width.
float resolves(float mark, float px) { return clamp(mark / max(px, 0.0001), 0.0, 1.0); }

// ---- breaking the roof photograph's tile -------------------------------------------------------
//
// 🔴 `roof_albedo` REPEATS EVERY 3 m AND A ROOF IS BIGGER THAN THAT. The flat deck showed it first
// because a deck is one broad plane facing the camera, but the fault is the map's and not the
// deck's: a 36 m pitch is twelve copies of one image side by side, and the eye finds a grid of
// identical blobs long before it finds a slate. ***The repair is the one `buildings.gdshader`
// already made for the brick*** -- sample the image three times at hashed offsets and blend, so
// the covering keeps the photograph's grain and loses its period.
//
// ⚠ A TRIANGLE GRID AND NOT A SQUARE ONE, and the reason is the wall's: four cells meeting at a
// corner put the seams on two straight lines a person can see, and three-way blending has no such
// alignment. This is the wall's own geometry with its constants inlined -- the wall tunes its
// three through uniforms because a study was tuning them, and nothing is tuning these.
const float ROOF_SPREAD = 3.0;
const float ROOF_BIAS = 6.0;

vec2 roof_hash(vec2 cell) {
    float seed = dot(cell, vec2(127.1, 311.7));
    return fract(sin(vec2(seed, seed * 1.7)) * 43758.5453);
}

void roof_grid(vec2 uv, out vec2 first, out vec2 second, out vec2 third, out vec3 weight) {
    vec2 skewed = mat2(vec2(1.0, 0.0), vec2(-0.57735027, 1.15470054))
        * (uv * (3.4641016 / ROOF_SPREAD));
    vec2 base = floor(skewed);
    vec3 part = vec3(fract(skewed), 0.0);
    part.z = 1.0 - part.x - part.y;

    if (part.z > 0.0) {
        weight = vec3(part.z, part.y, part.x);
        first = base; second = base + vec2(0.0, 1.0); third = base + vec2(1.0, 0.0);
    } else {
        weight = vec3(-part.z, 1.0 - part.y, 1.0 - part.x);
        first = base + vec2(1.0, 1.0); second = base + vec2(1.0, 0.0); third = base + vec2(0.0, 1.0);
    }

    weight = pow(weight, vec3(ROOF_BIAS));
    weight /= weight.x + weight.y + weight.z;
}
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

    // The upstand is the wall carried PAST the roof, so it takes the wall's palette exactly as a
    // gable end does; the coping is a cap on top of it and is neither wall nor covering.
    bool coping = surface_kind == 3 && abs(face.y) >= .01 && heights.y < .06;
    bool upstand = abs(face.y) < .01
        && (surface_kind == 3 || (surface_kind == 2 && roof_treatment > 0));

    if (surface_kind == 3) {
        if (!coping && !upstand) {
            // THE DECK. Rolls welded at the laps, ponding where the falls are shallow, and a fine
            // mineral grain -- all three drawn, so nothing here has a period to repeat on.
            float across = mod(p.y, ROLL_METRES);
            float lap = min(across, ROLL_METRES - across);
            float weld = (1.0 - smoothstep(0.0, .035 + pixel.y, lap)) * resolves(.035, pixel.y);

            // ⚠ APERIODIC BY ARITHMETIC AND NOT BY LUCK. The two periods here are about 20 m and
            // 23 m and are incommensurate, so over a deck of any size this city builds the pattern
            // never comes back round -- which is the whole complaint the photograph earned.
            float pond = sin(p.x * .31 + sin(p.y * .53)) * sin(p.y * .27);
            float grain = tile_hash(floor(p * 9.0));
            float grainy = resolves(.11, max(pixel.x, pixel.y));

            detail = 1.0 + (grain - .5) * .09 * grainy;
            detail *= .965 + pond * .045;
            detail *= 1.0 - weld * .09;
            NORMAL_MAP = vec3(.5, .5 + weld * .06, 1.0);
            ROUGHNESS = .90 + grain * .05 * grainy;
        }
    } else if (surface_kind != 0) {
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

        // Three reads at hashed offsets, blended on the triangle grid. ⚠ THE GRADIENTS ARE THE
        // UNSHIFTED ONES and must be: an offset is a constant per cell, so its derivative is zero
        // and a `texture()` here would pick its own mip per sample and seam where they disagree.
        vec2 cell_a, cell_b, cell_c; vec3 share;
        roof_grid(uv, cell_a, cell_b, cell_c, share);
        vec3 photographed =
            (textureGrad(roof_albedo, uv + roof_hash(cell_a), dx, dy).rgb * share.x)
            + (textureGrad(roof_albedo, uv + roof_hash(cell_b), dx, dy).rgb * share.y)
            + (textureGrad(roof_albedo, uv + roof_hash(cell_c), dx, dy).rgb * share.z);

        detail = mix(1.0, clamp(dot(photographed, vec3(.2126,.7152,.0722)) / 0.25906122, .4, 1.8), .7);

        vec3 relief =
            ((textureGrad(roof_normal, uv + roof_hash(cell_a), dx, dy).rgb * share.x)
            + (textureGrad(roof_normal, uv + roof_hash(cell_b), dx, dy).rgb * share.y)
            + (textureGrad(roof_normal, uv + roof_hash(cell_c), dx, dy).rgb * share.z)) * 2.0 - 1.0;
        relief.y = -relief.y;
        NORMAL_MAP = normalize(mix(vec3(0,0,1),relief,.55))*.5+.5;
        ROUGHNESS = textureGrad(roof_roughness, uv + roof_hash(cell_a), dx, dy).r;
    }
    ALBEDO = COLOR.rgb * detail;
    if (surface_kind == 2 && roof_treatment > 0) {
        // A narrow ridge cap and eaves/valley band give construction scale without giant tiles.
        // ⚠ NOT APPLIED TO A DECK: a parapet has neither, and that absence is the detail.
        float cap = 1.0 - smoothstep(.09, .17, heights.y);
        float eave = 1.0 - smoothstep(.035, .09, heights.x);
        ALBEDO *= 1.0 - cap*.15 - eave*.22;
    }
    if (coping) {
        // A cap sheds both ways off the top of the wall, so it catches the light the wall does not.
        ALBEDO = wall.rgb * 1.06;
        NORMAL_MAP = vec3(.5,.5,1);
        ROUGHNESS = .78;
    }
    if (upstand) {
        // Gable ends and parapet upstands continue the wall palette; roofing belongs only on the
        // surfaces that are actually covered.
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
    if (surface_kind < 2 || (surface_kind == 2 && roof_treatment == 0))
        ROUGHNESS = surface_kind == 2 ? 0.82 : 0.96;
    SPECULAR = 0.25;
}
