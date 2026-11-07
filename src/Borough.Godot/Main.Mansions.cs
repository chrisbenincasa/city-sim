using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Appearance;
using Godot;

namespace Borough.Shell;

/// <summary>
/// A mansion's grounds, drawn on its whole footprint and the deep yard in front of it. One household
/// gets railings on grand piers, a gravel motor court before the door, a loop or a straight drive
/// through open gates, a drive to the garage doors and an allée of trees along a long drive. Several
/// households get a hedge, a walk to the door with lamps, a mailbox stand, a sign and a parking court
/// on the side the garage would stand.
/// </summary>
public partial class Main
{
    private const float DriveMetres = 3.2f;
    private const float StallMetres = 2.8f;
    private const float StallDeepMetres = 6f;
    private const float AisleMetres = 6f;
    private const float FountainCourtMetres = 10f;
    private const float AlleeEveryMetres = 6f;

    private static readonly Color Gravel = Hex(0xc2b79e);
    private static readonly Color ParkingTone = Hex(0x4a4c4e);
    private static readonly Color[] CarTones = [Hex(0x2b2e31), Hex(0xd8d6d0), Hex(0x6e7377), Hex(0x2d4a6b), Hex(0x7a2e2a), Hex(0x9a9ea2)];

    /// <param name="plan">The body's plan, or <c>null</c> where the Building draws a massing box.</param>
    private void DressMansion(YardDraw draw, float w, float near, float far, ulong id, List<FamilyDoor> doors, MansionPlan? plan,
        MansionGrounds grounds, int units)
    {
        uint h = (uint)id * 0x9E3779B1u;
        h ^= h >> 13;
        int pick = (int)(id % 997);
        _frontGroundEdits.Add(draw.Box(-w, w, -near, far, 0f, .06f, LawnTones[pick % LawnTones.Length]));

        float front = plan is null ? near : -plan.Masses[0].Y0;
        float entry = plan?.Entry ?? 0f;
        int side = plan?.GarageSide ?? 1;
        FamilyDoor door = doors.Count > 0 ? doors.MinBy(d => MathF.Abs(d.Across - entry)) : new FamilyDoor(entry, front, 1.2f, .3f);
        Color stone = StoneTones[pick % StoneTones.Length];
        float step = Mathf.Max(1f, (Mathf.Clamp(door.Width, 1f, 2.4f) + .4f) / 1.4f);
        Kit(FrontKit.Doorstep, draw.Module(door.Across, door.Forward + .3f, 0f, 0f, Colors.White, step));
        for (int k = -1; k <= 1; k += 2)
            Furnish(Furniture.Topiary, draw.Module(door.Across + (k * ((step * .7f) + .5f)), door.Forward + .45f, 0f, 0f, Colors.White,
                GardenTopiaryScale, GardenTopiaryScale, GardenTopiaryScale));

        if (grounds == MansionGrounds.OneHousehold) DressEstate(draw, w, front, far, h, door, plan, side, stone);
        else DressManor(draw, w, front, far, h, door, side, units, stone);
    }

    /// <summary>A single household's grounds: railings, a motor court, the drives and the trees along them.</summary>
    private void DressEstate(YardDraw draw, float w, float front, float far, uint h, FamilyDoor door, MansionPlan? plan, int side, Color stone)
    {
        const float Half = .22f;
        float line = far - .05f - Half;
        Color brick = BrickTones[(h >> 4) % (uint)BrickTones.Length];
        Color gates = GateTones[(h >> 9) % (uint)GateTones.Length];
        float yard = line - front;
        var openings = new List<(float From, float To)>();

        float courtWide = Mathf.Clamp(w * .8f, 10f, 18f);
        float courtDeep = Mathf.Min(12f, yard - 6f);
        float cx = Mathf.Clamp(door.Across, -w + (courtWide * .5f) + 1f, w - (courtWide * .5f) - 1f);
        bool court = courtDeep >= 5f;
        bool fountain = court && courtDeep >= FountainCourtMetres && courtWide >= 12f;
        bool loop = court && w >= 20f && (h >> 3) % 3 != 0;
        if (court)
        {
            _frontGroundEdits.Add(draw.Box(cx - (courtWide * .5f), cx + (courtWide * .5f), front, front + courtDeep, 0f, .08f, Gravel));
            if (fountain)
            {
                float island = Mathf.Min(courtWide - (2f * DriveMetres), courtDeep - (2f * DriveMetres));
                float middle = front + (courtDeep * .5f);
                _frontGroundEdits.Add(draw.At(cx, middle, island, island, 0f, .1f, LawnTones[(h >> 6) % (uint)LawnTones.Length]));
                Kit(FrontKit.Fountain, draw.Module(cx, middle, .1f, 0f, stone));
            }

            float reach = front + courtDeep;
            if (loop)
            {
                foreach (int k in (ReadOnlySpan<int>)[-1, 1])
                {
                    float x = cx + (k * ((courtWide * .5f) - (DriveMetres * .5f)));
                    Drive(draw, x, reach - .5f, line + Half, openings);
                }
            }
            else
            {
                float x = cx + (side * ((courtWide * .5f) - (DriveMetres * .5f)));
                Drive(draw, x, reach - .5f, line + Half, openings);
            }

            if (!fountain) Kit(FrontKit.Car, draw.Module(cx - (side * 2.5f), front + (courtDeep * .5f), .08f, Mathf.Pi * .5f,
                CarTones[(h >> 12) % (uint)CarTones.Length]));
        }
        else
        {
            Drive(draw, door.Across, door.Forward + .6f, line + Half, openings);
        }

        if (plan is not null && FamilyBodyBuilder.GarageApron(plan) is { } apron)
        {
            float gx = apron.X, gs = -apron.Y;
            if (apron.Facing == 0)
            {
                float wide = Mathf.Min(6f, w - MathF.Abs(gx));
                _frontGroundEdits.Add(draw.Box(gx - (wide * .5f), gx + (wide * .5f), gs, gs + 6f, 0f, .08f, Gravel));
                if (!Covered(openings, gx)) Drive(draw, gx, gs + 5.5f, line + Half, openings);
            }
            else
            {
                float x0 = apron.Facing > 0 ? gx : -w + .3f, x1 = apron.Facing > 0 ? w - .3f : gx;
                _frontGroundEdits.Add(draw.Box(x0, x1, gs - 4f, gs + 4f, 0f, .08f, Gravel));
                float lane = (x0 + x1) * .5f;
                if (!Covered(openings, lane)) Drive(draw, lane, gs + 3.5f, line + Half, openings);
            }
        }

        Merge(openings);
        float at = -w;
        foreach ((float from, float to) in openings)
        {
            GrandPier(draw, from - .45f, line, brick);
            GrandPier(draw, to + .45f, line, brick);
            Kit(FrontKit.Lamp, draw.Module(from - .45f, line - 1f, 0f, 0f, Colors.White));
            Kit(FrontKit.Lamp, draw.Module(to + .45f, line - 1f, 0f, 0f, Colors.White));
            float leaf = (to - from) * .5f;
            foreach (int k in (ReadOnlySpan<int>)[-1, 1])
            {
                float hinge = k < 0 ? from : to;
                Kit(FrontKit.GateIron, draw.Module(hinge - (k * .1f), line - (leaf * .5f), GateLift, k * Mathf.Pi * .5f, gates, leaf / 1.2f));
            }

            BoundaryRun(draw, FrontKit.Railing, at + .6f, from - .9f, line, brick, sideways: false);
            at = to + .9f;
        }

        GrandPier(draw, -w + .45f, line, brick);
        GrandPier(draw, w - .45f, line, brick);
        BoundaryRun(draw, FrontKit.Railing, at, w - .9f, line, brick, sideways: false);

        foreach ((float from, float to) in openings)
        {
            float x = (from + to) * .5f, length = line - (front + courtDeep);
            if (length < 10f) continue;
            int trees = (int)(length / AlleeEveryMetres);
            for (int k = 0; k < trees; k++)
            {
                float s = line - 3f - (k * AlleeEveryMetres);
                foreach (int edge in (ReadOnlySpan<int>)[-1, 1])
                {
                    float tx = x + (edge * ((to - from) * .5f + 1.6f));
                    if (MathF.Abs(tx) > w - 1f || openings.Any(o => tx > o.From - 1.2f && tx < o.To + 1.2f)) continue;
                    float tone = .88f + ((h + (uint)k) % 7 * .04f);
                    _frontTreeEdits.Add(draw.Tree(tx, s, 7f + ((h + (uint)k) % 3), new Color(tone, tone, tone)));
                }
            }
        }

        foreach (int k in (ReadOnlySpan<int>)[-1, 1])
        {
            float x = k * (w - 3.5f), s = line - 4f;
            if (openings.Any(o => x > o.From - 4f && x < o.To + 4f) || (court && MathF.Abs(x - cx) < (courtWide * .5f) + 3f && s < front + courtDeep + 3f)) continue;
            float tone = .9f + (((h >> 5) + (uint)k) % 5 * .04f);
            _frontTreeEdits.Add(draw.Tree(x, s, 8f + ((h >> 2) % 3), new Color(tone, tone, tone)));
        }
    }

    /// <summary>Several households' grounds: a hedge, a walk with lamps, a mailbox stand, a sign and a parking court.</summary>
    private void DressManor(YardDraw draw, float w, float front, float far, uint h, FamilyDoor door, int side, int units, Color stone)
    {
        const float Half = .42f;
        float line = far - .05f - Half;
        float shade = .8f + ((h >> 7) % 5 * .05f);
        var hedge = new Color(shade, shade, shade);
        var openings = new List<(float From, float To)>();

        float walk = 1.8f;
        float from = door.Forward + .6f;
        int bays = Math.Max(1, Mathf.RoundToInt(line + Half - from));
        float bay = (line + Half - from) / bays;
        for (int k = 0; k < bays && bay > .1f; k++)
            Kit(FrontKit.PathFlags, draw.Module(door.Across, from + (bay * (k + .5f)), 0f, 0f, stone, walk, bay));
        openings.Add((door.Across - (walk * .5f) - .3f, door.Across + (walk * .5f) + .3f));
        for (float s = from + 3f; s < line - 1f; s += 6f)
        {
            foreach (int k in (ReadOnlySpan<int>)[-1, 1]) Kit(FrontKit.Lamp, draw.Module(door.Across + (k * (walk * .5f + .5f)), s, 0f, 0f, Colors.White));
        }

        Kit(FrontKit.Mailbox, draw.Module(door.Across - (side * (walk * .5f + 1f)), line - 1.4f, 0f, side * Mathf.Pi * .5f,
            CarTones[(h >> 3) % 3]));
        float signX = door.Across + (side * (walk * .5f + 2.2f));
        _frontGroundEdits.Add(draw.At(signX, line - 1.2f, 2.6f, .5f, 0f, .9f, BrickTones[(h >> 4) % (uint)BrickTones.Length]));
        _frontGroundEdits.Add(draw.At(signX, line - .94f, 1.9f, .04f, .25f, .7f, Hex(0xd8d2c4)));

        float edge = door.Across + (side * (walk * .5f + 4f));
        float px0 = side > 0 ? edge : -w + 1f, px1 = side > 0 ? w - 1f : edge;
        int stalls = Math.Min((int)((px1 - px0) / StallMetres), Math.Max(2, units + 1));
        float courtDeep = StallDeepMetres + AisleMetres;
        if (stalls >= 2 && line - front >= courtDeep + 3f)
        {
            float width = stalls * StallMetres;
            if (side > 0) px1 = px0 + width + DriveMetres;
            else px0 = px1 - width - DriveMetres;
            float s0 = line - .5f - courtDeep, s1 = line - .5f;
            _frontGroundEdits.Add(draw.Box(px0, px1, s0, s1, 0f, .08f, ParkingTone));
            float drive = side > 0 ? px1 - (DriveMetres * .5f) : px0 + (DriveMetres * .5f);
            _frontGroundEdits.Add(draw.Box(drive - (DriveMetres * .5f), drive + (DriveMetres * .5f), s1, line + Half, 0f, .08f, ParkingTone));
            openings.Add((drive - (DriveMetres * .5f), drive + (DriveMetres * .5f)));
            float first = side > 0 ? px0 : px0 + DriveMetres;
            for (int k = 0; k < stalls; k++)
            {
                float x = first + ((k + .5f) * StallMetres);
                _frontGroundEdits.Add(draw.At(x - (StallMetres * .5f), s0 + (StallDeepMetres * .5f), .1f, StallDeepMetres, .08f, .09f, Hex(0xd8d6d0)));
                if ((h >> (k % 16)) % 4 != 0)
                    Kit(FrontKit.Car, draw.Module(x, s0 + (StallDeepMetres * .5f), .08f, Mathf.Pi, CarTones[(h + (uint)k) % (uint)CarTones.Length]));
            }
        }

        Merge(openings);
        float at = -w;
        foreach ((float a, float b) in openings)
        {
            BoundaryRun(draw, FrontKit.Hedge, at, a, line, hedge, sideways: false);
            at = b;
        }

        BoundaryRun(draw, FrontKit.Hedge, at, w, line, hedge, sideways: false);
        float treeX = door.Across - (side * Mathf.Min(8f, w * .5f));
        if (MathF.Abs(treeX) < w - 2f)
        {
            float tone = .9f + ((h >> 5) % 5 * .04f);
            _frontTreeEdits.Add(draw.Tree(treeX, (front + line) * .5f, 7.5f + ((h >> 2) % 3), new Color(tone, tone, tone)));
        }
    }

    private void Drive(YardDraw draw, float x, float from, float to, List<(float From, float To)> openings)
    {
        _frontGroundEdits.Add(draw.Box(x - (DriveMetres * .5f), x + (DriveMetres * .5f), from, to, 0f, .08f, Gravel));
        openings.Add((x - (DriveMetres * .5f) - .2f, x + (DriveMetres * .5f) + .2f));
    }

    /// <summary>Sorts gaps west to east and joins any that overlap or nearly touch.</summary>
    private static void Merge(List<(float From, float To)> openings)
    {
        openings.Sort();
        for (int k = openings.Count - 1; k > 0; k--)
        {
            if (openings[k].From > openings[k - 1].To + 2f) continue;
            openings[k - 1] = (openings[k - 1].From, Mathf.Max(openings[k - 1].To, openings[k].To));
            openings.RemoveAt(k);
        }
    }

    private static bool Covered(List<(float From, float To)> openings, float x) => openings.Any(o => x > o.From && x < o.To);

    private void GrandPier(YardDraw draw, float u, float line, Color brick) =>
        Kit(FrontKit.Pier, draw.Module(u, line, 0f, 0f, brick, 1.5f, 1.5f, 1.7f));
}
