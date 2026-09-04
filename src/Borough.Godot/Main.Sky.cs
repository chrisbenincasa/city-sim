using System;
using System.Globalization;
using System.IO;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

// ---- the sky -- the sun, the hour it says it is, and the weather line under the readout ------
//
// Sunward() and Clock() are static and take the Tick, so the light is a function of the city's
// own time and never of the wall clock the frame is running on.
//
// Weather() is here rather than with the readout because what it reports is the sky.
//
// Moved out of Main.cs on 2026-09-04, plans/0045 queue row 26. One class across nine files,
// no behaviour changed and no State Hash moved.

public partial class Main
{
    /// <summary>What the weather is doing, or nothing at all when it is doing nothing.</summary>
    /// <remarks>
    /// ⚠ <b>Silent on a world with no <c>[disasters]</c></b>, which is every shipped Ruleset but
    /// <c>flooded.toml</c> — a readout that said <c>floods 0</c> for ever would be a permanent line
    /// about an absent mechanism.
    /// </remarks>
    private string Weather(int under)
    {
        if (!_world.Rules.Disasters.Stated)
        {
            return string.Empty;
        }

        int live = _world.Disasters.Rows.LiveCount;
        int ruined = 0;
        int swept = 0;

        for (int slot = 0; slot < _world.Disasters.Rows.SlotCount; slot++)
        {
            if (_world.Disasters.Rows.IsLive(slot))
            {
                ruined += _world.Disasters.Ruined[slot];
                swept += _world.Disasters.Swept[slot];
            }
        }

        return live == 0
            ? "   no flood"
            : $"   FLOOD — {under:N0} Cells under water, {ruined:N0} ruined, {swept:N0} swept";
    }

    /// <summary>A light, so the boxes have faces rather than silhouettes.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE SUN CAST NOTHING UNTIL 2026-09-01, AND A CITY WITHOUT SHADOWS HAS NO GROUND.</b>
    /// Every Building met the grass at a hard seam, so a box read as a sprite pasted over the map
    /// rather than as a thing standing on it — and a roof's pitch, which is entirely a matter of
    /// two planes catching the light differently, was being read off a colour change alone.
    /// ***The lighting was correct and there was nothing under it***, which in a screenshot looks
    /// exactly like flat art.
    /// </para>
    /// <para>
    /// ⚠ <b>The split distances are set against the CITY and not against the map.</b> A directional
    /// shadow fits its cascades into <c>DirectionalShadowMaxDistance</c>, and the default 100 m
    /// puts every cascade inside one block — so at the standoff this game is played at the shadows
    /// would simply stop a few metres from the eye. The map is 65.5 km and framing the whole of it
    /// would spend every cascade on empty ground, so the distance is the neighbourhood a person
    /// actually looks at.
    /// </para>
    /// <para>
    /// ⚠ <b>The sky is a gradient rather than a colour, and it is the ambient source.</b> A flat
    /// background makes the map's edge read as the end of the world; a horizon puts the edge
    /// somewhere. Taking ambient from the same sky is what makes a north wall blue and a roof
    /// bright, which is the second thing a pitch is read by.
    /// </para>
    /// </remarks>
    private void Sun()
    {
        _light = new DirectionalLight3D
        {
            LightEnergy = 1.25f,
            LightColor = new Color(1.0f, 0.96f, 0.88f),
            ShadowEnabled = true,
            DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits,
            // ⚠ THE CASCADES ARE FITTED ROUND THE CAMERA AND A LONG RANGE IS NOT A FREE ONE. The
            // engine's default 100 m puts every split inside one block; the map is 65.5 km and
            // framing it would spend the whole atlas on empty ground and give the city texels
            // metres wide. This is the neighbourhood a person looks at, and past the fade a
            // distant city is unshadowed rather than wrong.
            //
            // ⚠ THE VALUE HERE IS ONLY THE OPENING ONE. Orbit() overwrites it on every camera
            // move, because how much range is worth spending depends on how far away the eye is
            // standing -- see the comment there, which carries the measurement.
            DirectionalShadowMaxDistance = 1_600f,
            DirectionalShadowFadeStart = 0.85f,
            DirectionalShadowSplit1 = 0.06f,
            DirectionalShadowSplit2 = 0.16f,
            DirectionalShadowSplit3 = 0.44f,

            // ⚠ THE OPENING VALUE ONLY. Daylight() sets this every frame from how high the sun is
            // standing, because the artifact it exists to hide is a function of that and of
            // nothing else -- see the comment there, which carries the measurement and the three
            // things that were tried instead and did not work.
            //
            // ⚠ And the faint concentric MOIRÉ over open ground is a different thing and is real:
            // it is one quantisation level, 1/255, and ShadowBias 1.0 erases it. Not taken,
            // because a bias that large detaches a shadow from its own wall to cure something
            // nobody has reported seeing.
            ShadowBlur = 1.1f,
        };

        // ⚠ NOT NOON, AND SINCE plans/0051 ROW 2 NOT A CONSTANT EITHER. The angle set here is the
        // one the first frame is drawn at before Daylight has run; every frame after it comes from
        // the clock. A sun overhead lights every roof identically and leaves no wall in shadow,
        // which is the one lighting angle at which a pitched roof and a flat one look the same --
        // so the arc below is capped well short of the zenith rather than being astronomically
        // right.
        _light.RotateX(-0.78f);
        _light.RotateY(-0.62f);
        _light.SetParam(Light3D.Param.ShadowNormalBias, 1.6f);
        _light.SetParam(Light3D.Param.ShadowBias, 0.06f);
        AddChild(_light);

        _above = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.28f, 0.42f, 0.66f),
            SkyHorizonColor = new Color(0.63f, 0.70f, 0.78f),
            // ⚠ THE LOWER HEMISPHERE IS WHAT THE FRAME IS FULL OF, and that is the camera rather
            // than a mistake: the eye is tipped 35° down and never moves, so the horizon sits
            // above the top of the picture at every zoom the city is looked at from. What stands
            // behind the map's edge is therefore always this, and a dark ground colour reads as
            // the world having a hole cut round it. Haze instead.
            GroundBottomColor = new Color(0.50f, 0.56f, 0.62f),
            GroundHorizonColor = new Color(0.58f, 0.65f, 0.71f),
            SunAngleMax = 12f,
            SunCurve = 0.2f,
        };

        _air = new Godot.Environment
        {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = _above },
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
                AmbientLightSkyContribution = 1f,
                AmbientLightEnergy = 1.05f,

                // ⚠ AN INSTRUMENT AS MUCH AS A LOOK. Occlusion is what puts a Building's own
                // shadow in the corner where it meets the ground and in the gap between two of
                // them, so a terrace reads as a terrace and a detached pair reads as two houses.
                // The radius is a metre and a half, which is the gap the fill factor leaves.
                SsaoEnabled = true,
                SsaoRadius = 2.2f,
                SsaoIntensity = 1.6f,
                SsaoPower = 1.8f,

                // The sun off a wall at this hour is bright enough to clip, and a clipped wall
                // loses exactly the shading the openings are drawn against.
                TonemapMode = Godot.Environment.ToneMapper.Filmic,
                TonemapWhite = 2.2f,
        };

        AddChild(new WorldEnvironment { Environment = _air });
        Daylight();
    }

    /// <summary>How high the sun climbs at noon, in radians — <b>54°, and short of the zenith.</b></summary>
    /// <remarks>
    /// ⚠ <b>It is a look and not a latitude.</b> A sun overhead lights every roof identically and
    /// leaves no wall in shadow, which is the one angle at which a pitched roof and a flat one are
    /// indistinguishable — and the pitch is what <c>plans/0049</c> spent a session varying. So the
    /// arc is capped where walls still catch light, and nowhere in this file is a claim about where
    /// on Earth the city is.
    /// </remarks>
    private const float NoonRadians = 0.95f;

    /// <summary>The clock fraction the lamps' night opens at, and how much of a Day it spans.</summary>
    /// <remarks>
    /// <para>
    /// <b>This sun rises at 06:00 and sets at 18:00</b> — <c>height</c> is
    /// <c>sin(NoonRadians)·cos(t)</c> and <c>t</c> is <c>(clock − ½)·τ</c>, so it crosses zero at
    /// <c>clock</c> 0.25 and 0.75 and there is no seasonal term. The window below is that night
    /// with a margin at each end: it opens at <b>0.70</b> and closes at <b>0.30</b> — the hour or
    /// so either side that a person would call dusk and dawn.
    /// </para>
    /// <para>
    /// 🔴 <b>THE MARGIN IS WHAT STOPS THE WRAP FROM BEING SEEN.</b> The phase steps from 1 back to
    /// 0 at the instant the window opens, and every lamp is off on both sides of that step — but
    /// only because <c>night</c> has ALSO reached zero by then. At <c>clock</c> 0.70 the sun stands
    /// at height <b>0.25</b>, above the 0.20 where <c>night</c>'s own ramp bottoms out, so the
    /// discontinuity is multiplied by nothing. ***Narrowing this window would put the wrap inside
    /// the lit hours and the whole city would blink once a Day.***
    /// </para>
    /// </remarks>
    private const float NightOpensAt = 0.70f;

    /// <inheritdoc cref="NightOpensAt"/>
    private const float NightSpans = 0.60f;

    /// <summary>Where the sun stands at this Tick, as a unit vector from the city towards it.</summary>
    /// <remarks>
    /// <para>
    /// <b>The whole of the model.</b> <c>t</c> is the hour angle, zero at noon, and the sun rides a
    /// circle through due east and due west tilted so that its high point is due south at
    /// <see cref="NoonRadians"/>. At <c>t = ±π/2</c> it is on the horizon at the two ends of that
    /// circle; at <c>t = π</c> it is as far below the horizon as it was above it. ⚠ <b>The vector
    /// is a unit vector by construction</b> — <c>sin²t + cos²t(sin²m + cos²m)</c> — so nothing here
    /// needs normalising and a rounding error cannot make the day longer.
    /// </para>
    /// <para>
    /// ⚠ <b>+Z is SOUTH in this shell and not north.</b> Every position is built as
    /// <c>(east, height, −north)</c>, so the sun's noon component is <c>+Z</c> and getting that sign
    /// wrong puts the light behind the camera all day, which reads as an overcast world rather than
    /// as a bug.
    /// </para>
    /// <para>
    /// <b>Tick 0 is 05:00</b> (<c>Ticks.DayBeginsAtHour</c>, <c>adr/0101</c> amended), so a run opened
    /// with no <c>--start-at</c> opens on a dawn. That is the clock the city keeps and not something
    /// to correct here; the offset is applied below rather than assumed.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>IT TAKES THE SUB-TICK FRACTION, AND THE REASON IS THAT A DAY IS ONLY 2,048 SAMPLES
    /// LONG.</b> One Tick is <b>0.176°</b> of the sun's circle. At 4× the shell steps 32 Ticks a
    /// second and the sweep reads as motion; at <b>1× it steps 8</b>, so a 60 fps frame holds the sun
    /// still for seven frames and then jumps it — and a dawn shadow a hundred metres long snaps
    /// visibly on every one of those jumps. ***The Tick is the simulation's resolution and it is not
    /// the picture's***, which is <see cref="_alpha"/>'s whole argument arriving at the one consumer
    /// that had not asked for it. The city is unmoved: nothing here is read by
    /// <c>Borough.Core</c> and the sun is not state.
    /// </para>
    /// <para>
    /// ⚠ <b>Computed as a fraction of the Day rather than through <c>Ticks.MinuteOfDay</c>.</b> That
    /// helper floors to the whole minute, which at 0.70 minutes a Tick is a <em>second</em>
    /// quantisation laid over the one being removed. The wall-clock offset it carries is applied here
    /// from the same constant, so there is still one place that decides when a Day begins.
    /// </para>
    /// </remarks>
    /// <param name="tick">The Tick the world is on.</param>
    /// <param name="within">How far into that Tick the frame is, in <c>[0, 1)</c>.</param>
    private static Vector3 Sunward(ulong tick, float within)
    {
        float t = (Clock(tick, within) - 0.5f) * Mathf.Tau;

        return new Vector3(
            -Mathf.Sin(t),
            Mathf.Sin(NoonRadians) * Mathf.Cos(t),
            Mathf.Cos(NoonRadians) * Mathf.Cos(t));
    }

    /// <summary>Where in its Day the world is, as a fraction with midnight at zero.</summary>
    /// <remarks>
    /// <para>
    /// Pulled out of <see cref="Sunward"/> so that the sun's angle and the hour the lamps keep are
    /// the same reading of the same clock. <b>Tick 0 is 05:00</b> (<c>Ticks.DayBeginsAtHour</c>),
    /// and applying that offset here is what keeps one place deciding when a Day begins.
    /// </para>
    /// <para>
    /// ⚠ <b>It is not wrapped into <c>[0, 1)</c>.</b> The offset can carry it past 1 and the two
    /// callers both want it that way — the sun takes a cosine, which does not care, and
    /// <see cref="Daylight"/> takes a <c>%</c> of its own against a different origin.
    /// </para>
    /// </remarks>
    /// <param name="tick">The Tick the world is on.</param>
    /// <param name="within">How far into that Tick the frame is, in <c>[0, 1)</c>.</param>
    private static float Clock(ulong tick, float within)
    {
        float ofDay = ((tick % (ulong)Ticks.PerDay) + within) / Ticks.PerDay;

        return ofDay + (Ticks.DayBeginsAtHour / (float)Ticks.HoursPerDay);
    }

    /// <summary>Points the sun, colours the sky and sets the ambient — all from the clock.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE LIGHT BECOMES THE MOON BELOW THE HORIZON, AND IT IS THE SAME LIGHT.</b> One
    /// <see cref="DirectionalLight3D"/> is aimed from the sun while the sun is up and from the sun's
    /// antipode while it is down — which is a full moon every single night, and is a fib. It is told
    /// here rather than hidden because the alternative is a city nobody can see for half of a
    /// <b>two-minute</b> Day, and because ***a second light would have to be dimmed to nothing at
    /// dawn anyway***, which is this one line spent twice.
    /// </para>
    /// <para>
    /// ⚠ <b>Both ramps start at exactly zero height, which is what stops the swap from popping.</b>
    /// The sun's energy reaches zero as it touches the horizon and the moon's leaves zero from the
    /// same instant, so the 180° flip happens while the light contributes nothing. An earlier
    /// spelling ramped the sun in from below the horizon and the swap was a visible lurch at every
    /// dawn and every dusk. ***What carries the twilight is the AMBIENT and the sky, not the
    /// directional light*** — which is also what happens outdoors.
    /// </para>
    /// <para>
    /// ⚠ <b>The dusk tint is keyed on <c>|height|</c> and therefore paints dawn as well.</b> They are
    /// the same geometry and the shell has no reason to tell them apart.
    /// </para>
    /// </remarks>
    private void Daylight()
    {
        float within = _alpha.Raw / 65_536f;
        Vector3 sunward = Sunward(_world.Tick.Raw, within);
        float height = sunward.Y;
        float up = Mathf.SmoothStep(0f, 0.12f, height);
        float down = Mathf.SmoothStep(0f, 0.10f, -height);

        _light.LookAtFromPosition(height > 0f ? sunward : -sunward, Vector3.Zero, Vector3.Up);

        // 🔴 THE SOFTNESS RIDES THE SUN'S HEIGHT, AND A CONSTANT ONE CANNOT BE RIGHT AT ANY HOUR.
        // A shadow edge is drawn one screen pixel at a time, so a SHALLOW edge holds its row for
        // several pixels before it steps down and the eye reads a comb. How shallow it lies is set
        // by how low the sun is: measured at a 40 m standoff, the tread is four to seven pixels at
        // 08:00 and the comb is invisible by midday. ***That is why the artifact was reported as
        // going away later in the day.***
        //
        // ⚠ A staircase is hidden when the SOFTNESS EXCEEDS THE TREAD, and nothing else hides it.
        // Measured against the same frame, and this is the finding rather than a preference: FXAA
        // moves nothing, PCF filter quality Ultra moves nothing, and 2× supersampling moves
        // nothing -- all three leave the same two-pixel riser, because none of them changes the
        // edge's SLOPE and the slope is what sets the tread. ⚠ Blur is the only lever that
        // reaches it, which is worth stating plainly because blur was tried and rejected once: a
        // FIXED blur is not the fix, and it was measured with the range fixed too, so its screen
        // width swung wildly with the zoom.
        //
        // So it is spent where it is needed and nowhere else. ⚠ THE LAW IS SQUARED AND A LINEAR
        // ONE WAS TRIED FIRST AND MEASURED SHORT: 0.9/height reaches only 2.2 at 08:00, where the
        // sun stands at 0.41, and 2.2 leaves the same two-pixel riser the unblurred edge has. The
        // sun's own height is capped at sin(0.95) = 0.81 here, so the whole usable range is narrow
        // and a gentle curve cannot cross it. Squared, it sits at the 3.5 ceiling through the
        // morning and the evening and eases to 1.1 only around midday, where the edge is steep
        // enough that blur would buy nothing and cost sharpness. The floor on the divisor stops it
        // running away as the sun sets.
        // 🔴 THE FLOOR AND THE CEILING BOTH BIT AT SUNRISE, SO THE LAW WAS FLAT ACROSS THE ONLY
        // HOURS IT EXISTS FOR. Worked at the shipped constants: at 06:45 the sun stands at
        // sin(0.95) x cos(-1.372) = 0.161, the 0.30 floor replaces it, and 0.73 / 0.09 = 8.11 is
        // then clamped to 3.5. ***The ceiling is reached at height 0.457, which is about 09:00***
        // -- so from first light to mid-morning the blur was a CONSTANT 3.5 however raking the sun
        // got, and the staircase it is spent on is worst exactly there. ⚠ The law was correct and
        // it was never reached: two clamps had turned a curve into a plateau over its own domain.
        //
        // The floor comes down to 0.12, which is a sun about twenty minutes up, and the ceiling
        // goes to 6.0. ⚠ A BLUR THIS WIDE IS NOT FREE -- it detaches a shadow from the wall that
        // casts it -- which is why it is spent only where the tread is widest and eases back to
        // 1.1 by midday exactly as before.
        float raking = Mathf.Max(Mathf.Abs(height), 0.12f);

        _light.ShadowBlur = Mathf.Clamp(0.73f / (raking * raking), 1.1f, 6.0f);

        // 🔴 THE SECOND LEVER, AND project.godot NAMED IT WITHOUT ANYONE PULLING IT: the range.
        // Four cascades share one 8192 atlas over DirectionalShadowMaxDistance, so a texel is the
        // range divided by 4096 -- and a texel's footprint on the GROUND is that divided by the
        // sine of the sun's height. At a raking sun the same texel smears six times as far, which
        // is the tread the blur is being asked to hide.
        //
        // ⚠ SO THE RANGE IS TIGHTENED WHERE THE SUN IS LOW rather than the blur alone being
        // widened. Spending the atlas on ground the eye is not on buys nothing; past the fade a
        // distant city is unshadowed rather than wrong, which is the bargain Sun() already struck.
        // ⚠ Orbit() no longer sets this -- it is a function of the standoff AND the hour, and two
        // writers would race whichever ran last.
        _light.DirectionalShadowMaxDistance = Mathf.Clamp(
            _distance * Mathf.Lerp(1.7f, 3f, Mathf.SmoothStep(0.08f, 0.55f, Mathf.Abs(height))),
            300f,
            3_000f);

        // A raking sun is BRIGHTER than a high one and not dimmer -- it is the same disc through
        // more air, and what the air takes is the blue end. The energy peak sits at the horizon for
        // that reason and the colour goes with it.
        float low = 1f - Mathf.SmoothStep(0.02f, 0.30f, height);

        _light.LightEnergy = height > 0f ? Mathf.Lerp(0f, Mathf.Lerp(1.25f, 1.4f, low), up) : down * 0.18f;
        _light.LightColor = height > 0f
            ? new Color(1.0f, 0.96f, 0.88f).Lerp(new Color(1.0f, 0.71f, 0.44f), low)
            : new Color(0.60f, 0.72f, 1.0f);

        // Twilight is wider than the sun's own ramp on purpose: the sky is still lit for a good
        // while after the disc has gone, and an ambient that fell with the sun would black the city
        // out the instant it touched the horizon.
        float lit = Mathf.SmoothStep(-0.26f, 0.22f, height);
        float dusk = 1f - Mathf.SmoothStep(0.02f, 0.40f, Mathf.Abs(height));

        // ⚠ AMBIENT IS THE SKY HERE -- AmbientSource.Sky at full contribution -- so the four colours
        // below are not a backdrop, they are the fill light on every wall the sun is not on. That is
        // why the dusk tint is worth having at all: it puts warm light INTO the shadows, which is
        // the half of golden hour a directional light cannot do.
        _air.AmbientLightEnergy = Mathf.Lerp(0.26f, 1.05f, lit);

        // 🔴 HOW FAR THROUGH THE NIGHT IT IS, AND NOT HOW DARK IT IS. A darkness was sent here
        // first and it was the wrong quantity twice: it is symmetrical, so a window keyed on it
        // said the same thing at nine in the evening and at four in the morning; and it is a
        // brightness, so it reached the emission as a dimmer and faded every lamp in the city up
        // through dusk and down through dawn. ***A lamp is switched, not dimmed.*** The shader now
        // takes an hour and decides for itself, per window, whether that window is lit.
        //
        // ⚠ TAKEN OFF THE CLOCK AND NOT OFF THE SUN'S HEIGHT, because height is a cosine: it barely
        // moves for the hours either side of midnight and then races at the horizon, so a phase
        // built from it would have the whole city going to bed in the first twenty minutes. This is
        // linear in TIME, which is what an hour is.
        //
        // Clamped rather than wrapped, so all day it sits at 1 — past every lamp's hour, which is
        // the same as saying they are all out.
        _paint?.SetShaderParameter(
            "night_phase",
            Mathf.Min(1f, (Clock(_world.Tick.Raw, within) + (1f - NightOpensAt)) % 1f / NightSpans));

        // 🔴 WHICH NIGHT IT IS, so that not every lamp in the city keeps the same hours for ever.
        // Every draw in the shader was keyed on the bay, the storey and the Building -- none of
        // which moves -- so the same windows lit at the same times on every night of the world's
        // life. ***A city you cannot tell two nights apart is a screensaver.***
        //
        // ⚠ IT TURNS OVER AT DUSK AND NOT AT MIDNIGHT. A night spans the date boundary, so a
        // counter incrementing at 00:00 would redraw every lamp halfway through the evening and
        // the city would visibly reshuffle. The offset here is `night_phase`'s own, so the floor
        // steps at the one instant the phase wraps to zero and nothing is keyed on it.
        //
        // ⚠ WRAPPED AT 1024 BECAUSE IT IS A HASH SEED AND NOT A COUNT. A float loses whole numbers
        // past 2^24, and a seed that stops incrementing is a city that stops changing -- which is
        // the defect this exists to fix, arriving quietly a few thousand Days in.
        _paint?.SetShaderParameter(
            "night_index",
            Mathf.Floor(
                (((_world.Tick.Raw % (ulong)(Ticks.PerDay * 1024)) + within) / Ticks.PerDay)
                + (Ticks.DayBeginsAtHour / (float)Ticks.HoursPerDay)
                + (1f - NightOpensAt)));

        _above.SkyTopColor = new Color(0.03f, 0.04f, 0.11f)
            .Lerp(new Color(0.28f, 0.42f, 0.66f), lit)
            .Lerp(new Color(0.24f, 0.26f, 0.52f), dusk * 0.85f);
        _above.SkyHorizonColor = new Color(0.06f, 0.08f, 0.17f)
            .Lerp(new Color(0.63f, 0.70f, 0.78f), lit)
            .Lerp(new Color(1.00f, 0.56f, 0.28f), dusk);
        _above.GroundBottomColor = new Color(0.04f, 0.05f, 0.09f)
            .Lerp(new Color(0.50f, 0.56f, 0.62f), lit);
        _above.GroundHorizonColor = new Color(0.05f, 0.07f, 0.13f)
            .Lerp(new Color(0.58f, 0.65f, 0.71f), lit)
            .Lerp(new Color(0.62f, 0.42f, 0.34f), dusk);
    }
}
