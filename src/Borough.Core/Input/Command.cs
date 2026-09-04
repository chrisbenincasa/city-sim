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
/// ✅ <b>ALL FOUR ARE NOW APPLIED.</b> This remark read <em>"two of the four are declared and not yet
/// applied — Service needs service Buildings and Govern needs Policy, neither of which exists"</em>
/// for most of the project's life. Govern was applied first; <see cref="Service"/> followed once a
/// <c>[[building]]</c> kind could declare what it is attended for. Declaring them early meant the log
/// format already had their slot, so the artefact a bug report is made of never changed shape.
/// </para>
/// <para>
/// ⚠ <b>That was a claim about the log's <em>shape</em> and never was a claim about its
/// <em>version</em>.</b> This remark used to run on into <em>"and this format version does not have
/// to be bumped for their arrival"</em>, which <c>InputLogCodec</c> states the refutation of in its
/// own file: <em>what would bump it is a sixth field on a command</em>. Whether a verb's arrival is
/// free depends entirely on whether its payload fits the four fields below — <c>Connect</c>'s was
/// made to fit (<c>adr/0077</c>). <b>Both have now been examined against that test and both fit</b>;
/// <c>adr/0118</c> ran Govern's, and <see cref="Service"/>'s own remark runs its — where the answer
/// turned on the catchment not being a payload at all.
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

    /// <summary>
    /// Place a service Building — schools and clinics. <b>The design's one placement exception</b>
    /// (<c>01 §5</c>), and applied since milestone 29.
    /// </summary>
    /// <remarks>
    /// <b>This summary said <em>"place a Building with a catchment"</em> and the catchment is not
    /// part of it.</b> <c>adr/0032</c> demoted service coverage from <b>mechanism</b> to
    /// <b>overlay</b>: a Service reaches people because somebody makes a journey, and what a
    /// catchment describes is composed from that same reachability afterwards. So the verb places a
    /// Building and names no radius — see <see cref="Command.Service"/>, and
    /// <c>Rules.KindDefinition.Serves</c> for why the kind carries no catchment key either.
    /// </remarks>
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

    /// <summary>
    /// Take a Building away. <c>01 §2</c>'s <b>sixth</b> verb and
    /// <c>adr/0091</c>'s, and — unlike <see cref="Populate"/>, <see cref="Trip"/> and
    /// <see cref="Arrive"/> — a verb a player really has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Applied over ABANDONED stock only, and a standing Building is refused by name.</b>
    /// <c>adr/0091</c> settles that every route to clearing <em>occupied</em> ground pays market
    /// value read off the land value Map Layer, and deliberately refuses to choose the composition —
    /// so compulsory purchase is a designed mechanism whose price does not exist, and it is blocked
    /// on the land value target rather than on this verb. Abandoned stock needs no compensation term
    /// because there is nobody left in it to compensate, which is the half that could ship.
    /// ⚠ <b>Refused by name with its successor beside it</b>, on
    /// <see cref="ConnectPayload"/>'s Arterial precedent: <c>adr/0070</c> only lets a later sitting
    /// reason from an absence that was <em>refused</em>, never from one that is silently missing.
    /// </para>
    /// <para>
    /// <b>It is the player's FAST path and not the city's only one</b>
    /// (<c>adr/0172</c>). A shell falls on its own after
    /// <c>[[building]] collapses_after_days</c>, because a player is not a sink and a headless run has
    /// no player in it. What this verb buys is the Lot back <em>sooner</em>, which is a choice rather
    /// than maintenance the simulation could not perform for itself.
    /// </para>
    /// <para>
    /// <b>No payload, and the Tile is matched EXACTLY.</b> There is nothing to say about a demolition
    /// beyond where it is, and <see cref="Command.Zone"/> stays zero. The exactness is
    /// <c>Simulation.GateOn</c>'s narrowing taken for its reason rather than by analogy:
    /// <c>[lots] lots_per_segment</c> is five, so <em>the Building in this block</em> names up to
    /// twenty of them, and a verb that removes a neighbour's house because the click resolved to the
    /// first Lot in the block is worse than one that refuses.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b><c>Connect</c>'s bulldoze flag is NOT superseded here, and <c>adr/0091</c> says it
    /// should be</b> — <em>"one spelling of remove a thing in the Input Log rather than two"</em>.
    /// Retiring <see cref="ConnectAction"/> re-spells six of the committed golden session's seven
    /// <c>connect</c> commands, so it is a change to the baseline artefact rather than to a
    /// mechanism, and it is owed rather than done. ***Two spellings stand today and that is a debt,
    /// not the design.***
    /// </para>
    /// </remarks>
    Demolish = 8,

    /// <summary>
    /// Make the ground and nothing that stands on it — terrain, Woodland, water and the Hazard
    /// Regions. <b><c>adr/0090</c>'s generator remit as a verb</b>, and — like
    /// <see cref="Populate"/> — an instrument rather than one of <c>01 §2</c>'s five.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>IT EXISTS SO THAT THE WORLD THE DESIGN DESCRIBES CAN BE REACHED AT ALL.</b>
    /// <c>adr/0090</c> gives the generator <em>"terrain, Woodland, hazard regions, and the Outside
    /// Connections with their stubs. Nothing else"</em> and gives the player every road, but the
    /// only verb that laid ground was <see cref="Populate"/>, which lays a whole synthetic city
    /// with it. So the reachable worlds were <em>a generated lattice</em> or <em>a bare map</em>,
    /// and the design's own world was neither. <c>SyntheticCity.GroundInto</c> is the split.
    /// </para>
    /// <para>
    /// <b>It is a Command and not a runner switch, for <see cref="Populate"/>'s reason exactly.</b>
    /// Ground that arrived beside <c>Simulation.Apply</c> would be a state change no replay
    /// reproduces and no State Hash divergence explains — and this is the one kind of state where
    /// that would be silent, because a forest and a coastline are not things a readout counts.
    /// </para>
    /// <para>
    /// <b>No payload, and the format version does not move.</b> <c>InputLogCodec.Version</c>'s rule
    /// is that a <em>sixth field</em> bumps it; this adds none. <see cref="Command.East"/>,
    /// <see cref="Command.North"/> and <see cref="Command.Zone"/> all stay zero — the ground's size
    /// and shape are the Ruleset's and the <see cref="WorldKey"/>'s, neither of which is a payload.
    /// </para>
    /// <para>
    /// ⚠ <b>It and <see cref="Populate"/> are ALTERNATIVES rather than a sequence.</b>
    /// <see cref="Populate"/> lays the ground itself, so a world that took this verb refuses the
    /// other one by name. ***A world gets its ground from exactly one verb at Tick 0.***
    /// </para>
    /// <para>
    /// <b>It is NOT expected to be deleted on <see cref="Populate"/>'s terms.</b> The generator
    /// making land is the design rather than a scaffold, so what this verb does outlives the
    /// instrument that first needed it; what is open is whether world creation should be a verb at
    /// all, which is a different question from whether the generator should lay ground.
    /// </para>
    /// </remarks>
    Ground = 9,

    /// <summary>
    /// Raise Buildings, Households and Citizens on whatever Lots already stand.
    /// <b><see cref="Populate"/>'s people half as a verb</b>, and — like it — an instrument rather
    /// than one of <c>01 §2</c>'s five.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A PLAYER-BUILT WORLD COULD NOT BE LIVED IN, AND THE DOOR HAD BEEN BUILT FOR IT SINCE
    /// 2026-08-15.</b> <see cref="Ground"/> made <c>adr/0090</c>'s world reachable, so a player can
    /// lay Streets with <see cref="Connect"/> and carve Lots with <see cref="Zone"/> — and nothing
    /// was ever built on them, because the chain is Households → the Unplaced Pool → placement →
    /// Buildings and ***an empty world has an empty Pool, which is the Pool working rather than
    /// failing***. <c>SyntheticCity.PeopleInto</c> is exactly the half that was missing and had no
    /// caller outside the test suite.
    /// </para>
    /// <para>
    /// <b>It is a Command and not a runner switch, for <see cref="Populate"/>'s reason exactly.</b>
    /// A population arriving beside <c>Simulation.Apply</c> is a state change no replay reproduces
    /// and no State Hash divergence explains.
    /// </para>
    /// <para>
    /// <b>No payload, and the format version does not move.</b> <c>InputLogCodec.Version</c>'s rule
    /// is that a <em>sixth field</em> bumps it; this adds none. The size is
    /// <see cref="WorldConfiguration.Citizens"/>, which the log already states —
    /// <see cref="Populate"/>'s argument, and it applies here unchanged because the two verbs read
    /// the same number.
    /// </para>
    /// <para>
    /// ⚠ <b>It is the LAND that separates it from <see cref="Populate"/> and never the people.</b>
    /// <see cref="Populate"/> lays the ground, the lattice and the Lots and then calls this; a world
    /// that took <see cref="Ground"/> has the ground and owes the rest to the player. So this verb
    /// builds on whatever Lots are standing at the Tick it applies, and a world with none is refused
    /// by name (<c>Refusal.PeopleWorldHasNoLots</c>) rather than populated into nowhere.
    /// </para>
    /// <para>
    /// <b>It is expected to be deleted</b>, on <see cref="Populate"/>'s terms: when a Household can
    /// decide to arrive — milestone 16, <c>adr/0128</c> — this stops being the only thing that puts
    /// anybody in a city the player built.
    /// </para>
    /// </remarks>
    People = 10,
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

    /// <summary>Set a declared Policy's amount — <c>01 §2</c>'s <c>Govern</c>.</summary>
    /// <remarks>
    /// <para>
    /// <b>The Policy is named by its position in declaration order, and that is sound HERE where it
    /// would not be in saved state.</b> A command is applied at a known Tick against the Ruleset in
    /// force at that Tick, and a replay reproduces both — so the index resolves to the same Policy
    /// every time. ***What cannot survive an index is a stored decision***, which is why
    /// <c>Entities.PolicyTable</c> keys its rows by name instead.
    /// </para>
    /// <para>
    /// ⚠ <b><see cref="East"/> carries the amount and <see cref="North"/> is unused</b>, which is a
    /// repurposing of two fields whose names say <em>where</em>. The struct is twelve fully-defined
    /// bytes and widening it would re-spell every committed Input Log, so the payload is packed
    /// exactly as <see cref="ConnectPayload"/> packs into <see cref="Zone"/>. <b>This factory exists so
    /// the packing is named in one place</b> rather than spelled at each call site.
    /// </para>
    /// </remarks>
    /// <param name="policy">Which Policy, by position in declaration order.</param>
    /// <param name="amount">What one application moves from now on.</param>
    public static Command Govern(int policy, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(policy);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(policy, ushort.MaxValue);

        return new Command(CommandKind.Govern, new Tiles(amount), default, (ushort)policy);
    }

    /// <summary>
    /// Place a service Building of this kind on the vacant Lot at this Tile — <c>01 §2</c>'s
    /// <c>Service</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>adr/0118</c> left this verb's payload EXAMINED-NOT-YET, and the examination comes out
    /// clean.</b> That record's own words: <em>"`Service` inherits the method rather than the answer.
    /// Its payload is a Building and a catchment, which is a place and a kind, so it looks like it
    /// fits — but <b>looks like it fits</b> is what this examination existed to replace."</em> It
    /// fits, and the reason is not the one the sentence anticipated: ***there is no catchment in the
    /// payload at all***. <c>adr/0032</c> demoted the catchment from <b>mechanism</b> to
    /// <b>overlay</b> — coverage is composed from the same reachability the Trips use — so the thing
    /// that would not have fitted turned out not to be a payload. <b><see cref="East"/> and
    /// <see cref="North"/> name the Tile and <see cref="Zone"/> carries the kind</b>: no fifth field,
    /// so <c>InputLogCodec.Version</c> does not move.
    /// </para>
    /// <para>
    /// ⚠ <b>The Tile is matched EXACTLY, on <c>Demolish</c>'s reasoning rather than by analogy.</b>
    /// <c>[lots] lots_per_segment</c> is five, so <em>the Lot in this block</em> names up to twenty of
    /// them — and a verb that puts a school on a neighbour's plot because the click resolved to the
    /// first Lot in the block is worse than one that refuses.
    /// </para>
    /// <para>
    /// <b>This is the design's one acknowledged placement exception</b> (<c>01 §5</c>). Pillar 3 is
    /// govern-don't-place, and a fire station appearing wherever the simulation likes is bad play — so
    /// the player places service Buildings and only those. ⚠ <b>What the player still does not control
    /// is staffing</b>, which <c>adr/0026</c> makes demand-determined by catchment and which is
    /// <c>adr/0070</c> <em>unbuilt</em> here: a school employs whatever its kind's <c>jobs</c> says.
    /// </para>
    /// </remarks>
    /// <param name="east">The Tile's eastward coordinate.</param>
    /// <param name="north">The Tile's northward coordinate.</param>
    /// <param name="kind">Which <c>[[building]]</c> kind, by id.</param>
    public static Command Service(Tiles east, Tiles north, byte kind)
    {
        ArgumentOutOfRangeException.ThrowIfZero(kind);

        return new Command(CommandKind.Service, east, north, kind);
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
/// which Life Stage they are, and how many people each one is.
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
/// <para>
/// ⚠ <b>The household size is stated for the same reason and it is a stronger case.</b>
/// <c>CONTEXT.md</c> → Life Stage makes composition — *how many adults, how many children* — a
/// property of the stage, and that table is <c>adr/0011</c>'s and Phase 2's. Until it exists the
/// count is either stated here or invented somewhere that will read as a model: a constant is a
/// hash-bearing number with no ratifier (<c>adr/0052</c>), and the city's own
/// population-to-Household ratio is a stand-in returning plausible results, which is milestone 9's
/// <b>F13</b>. <b>The move-in Trip is what needs it</b> — <c>adr/0075</c> makes a Traveller a cursor
/// over a <em>Citizen's</em> journey, so a Household with no members arrives and then never travels.
/// </para>
/// <para>
/// <b>Four bits each, and the packing is what keeps the format version still.</b> The count of
/// Households keeps the low byte; the Life Stage and the household size split the high one.
/// <see cref="Encode"/> <b>refuses</b> a value that will not fit rather than masking it: a silently
/// truncated field is a command that replays as a different one, and the log would then be a
/// faithful record of a session that never happened.
/// </para>
/// </remarks>
public readonly record struct ArrivePayload(byte Households, byte LifeStage, byte Citizens)
{
    /// <summary>The widest value the four-bit fields carry.</summary>
    public const byte MaxNibble = 0x0F;

    /// <summary>Reads a payload out of a <see cref="Command.Zone"/> word.</summary>
    public static ArrivePayload Decode(ushort word) =>
        new(
            (byte)(word & 0xFF),
            (byte)((word >> 8) & MaxNibble),
            (byte)((word >> 12) & MaxNibble));

    /// <summary>Packs this payload into a <see cref="Command.Zone"/> word.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A four-bit field will not hold its value.</exception>
    public ushort Encode()
    {
        // Refused rather than truncated. A silently masked Life Stage or household size is a command
        // that replays as a different one -- and the log would be a faithful record of a session that
        // never happened, which is the one outcome the single-door discipline exists to prevent.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(LifeStage, MaxNibble, nameof(LifeStage));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Citizens, MaxNibble, nameof(Citizens));

        return (ushort)(Households | (LifeStage << 8) | (Citizens << 12));
    }
}
