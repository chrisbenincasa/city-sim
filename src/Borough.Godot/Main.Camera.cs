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

// ---- the camera -- pan, edge scroll, dolly, orbit, tilt, and the frame the city is fitted into 
//
// THE CAMERA CANNOT INFLUENCE THE SIMULATION (adr/0007). Nothing in this file reads a Tick or
// writes a Command; Paved() and Frame() read the city's extent to decide where to point, which
// is the traffic in the one direction that is allowed.
//
// Moved out of Main.cs on 2026-09-04, plans/0045 queue row 26. One class across nine files,
// no behaviour changed and no State Hash moved.

public partial class Main
{
    /// <summary>
    /// Slides the eye across the ground by a screen-space delta. <b>The one place panning happens.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>It runs along the EYE's axes and not the world's</b>, because a turn parts the two and a
    /// drag would then go off at an angle. ⚠ <b>Across the ground and never through it</b>, so the
    /// city cannot be lost behind the camera by a careless sweep.
    /// </para>
    /// <para>
    /// <b>Scaled by the standoff rather than by the city</b>, so a close-up pans a street at a time
    /// and a wide shot crosses a district. ***Three callers share it*** — a right-drag, a trackpad
    /// pan gesture and the edge scroll — and they differ only in where the delta comes from.
    /// </para>
    /// </remarks>
    /// <param name="by">Screen-space movement, in pixels, of the kind a drag reports.</param>
    private void Pan(Vector2 by)
    {
        float reach = _distance * 0.002f;
        var right = new Vector3(Mathf.Cos(_yaw), 0f, -Mathf.Sin(_yaw));
        var ahead = new Vector3(-Mathf.Sin(_yaw), 0f, -Mathf.Cos(_yaw));

        _focus += ((right * -by.X) + (ahead * by.Y)) * reach;

        Orbit();
    }

    /// <summary>
    /// Pans while the pointer rests near an edge of the window. <b>The camera control that needs no
    /// button at all.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every verb holds the left button, so a person mid-edit has no hand free to move the
    /// city.</b> Edge scrolling is the affordance that costs no button, which is why every builder
    /// in the reference set has it.
    /// </para>
    /// <para>
    /// ⚠ <b>The speed RAMPS with depth into the margin</b> rather than switching on, so resting a
    /// cursor just inside the boundary creeps and pushing it to the frame's edge moves properly.
    /// ***A binary edge scroll is unusable at both settings***: fast enough to cross the map is too
    /// fast to stop on a street.
    /// </para>
    /// <para>
    /// 🔴 <b>THE TWO PANELS ARE EXCLUDED, AND BY THEIR OWN RECTANGLES RATHER THAN BY A CONSTANT.</b>
    /// The readout is top-left and the hover bottom-left, both inside the margin, so reading either
    /// would otherwise creep the camera the whole time somebody looked at it — ***the one place a
    /// person deliberately parks the cursor is the one place this must not fire.*** ⚠ <b>The first
    /// spelling guessed a 560×200 corner</b> and killed edge-scrolling north-west entirely, which is
    /// a real direction to want; asking the <c>Control</c> for <c>GetGlobalRect</c> makes the dead
    /// zone exactly the size of the text and no larger, and it cannot drift when a panel grows a line.
    /// </para>
    /// <para>
    /// ⚠ <b>It is skipped while a panel is open</b>, because the tuner and the governing panel are
    /// full of fields somebody is aiming at.
    /// </para>
    /// </remarks>
    /// <param name="delta">Seconds since the last frame, so the pan is a rate and not a per-frame step.</param>
    private void Edge(double delta)
    {
        if (_governing || _tuner.Visible)
        {
            return;
        }

        Rect2 frame = GetViewport().GetVisibleRect();
        Vector2 at = GetViewport().GetMousePosition();

        // Outside the window entirely: a pointer that has left is not pointing at an edge, and
        // following it would pan for as long as the cursor was somewhere else.
        if (!frame.HasPoint(at))
        {
            return;
        }

        // Over a panel: reading is not an instruction to move. ⚠ EVERY Control's own rect is asked
        // rather than a guessed corner -- a guess killed edge-scrolling north-west outright on the
        // first attempt -- and the palette is the third, sitting on the bottom edge where the pan
        // would otherwise fight the buttons.
        if (_readout.GetGlobalRect().HasPoint(at)
            || _hover.GetGlobalRect().HasPoint(at)
            || _palette.GetGlobalRect().HasPoint(at))
        {
            return;
        }

        float push = 0f;
        var by = Vector2.Zero;

        Ramp(at.X, frame.Size.X, ref by, ref push, Vector2.Right);
        Ramp(at.Y, frame.Size.Y, ref by, ref push, Vector2.Down);

        if (push > 0f)
        {
            // A rate in pixels a second, converted to the delta Pan wants.
            Pan(by * (float)delta * EdgePixelsPerSecond);
        }
    }

    /// <summary>One axis of the edge ramp: how far into the margin, and which way that points.</summary>
    private static void Ramp(
        float at, float span, ref Vector2 by, ref float push, Vector2 axis)
    {
        if (at < EdgeMarginPixels)
        {
            float depth = (EdgeMarginPixels - at) / EdgeMarginPixels;

            by += axis * depth;
            push += depth;
        }
        else if (at > span - EdgeMarginPixels)
        {
            float depth = (at - (span - EdgeMarginPixels)) / EdgeMarginPixels;

            by -= axis * depth;
            push += depth;
        }
    }

    /// <summary>
    /// Change the eye's standoff rather than the field of view: a zoom that moves the eye keeps
    /// the perspective the picture was framed with. One step is a wheel notch, and a gesture
    /// arrives as a stream of fractions of one.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A step is a RATIO and not a distance.</b> A fixed distance a notch is the same nudge
    /// over the whole city as it is over one street, so the last notches in cross the ground and
    /// come out underneath it — which is what the bounds here are for.
    /// </remarks>
    private void Dolly(float steps)
    {
        _distance = Mathf.Clamp(
            _distance * Mathf.Pow(DollyPerStep, steps), Nearest(), Furthest());

        Orbit();
    }

    /// <summary>Fits the eye's orbit to the city that is actually standing.</summary>
    /// <remarks>
    /// ⚠ <b>Framed on the EXTENT and not on the far corner.</b> A lattice may sit at the map's
    /// origin, and a camera pulled back by the largest coordinate then frames the empty map rather
    /// than the city standing in one corner of it.
    /// </remarks>
    private void Frame()
    {
        LotTable lots = _world.Lots;
        float east = float.MaxValue;
        float north = float.MaxValue;
        float eastEnd = float.MinValue;
        float northEnd = float.MinValue;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            east = Mathf.Min(east, lots.East[slot].Raw * MetresPerTile);
            north = Mathf.Min(north, lots.North[slot].Raw * MetresPerTile);
            eastEnd = Mathf.Max(eastEnd, lots.East[slot].Raw * MetresPerTile);
            northEnd = Mathf.Max(northEnd, lots.North[slot].Raw * MetresPerTile);
        }

        if (east > eastEnd)
        {
            east = north = 0f;
            eastEnd = northEnd = 512f;
        }

        float span = Mathf.Max(256f, Mathf.Max(eastEnd - east, northEnd - north));
        var centre = new Vector3((east + eastEnd) * 0.5f, 0f, -(north + northEnd) * 0.5f);

        _span = span;
        _focus = centre;
        _distance = span * 0.95f;
        _laid = new Rect2(east, north, eastEnd - east, northEnd - north);
        _paved = Paved(_laid);
    }

    /// <summary>The rectangle the Road Graph's Nodes span, in metres — the ground that is paved.</summary>
    /// <remarks>
    /// ⚠ <b>It falls back to the Lots rather than to nothing</b>, because a Ruleset stating no
    /// <c>[roads]</c> has no Nodes at all and the caller would otherwise get an empty rectangle that
    /// suppresses nothing — which is the same defect as suppressing everything, pointing the other
    /// way.
    /// </remarks>
    private Rect2 Paved(Rect2 fallback)
    {
        RoadNodeTable nodes = _world.Roads.Nodes;
        float east = float.MaxValue;
        float north = float.MaxValue;
        float eastEnd = float.MinValue;
        float northEnd = float.MinValue;

        for (int slot = 0; slot < nodes.Rows.SlotCount; slot++)
        {
            if (!nodes.Rows.IsLive(slot))
            {
                continue;
            }

            east = Mathf.Min(east, nodes.East[slot].Raw * MetresPerTile);
            north = Mathf.Min(north, nodes.North[slot].Raw * MetresPerTile);
            eastEnd = Mathf.Max(eastEnd, nodes.East[slot].Raw * MetresPerTile);
            northEnd = Mathf.Max(northEnd, nodes.North[slot].Raw * MetresPerTile);
        }

        return east > eastEnd ? fallback : new Rect2(east, north, eastEnd - east, northEnd - north);
    }

    /// <summary>
    /// Frames the city and creates the eye. <b>Called once</b> — a regenerate re-frames with
    /// <see cref="Frame"/> and keeps the camera, so a rebuilt world does not arrive with a second
    /// <see cref="Camera3D"/> in the tree and the viewer's own yaw survives the rebuild.
    /// </summary>
    private void Look()
    {
        Frame();

        _lens = new CameraAttributesPractical();
        _camera = new Camera3D { Far = 200_000f, Fov = 60f, Attributes = _lens };

        AddChild(_camera);
        Orbit();
    }

    /// <summary>Puts the lens on or takes it off — the HUD and the depth of field together.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE HUD IS THE HALF THAT MATTERS AND THE CURSOR IS THE WORST OF IT.</b> The pick marker
    /// is a Cell-sized quad — 128 m — so at the framings a person would photograph from it is a
    /// sixth of the picture in flat yellow. It is exactly right as an instrument and ruinous as a
    /// subject, which is the whole shape of this verb: ***nothing here is wrong, it is just not what
    /// a photograph is of.***
    /// </para>
    /// <para>
    /// ⚠ <b>The focus distance is the ORBIT's and not a number.</b> The eye is always exactly
    /// <see cref="_distance"/> from what it is looking at, so the plane in focus is known for free
    /// and no key has to be authored for it. The near and far bounds are fractions of that, which is
    /// what makes the effect scale: at 300 m it is a tilt-shift over a few streets, and at 3 km it
    /// is a haze over the far half of the city.
    /// </para>
    /// <para>
    /// ⚠ <b>It re-applies on every zoom and every tilt</b> — <see cref="Orbit"/> calls it — because
    /// a focus plane pinned to a distance the camera has since left is a blur over the subject.
    /// </para>
    /// <para>
    /// ⚠ <b>The Cell lattice goes off and does NOT come back</b>, which is the one place this verb
    /// is not its own inverse. Turning it back on would override a person who had switched it off
    /// themselves, and the alternative — remembering what it was — is a second piece of state for a
    /// grid one keypress restores. ***Asymmetric on purpose, and small.***
    /// </para>
    /// </remarks>
    private void Lens(bool on)
    {
        _photographing = on;
        _hud.Visible = !on;
        _cursor.Visible = !on;
        _cells.Visible = _cells.Visible && !on;

        _lens.DofBlurNearEnabled = on;
        _lens.DofBlurFarEnabled = on;
        _lens.DofBlurAmount = 0.12f;
        _lens.DofBlurNearDistance = _distance * 0.62f;
        _lens.DofBlurNearTransition = _distance * 0.34f;
        _lens.DofBlurFarDistance = _distance * 1.34f;
        _lens.DofBlurFarTransition = _distance * 0.7f;
    }

    /// <summary>
    /// Put the eye where the yaw, the pitch and the distance say, looking at the focus.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The eye is derived from the orbit and never read back out of it.</b> Every verb moves
    /// one of the three fields and calls this, so a rotation cannot drift the framing and a zoom
    /// cannot leave the camera pointing somewhere a pan did not put it.
    /// </remarks>
    private void Orbit()
    {
        float flat = Mathf.Cos(_pitch) * _distance;

        _camera.LookAtFromPosition(
            _focus + new Vector3(
                Mathf.Sin(_yaw) * flat, Mathf.Sin(_pitch) * _distance, Mathf.Cos(_yaw) * flat),
            _focus,
            Vector3.Up);

        // 🔴 THE SHADOW RANGE RIDES THE STANDOFF TOO, and it is the second half of the fix for a
        // crawling shadow edge. A cascade is a fixed count of texels spread over whatever distance
        // it is told to cover, so a range set for the widest shot spends most of its resolution on
        // ground the eye is nowhere near — and at a close orbit the edge under the nose is
        // quantised to texels several screen pixels across. Measured against the same burst: at a
        // 140 m standoff, 1,600 m leaves a fuzzy two-to-three-pixel serration and 400 m leaves a
        // clean line. Tying the two keeps both ends: near work gets tight cascades, and a wide
        // shot still has shadows out to the horizon it can see.
        //
        // ⚠ The floor exists because a very close orbit must still shadow the whole block, and the
        // ceiling because past it the cascades are coarse again and nothing is gained.
        //
        // ⚠ HOW THE CRAWL WAS MEASURED, because it is not a thing a still can show: twenty-four
        // frames one Tick apart at half a Tick a second -- slow enough that the sub-Tick sun left
        // over in each shot is a fiftieth of a Tick rather than a whole one -- tracking one edge.
        // It should advance +3.4 px a Tick. Before the atlas key was repaired and the range tied
        // here, it advanced between -2.4 and +9.5 and WENT BACKWARDS ON SIX OF TWENTY-THREE
        // FRAMES; after, the residual span is 2.9 px against 8.6 and not one step reverses.
        // ***A shadow edge that reverses is what could not be described from the chair.***
        // ⚠ THE RANGE MOVED TO Daylight() AND IS NOT SET HERE, because it is a function of the
        // standoff and of the sun's height together -- see the comment there, which carries the
        // texel arithmetic. Daylight runs every frame and this runs on a camera move, so a copy
        // here would win for exactly one frame and then lose, which is the worst of both.

        // The focus plane rides the standoff, so a zoom or a tilt cannot leave the subject blurred.
        if (_photographing)
        {
            Lens(true);
        }
    }

    /// <summary>Where a step of the tilt key lands, in whole degrees, already clamped.</summary>
    /// <remarks>
    /// ⚠ <b>It rounds before it steps, so the ladder is the same going up and coming back down.</b>
    /// Stepping the radian and converting afterwards would leave 35.26° at 40.26°, 45.26° and so on
    /// — a ladder whose rungs depend on where it was started, which is exactly what an absolute
    /// verb exists to avoid.
    /// </remarks>
    private int Tipped(int degrees) => Mathf.Clamp(
        (int)Mathf.Round(Mathf.RadToDeg(_pitch)) + degrees,
        (int)Mathf.Round(Mathf.RadToDeg(LowestRadians)),
        (int)Mathf.Round(Mathf.RadToDeg(HighestRadians)));

    /// <summary>The nearest the eye may stand, which is close enough to read one Building.</summary>
    private float Nearest() => Mathf.Max(32f, _span * 0.02f);

    /// <summary>The furthest, which is the whole city and a margin, never the whole map.</summary>
    private float Furthest() => _span * 3f;
}
