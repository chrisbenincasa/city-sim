// The wall of a Building — a box, with the openings drawn on rather than modelled.
//
// ⚠ EVERY DIMENSION HERE IS IN METRES AND NONE OF THEM IS THE CITY'S. A window is
// PlotDepthMetres' class of thing: geometry the simulation does not hold, invented so the
// picture reads as a street rather than as a bar chart, and drawn procedurally because 65,536
// boxes cannot each carry their own mesh. The size is recovered from the instance transform
// rather than passed in, so a Building drawn taller gets more storeys without being told.
//
// ⚠ TWO CHANNELS OF INSTANCE_CUSTOM ARE THE CITY'S AND THE OTHER TWO ARE THE DRAWING'S.
//   r, g  the street face's outward normal, remapped from -1..1 — which kerb the Lot is on
//         (adr/0074), and therefore where the front door goes
//   b     occupancy as a share of the kind's declared room — held / occupants
//   a     the Building's own draw off its scramble, so two identical boxes are not identical
shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

// One storey, and the same 3.5 m the C# side uses to turn `occupants` into a height.
const float STOREY = 3.5;

// How wide a window bay is, kerb to kerb along the wall.
const float BAY = 3.6;

// Below this a wall is a plinth rather than a storey, and takes no opening.
const float PLINTH = 0.9;

// Past this the openings are noise, so they are faded out rather than aliased.
const float NEAR_FADE = 500.0;
const float FAR_FADE = 1100.0;

// HOW FAR THROUGH THE NIGHT IT IS: 0 at the last of the light, 0.5 at solar midnight, 1 at the
// first of it, and pinned at 1 all day. Set from the clock by Main.Daylight, which is the same
// place the sun's angle comes from.
//
// 🔴 IT IS THE ONLY THING IN THIS FILE THAT COMES FROM OUTSIDE THE INSTANCE, and it has to be:
// every other input here is a property of the Building, and the hour is a property of the world.
//
// 🔴 IT REPLACED A `night` UNIFORM THAT WAS A DARKNESS, AND A DARKNESS IS THE WRONG QUANTITY
// TWICE OVER. It is symmetrical, so it said the same thing at nine in the evening and at four in
// the morning and no window keyed on it could be lit for part of a night -- and it was a
// BRIGHTNESS, so it arrived at the emission as a dimmer and every lamp in the city faded up
// through dusk and down through dawn. ***A lamp does not fade. It is switched.*** What the sky
// does between is the sky's business, and the wall is lit or it is not.
uniform float night_phase = 1.0;

varying vec3 v_local;
varying vec3 v_size;
varying vec3 v_face;
varying vec4 v_custom;

void vertex() {
    v_local = VERTEX;
    v_face = NORMAL;
    v_custom = INSTANCE_CUSTOM;

    // The instance transform's own column lengths, which is the box's size in metres. A unit
    // BoxMesh is what every Building shares, so this is the only place the real size exists.
    v_size = vec3(
        length(MODEL_MATRIX[0].xyz),
        length(MODEL_MATRIX[1].xyz),
        length(MODEL_MATRIX[2].xyz));
}

// A cheap hash, for deciding which openings are shuttered. It is the DRAWING's randomness and
// draws on no purpose_tag: nobody in the city can perceive a window.
float draw(vec3 seed) {
    return fract(sin(dot(seed, vec3(12.9898, 78.233, 37.719))) * 43758.5453);
}

void fragment() {
    vec3 wall = COLOR.rgb;
    vec3 n = normalize(v_face);
    vec3 metres = v_local * v_size;

    // Metres above the pavement, which is what a sill is measured from.
    float up = metres.y + (v_size.y * 0.5);

    // The roof and the underside get no openings, and the branch is a mask rather than a return
    // so the whole wall still shades in one pass.
    float upright = 1.0 - step(0.5, abs(n.y));

    // Which way along the wall we are, and how much wall there is. A face whose normal is X runs
    // along Z and vice versa.
    float sideways = step(0.5, abs(n.x));
    float u = mix(metres.x, metres.z, sideways);
    float span = mix(v_size.x, v_size.z, sideways);

    // ⚠ THE STOREY SPACING IS FITTED RATHER THAN FIXED. A wall 9 m tall gets two storeys of 4.5 m
    // and not two of 3.5 with a metre of blank above, because a blank strip under the eaves reads
    // as a mistake and an over-tall storey reads as a warehouse.
    float storeys = max(1.0, floor((v_size.y - PLINTH) / STOREY));
    float lift = (v_size.y - PLINTH) / storeys;
    float floorAt = floor(max(up - PLINTH, 0.0) / lift);
    float withinY = mod(max(up - PLINTH, 0.0), lift);

    float bays = max(1.0, floor(span / BAY));
    float wide = span / bays;
    float bayAt = floor((u + (span * 0.5)) / wide);
    float withinX = mod(u + (span * 0.5), wide);

    float paneH = min(1.6, lift * 0.52);
    float paneW = min(1.15, wide * 0.42);
    float sill = (lift - paneH) * 0.62;
    float jamb = (wide - paneW) * 0.5;

    // Smoothed against the pixel's own footprint, so a wall a hundred metres off does not sparkle.
    //
    // 🔴 THE FOOTPRINT IS TAKEN OFF THE UNWRAPPED WALL AND NOT OFF THE BAY, AND TAKING IT OFF THE
    // BAY IS WHAT MADE THE WALLS LEAK LIGHT. `withinX` is a `mod`, so at every bay boundary it
    // falls from `wide` to zero in one pixel and `fwidth` of it reports the whole bay -- three
    // and a half METRES of blur handed to a smoothstep whose whole job is to be a centimetre
    // wide. The window then half-opens along the seam: measured at the shipped bay, `inX` comes
    // out at 0.24 on the boundary pixel, which is a quarter of a lit pane laid down as a hairline
    // between every column of windows and, from `withinY`'s own wrap, between every storey.
    // ***The dashes were windows, drawn one pixel wide, in the gaps.***
    //
    // ⚠ IT IS INVISIBLE BY DAY AND THE NIGHT IS WHERE IT WAS REPORTED, for the reason the lit
    // window exists at all: by day a quarter-open pane is a shade of grey on a grey wall, and at
    // night it is a quarter of a lamp on a black one. A defect that only the emission shows is
    // still a defect in the ALBEDO.
    //
    // `u` and `up` run the length of the wall without wrapping and have the same derivative
    // everywhere the `mod` does, so this is the same blur minus the seam.
    vec2 grain = vec2(fwidth(u), fwidth(up)) + 0.0001;
    float inX = smoothstep(jamb - grain.x, jamb + grain.x, withinX)
        * (1.0 - smoothstep(jamb + paneW - grain.x, jamb + paneW + grain.x, withinX));
    float inY = smoothstep(sill - grain.y, sill + grain.y, withinY)
        * (1.0 - smoothstep(sill + paneH - grain.y, sill + paneH + grain.y, withinY));

    float above = step(PLINTH, up) * step(up, v_size.y - 0.35);
    float opening = inX * inY * upright * above;

    // THE FRONT DOOR, and it is the one mark here that says something the city knows. The Lot's
    // Side decides which kerb the Building steps to, so the face carrying the door is the face
    // on the Street — which makes a block's four rows read as four rows facing outward rather
    // than as a field of boxes at arbitrary angles.
    vec2 street = (v_custom.rg * 2.0) - 1.0;
    float frontage = step(0.5, dot(n.xz, street)) * upright;
    float doorBay = floor(bays * 0.5) + floor(v_custom.a * 2.0) * step(2.5, bays);
    float onDoorBay = 1.0 - step(0.5, abs(bayAt - doorBay));
    float doorW = min(1.2, wide * 0.4);
    float doorJamb = (wide - doorW) * 0.5;
    float doorX = smoothstep(doorJamb - grain.x, doorJamb + grain.x, withinX)
        * (1.0 - smoothstep(doorJamb + doorW - grain.x, doorJamb + doorW + grain.x, withinX));
    float doorY = 1.0 - smoothstep(2.2 - grain.y, 2.2 + grain.y, up);
    float door = doorX * doorY * onDoorBay * frontage;

    // 🔴 A DOOR ALONE IS TWO METRES ON A TWENTY-METRE WALL AND CANNOT BE SEEN. It was drawn
    // correctly and was invisible at every zoom the city is looked at from, which is the failure
    // mode a screenshot reports as "the feature is missing". The surround is what doubles its
    // area and gives it an edge in the OTHER direction — pale against the wall where the door is
    // dark against it — so the mark survives being three pixels tall.
    float caseW = doorW + 0.55;
    float caseJamb = (wide - caseW) * 0.5;
    float caseX = smoothstep(caseJamb - grain.x, caseJamb + grain.x, withinX)
        * (1.0 - smoothstep(caseJamb + caseW - grain.x, caseJamb + caseW + grain.x, withinX));
    float caseY = 1.0 - smoothstep(2.55 - grain.y, 2.55 + grain.y, up);
    float doorcase = caseX * caseY * onDoorBay * frontage;

    // ⚠ A SHUTTERED OPENING IS THE OCCUPANCY AND NOT ONE HOUSEHOLD. The share of openings left
    // shut is `held / occupants`; which ones is the drawing's own draw. A Building nobody lives
    // in is shut all over, one at its declared room is open all over, and the half-let ones in
    // between are what a street of them actually looks like.
    float lived = draw(vec3(floorAt, bayAt, dot(n, vec3(1.0, 3.0, 7.0)) + v_custom.a * 91.0));
    float shut = step(v_custom.b, lived);

    // ⚠ A SHELL IS READ OFF ITS OWN PAINT AND NOT OUT OF A CHANNEL. The C# side already darkens
    // an abandoned Building to Derelict, and that is a luminance no standing wall reaches — so
    // the openings can tell a ruin from a house without a fifth thing to pack and keep in step.
    // A ruin's openings are HOLES: nothing is shuttered, because nobody is there to shutter it.
    float ruin = 1.0 - smoothstep(0.06, 0.13, dot(wall, vec3(0.299, 0.587, 0.114)));
    shut *= 1.0 - ruin;

    // The pane itself: darker than its wall always, so a standing Building's openings read as
    // glass. A shutter is the wall, a shade down.
    vec3 pane = mix(wall * 0.24, vec3(0.045, 0.062, 0.085), 0.45);
    vec3 glass = mix(mix(pane, vec3(0.010, 0.011, 0.013), ruin), wall * 0.72, shut);

    float far = 1.0 - smoothstep(NEAR_FADE, FAR_FADE, length(VERTEX));
    float glazed = clamp(opening * (1.0 - doorcase) * far, 0.0, 1.0);
    float cased = clamp(doorcase * far, 0.0, 1.0);
    float doored = clamp(door * far, 0.0, 1.0);

    // A plinth, because a wall meeting grass at exactly its own colour has no bottom.
    float base = 1.0 - smoothstep(0.0, PLINTH, up);
    vec3 body = mix(wall, wall * 0.72, base * upright * far);

    vec3 painted = mix(body, glass, glazed);
    painted = mix(painted, (wall * 1.45) + 0.03, cased);
    painted = mix(painted, mix(wall * 0.30, vec3(0.05, 0.055, 0.06), 0.4), doored);

    ALBEDO = painted;

    // 🔴 A RUIN'S OPENINGS CAME OUT LIGHTER THAN ITS WALLS AND THE ALBEDO WAS NOT THE REASON.
    // Glass is smooth, and a smooth surface with a near-black albedo is mostly the highlight it
    // catches off the sky — so the holes in a shell read as glints. ***There is no glass left in
    // a shell***, which is the physical fact the material was not being told.
    ROUGHNESS = mix(0.9, mix(0.28, 0.85, max(shut, ruin)), glazed);
    SPECULAR = mix(0.5, 0.68, glazed * (1.0 - shut) * (1.0 - ruin));

    // 🔴 THE LIT WINDOW IS THE OCCUPANCY SAYING SOMETHING A DAYTIME PICTURE CANNOT. By day a shut
    // opening and an open one differ by a shade of grey nobody reads at street distance; at night
    // the same number is the difference between a black wall and a lamp. ***So this is the same
    // fact drawn twice, and the second drawing is the legible one*** -- a half-empty quarter reads
    // as half-empty from a kilometre up, which no overlay has ever managed.
    //
    // ⚠ NOT EVERY OCCUPIED WINDOW. `shut` already spends the occupancy share; a second, independent
    // draw decides which of what is left has a light on at this hour, because a household with the
    // lights off in one room is not a household that has moved out. ⚠ AND A RUIN LIGHTS NOTHING,
    // for the reason it shutters nothing.
    // ⚠ AND NOT EVERY WINDOW AT ONCE, WHICH IS THE WHOLE OF WHAT MAKES A CITY LOOK INHABITED.
    // A block that lights in one frame and darkens in one frame is a switch; a block whose
    // windows come on over the evening in no order, thin out through the small hours and are
    // dark before the sun is up is a place people live in. ***The hour a lamp goes on is a
    // property of the household and not of the sky***, so each opening draws its own two, and
    // the sky's only remaining job is to say which night it is.
    //
    //   `lamp`   whether this window is ever lit at all -- the unused room, the empty back
    //            bedroom. It is the old constant, kept, because a wall lit end to end reads as
    //            an office block whatever the hour.
    //   `wakes`  when it comes on, spread across the evening: from about sunset to a third of
    //            the way through the night.
    //   `sleeps` when it goes out. ⚠ CUBED RATHER THAN FLAT, and that is the shape of the
    //            request rather than a tuning: a flat draw empties the city at an even rate all
    //            night, and what a city actually does is empty in a rush after everyone is home
    //            and then hold a long thin tail of people still up. Cubed, the wall is fully lit
    //            at 21:00, 29% of it is still burning at solar midnight and 3% an hour and a
    //            half before dawn.
    //
    // ⚠ THE LAST OF THEM GOES OUT BEFORE THE SUN COMES UP AND NOT WITH IT. The ceiling is 0.86
    // against a sunrise at 0.92, so the city is dark for a moment first -- which is the order
    // the two things happen in outdoors, and reversing it makes the dawn read as a light switch.
    float lamp = draw(vec3(bayAt, floorAt, 13.0 + (v_custom.a * 57.0)));
    float wakes = draw(vec3(bayAt, floorAt, 29.0 + (v_custom.a * 23.0)));
    float sleeps = draw(vec3(floorAt, bayAt, 71.0 + (v_custom.a * 41.0)));

    float onAt = 0.06 + (wakes * 0.24);
    float offAt = max(onAt + 0.08, 0.30 + (0.56 * sleeps * sleeps * sleeps));

    // 🔴 A STEP AND NOT A RAMP, AND THIS IS THE SECOND TIME THE SAME MISTAKE WAS MADE IN ONE
    // FILE. A `smoothstep` was here, twelve thousandths of a night wide, on the argument that
    // several thousand windows switching hard would arrive as a sparkle. ***It arrives as a
    // window that fades on, and nothing in the world fades on*** -- a switch is thrown and the
    // room is lit. The sparkle it was guarding against does not exist either: every opening
    // draws its own hour off a continuous hash, so no two switch on the same frame anyway.
    float wired = step(lamp, 0.70) * (1.0 - shut) * (1.0 - ruin) * upright;
    float burning = wired * step(onAt, night_phase) * (1.0 - step(offAt, night_phase));

    // ⚠ THE SECOND TERM IS WHAT SURVIVES THE FADE, and without it a city goes BLACK from exactly
    // the distance at which its lights would be the whole picture: `glazed` carries the far fade,
    // so a window past FAR_FADE contributes nothing. This is the same warmth spread over the wall
    // instead of the pane, keyed on occupancy alone -- the per-window hash is noise at that range,
    // and noise that flickers is worse than no lights at all.
    //
    // ⚠ IT KEEPS THE SAME HOURS, and it has to work them out for itself because there is no one
    // window here to ask: `wakeful` approximates the share of a wall that would be lit at this
    // phase if the two draws above were run over the whole of it.
    //
    // ⚠ AND IT IS THE ONE TERM HERE THAT MAY RAMP, because it is not a lamp. It is a few thousand
    // of them summed at a kilometre, and a sum moves smoothly however hard each switch is -- so
    // the smoothstep that was wrong on a window is right on the smudge.
    float wakeful = smoothstep(0.0, 0.10, night_phase)
        * (1.0 - smoothstep(0.10, 0.86, night_phase));
    float haze = (1.0 - far) * upright * v_custom.b * (1.0 - ruin) * wakeful * 0.16;

    EMISSION = vec3(1.0, 0.74, 0.40) * ((glazed * burning * 2.6) + haze);
}
