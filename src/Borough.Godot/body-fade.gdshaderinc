// Both bodies use the instance origin from the vertex stage. In a MultiMesh, the fragment
// stage's MODEL_MATRIX belongs to the node. The main camera also owns the weight in shadow passes.
uniform float fade_inner_metres = 0.0;
uniform float fade_outer_metres = 0.0;
uniform bool fade_near_side = false;
uniform float fade_depth_metres = 0.0;

varying float fade_in;

float body_fade_weight(vec3 instance_origin, vec3 camera_origin) {
    if (fade_outer_metres <= fade_inner_metres) {
        return fade_near_side ? -1.0 : 2.0;
    }
    return clamp(smoothstep(fade_inner_metres, fade_outer_metres,
        distance(camera_origin, instance_origin)), 0.0, 1.0);
}

// Only depth moves, so a far surface wins against its flush near wall without shifting pixels.
vec4 body_fade_position(vec3 vertex, mat4 modelview, mat4 projection) {
    vec4 view = modelview * vec4(vertex, 1.0);
    vec4 clip = projection * view;
    view.xyz *= 1.0 - (fade_depth_metres / max(length(view.xyz), 1.0));
    vec4 nearer = projection * view;
    clip.z = nearer.z * (clip.w / nearer.w);
    return clip;
}

bool body_fade_discards(float weight, vec2 fragment) {
    const vec3 magic = vec3(0.06711056, 0.00583715, 52.9829189);
    float threshold = fract(magic.z * fract(dot(fragment, magic.xy)));
    return fade_near_side ? weight > threshold : weight <= threshold;
}
