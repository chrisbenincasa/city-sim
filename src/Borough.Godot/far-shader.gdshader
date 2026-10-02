// UV counts bays and storeys; UV2.x selects the facade cell. COLOR carries linear family paint.
// The pulse integral averages each opening over the pixel footprint to suppress distant shimmer.

shader_type spatial;
render_mode cull_back, depth_draw_opaque;

#include "body-fade.gdshaderinc"

// Each cell names an opening (u0, u1, v0, v1) and an inset frame thickness (u, v).
uniform vec4 opening[16];
uniform vec4 band[16];

// Linear RGB, converted by the factory. Deliberately not `source_color`, which would convert a
// second time.
uniform vec4 glass_color;
uniform vec4 frame_color;

uniform float wall_roughness = 0.82;
uniform float glass_roughness = 0.24;

// The mean of the unit-period pulse that is 1 on [a, b], over the footprint [t - w, t + w].
float pulse(float t, float a, float b, float w) {
    if (b <= a) return 0.0;
    float at = fract(t);
    if (w <= 0.0) return step(a, at) * (1.0 - step(b, at));

    // Folding into one period keeps high storeys from losing precision in the subtraction.
    float duty = b - a;
    float lo = at - w;
    float hi = at + w;
    float below = (floor(lo) * duty) + clamp(fract(lo), a, b) - a;
    float above = (floor(hi) * duty) + clamp(fract(hi), a, b) - a;
    return clamp((above - below) / (hi - lo), 0.0, 1.0);
}

float rectangle(vec2 at, vec4 box, vec2 footprint) {
    return pulse(at.x, box.x, box.y, footprint.x) * pulse(at.y, box.z, box.w, footprint.y);
}

void vertex() {
    fade_in = body_fade_weight(MODEL_MATRIX[3].xyz, MAIN_CAM_INV_VIEW_MATRIX[3].xyz);
    POSITION = body_fade_position(VERTEX, MODELVIEW_MATRIX, PROJECTION_MATRIX);
}

void fragment() {
    if (body_fade_discards(fade_in, FRAGCOORD.xy)) {
        discard;
    }

    int cell = clamp(int(UV2.x + 0.5), 0, 15);
    vec4 outer = opening[cell];
    vec2 inset = band[cell].xy;

    // Half the pixel's own footprint in cell units. The box filter over a screen-aligned footprint
    // separates exactly for an axis-aligned rectangle, so the two pulses may simply multiply.
    vec2 footprint = 0.5 * fwidth(UV);

    float open = rectangle(UV, outer, footprint);
    float glass = rectangle(UV, outer + vec4(inset.x, -inset.x, inset.y, -inset.y), footprint);
    float frame = max(open - glass, 0.0);

    ALBEDO = (COLOR.rgb * (1.0 - open)) + (frame_color.rgb * frame) + (glass_color.rgb * glass);
    ROUGHNESS = mix(wall_roughness, glass_roughness, glass);
}
