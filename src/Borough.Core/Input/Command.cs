using Borough.Core.Quantities;

namespace Borough.Core.Input;

/// <summary>
/// The player's verbs, as they enter the simulation.
/// </summary>
/// <remarks>
/// <para>
/// <b>These are <c>01 §2</c>'s five verbs and not a set invented for the Tick loop</b> — four of
/// them, because the fifth is <em>Inspect</em> and Inspect never enters the Input Log. Inspecting is
/// reading, so it changes nothing, so a replay that reproduces a session need not know it happened.
/// The same argument deletes the camera from the log (<c>adr/0007</c>): what the player looked at is
/// not an input.
/// </para>
/// <para>
/// <b>Two of the four are declared and not yet applied.</b> Service needs service Buildings and
/// Govern needs Policy, neither of which exists. Declaring them means the log format already has
/// their slot, so the artefact a bug report is made of does not change shape when they arrive.
/// </para>
/// <para>
/// ⚠ <b>That is a claim about the log's <em>shape</em> and never was a claim about its
/// <em>version</em>.</b> This remark used to run on into <em>"and this format version does not have
/// to be bumped for their arrival"</em>, which <c>InputLogCodec</c> states the refutation of in its
/// own file: <em>what would bump it is a sixth field on a command</em>. Whether a verb's arrival is
/// free depends entirely on whether its payload fits the four fields below — <c>Connect</c>'s was
/// made to fit (<c>adr/0077</c>), and a verb carrying two coordinate pairs would not have.
/// <b>Service and Govern have not been examined against that test.</b>
/// </para>
/// </remarks>
public enum CommandKind : ushort
{
    /// <summary>
    /// Reserved, and never a valid command. A zeroed <see cref="Command"/> must be recognisable
    /// rather than reading as whichever verb happened to be declared first.
    /// </summary>
    None = 0,

    /// <summary>
    /// Paint a permission set over land. <b>Since 5a-bis it zones the <em>block</em> the named Tile
    /// falls in</b>, and the subdivider carves it against the Street network — <c>02 §2.2</c>: Lots
    /// are generated, not painted.
    /// </summary>
    Zone = 1,

    /// <summary>
    /// Lay Streets, draw Arterials, place Junction pieces. <b>Applied since 5a-bis, for Streets
    /// only</b> — Arterials and Junction pieces are refused by name with their successor written
    /// beside the refusal (<c>adr/0077</c>).
    /// </summary>
    Connect = 2,

    /// <summary>Place a Building with a catchment — the design's one placement exception.</summary>
    Service = 3,

    /// <summary>Set taxes, funding, transfers, constraints. Every Policy is a Rule.</summary>
    Govern = 4,

    /// <summary>
    /// Fill the world with a synthetic city sized to its configuration. Spike <c>S0</c>'s verb, and
    /// not one of <c>01 §2</c>'s five.
    /// </summary>
    /// <remarks>
    /// <b>A verb no player has, and it is here rather than in the runner on purpose.</b> Until Zone
    /// Rules land in slice 10 there is no way to make a Citizen through a verb, so a run at the 1M
    /// target either enters through this door or through none — and a population that entered through
    /// none is a state change no replay reproduces and no hash divergence explains, which is exactly
    /// what <c>Simulation</c>'s single door exists to prevent. It carries no payload: the size is
    /// <see cref="WorldConfiguration.Citizens"/>, which the log already states.
    /// <b>It is expected to be deleted</b> when the player can grow a city instead of declaring one.
    /// </remarks>
    Populate = 5,

    /// <summary>
    /// Send somebody from the Building here to the Building a block-offset away. <c>adr/0080</c>'s
    /// verb, and — like <see cref="Populate"/> — an instrument rather than one of <c>01 §2</c>'s five.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Phase 4 is built before anything generates a Trip, so a Trip has to enter through a door.</b>
    /// Every generator the corpus names is unmilestoned (<c>adr/0080</c>), and a sampled stand-in would
    /// have fabricated the one thing every measurement downstream is a property of — the
    /// origin-destination distribution. A commanded Trip asserts nothing about the city, and being in
    /// the Input Log means replay reproduces it <em>by construction</em>, which is exactly why
    /// <see cref="Populate"/> is a verb rather than a runner switch.
    /// </para>
    /// <para>
    /// <b>Payload: <see cref="TripPayload"/> in <see cref="Command.Zone"/>'s sixteen bits</b>, so the
    /// log format version does not move — <c>InputLogCodec.Version</c>'s rule is that a <em>sixth
    /// field</em> bumps it, and this adds none. <see cref="Command.East"/> and
    /// <see cref="Command.North"/> name the origin Tile; the payload names the destination as a signed
    /// block delta, which is <see cref="ConnectPayload"/>'s pattern of deriving the far endpoint from
    /// an origin plus a descriptor the world can resolve.
    /// </para>
    /// <para>
    /// <b>It is expected to be deleted</b>, on <see cref="Populate"/>'s terms: when milestone 5b-bis
    /// ships the commute generator (<c>adr/0081</c>), this stops being the only way a Trip exists.
    /// </para>
    /// </remarks>
    Trip = 6,

    /// <summary>
    /// Admit Households through the Outside Connection standing at the named Tile.
    /// <c>adr/0128</c>'s door, and — like <see cref="Populate"/> — an instrument rather than one of
    /// <c>01 §2</c>'s five.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The gate ships at milestone 11 and what decides to arrive ships at 16</b>
    /// (<c>adr/0128</c>), so the door needs a caller and has no autonomous one. That is
    /// <see cref="Populate"/>'s position exactly, and it is taken for
    /// <see cref="Populate"/>'s reason: a population that entered any other way is a state change no
    /// replay reproduces and no State Hash divergence explains. ⚠ <b>The alternative was a
    /// deliberately crude acceptance rule</b>, refused by name as milestone 9's <b>F13</b> — a hole
    /// that throws is safe, and one that returns plausible numbers is a working mechanism that says
    /// something false.
    /// </para>
    /// <para>
    /// <b>Payload: <see cref="ArrivePayload"/> in <see cref="Command.Zone"/>'s sixteen bits</b>, so
    /// the log format version does not move — <c>InputLogCodec.Version</c>'s rule is that a
    /// <em>sixth field</em> bumps it, and this adds none. <see cref="Command.East"/> and
    /// <see cref="Command.North"/> name the gate's Tile, which the world resolves to a Building the
    /// same way <see cref="Trip"/>'s origin is resolved.
    /// </para>
    /// <para>
    /// ⚠ <b>Asking for more than the gate admits is an ordinary outcome and not an error.</b> The
    /// count is a request; <c>[[building]] arrivals_per_day</c> is the bound, metered per Day per
    /// gate, and the surplus is simply not admitted. ***A command that asks for a hundred and gets
    /// twelve is the ceiling being observable***, which is what
    /// <c>plans/0002</c> §D1 needs of it.
    /// </para>
    /// <para>
    /// <b>It is expected to be deleted</b>, on <see cref="Populate"/>'s terms: when milestone 16
    /// ships the comparison, this stops being the only thing that decides anybody arrives.
    /// </para>
    /// </remarks>
    Arrive = 7,
}

/// <summary>
/// One player command, as an unmanaged record: a verb, a place, and the verb's payload.
/// </summary>
/// <remarks>
/// <para>
/// <b>One struct rather than one per verb, because the log is a homogeneous sequence.</b> A command
/// stream of mixed record types needs either a length prefix per record or a reader that dispatches
/// before it can advance — both of which make a truncated log unreadable at exactly the moment it
/// matters most, which is the crash artifact.
/// </para>
/// <para>
/// <b>There are no padding bytes here, and that is checked by arithmetic rather than assumed.</b>
/// Two <see cref="ushort"/>s pack to four bytes and two <see cref="Tiles"/> are four each: twelve
/// bytes, fully defined. The rule comes from <see cref="Tables.Column{T}"/>, where undefined bytes
/// would reach the State Hash. Nothing folds a Command today — commands are inputs, not state — but
/// the discipline is cheaper to keep than to reinstate.
/// </para>
/// </remarks>
public readonly struct Command
{
    /// <param name="kind">Which verb.</param>
    /// <param name="east">Where, eastward. See <see cref="Entities.LotTable"/> for the axes.</param>
    /// <param name="north">Where, northward.</param>
    /// <param name="zone">The permission set, for <see cref="CommandKind.Zone"/>.</param>
    public Command(CommandKind kind, Tiles east, Tiles north, ushort zone = 0)
    {
        Kind = kind;
        Zone = zone;
        East = east;
        North = north;
    }

    /// <summary>Which verb.</summary>
    public CommandKind Kind { get; }

    /// <summary>
    /// <b>The verb's payload word.</b> The permission set for <see cref="CommandKind.Zone"/>, and a
    /// <see cref="ConnectPayload"/> for <see cref="CommandKind.Connect"/>.
    /// </summary>
    /// <remarks>
    /// <b>A set, not a kind</b> — <c>01 §2</c>'s verb paints a <em>permission set</em>, and a Lot
    /// permitting two uses is a real thing the design wants. Slice 5 carried it at full width here and
    /// narrowed it to <see cref="Entities.LotTable"/>'s single zone byte on application, on the
    /// reasoning that losing the authored value on the way in would have been that slice's bug while
    /// widening the Lot was slice 10's job. <b>Slice 10 did that, and the narrowing is discharged</b>:
    /// <see cref="Entities.LotTable.Zone"/> is a <see cref="ushort"/> and this value reaches it whole.
    /// </remarks>
    public ushort Zone { get; }

    /// <summary>Where, eastward.</summary>
    public Tiles East { get; }

    /// <summary>Where, northward.</summary>
    public Tiles North { get; }
}

/// <summary>What a <see cref="CommandKind.Connect"/> does to the edge it names.</summary>
public enum ConnectAction : byte
{
    /// <summary>Put a road there. <c>adr/0012</c>'s <em>addition</em> — boundedly wrong.</summary>
    Lay = 0,

    /// <summary>Take the road away. <c>adr/0012</c>'s <em>removal</em> — never wrong.</summary>
    Bulldoze = 1,
}

/// <summary>
/// <see cref="CommandKind.Connect"/>'s payload, packed into <see cref="Command.Zone"/>'s sixteen bits.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is why the Input Log format stays at version 1</b> (<c>adr/0077</c>). The codec's own rule
/// is that <em>a sixth field on a command</em> is what bumps a version; an origin, an axis, an action
/// and a kind fit the four fields <see cref="Command"/> already has, so nothing is added. <b>The far
/// endpoint is derived rather than carried</b> — an origin plus an axis names an adjacent lattice
/// pair uniquely, because the grid spacing is <c>[roads] block_tiles</c> and the world already holds
/// it. Carrying both endpoints would be carrying a fact the world has, at the price of a version bump
/// that <em>"would cost every log ever written — including the committed golden baseline"</em>.
/// </para>
/// <para>
/// <b>The kind travels even though only one value is accepted</b>, and that is <c>adr/0070</c>'s
/// discipline rather than speculative generality: an Arterial is <b>refused by name</b>, with its
/// successor written beside the refusal, so a later sitting reads <em>refused-for-now</em> rather than
/// <em>silently missing</em>. Those are different premises and only one of them is evidence.
/// </para>
/// </remarks>
public readonly record struct ConnectPayload(Space.StreetAxis Axis, ConnectAction Action, Space.RoadKind Kind)
{
    /// <summary>Reads a payload out of a <see cref="Command.Zone"/> word.</summary>
    public static ConnectPayload Decode(ushort word) =>
        new(
            (Space.StreetAxis)(word & 1),
            (ConnectAction)((word >> 1) & 1),
            (Space.RoadKind)((word >> 8) & 0xFF));

    /// <summary>Packs this payload into a <see cref="Command.Zone"/> word.</summary>
    public ushort Encode() =>
        (ushort)((int)Axis | ((int)Action << 1) | ((int)Kind << 8));
}

/// <summary>
/// <see cref="CommandKind.Trip"/>'s payload: where the destination is, relative to the origin, in
/// whole blocks of the Street lattice.
/// </summary>
/// <remarks>
/// <para>
/// <b>A delta rather than a second coordinate pair, and the reason is the format version.</b>
/// <c>InputLogCodec</c>'s rule is that a <em>sixth field on a command</em> is what bumps it, and a
/// bump <em>"would cost every log ever written — including the committed golden baseline"</em>. Two
/// absolute Tile pairs are six fields; an origin plus this is four.
/// </para>
/// <para>
/// <b>A signed byte per axis reaches every block on the map, and that is arithmetic rather than
/// luck.</b> The map is 4,096 Tiles a side and <c>[roads] block_tiles</c> ships at 32, so the lattice
/// is <b>128 blocks</b> a side and <c>-128..127</c> spans it whole. <b>It is a property of the
/// Ruleset, not a constant</b> — a coarser <c>block_tiles</c> makes the reach longer in Tiles and a
/// finer one makes it shorter, and at <c>block_tiles = 8</c> the range covers only a quarter of the
/// map. That is the same dependence <see cref="ConnectPayload"/> already carries, stated here because
/// this one can silently under-reach where that one cannot.
/// </para>
/// <para>
/// <b>Blocks rather than Tiles, because a Lot is a property of a block.</b> The subdivider carves a
/// block against its four Street faces, so <em>the Building one block east</em> is a thing the world
/// can resolve and <em>the Building seventeen Tiles east</em> is not. A Tile delta of the same width
/// would reach ±127 Tiles — about four blocks — which does not span a neighbourhood, let alone a city.
/// </para>
/// </remarks>
public readonly record struct TripPayload(sbyte BlocksEast, sbyte BlocksNorth)
{
    /// <summary>Reads a payload out of a <see cref="Command.Zone"/> word.</summary>
    public static TripPayload Decode(ushort word) =>
        new((sbyte)(word & 0xFF), (sbyte)((word >> 8) & 0xFF));

    /// <summary>Packs this payload into a <see cref="Command.Zone"/> word.</summary>
    public ushort Encode() =>
        (ushort)((BlocksEast & 0xFF) | ((BlocksNorth & 0xFF) << 8));
}

/// <summary>
/// <see cref="CommandKind.Arrive"/>'s payload: how many Households present themselves at the gate,
/// and which Life Stage they are.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two bytes in one word, on <see cref="TripPayload"/>'s pattern and for its reason.</b>
/// <c>InputLogCodec</c>'s rule is that a <em>sixth field on a command</em> bumps the format version,
/// and a bump <em>"would cost every log ever written — including the committed golden baseline"</em>.
/// A count and a Life Stage fit the field <see cref="Command.Zone"/> already has.
/// </para>
/// <para>
/// <b>A count, and it is a request rather than an outcome.</b> The gate admits at most
/// <c>[[building]] arrivals_per_day</c> in a Day, so a command asking for more gets what is left —
/// see <see cref="Entities.World.TryArrive"/>. That is the ceiling being observable, and it is
/// exactly what <c>plans/0002</c> §D1 asks of the number.
/// </para>
/// <para>
/// ⚠ <b>The Life Stage is stated by the command because nothing in the build decides one.</b> Who
/// arrives is a property of the Hinterland's willingness ordering, which is milestone <b>16</b>'s
/// comparison (<c>adr/0128</c>); a mix invented here would be that model's shape with no argument
/// behind it. ***An instrument states what it is standing in for, and does not model it.***
/// </para>
/// </remarks>
public readonly record struct ArrivePayload(byte Households, byte LifeStage)
{
    /// <summary>Reads a payload out of a <see cref="Command.Zone"/> word.</summary>
    public static ArrivePayload Decode(ushort word) =>
        new((byte)(word & 0xFF), (byte)((word >> 8) & 0xFF));

    /// <summary>Packs this payload into a <see cref="Command.Zone"/> word.</summary>
    public ushort Encode() => (ushort)(Households | (LifeStage << 8));
}
