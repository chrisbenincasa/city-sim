using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Golden;

/// <summary>
/// What the golden session <em>reaches</em>, as distinct from what it hashes to.
/// </summary>
/// <remarks>
/// <para>
/// <b>A baseline records what a run did, so a change that narrows what the run reaches is invisible
/// in it by construction.</b> That sentence is slice 10 task 11's finding and it is the whole reason
/// this file exists: a derived sample of 1 stopped the committed trace ever landing on the Zone
/// Rule's create branch, every hash still moved, every test still passed, and half a mechanism went
/// uncovered for a slice. <c>GoldenHashTests</c> can only ever say <i>the number is the number</i>.
/// These tests say <i>the run went through the door</i>.
/// </para>
/// <para>
/// <b>5a-bis made the hazard concrete rather than theoretical.</b> Zoning a Tile now zones the
/// <em>block</em> it falls in, and eight of the session's eleven original commands named Tiles inside
/// a single 32-Tile block that the populator had already carved. Re-recording without moving them
/// would have retired the <c>zone</c> verb from the baseline while producing a full set of freshly
/// correct hashes — the failure mode exactly, on its second outing in three slices.
/// </para>
/// <para>
/// <b>Every assertion here is an inequality or a branch, never a hash.</b> Pinning a Lot count in
/// two places would put this file in competition with the committed trace, and
/// <c>plans/0012</c> <i>Cause 1</i> is about what happens next.
/// </para>
/// </remarks>
public sealed class GoldenSessionCoverageTests
{
    /// <summary>The block the session bulldozes a face off, zones, restores and bulldozes again.</summary>
    private static (int Column, int Row) Edited => (50, 50);

    /// <summary>The block the session strips of all four faces before zoning it.</summary>
    private static (int Column, int Row) Stripped => (60, 60);

    /// <summary>
    /// <b>The lattice spacing the fixture states is the one the Ruleset states.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="GoldenFixtures.BlockTiles"/> is a copy of <c>[roads] block_tiles</c>, held here
    /// rather than read so that retuning the Ruleset breaks a test instead of silently relocating
    /// every command in the session. This is the test it breaks.
    /// </remarks>
    [Fact]
    public void The_fixtures_lattice_is_the_rulesets_lattice()
    {
        Assert.True(
            GoldenFixtures.Catalogue().TryResolve(GoldenFixtures.RulesetHash, out Ruleset rules),
            "the golden Ruleset is not in its own catalogue");

        Assert.Equal(GoldenFixtures.BlockTiles, rules.Roads.BlockTiles);
    }

    /// <summary>
    /// <b>Every Zone command in the session carves land, and none of them lands on another's block.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Against a populate-only control, because the populator carves too.</b> The session's own
    /// <c>populate</c> subdivides along lattice row 0 until it has land for the Buildings it wants, so
    /// a raw Lot count says nothing about the commands. The control is the same log with every command
    /// but <c>populate</c> removed; the difference is what the player did.
    /// </para>
    /// <para>
    /// <b>The two deliberate exceptions are named rather than averaged away.</b>
    /// <see cref="Stripped"/> is zoned after all four of its faces are gone and yields nothing — that
    /// is how the refusal branch reaches the baseline at all — and <see cref="Edited"/> ends the run
    /// three Lots short because the session bulldozes its south face again at Tick 300. Folding either
    /// into a tolerance is what would let an accidental no-op back in.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_zone_command_in_the_session_reaches_land_it_can_carve()
    {
        int zoneCommands = 0;
        InputLog session = GoldenFixtures.Session();

        for (int i = 0; i < session.Count; i++)
        {
            if (session.Entry(i).Command.Kind == CommandKind.Zone)
            {
                zoneCommands++;
            }
        }

        int carved = At(session, Applied).Lots.Rows.LiveCount
            - At(PopulateOnly(session), Applied).Lots.Rows.LiveCount;

        // Eleven ordinary blocks at ten Lots each, plus Edited's seven, plus Stripped's nothing.
        int expected = ((zoneCommands - 2) * LotsPerBlock) + (LotsPerBlock - 3);

        Assert.True(
            carved == expected,
            $"the session's {zoneCommands} zone commands carved {carved} Lots where {expected} were "
            + "expected. A shortfall that is a multiple of a block face means two commands are naming "
            + "the same block, or a block the populator already carved -- which costs the baseline the "
            + "coverage rather than the correctness, so no hash test can see it. Move the command, do "
            + "not lower this.");
    }

    /// <summary>
    /// <b>The session refuses a block it has stripped of frontage, and refusal is silent.</b>
    /// </summary>
    /// <remarks>
    /// <c>02 §2.2</c>'s third rule — <i>"land that cannot be given frontage stays unlotted and
    /// undevelopable"</i> — is the half of the subdivider that produces nothing, so it is the half a
    /// baseline covers by accident and never on purpose. The session bulldozes all four faces of
    /// <see cref="Stripped"/> in one Tick and zones it in the next.
    /// </remarks>
    [Fact]
    public void The_session_zones_a_block_with_no_frontage_and_gets_nothing()
    {
        Assert.Equal(0, LotsOn(At(GoldenFixtures.Session(), Applied), Stripped));
    }

    /// <summary>
    /// <b>A road edit re-subdivides: the session both frees Lots and creates them after the fact.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three readings of one block, because the two directions are separate branches.</b> At Tick
    /// 131 <see cref="Edited"/> has been zoned with its south face already gone, so it holds the seven
    /// Lots its three remaining faces can carry rather than ten. At Tick 201 the face has been laid
    /// back and <c>Resubdivide</c> has found a block that gained frontage — three more. At Tick 301 it
    /// is gone again and the three are freed, because they are vacant.
    /// </para>
    /// <para>
    /// <b>Seven and not eight, and the asymmetry is real.</b> A block takes the Left side of its south
    /// face, and Left is the even indices of five — three of them — against Right's two. So a block's
    /// four faces carry 3, 2, 2 and 3, which is odd-and-even house numbering showing through
    /// (<c>adr/0078</c>) rather than an off-by-one.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_road_edit_in_the_session_re_subdivides_in_both_directions()
    {
        InputLog session = GoldenFixtures.Session();

        Assert.Equal(LotsPerBlock - 3, LotsOn(At(session, new Ticks(131)), Edited));
        Assert.Equal(LotsPerBlock, LotsOn(At(session, new Ticks(201)), Edited));
        Assert.Equal(LotsPerBlock - 3, LotsOn(At(session, new Ticks(301)), Edited));
    }

    /// <summary>How many Lots one block of the lattice carries when all four of its faces are Streets.</summary>
    private const int LotsPerBlock = 10;

    /// <summary>
    /// The Tick every command has been applied by, and no further.
    /// </summary>
    /// <remarks>
    /// <b>Short of the run's 2,048 on purpose.</b> The Zone Rules demolish, and a Lot count taken at
    /// the end would be measuring them rather than the subdivider.
    /// </remarks>
    private static Ticks Applied => new(402);

    /// <summary><paramref name="session"/>'s world, run to <paramref name="tick"/> and no further.</summary>
    private static World At(InputLog session, Ticks tick)
    {
        Simulation simulation = Replay.Start(session, GoldenFixtures.Catalogue());

        Replay.Trace(simulation, session, tick, (int)tick.Raw, []);

        return simulation.World;
    }

    /// <summary>
    /// The same session with every command but <c>populate</c> removed.
    /// </summary>
    /// <remarks>
    /// <b>The control, and it is built from the session rather than beside it</b> — a hand-written
    /// twin would be a second copy of the seed, the sizing and the Ruleset hash, and the useful
    /// property of a control is that it differs in exactly one thing.
    /// </remarks>
    private static InputLog PopulateOnly(InputLog session)
    {
        InputLogBuilder builder = new(
            session.Seed, session.Configuration, session.RulesetHash);

        for (int i = 0; i < session.Count; i++)
        {
            (Ticks tick, Command command) = session.Entry(i);

            if (command.Kind == CommandKind.Populate)
            {
                builder.Append(tick, command);
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// How many Lots belong to a block — <b>which is not how many fall inside its bounds</b>.
    /// </summary>
    /// <remarks>
    /// A Lot sits <em>on</em> a Segment, so half of every face's Lots belong to the block across the
    /// street. The side is what decides, exactly as <c>LotSubdivider.BlockOf</c> decides it, and this
    /// walks the frontage rather than the coordinates so that a Lot which has lost its Street counts
    /// for neither block.
    /// </remarks>
    private static int LotsOn(World world, (int Column, int Row) block)
    {
        StreetGrid streets = world.Roads.Streets;
        int count = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || !world.Lots.HasFrontage(slot))
            {
                continue;
            }

            var side = (StreetSide)world.Lots.Side[slot];
            int east = world.Lots.East[slot].Raw;
            int north = world.Lots.North[slot].Raw;

            int column = Borough.Core.Arithmetic.IntegerMath.FloorDiv(east, streets.BlockTiles);
            int row = Borough.Core.Arithmetic.IntegerMath.FloorDiv(north, streets.BlockTiles);

            if (north == row * streets.BlockTiles)
            {
                if (side == StreetSide.Right)
                {
                    row--;
                }
            }
            else if (side == StreetSide.Left)
            {
                column--;
            }

            if (column == block.Column && row == block.Row)
            {
                count++;
            }
        }

        return count;
    }
}
