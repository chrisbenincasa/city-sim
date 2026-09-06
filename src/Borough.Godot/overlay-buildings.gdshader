shader_type spatial;
render_mode unshaded, cull_back;

// Review variants: 0 flat reference, 1 fixed face shading, 2 face shading with edges.
uniform int treatment = 2;
uniform bool context_only = false;
uniform vec3 context_colour : source_color = vec3(0.08, 0.10, 0.095);
varying vec3 local_position;
varying vec3 world_normal;
varying vec3 triangle;
varying flat int roof_mask;

void vertex() {
    local_position = VERTEX;
    triangle = vec3(UV2, 1.0 - UV2.x - UV2.y);
    roof_mask = UV.y < -16.0 ? int(UV.x + 0.5) : -1;
    world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
    vec3 ink = context_only ? context_colour : COLOR.rgb;
    if (treatment > 0) {
        // A fixed diagram light: the reading cannot change with time of day.
        float face = 0.66 + 0.34 * max(0.0, dot(normalize(world_normal), normalize(vec3(-0.4, 0.85, 0.3))));
        ink *= face;
    }
    if (treatment > 1 && !context_only) {
        // Second-nearest unit-mesh bound finds geometric edges, not triangle diagonals.
        vec3 pixels = (vec3(0.5) - abs(local_position)) / max(fwidth(local_position), vec3(0.00001));
        float nearest = min(pixels.x, min(pixels.y, pixels.z));
        float furthest = max(pixels.x, max(pixels.y, pixels.z));
        float edge = 1.0 - smoothstep(0.65, 1.65, pixels.x + pixels.y + pixels.z - nearest - furthest);
        if (roof_mask >= 0) {
            vec3 d = triangle / max(fwidth(triangle), vec3(0.00001));
            if ((roof_mask & 1) == 0) d.x = 1000.0;
            if ((roof_mask & 2) == 0) d.y = 1000.0;
            if ((roof_mask & 4) == 0) d.z = 1000.0;
            edge = 1.0 - smoothstep(0.65, 1.65, min(d.x, min(d.y, d.z)));
        }
        ink = mix(ink, COLOR.rgb * 0.23, edge * 0.75);
    }
    ALBEDO = ink;
}
