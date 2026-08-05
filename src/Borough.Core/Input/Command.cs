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
/// <b>Three of the four are declared and not yet applied</b>, which is deliberate in the way the
/// phase list is. Connect needs a Road Graph, Service needs service Buildings and Govern needs
/// Policy — none of which exist before slice 7. Declaring them now means the log format already has
/// their slot, so the artefact a bug report is made of does not change shape when they arrive.
/// </para>
/// </remarks>
public enum CommandKind : ushort
{
    /// <summary>
    /// Reserved, and never a valid command. A zeroed <see cref="Command"/> must be recognisable
    /// rather than reading as whichever verb happened to be declared first.
    /// </summary>
    None = 0,

    /// <summary>Paint a permission set over land. The only verb slice 5 applies.</summary>
    Zone = 1,

    /// <summary>Lay Streets, draw Arterials, place Junction pieces. Slice 7 and the S2 spike.</summary>
    Connect = 2,

    /// <summary>Place a Building with a catchment — the design's one placement exception.</summary>
    Service = 3,

    /// <summary>Set taxes, funding, transfers, constraints. Every Policy is a Rule.</summary>
    Govern = 4,
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
    /// The permission set a <see cref="CommandKind.Zone"/> paints.
    /// </summary>
    /// <remarks>
    /// <b>A set, not a kind</b> — <c>01 §2</c>'s verb paints a <em>permission set</em>, and a Lot
    /// permitting two uses is a real thing the design wants. It is carried at full width here and
    /// narrowed to <see cref="Entities.LotTable"/>'s single zone byte on application, because Lots do
    /// not have a permission column until Zone Rules arrive in slice 10. Widening the Lot is that
    /// slice's job; losing the authored value on the way in would have been this one's bug.
    /// </remarks>
    public ushort Zone { get; }

    /// <summary>Where, eastward.</summary>
    public Tiles East { get; }

    /// <summary>Where, northward.</summary>
    public Tiles North { get; }
}
