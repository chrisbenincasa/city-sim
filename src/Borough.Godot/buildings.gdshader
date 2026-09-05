// The wall of a Building — a box, with the openings drawn on rather than modelled.
//
// ⚠ EVERY DIMENSION HERE IS IN METRES AND NONE OF THEM IS THE CITY'S. A window is
// PlotDepthMetres' class of thing: geometry the simulation does not hold, invented so the
// picture reads as a street rather than as a bar chart, and drawn procedurally because 65,536
// boxes cannot each carry their own mesh. The size is recovered from the instance transform
// rather than passed in, so a Building drawn taller gets more storeys without being told.
//
// 🔴 THE STOREY COUNT IS THE ONE THING HERE THAT IS THE CITY'S, and it arrives through the same
// door as the size: `LotTable.Storeys` times `Main.StoreyMetres` IS the height of the box, so
// dividing it back out returns the city's number exactly. ⚠ A window is still invented geometry;
// HOW MANY ROWS OF THEM there are is not.
//
// ⚠ TWO CHANNELS OF INSTANCE_CUSTOM ARE THE CITY'S AND THE OTHER TWO ARE THE DRAWING'S.
//   r, g  the street face's outward normal, remapped from -1..1 — which kerb the Lot is on
//         (adr/0074), and therefore where the front door goes
//   b     occupancy as a share of the kind's declared room — held / occupants
//   a     the Building's own draw off its scramble, so two identical boxes are not identical
shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

// Set by the shell from the same height used to draw each storey.
uniform float storey_metres = 3.5;

// How wide a window bay is, kerb to kerb along the wall.
const float BAY = 3.6;

// Below this a wall is a plinth rather than a storey, and takes no opening.
const float PLINTH = 0.9;

// Past this the openings are noise, so they are faded out rather than aliased.
const float NEAR_FADE = 500.0;
const float FAR_FADE = 1100.0;

// The last of the night at which a lamp may come ON, as a share of the night.
//
// 🔴 IT EXISTS BECAUSE YOU COULD SEE THE CITY CHANGE GEAR. The two draws below used to occupy
// DISJOINT halves of the night -- every lamp was on by 0.30 and none went out before 0.30 -- so
// the city filled up, hit a seam, and emptied. ***Two processes that cannot overlap read as one
// switch***, however smoothly each of them is spread, and the seam was legible at a glance.
// They now overlap across the whole middle of the night: some windows are going dark while
// others are still coming on, and only past this does the city do one thing at a time.
const float LATEST_ON = 0.48;

// The least time a lamp stays lit once it is lit.
//
// ⚠ WITHOUT IT THE OVERLAP BUYS A FLICKER. The moment `offAt` is allowed to fall below `onAt`
// the two draws are independent, so a window can be scheduled to go out before it came on and
// -- worse -- to go out just after, which is a lamp switched on and off inside a few minutes.
// ***A room somebody walked into is lit for the evening, not for a moment.*** About an hour and
// a half of a night, which is the shortest stay that still reads as somebody being in.
const float MIN_LIT = 0.17;

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

// WHICH NIGHT IT IS, counted from the world's first, and wrapped so the float stays exact.
//
// 🔴 WITHOUT IT EVERY NIGHT IS THE SAME NIGHT, and that is what it was. Every draw below keyed on
// the bay, the storey and the Building -- all three constant for as long as the Building stands --
// so the same windows lit at the same hours for ever. ***A city that repeats itself exactly is a
// screensaver***, and the tell is that you cannot tell two nights apart.
//
// ⚠ IT TURNS OVER AT DUSK AND NOT AT MIDNIGHT, because a night spans the date boundary and a
// counter that ticked at 00:00 would relight the whole city halfway through the evening. Main
// increments it at the same moment `night_phase` wraps to zero, which is the one instant no lamp
// is keyed on.
uniform float night_index = 0.0;

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

    // 🔴 THE STOREY COUNT IS RECOVERED EXACTLY AND WAS FITTED. It read
    // `floor((v_size.y - PLINTH) / storey_metres)` against a height the C# side had multiplied by a
    // 0.55–1.85 jitter, so ***the number of window rows on a wall was not the number of storeys the
    // city holds*** -- measured on `platted.toml`, a ladder of five rungs came out as eight distinct
    // counts, 130 Buildings drawn one storey tall on a city whose floor is two. The jitter is gone
    // and the height is now `storeys * STOREY` exactly, so a `round` returns the city's own number
    // and `lift` is the storey height it was built from.
    //
    // ⚠ THE PLINTH IS NO LONGER SUBTRACTED FROM THE GRID, and the sentence that used to stand here
    // was about the jitter rather than about the plinth: a wall 9 m tall got two storeys of 4.5 m
    // so that no blank strip was left under the eaves. An exact multiple leaves none, so the fit
    // has nothing to absorb. The plinth stays what it separately is -- `above` below, which is the
    // mask that keeps an opening off the bottom 0.9 m of a wall.
    float storeys = max(1.0, floor((v_size.y / storey_metres) + 0.5));
    float lift = v_size.y / storeys;
    float floorAt = floor(up / lift);
    float withinY = mod(up, lift);

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

    // 🔴 THE REVEAL, AND IT IS THE DOOR'S OWN ARGUMENT APPLIED TO A WINDOW. A pane is drawn DARKER
    // than its wall, so on a wall that is already dark -- one in shadow, or one painted slate --
    // the pane and the wall land in the same crushed end of the tonemapper and the openings simply
    // vanish. ***Measured: at 06:45 a sunlit facade shows its full window grid and the shadowed
    // facade beside it reads as a blank slab, on the same Building.*** The night frame is what
    // proves the openings were there all along -- every one of those blank walls lights up.
    //
    // ⚠ WHAT SURVIVES A DIM IS AN EDGE AND NOT A TONE. The surround is the wall turned OUT, so the
    // opening becomes a light–dark PAIR rather than a single dark patch, and a pair still reads
    // when both halves are dark because the eye measures the step between them. It is exactly what
    // `doorcase` below already does for the door, and for the same reason.
    float outerW = paneW + 0.42;
    float outerH = paneH + 0.36;
    float outerJamb = (wide - outerW) * 0.5;
    float outerSill = sill - 0.18;
    float outX = smoothstep(outerJamb - grain.x, outerJamb + grain.x, withinX)
        * (1.0 - smoothstep(outerJamb + outerW - grain.x, outerJamb + outerW + grain.x, withinX));
    float outY = smoothstep(outerSill - grain.y, outerSill + grain.y, withinY)
        * (1.0 - smoothstep(outerSill + outerH - grain.y, outerSill + outerH + grain.y, withinY));
    float reveal = clamp((outX * outY * upright * above) - opening, 0.0, 1.0);

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

    // PROVISIONAL masonry and joinery; these add construction detail, not condition.
    float close_detail = 1.0 - smoothstep(0.10, 0.55, max(grain.x, grain.y));
    float course = mod(up, 0.32);
    float joint = 1.0 - smoothstep(0.012 - grain.y, 0.012 + grain.y, min(course, 0.32 - course));
    float masonry = step(v_custom.a, 0.38) * upright * close_detail;
    body *= 1.0 - joint * masonry * 0.13;
    float cornice = smoothstep(v_size.y - 0.32 - grain.y, v_size.y - 0.32 + grain.y, up) * upright;
    body = mix(body, wall * 1.22, cornice * far);
    float barX = 1.0 - smoothstep(0.045 - grain.x, 0.045 + grain.x, abs(withinX - wide * 0.5));
    float barY = 1.0 - smoothstep(0.035 - grain.y, 0.035 + grain.y, abs(withinY - sill - paneH * 0.55));
    float frame = max(barX, barY) * close_detail;
    glass = mix(glass, wall * 1.15, frame);
    vec3 painted = mix(body, glass, glazed);
    painted = mix(painted, (wall * 1.32) + 0.02, clamp(reveal * far, 0.0, 1.0) * (1.0 - cased));
    painted = mix(painted, (wall * 1.45) + 0.03, cased);
    painted = mix(painted, mix(wall * 0.30, vec3(0.05, 0.055, 0.06), 0.4), doored);

    ALBEDO = painted;

    // 🔴 A RUIN'S OPENINGS CAME OUT LIGHTER THAN ITS WALLS AND THE ALBEDO WAS NOT THE REASON.
    // Glass is smooth, and a smooth surface with a near-black albedo is mostly the highlight it
    // catches off the sky — so the holes in a shell read as glints. ***There is no glass left in
    // a shell***, which is the physical fact the material was not being told.
    // 🔴 GLASS IS A MIRROR AND A WALL IS NOT, WHICH IS THE OTHER HALF OF WHY A SHADOWED FACADE
    // READ AS BLANK. Ambient here is the SKY at full contribution, so a smooth surface returns the
    // sky and a rough one returns almost nothing -- and the roughness was 0.28, which is a broad
    // weak sheen that a wall's own diffuse swamps. At 0.06 the pane picks the sky up properly, and
    // ***the openings on a wall the sun is not on come out BRIGHTER than the wall rather than
    // darker***, which is what a real window does and is the reverse of the sunlit case. Two
    // opposite readings of the same geometry, both correct, and the wall is legible under either.
    //
    // ⚠ A SHUT PANE STAYS ROUGH AND SO DOES A RUIN. A shutter is boards and a shell has no glass
    // left in it, which is the fact the material is being told rather than a look being chosen.
    ROUGHNESS = mix(0.9, mix(0.06, 0.85, max(shut, ruin)), glazed);
    SPECULAR = mix(0.5, 0.9, glazed * (1.0 - shut) * (1.0 - ruin));

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
    //   `ever`   whether this window is ever lit at all -- the unused room, the empty back
    //            bedroom, the stairwell. ⚠ IT IS THE ONE DRAW HERE THAT DOES NOT TURN OVER, and
    //            that is the point: a room with no lamp in it has no lamp in it tomorrow either,
    //            so a Building keeps a few dead openings for as long as it stands.
    //   `in`     whether anybody is in TONIGHT, redrawn every night off `night_index`.
    //            🔴 WITHOUT THIS EVERY NIGHT WAS THE SAME NIGHT -- the same windows at the same
    //            hours for ever, because every draw was keyed on things that do not change. ***A
    //            city you cannot tell two nights apart is a screensaver.*** Roughly half of what
    //            is wired is in on a given night, and it is a different half each time.
    //   `wakes`  when it comes on, spread from the last of the light to LATEST_ON -- which is
    //            nearly half the night rather than the third it was, so the arrivals are still
    //            running while the early leavers are going dark.
    //   `sleeps` when it goes out. ⚠ CUBED RATHER THAN FLAT, and that is the shape of the
    //            request rather than a tuning: a flat draw empties the city at an even rate all
    //            night, and what a city actually does is empty in a rush after everyone is home
    //            and then hold a long thin tail of people still up.
    //
    // ⚠ THE LAST OF THEM GOES OUT BEFORE THE SUN COMES UP AND NOT WITH IT. The ceiling is 0.86
    // against a sunrise at 0.92, so the city is dark for a moment first -- which is the order
    // the two things happen in outdoors, and reversing it makes the dawn read as a light switch.
    //
    // 🔴 THE TWO SCHEDULES OVERLAP AND THAT IS THE WHOLE POINT OF THE NUMBERS ABOVE. `offAt`
    // opens at 0.18 while `onAt` runs to 0.48, so through the middle of every night both things
    // are happening at once and there is no instant the city turns round. MIN_LIT is what stops
    // that overlap from producing a lamp that blinks: it is a FLOOR on the duration and not on
    // the hour, so a window that came on late simply stays on later.
    float ever = draw(vec3(bayAt, floorAt, 13.0 + (v_custom.a * 57.0)));
    float in_tonight = draw(
        vec3(bayAt, floorAt, 101.0 + (v_custom.a * 19.0) + (night_index * 7.31)));
    float wakes = draw(
        vec3(bayAt, floorAt, 29.0 + (v_custom.a * 23.0) + (night_index * 4.17)));
    float sleeps = draw(
        vec3(floorAt, bayAt, 71.0 + (v_custom.a * 41.0) + (night_index * 2.53)));

    float onAt = 0.02 + (wakes * (LATEST_ON - 0.02));
    float offAt = max(onAt + MIN_LIT, 0.18 + (0.68 * sleeps * sleeps * sleeps));

    // 🔴 A STEP AND NOT A RAMP, AND THIS IS THE SECOND TIME THE SAME MISTAKE WAS MADE IN ONE
    // FILE. A `smoothstep` was here, twelve thousandths of a night wide, on the argument that
    // several thousand windows switching hard would arrive as a sparkle. ***It arrives as a
    // window that fades on, and nothing in the world fades on*** -- a switch is thrown and the
    // room is lit. The sparkle it was guarding against does not exist either: every opening
    // draws its own hour off a continuous hash, so no two switch on the same frame anyway.
    // ⚠ TWO GATES AND NOT ONE, AND THEY ANSWER DIFFERENT QUESTIONS. `ever` asks whether there
    // is a lamp in this room at all and never changes; `in_tonight` asks whether anybody is home
    // to switch it on and is redrawn nightly. 0.80 x 0.62 is about half of an unshuttered wall lit
    // on any one night -- against the 0.70 that stood here, which lit the same seven windows in
    // ten every night of the world's life.
    float wired =
        step(ever, 0.80) * step(in_tonight, 0.62) * (1.0 - shut) * (1.0 - ruin) * upright;
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
    // ⚠ ITS SHOULDERS FOLLOW THE TWO SCHEDULES ABOVE and are not independent of them: the rise
    // is `onAt`'s spread and the fall is `offAt`'s, so the smudge a distant city reads as peaks
    // where the count of lit windows actually peaks rather than at a phase nobody chose.
    float wakeful = smoothstep(0.0, 0.22, night_phase)
        * (1.0 - smoothstep(0.30, 0.86, night_phase));
    float haze = (1.0 - far) * upright * v_custom.b * (1.0 - ruin) * wakeful * 0.16;

    EMISSION = vec3(1.0, 0.74, 0.40) * ((glazed * (1.0 - frame) * burning * 2.6) + haze);
}
