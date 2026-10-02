shader_type spatial;
render_mode cull_disabled;

#include "body-fade.gdshaderinc"

// Main.FarFade.cs copies these properties and warns on unsupported authored material settings.
uniform vec4 albedo : source_color = vec4(1.0);
uniform float roughness : hint_range(0.0, 1.0) = 0.8;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform float specular : hint_range(0.0, 1.0) = 0.5;
uniform bool tint_by_vertex_colour = false;

void vertex() {
    fade_in = body_fade_weight(MODEL_MATRIX[3].xyz, MAIN_CAM_INV_VIEW_MATRIX[3].xyz);
    POSITION = body_fade_position(VERTEX, MODELVIEW_MATRIX, PROJECTION_MATRIX);
}

void fragment() {
    if (body_fade_discards(fade_in, FRAGCOORD.xy)) {
        discard;
    }

    vec4 base = albedo;
    if (tint_by_vertex_colour) {
        base *= COLOR;
    }

    ALBEDO = base.rgb;
    ROUGHNESS = roughness;
    METALLIC = metallic;
    SPECULAR = specular;
}
