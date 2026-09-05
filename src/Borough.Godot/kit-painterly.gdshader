shader_type spatial;
render_mode specular_disabled;
uniform vec4 base_color : source_color;
uniform float stroke_scale = 2.0;
varying vec3 world_point;
varying vec3 world_normal;
float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p) {
    vec2 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
void vertex() {
    world_point=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
    world_normal=normalize(mat3(MODEL_MATRIX)*NORMAL);
}
void fragment() {
    vec3 n=abs(world_normal);
    vec2 p=n.y>n.x && n.y>n.z ? world_point.xz : n.x>n.z ? world_point.zy : world_point.xy;
    p*=stroke_scale;
    float wash=noise(p*.65);
    vec2 cell=floor(p);
    float paint=0.0;
    float coverage=0.0;
    for (int y=-1;y<=1;y++) {
        for (int x=-1;x<=1;x++) {
            vec2 id=cell+vec2(float(x),float(y));
            vec2 centre=id+vec2(hash(id+13.0),hash(id+37.0));
            float angle=(hash(id+91.0)-.5)*1.8;
            vec2 q=mat2(vec2(cos(angle),sin(angle)),vec2(-sin(angle),cos(angle)))*(p-centre);
            float edge=max(abs(q.x)/.8,abs(q.y)/.23);
            float dab=1.0-smoothstep(.55,1.0,edge);
            float bristle=noise(vec2(q.x*2.0,q.y*32.0));
            paint+=dab*(hash(id+53.0)*.85+bristle*.15);
            coverage+=dab;
        }
    }
    float brush=coverage>.01 ? paint/coverage : .5;
    float grain=noise(p*65.0);
    vec3 warm=vec3(1.10,1.025,.91), cool=vec3(.91,.96,1.09);
    ALBEDO=base_color.rgb*mix(cool,warm,wash)*(.93+brush*.15+grain*.006);
    ROUGHNESS=1.0;
}
void light() {
    float facing=dot(NORMAL,LIGHT);
    float wash=smoothstep(-.25,.95,facing);
    DIFFUSE_LIGHT += mix(vec3(.20,.24,.32),vec3(1.06,1.01,.94),wash)*ATTENUATION*LIGHT_COLOR/PI;
}
