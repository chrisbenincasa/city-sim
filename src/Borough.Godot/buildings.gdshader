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
// INSTANCE_CUSTOM carries frontage, occupancy and packed appearance flags.
//   r, g  the street face's outward normal, remapped from -1..1 — which kerb the Lot is on
//         (adr/0074), and therefore where the front door goes
//   b     tenants / declared capacity, zero when abandoned
//   a     FacadeAppearance: byte seed, commercial premises, abandonment and upper-part flags
shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

// Set by the shell from the same height used to draw each storey.
uniform float storey_metres = 3.5;

// PROVISIONAL material comparisons. Source files and linear means live beside the maps.
uniform sampler2D plaster_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D plaster_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D plaster_roughness : filter_linear_mipmap, repeat_enable;
uniform sampler2D wear_height : filter_linear_mipmap, repeat_enable;
uniform sampler2D wear_normal : hint_normal, filter_linear_mipmap, repeat_enable;
uniform vec3 plaster_mean = vec3(0.40807008, 0.36641045, 0.35170243);
uniform float plaster_metres = 4.0;
uniform float plaster_mix = 1.0;
uniform float wear_strength = 1.0;


// How wide a window bay is, kerb to kerb along the wall.
//
// ⚠ IT IS THE ZERO END OF `facade_variety` AND NOT A WIDTH ANYBODY CHOSE. Every wall in the city
// used this one number, which is half of why every wall in the city was the same wall.
const float BAY = 3.6;

// The band a bay is drawn from when the dial is up.
const float BAY_LEAST = 3.0;
const float BAY_MOST = 5.2;

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

// ---- the photographed masonry -----------------------------------------------------------------
//
// 🔴 THE ONE THING plans/0063 ESTABLISHED THAT NO PROCEDURAL MARK REPLACES. Nine rounds of the
// visual study moved from painted boxes to shader variants to authored geometry, and the round the
// player called *amazing* was the one that put a PHOTOGRAPH on the wall -- Poly Haven's CC0 brick,
// mapped at a real course height. Everything below this line in the old file was a procedural
// approximation of what these two images are: `joint` drew one mortar line every 0.32 m and nothing
// else, and a single ruled line is a wall of the same grey with a stripe on it.
//
// ⚠ IT MODULATES THE BUILDING'S OWN PAINT AND DOES NOT REPLACE IT, which is the whole reason this
// can be turned on across a city rather than on one specimen. `Main.Massing.Rendered` spends a
// Building's scramble on VALUE and WARMTH so that a terrace reads as one stone weathered
// differently -- and a texture pasted flat over that gives every wall in the city the same red and
// throws the terrace away. So the image is divided by its own mean and applied as a RATIO: the
// grain, the courses and the weathering are the photograph's, the colour stays the city's.
//
// ⚠ AND IT IS A FAMILY AND NOT A FINISH. `masonry` below already picks roughly the cheaper third
// of Buildings off the same draw; those get brick and the rest keep the smooth render. Two
// families is what a street has.
uniform sampler2D masonry_albedo : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D masonry_normal : hint_normal, filter_linear_mipmap, repeat_enable;

// How much wall one width of the image covers, in metres.
//
// 🔴 MEASURED OFF THE IMAGE AND NOT TAKEN FROM A CATALOGUE. Counted on the file itself: five
// stretchers across its width and fifteen courses down its height. Against a standard brick --
// 215 mm plus a 10 mm perpend, 65 mm plus a 10 mm bed -- that is 5 x 0.225 = 1.125 m wide and
// 15 x 0.075 = 1.125 m tall, so the photograph happens to be square in the world as well as in
// pixels and ONE number scales both axes without stretching a brick.
//
// ⚠ IT IS NOT THE CATALOGUE'S 3 m, which is the size of the wall that was photographed rather than
// an instruction: read literally it gives bricks the size of a door. ⚠ AND IT IS NOT THE FIDELITY
// PILOT'S 1.2 either -- that round wrote down five stretchers and then rounded, and the 6% it lost
// is the difference between a 225 mm brick and a 240 mm one.
//
// ⚠ NOBODY HAS CHOSEN AN APPARENT BRICK SIZE. plans/0063 ran a brick-scale round comparing three
// against these same maps and the player selected none, so this is the size a real brick is and
// not a size anybody preferred. It is the dial to turn when that round is finally settled.
uniform float masonry_metres = 1.125;

// How much of the photograph's relief reaches the surface normal.
uniform float masonry_relief = 0.7;

// How much of the photograph's own COLOUR is kept, against how much is taken as light and dark only.
//
// 🔴 IT IS THE DIAL BETWEEN THE STUDY'S WALL AND THIS CITY'S PALETTE, and both readings are wrong
// at the ends. At 1 the sample is divided by its mean channel by channel, so the photograph's
// chroma rides on top of the paint -- and `brick_wall_001` is a MIXED brick with grey and blue in
// it, so a pale Building comes out mottled cold grey rather than the warm terrace the fidelity
// round was praised for. At 0 only the luminance survives, which keeps every Building exactly the
// colour `Rendered` drew and makes the wall a grey-scale rubbing of a brick.
//
// ⚠ WHAT THE STUDY DID IS NOT AVAILABLE HERE AND THAT IS THE POINT. Its terrace used the image as
// the albedo outright, which is why it looked like that photograph -- and a city drawn that way is
// one red for every masonry Building in it. ***A specimen may borrow a photograph's colour; a city
// has to keep its own.***
uniform float masonry_hue : hint_range(0.0, 1.0) = 0.55;

// How many texture tiles across one cell of the shuffling grid below.
//
// 🔴 THE IMAGE IS 1.125 m ACROSS AND A TERRACE IS TWENTY, so a wall shows the same photograph
// EIGHTEEN TIMES SIDE BY SIDE and the eye locks onto it immediately. What it locks onto is not the
// courses -- those are supposed to repeat, that is what brickwork is -- but the CONSTELLATION of
// burnt and pale bricks, which is a landmark, and a landmark that recurs every 1.125 m reads as
// wallpaper. ***A tiling artefact is a large-scale feature repeating, never a small one.***
//
// ⚠ SMALL MEANS MORE SHUFFLING AND MORE BLENDING, and both cost. At 1 nearly every pixel sits in a
// blend between two draws; at 6 the grid is coarse enough that whole panels come from one draw and
// the repeat comes back at the grid's own scale instead of the tile's.
uniform float masonry_spread : hint_range(1.0, 8.0) = 3.0;

// How hard the three-way blend is pushed toward whichever draw is nearest.
//
// 🔴 A LINEAR BLEND OF TWO PHOTOGRAPHS IS NEITHER PHOTOGRAPH. Averaging two brick images halves the
// contrast between brick and mortar wherever the weights are even, so a soft blend lays a grey
// wash in a band across every cell boundary -- the defect the stochastic-texturing literature calls
// variance loss, arriving here as ***a wall that looks rubbed out along diagonal seams.*** Raising
// the weights to a power and renormalising narrows those bands until only one draw is showing over
// nearly all of the wall.
uniform float masonry_bias : hint_range(1.0, 12.0) = 6.0;

// Whether the shuffling happens at all — <b>0 is the plain tiled wall</b>.
//
// ⚠ IT EXISTS SO THE TWO CAN BE PHOTOGRAPHED FROM ONE CAMERA. At zero every cell draws the image at
// the same offset, so the three taps are the same tap and the result is bit-for-bit the tiled wall
// this was written to cure. ***A repetition artefact is a thing you either see or do not, and an
// A/B taken from two different camera positions cannot settle it.***
uniform float masonry_shuffle : hint_range(0.0, 1.0) = 1.0;

// Whether the facade's COMPOSITION varies at all — <b>0 is the one facade the whole city had</b>.
//
// 🔴 IT IS `masonry_shuffle`'S ARGUMENT APPLIED TO A DIFFERENT SCALE OF THE SAME COMPLAINT.
// `plans/0063` separates four: the material repeats every `masonry_metres`, an instance repeats
// against its neighbour, a facade repeats its own window grid, and a flat pane repeats itself down
// the elevation. The shuffling above closed the first and touched none of the other three.
//
// ⚠ AT ZERO THIS IS BIT-FOR-BIT THE FACADE F6 MEASURED — `BAY` exactly, and the three `min`
// expressions that saturate at a 3.5 m storey — so the two can be photographed from ONE camera.
// ***A repetition artefact is a thing you either see or do not, and an A/B taken from two
// different camera positions cannot settle it.***
uniform float facade_variety : hint_range(0.0, 1.0) = 1.0;

// How deep the room behind a pane is, in metres — <b>0 is the flat pane the city had</b>.
//
// 🔴 IT IS THE FOURTH SCALE, AND IT IS THE ONE THE OTHER THREE CANNOT REACH. `facade_variety`
// varies the grid; this varies what is IN the grid. A pane was one constant colour, so a wall of
// forty windows was one mark stamped forty times however irregularly the marks were placed —
// ***a varied arrangement of identical things is still a repetition.***
//
// ⚠ IT COSTS NO INSTANCE, NO MESH AND NO DRAW CALL, which is the property that let the masonry
// cross into the city (`plans/0063`). It is a slab trace against a box and four flat colours; there
// is no room texture, so there is no asset and no licence record to keep.
uniform float interior_depth : hint_range(0.0, 6.0) = 2.6;

// The masonry image's own average, in LINEAR light, which is what divides it back out to a ratio.
//
// ⚠ LINEAR AND NOT sRGB, and the two are far enough apart to matter: this image averages
// (0.396, 0.290, 0.239) as stored and (0.140, 0.078, 0.052) once decoded, because the transfer
// curve is not linear and an average does not survive it. `masonry_albedo` is declared
// `source_color`, so a sample is ALREADY decoded by the time it is divided -- dividing it by the
// stored average would leave every brick wall in the city roughly three times too bright and
// wrongly warm.
//
// ⚠ IT IS A PROPERTY OF THE FILE. Replacing `brick-wall-diffuse.jpg` means retaking this number;
// the command is recorded in `assets/city/materials.json`.
const vec3 MASONRY_MEAN = vec3(0.1401, 0.0776, 0.0515);

varying vec3 v_local;
varying vec3 v_size;
varying vec3 v_face;
varying vec4 v_custom;
varying float v_shop;
varying float v_ruin;

void vertex() {
    v_local = VERTEX;
    v_face = NORMAL;
    v_custom = INSTANCE_CUSTOM;
    // FacadeAppearance packs an exact byte seed and independent state flags.
    float flags = floor(v_custom.a / 256.0);
    v_custom.a = mod(v_custom.a, 256.0) / 255.0;
    v_shop = mod(flags, 2.0) * (1.0 - step(4.0, flags));
    v_ruin = mod(floor(flags / 2.0), 2.0);

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

// ---- breaking the tile ------------------------------------------------------------------------
//
// The image is shuffled across a triangular grid: every cell draws the photograph at its own offset,
// and a fragment blends the three cells whose corners surround it. Nothing repeats until the grid
// does, and the grid is as large as you care to make it.
//
// 🔴 THE OFFSETS ARE SNAPPED TO THE BRICK AND THIS IS THE WHOLE DIFFICULTY. The published technique
// assumes a STOCHASTIC texture -- gravel, bark, rust -- where any offset is as good as any other. A
// brick wall is the opposite: it is a STRUCTURE, and an arbitrary offset slides one draw's courses
// off another's, so the blend band shows two sets of mortar lines crossing at a few centimetres'
// separation. ***Doubled mortar reads as damage, and it is worse than the repetition it cures.***
//
// ⚠ SO AN OFFSET IS A WHOLE NUMBER OF BRICKS. Horizontally the image is five stretchers, so fifths
// of it land a draw exactly on the bond. Vertically it is fifteen courses and the bond alternates
// every course, so the step is TWO courses -- 2/15 -- and an odd number of courses would put a
// perpend over the middle of the brick below it. Five by seven is 35 arrangements, all of them
// correctly bonded, and the blend then cross-fades brick COLOUR against brick colour, which reads
// as another brick rather than as a smear.
vec2 masonry_hash(vec2 cell) {
    vec2 seed = vec2(dot(cell, vec2(127.1, 311.7)), dot(cell, vec2(269.5, 183.3)));

    return fract(sin(seed) * 43758.5453);
}

vec2 masonry_offset(vec2 cell) {
    vec2 drawn = masonry_hash(cell);

    return vec2(floor(drawn.x * 5.0) / 5.0, floor(drawn.y * 7.0) * 2.0 / 15.0)
        * masonry_shuffle;
}

// The three cells around a point on the triangular grid, and how much each is owed.
//
// ⚠ A TRIANGLE GRID AND NOT A SQUARE ONE, because a square grid meets four cells at every corner
// and its seams run along two straight lines a person can see. Three-way blending has no such
// alignment: the seams are diagonals of two different slopes and never form a lattice.
void masonry_grid(vec2 uv, out vec2 first, out vec2 second, out vec2 third, out vec3 weight) {
    vec2 skewed = mat2(vec2(1.0, 0.0), vec2(-0.57735027, 1.15470054))
        * (uv * (3.4641016 / max(masonry_spread, 0.5)));
    vec2 base = floor(skewed);
    vec3 part = vec3(fract(skewed), 0.0);
    part.z = 1.0 - part.x - part.y;

    if (part.z > 0.0) {
        weight = vec3(part.z, part.y, part.x);
        first = base;
        second = base + vec2(0.0, 1.0);
        third = base + vec2(1.0, 0.0);
    } else {
        weight = vec3(-part.z, 1.0 - part.y, 1.0 - part.x);
        first = base + vec2(1.0, 1.0);
        second = base + vec2(1.0, 0.0);
        third = base + vec2(0.0, 1.0);
    }

    weight = pow(weight, vec3(masonry_bias));
    weight /= weight.x + weight.y + weight.z;
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

    // ---- the facade's composition -------------------------------------------------------------
    //
    // 🔴 EVERY WINDOW IN THE CITY WAS THE SAME WINDOW, AT THE SAME HEIGHT, ON THE SAME PITCH, and
    // the four expressions this replaces are where that came from. `Main.Massing` writes a height
    // of `storeys * StoreyMetres` exactly, so `lift` is 3.5 m on every wall WITHOUT EXCEPTION; at
    // 3.5 all three `min` calls saturate, and `paneH`, `paneW` and `sill` came out at a fixed 1.6,
    // 1.15 and 1.178 m citywide. Only `jamb` moved, and it moved with the FOOTPRINT alone — so
    // ***two Buildings on one footprint had one facade***, but for which panes the occupancy draw
    // shut and which bay took the door. `plans/0063` **F6**.
    //
    // 🔴 THE STOREY PITCH IS NOT WHAT MOVES HERE AND MUST NOT. `lift` is the city's own storey
    // count divided back out of the city's own height, and the exactness of that recovery is what
    // F6 is a statement about. What varies below is the OPENING drawn inside a storey and the BAY
    // it is drawn in, both of which are invented geometry under this file's opening rule.
    // ***A Building three storeys tall still has three rows of windows.***
    //
    // ⚠ THE FRONTAGE IS COMPUTED HERE RATHER THAN BELOW because the ground storey needs it: a
    // commercial premises already has `shop_floor` on its Street face and must not get a second,
    // different ground-floor treatment fighting it.
    vec2 street = (v_custom.rg * 2.0) - 1.0;
    float frontage = step(0.5, dot(n.xz, street)) * upright;

    // ⚠ THREE INDEPENDENT DRAWS OFF ONE SEED AND NOT ONE DRAW SPENT THREE TIMES. A wall whose bay,
    // pane and sill all move together is one dial wearing three names, and a street of them reads
    // as a slider rather than as a street.
    float seed = v_custom.a * 255.0;
    float bay_draw = draw(vec3(seed, 13.0, 29.0));
    float pane_draw = draw(vec3(seed, 47.0, 5.0));
    float sill_draw = draw(vec3(seed, 3.0, 61.0));

    // ⚠ THE DRAWN BAY IS A WANT AND NOT A WIDTH. The fit still has to divide the span, so what a
    // Building actually gets is the nearest whole number of bays to what it asked for — which is
    // how a real elevation works and is why two Buildings on one footprint can now differ.
    float want = mix(BAY, mix(BAY_LEAST, BAY_MOST, bay_draw), facade_variety);
    float bays = max(1.0, floor(span / want));
    float wide = span / bays;
    float bayAt = floor((u + (span * 0.5)) / wide);
    float withinX = mod(u + (span * 0.5), wide);

    // 🔴 A TALLER GROUND STOREY IS THE OPENING AND NOT THE FLOOR. Making the bottom storey
    // physically taller is the one thing here that would break the recovery above, so the pitch
    // stays and the WINDOW grows inside it — which is what the eye reads as a ground floor anyway.
    float ground = (1.0 - step(0.5, floorAt)) * (1.0 - v_shop * frontage);

    float plainH = min(1.6, lift * 0.52);
    float plainW = min(1.15, wide * 0.42);
    float plainSill = (lift - plainH) * 0.62;

    float upperH = clamp(lift * mix(0.44, 0.60, pane_draw), 1.15, lift - 1.00);
    float upperSill = (lift - upperH) * mix(0.44, 0.72, sill_draw);

    // ⚠ THE GROUND SILL CLEARS THE PLINTH AND THE PANE GROWS UPWARD FROM IT. A sill drawn as a
    // share of the leftover lands below `PLINTH` on a tall opening, and `above` then masks the
    // window off entirely — ***a ground floor that is drawn taller and shows nothing.***
    float groundSill = PLINTH + 0.22;
    float groundH = min(upperH + 0.42, lift - groundSill - 0.30);

    float paneH = mix(plainH, mix(upperH, groundH, ground), facade_variety);
    float paneW = mix(plainW, clamp(wide * mix(0.28, 0.42, pane_draw), 0.85, 2.30), facade_variety);
    float sill = mix(plainSill, mix(upperSill, groundSill, ground), facade_variety);
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

    // Condition survives palette and overlay changes.
    float ruin = v_ruin;
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
    float masonry = step(v_custom.a, 0.38) * upright * close_detail;

    // 🔴 THE PHOTOGRAPH, AND IT IS APPLIED AS A RATIO RATHER THAN AS A COLOUR. Dividing the sample
    // by the image's own mean leaves only what varies across it -- the courses, the mortar, the
    // weathering, the fired unevenness of one brick against the next -- so the wall keeps the value
    // and warmth `Rendered` drew for it and gains the grain it never had. ***Pasted flat instead,
    // every masonry Building in the city comes out the same red***, which is the terrace argument
    // in `Main.Massing.Rendered` thrown away by the renderer that is supposed to serve it.
    //
    // ⚠ THE MEAN IS A CONSTANT AND NOT A SECOND FETCH. It is this image's average linear value,
    // taken once off the file; a `textureLod` at the top of the chain would cost a sample a pixel
    // to recover a number that cannot change.
    //
    // ⚠ AND IT FADES ON `close_detail` LIKE EVERY OTHER MARK HERE. A 2K image on a wall four
    // pixels tall is noise however good the mip chain is, and the city is looked at from up there
    // most of the time.
    // ⚠ THE WALL IS OFFSET BY ITS OWN BUILDING BEFORE ANYTHING ELSE. `u` is measured from the box's
    // CENTRE, not from anywhere in the world, so without this every Building in the city starts its
    // brickwork at the same phase and lands on the same cell of the grid below -- and the shuffling
    // would then repeat perfectly from house to house while looking varied within each one.
    // ⚠ Snapped to two courses for the same reason the cell offsets are, and so that the bottom
    // course still meets the ground rather than starting half a brick up.
    //
    // 🔴 AND ALL FOUR FACES OF ONE BUILDING STILL STARTED THE PHOTOGRAPH AT ONE PHASE. The offset
    // above is the BUILDING's, so it separates a house from its neighbour and does nothing for the
    // CORNER where two walls of the same house meet — which is the one place two walls are seen at
    // once. ***A per-Building offset cannot break a per-Building repeat.***
    //
    // ⚠ MIRRORING IS FREE AND THE BOND SURVIVES IT. A running bond is symmetric about a vertical
    // line, so the mirrored image is a wall that could have been built: the courses stay level, the
    // perpends stay plumb, and what changes is which bricks are burnt — which is the thing F5 says
    // the eye actually locks onto. Two of the four faces take it, chosen off the outward normal so
    // it is a property of the wall rather than a draw.
    float mirror = step(dot(n.xz, vec2(1.0, 1.0)), 0.0);
    float across = mix(u, -u, mirror);
    vec2 wall_uv = (vec2(across, -up) / masonry_metres)
        + vec2(floor(v_custom.a * 37.0) / 5.0, floor(v_custom.a * 53.0) * 2.0 / 15.0);

    // 🔴 THE DERIVATIVES ARE TAKEN ONCE AND HANDED TO EVERY TAP. An offset that jumps between cells
    // makes the automatic mip derivative jump with it, so `texture` would pick a wildly wrong level
    // along every seam -- ***a blurred or aliased line exactly where the join is supposed to be
    // invisible.*** The unshuffled coordinate has the derivative the surface actually has.
    vec2 stride_x = dFdx(wall_uv);
    vec2 stride_y = dFdy(wall_uv);

    vec2 cell_a;
    vec2 cell_b;
    vec2 cell_c;
    vec3 share;
    masonry_grid(wall_uv, cell_a, cell_b, cell_c, share);

    vec2 draw_a = masonry_offset(cell_a);
    vec2 draw_b = masonry_offset(cell_b);
    vec2 draw_c = masonry_offset(cell_c);

    vec3 sampled =
        (textureGrad(masonry_albedo, wall_uv + draw_a, stride_x, stride_y).rgb * share.x)
        + (textureGrad(masonry_albedo, wall_uv + draw_b, stride_x, stride_y).rgb * share.y)
        + (textureGrad(masonry_albedo, wall_uv + draw_c, stride_x, stride_y).rgb * share.z);

    vec3 tinted = sampled / MASONRY_MEAN;

    // ⚠ THE LUMINANCE RATIO USES THE SAME WEIGHTS ON BOTH SIDES, so a neutral pixel comes out at
    // exactly 1 and a wall with `masonry_hue` at zero is the paint untouched but for the courses.
    const vec3 EYE = vec3(0.299, 0.587, 0.114);
    float lit = dot(sampled, EYE) / dot(MASONRY_MEAN, EYE);
    vec3 grain_rgb = mix(vec3(lit), tinted, masonry_hue);
    body *= mix(vec3(1.0), grain_rgb, masonry * far * 0.85);

    float rendered = (1.0 - step(v_custom.a, 0.38)) * upright;
    vec2 plaster_uv = vec2(u, -up) / plaster_metres + vec2(v_custom.a * 11.0, v_custom.a * 7.0);
    vec3 plaster_sample = texture(plaster_albedo, plaster_uv).rgb;
    // Luminance modulation retains the paint; a plaster photograph does not choose the palette.
    float plaster_grain = dot(plaster_sample, EYE) / max(dot(plaster_mean, EYE), 0.001);
    body *= mix(1.0, clamp(plaster_grain, 0.25, 1.9), rendered * plaster_mix * far * 0.85);

    vec2 wear_uv = vec2(u, -up) / 5.0 + vec2(v_custom.a * 17.0, v_custom.a * 23.0);
    float wear = texture(wear_height, wear_uv).r;
    float broken = (1.0 - smoothstep(0.75, 0.81, wear)) * ruin * wear_strength * upright;
    // The height map supplies missing plaster, while the exposed brick stays our existing brick.
    vec3 exposed_brick = wall * grain_rgb * vec3(0.78, 0.67, 0.56);
    body = mix(body, exposed_brick, broken * rendered * far);
    float damp = (1.0 - smoothstep(0.0, 2.4, up)) * (0.35 + 0.65 * wear);
    float streaks = 1.0 - texture(wear_height, vec2(wear_uv.x, wear_uv.y * 0.17)).r;
    float under_sill = smoothstep(sill - 1.2, sill, withinY) * (1.0 - step(sill, withinY));
    float runoff = streaks * under_sill * 2.0;
    float grime = ruin * wear_strength * upright * far
        * clamp(0.30 + broken * 0.50 + damp * 0.35 + runoff * 0.18, 0.0, 0.90);
    body = mix(body, body * vec3(0.25, 0.29, 0.22), grime);

    // 🔴 THE RELIEF, AND THE TANGENT FRAME IS BUILT HERE RATHER THAN TAKEN FROM THE MESH. Godot's
    // `NORMAL_MAP` uses the mesh's own TANGENT/BINORMAL, and this mesh is a UNIT BoxMesh scaled
    // per instance to the Building's footprint and height -- so the frame arrives skewed by a
    // non-uniform scale, and the UV sampled above is not the box's UV in any case. ***A tangent
    // frame that disagrees with the UV it lights lights the brick from the wrong side.***
    //
    // ⚠ IT IS EXACT BECAUSE A WALL IS VERTICAL. The tangent is the horizontal direction lying in
    // the wall, which is the cross product of world up with the face normal, and no varying or
    // matrix is needed to get it. A face that is not upright takes no masonry, so the degenerate
    // case is masked out rather than guarded against.
    vec3 shaded = normalize(NORMAL);
    vec3 up_view = normalize((VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);
    vec3 tangent = cross(up_view, shaded);
    vec3 along = length(tangent) > 0.0001 ? normalize(tangent) : vec3(1.0, 0.0, 0.0);
    vec3 down = cross(shaded, along);
    // ⚠ ONE TAP FOR THE RELIEF AND THREE FOR THE COLOUR, WHICH IS NOT AN OVERSIGHT. The relief this
    // image carries is almost entirely the MORTAR RECESS, and the offsets are bonded — so every draw
    // puts its mortar in the same place and the dominant one is indistinguishable from a blend of
    // three. ***What repeats visibly is which bricks are burnt, and that is colour.*** Blending the
    // normal as well would double the fetch count to serve a difference nobody can see.
    vec2 relief_draw = share.x > share.y
        ? (share.x > share.z ? draw_a : draw_c)
        : (share.y > share.z ? draw_b : draw_c);
    vec3 relief =
        (textureGrad(masonry_normal, wall_uv + relief_draw, stride_x, stride_y).xyz * 2.0) - 1.0;

    // 🔴 THE RELIEF'S X FLIPS WITH THE UV OR THE BRICK IS LIT FROM THE WRONG SIDE. This file's own
    // tangent-frame remark is about exactly this failure, and a mirrored coordinate paired with an
    // unmirrored normal IS that disagreement — the mortar recess would come out proud on half the
    // walls in the city, and only on the two faces the mirror took.
    relief.x *= mix(1.0, -1.0, mirror);
    NORMAL = normalize(mix(
        shaded,
        normalize((along * relief.x) + (down * relief.y) + (shaded * relief.z)),
        masonry * far * masonry_relief * (1.0 - opening) * (1.0 - doorcase)
            * (1.0 - v_shop * frontage * step(up, lift))));
    vec3 render_relief = texture(plaster_normal, plaster_uv).xyz * 2.0 - 1.0;
    vec3 damage_relief = texture(wear_normal, wear_uv).xyz * 2.0 - 1.0;
    render_relief = normalize(mix(render_relief, damage_relief, broken * (1.0 - broken) * 4.0));
    render_relief = normalize(mix(render_relief, relief, broken));
    float solid_wall = upright * (1.0 - opening) * (1.0 - doorcase)
        * (1.0 - v_shop * frontage * step(up, lift));
    NORMAL = normalize(mix(NORMAL,
        normalize(along * render_relief.x + down * render_relief.y + shaded * render_relief.z),
        rendered * plaster_mix * solid_wall * close_detail * far));
    // 🔴 THE CORNICE WAS ALREADY HERE AND IT WAS A TONE. A pale band on a pale wall is the reveal's
    // failure repeated at the roofline — it vanishes on a Building in shadow, which is the half of
    // the city the sun is not on at any hour. Under `facade_variety` it deepens and gains its own
    // shadow beneath, so the top of an elevation becomes a light–dark PAIR and reads at a distance
    // and in a dim. ⚠ **It does NOT fade on `close_detail`**: half a metre of parapet is the shape
    // of the roofline, and the roofline is what a city is read by from up there.
    float capTop = mix(v_size.y - 0.32, v_size.y - 0.46, facade_variety);
    float capShade = capTop - 0.16;
    float cornice = smoothstep(capTop - grain.y, capTop + grain.y, up) * upright;
    float corniceShade = smoothstep(capShade - grain.y, capShade + grain.y, up)
        * (1.0 - smoothstep(capTop - grain.y, capTop + grain.y, up))
        * upright * far * facade_variety;
    body = mix(body, wall * mix(1.22, 1.32, facade_variety), cornice * far);
    body = mix(body, wall * 0.66, corniceShade);
    // ---- the room behind the pane -----------------------------------------------------------
    //
    // 🔴 INTERIOR MAPPING, AND THE FRAME IT NEEDS IS THE ONE THE RELIEF ALREADY BUILT. `along`,
    // `down` and `shaded` are an orthonormal view-space basis for this wall — and ⚠ **`down` is
    // world UP despite its name**: it is `cross(shaded, cross(up_view, shaded))`, which reduces to
    // `up_view` on a vertical wall. So the incoming view direction resolves into the wall's own
    // (across, up, outward) axes with three dot products and no matrix.
    //
    // The window's aperture IS the room's front face, so the box is `paneW` × `paneH` × depth and
    // the entry point is where the fragment sits inside the pane. A slab test gives the far
    // intersection; whichever slab it lands on is the wall, floor, ceiling or back that is being
    // seen. ⚠ **`r.z` is negative on a front face** — the ray goes in against the outward normal —
    // so it is flipped once, and `d.z` is then positive by construction and the z slab cannot
    // divide by zero. The other two are nudged off zero for the head-on case.
    //
    // ⚠ EVERY WINDOW DRAWS ITS OWN ROOM, keyed on the same cell the shutter draw uses, so the grid
    // stops being one mark repeated. ⚠ AND IT IS BOUNDED BY `close_detail` AND `far` LIKE EVERY
    // OTHER MARK HERE: ***a room nobody can resolve is a trace nobody sees.***
    //
    // ⚠ A SHUT PANE AND A RUIN SHOW NO ROOM, for the reason they take no highlight below — boards
    // have nothing behind them and a shell has nothing in it.
    //
    // ⚠ IT IS BRANCHED, AND THE BRANCH IS SAFE BECAUSE NOTHING INSIDE TAKES A DERIVATIVE. There is
    // no texture fetch and no `dFdx` in the trace — `grain` was taken above, outside — so the whole
    // block may be skipped without the undefined mip level a fetch in divergent control flow gets.
    // ⚠ **WHAT IT SAVES IS A WHOLE WARP AND NOT A FRAGMENT**: the wall a kilometre off, the
    // shuttered Building, the shell with no glass in it. A pane at the edge of the fade shares its
    // warp with wall around it and pays anyway. ***A branch on a smoothly-varying mask buys the
    // extremes and not the middle.*** ⚠ **No timing figure is claimed and none was taken**
    // (`adr/0106`); this is a statement about what is skipped, not about what it costs.
    float room_on = step(0.01, interior_depth) * close_detail * far * (1.0 - shut) * (1.0 - ruin);
    if (room_on > 0.0) {
        vec3 dir = normalize(-VIEW);
        vec3 r = vec3(dot(dir, along), dot(dir, down), dot(dir, shaded));
        vec3 d = vec3(r.x, r.y, -r.z);
        d.xy += (1.0 - step(0.0001, abs(d.xy))) * 0.0001;
        vec3 sz = vec3(max(paneW, 0.05), max(paneH, 0.05), interior_depth);
        vec3 at = vec3(clamp(withinX - jamb, 0.0, sz.x), clamp(withinY - sill, 0.0, sz.y), 0.0);
        vec3 far_t = max((vec3(0.0) - at) / d, (sz - at) / d);
        float hit_t = min(min(far_t.x, far_t.y), far_t.z);

        // Which slab was hit. The back is the common case; a side, floor or ceiling only appears at an
        // angle, which is exactly what makes the effect read as depth rather than as a picture.
        float on_back = step(far_t.z, hit_t + 0.0001);
        float on_side = step(far_t.x, hit_t + 0.0001) * (1.0 - on_back);
        float on_cap = clamp(1.0 - on_back - on_side, 0.0, 1.0);
        float looking_up = step(0.0, d.y);

        // The room's own draw — a dwelling has warm lamps and bare ceilings, and no two rooms on a wall
        // are the same room. ⚠ THE DRAW IS THE CELL'S, not the fragment's, so a room is one room.
        float room_draw = draw(vec3(floorAt, bayAt, dot(n, vec3(5.0, 11.0, 2.0)) + v_custom.a * 47.0));
        vec3 room_wall = mix(vec3(0.088, 0.079, 0.066), vec3(0.185, 0.163, 0.131), room_draw);

        // 🔴 THE FOUR FACES NEED CONTRAST BETWEEN THEM OR THE BOX IS A DARK PATCH. Measured on the
        // first version: every face within a factor of two of every other, and the whole effect read
        // as ***the windows went darker*** rather than as depth. What makes a real room legible
        // through a window is that its CEILING catches light and its floor does not, so the spread
        // between the two is the effect and the average of them is not.
        vec3 back = room_wall;
        vec3 side = room_wall * 0.46;
        vec3 cap = mix(room_wall * 0.30, room_wall * 2.10, looking_up);
        vec3 room = (back * on_back) + (side * on_side) + (cap * on_cap);

        // Depth darkens what is furthest in, which is what sells the box as a volume.
        room *= mix(1.0, 0.58, clamp(hit_t / max(interior_depth, 0.05), 0.0, 1.0));
        // ⚠ INTO `glass` AND NOT INTO `painted`, so the room is masked by `glazed` exactly as the flat
        // pane was — the shopfront's `display_glass` is a separate surface and keeps its own treatment.
        glass = mix(glass, room, room_on);
    }

    float barX = 1.0 - smoothstep(0.045 - grain.x, 0.045 + grain.x, abs(withinX - wide * 0.5));
    float barY = 1.0 - smoothstep(0.035 - grain.y, 0.035 + grain.y, abs(withinY - sill - paneH * 0.55));
    float frame = max(barX, barY) * close_detail;
    glass = mix(glass, wall * 1.15, frame);
    vec3 painted = mix(body, glass, glazed);
    painted = mix(painted, (wall * 1.32) + 0.02, clamp(reveal * far, 0.0, 1.0) * (1.0 - cased));
    painted = mix(painted, (wall * 1.45) + 0.03, cased);
    painted = mix(painted, mix(wall * 0.30, vec3(0.05, 0.055, 0.06), 0.4), doored);

    // ---- the horizontal marks -------------------------------------------------------------
    //
    // 🔴 A WALL'S VERTICAL RHYTHM WAS ONE PITCH AND NOTHING ELSE, so a Building six storeys tall
    // read as one row of windows stacked six times — which is **F6** showing up in the direction
    // the bay above cannot reach. A cornice gives the elevation a TOP and a string course gives it
    // a COUNT, and neither is a window.
    //
    // ⚠ BOTH ARE PAIRS RATHER THAN TONES, for the reason the reveal is: a mark drawn only as a
    // shade vanishes on a wall that is already dark, and ***what survives a dim is an edge.*** The
    // cornice is a pale band with its own shadow under it; the course is a shadow with a light
    // above it.
    //
    // ⚠ THE COURSE FADES ON `close_detail` AND THE CORNICE ABOVE DOES NOT. A ten-centimetre line is
    // noise from a kilometre up; half a metre of parapet is still the roofline. ***They are marks
    // at two different scales and they must not share a fade.***
    //
    // ⚠ NOT ON THE GROUND FLOOR'S OWN LINE, which is the plinth's, and never over glazing.
    float course = (1.0 - smoothstep(0.09 - grain.y, 0.09 + grain.y, withinY))
        * step(0.5, floorAt) * upright * close_detail * facade_variety * (1.0 - glazed);
    float courseLip = smoothstep(0.09 - grain.y, 0.09 + grain.y, withinY)
        * (1.0 - smoothstep(0.15 - grain.y, 0.15 + grain.y, withinY))
        * step(0.5, floorAt) * upright * close_detail * facade_variety * (1.0 - glazed);

    painted = mix(painted, wall * 0.74, course);
    painted = mix(painted, (wall * 1.16) + 0.01, courseLip);

    // Commercial ground-floor glazing stays on the Street face and below the first floor.
    float shop_floor = v_shop * frontage * (1.0 - smoothstep(lift - 0.15, lift, up)) * far;
    float shop_x = smoothstep(0.22 - grain.x, 0.22 + grain.x, withinX)
        * (1.0 - smoothstep(wide - 0.22 - grain.x, wide - 0.22 + grain.x, withinX));
    float shop_y = smoothstep(0.28 - grain.y, 0.28 + grain.y, up)
        * (1.0 - smoothstep(lift - 0.85 - grain.y, lift - 0.85 + grain.y, up));
    float display = shop_x * shop_y * shop_floor;
    float fascia = smoothstep(lift - 0.78 - grain.y, lift - 0.78 + grain.y, up)
        * (1.0 - smoothstep(lift - 0.22 - grain.y, lift - 0.22 + grain.y, up)) * shop_floor;
    vec3 joinery = mix(vec3(0.045, 0.075, 0.065), wall * 0.36, v_custom.a);
    float shutter_line = 1.0 - smoothstep(0.012, 0.012 + grain.y, abs(mod(up, 0.12) - 0.06));
    vec3 display_glass = mix(pane, wall * (0.58 - 0.09 * shutter_line * close_detail), shut);
    display_glass = mix(display_glass, vec3(0.010, 0.011, 0.013), ruin);
    painted = mix(painted, joinery, shop_floor);
    painted = mix(painted, display_glass, display);
    painted = mix(painted, joinery * 1.7, fascia);
    // Keep the entrance distinguishable from display windows, without invented shop text.
    painted = mix(painted, wall * 0.22, doored * shop_floor);
    glazed = mix(glazed, display, shop_floor);
    frame *= 1.0 - shop_floor;
    float trim_wear = ruin * wear_strength * far * (0.22 + 0.45 * broken);
    float trim = clamp(cased + reveal * far + fascia + frame * glazed, 0.0, 1.0);
    painted = mix(painted, painted * vec3(0.38, 0.37, 0.31), trim_wear * trim);
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
    float wall_roughness = mix(0.9, texture(plaster_roughness, plaster_uv).r,
        rendered * plaster_mix * far * (1.0 - ruin));
    ROUGHNESS = mix(wall_roughness, mix(0.06, 0.85, max(shut, ruin)), glazed);
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
