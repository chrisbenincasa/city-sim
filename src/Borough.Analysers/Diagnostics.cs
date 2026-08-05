using Microsoft.CodeAnalysis;

namespace Borough.Analysers;

/// <summary>
/// Every diagnostic this assembly can report, in one place.
/// </summary>
/// <remarks>
/// <para>
/// <b>The id encodes the rule.</b> <c>BOR0<i>r</i><i>nn</i></c>, where <i>r</i> is the CI lint number
/// in <c>docs/05 §4</c> — every <c>BOR02xx</c> is lint 2, every <c>BOR03xx</c> is lint 3, every
/// <c>BOR07xx</c> is lint 7. <c>BOR08xx</c> is the <c>purpose_tag</c> row of the same section's
/// banned-construct table, which is stated as a build-time check but is not one of the seven
/// numbered lints.
/// </para>
/// <para>
/// <b>Every message names its rule and the document that states it, and this is not decoration.</b>
/// A diagnostic reading <c>float in Core</c> teaches nothing. The moment somebody hits one of these
/// is very nearly the only moment they will read a design document about arithmetic, so the message
/// has to be the document. The messages below are long on purpose.
/// </para>
/// <para>
/// <b>Every one is an error, never a warning.</b> These rules fail silently — that is the entire
/// reason they are enforced mechanically rather than by review — and a warning in a codebase with
/// warnings is a rule that has been repealed without anybody saying so. adr/0002 and adr/0036 both
/// name a lint being *waived* as their own revisit trigger, which is the corpus admitting in advance
/// that the soft form does not hold.
/// </para>
/// </remarks>
internal static class Diagnostics
{
    private const string Determinism = "Borough.Determinism";
    private const string SimulationState = "Borough.SimulationState";

    private static DiagnosticDescriptor Error(
        string id, string category, string title, string message, string description) =>
        new(id, title, message, category, DiagnosticSeverity.Error,
            isEnabledByDefault: true, description: description);

    // ---- Lint 2: no floating point in simulation state or arithmetic --------------------------

    internal static readonly DiagnosticDescriptor FloatingPoint = Error(
        "BOR0201", Determinism,
        "Floating point in Borough.Core",
        "{0} is floating point. 05 §4 and 02 §8 rule 1 ban float, double and decimal in the core — " +
        "in any arithmetic, not merely in stored state. Use an integer, or Q16.16 through " +
        "Borough.Core.Arithmetic.Fixed.",
        "The rule was widened deliberately. Its old wording said 'in simulation state', which " +
        "permitted `int r = (int)(a * 1.5f)` — a float temporary that is exactly as " +
        "non-deterministic as a stored one, via x87 80-bit intermediates, FMA contraction and " +
        "differing SIMD widths. A reflection test over fields cannot see a temporary, which is why " +
        "this is an analyser.");

    internal static readonly DiagnosticDescriptor MathMember = Error(
        "BOR0202", Determinism,
        "System.Math in Borough.Core",
        "'{0}' is a System.Math member. 05 §4 bans every Math.* in the core: the JIT is free to " +
        "select different intrinsics on different machines for the same call. Use the tabulated " +
        "fixed-point Borough.Core.Arithmetic.Transcendental (adr/0038), or an integer helper in " +
        "Borough.Core.Arithmetic.IntegerMath.",
        "adr/0003 promoted tabulated transcendentals from a contingency to a required core " +
        "component, and adr/0038 fixed the table's resolution as a hash-bearing world constant. " +
        "A Math.* call is not a faster route to the same number; it is a different number.");

    internal static readonly DiagnosticDescriptor RawDivision = Error(
        "BOR0203", Determinism,
        "Raw integer division in Borough.Core",
        "raw '/' on {0}. 05 §4: C# truncates toward zero, so -7/2 == -3 while 7/2 == 3 — a " +
        "directional bias at every zero crossing, silently applied. State the rounding: " +
        "IntegerMath.FloorDiv, CeilDiv or RoundDiv, or Fixed.Div for Q16.16.",
        "This is an error and not a warning by a judgement recorded in plans/0006: a division whose " +
        "operands are provably non-negative has no zero crossing to be biased at, so a warning is " +
        "defensible. It is rejected because the helper is only load-bearing if it is the only " +
        "spelling, and a lint that is sometimes right is a lint that gets suppressed at the site " +
        "where it was right.");

    internal static readonly DiagnosticDescriptor NonConstantShift = Error(
        "BOR0204", Determinism,
        "Shift by a non-constant count in Borough.Core",
        "shift by a non-constant count. 05 §4: C# masks the count against the operand width, so " +
        "'x << 32' silently means 'x << 0' rather than zero. Use IntegerMath.ShiftLeft or " +
        "IntegerMath.ShiftRight, which range-check it.",
        "A constant count is legal and is not reported: it is visible in the source, checkable by " +
        "eye, and cannot vary at runtime. The masking hazard only exists when the count is " +
        "computed.");

    internal static readonly DiagnosticDescriptor WallClock = Error(
        "BOR0205", Determinism,
        "Wall-clock time in Borough.Core",
        "'{0}' reads wall-clock time. 05 §4: the host decides when to advance and the library has " +
        "no clock. Use the u64 Tick counter — Borough.Core.Quantities.Ticks.",
        "A simulation that can read the clock has an input the Input Log does not carry, so its " +
        "replay is not a replay. This is the same class of defect as System.Random and it is " +
        "harder to notice, because a clock read usually looks like instrumentation.");

    internal static readonly DiagnosticDescriptor UnstableIdentity = Error(
        "BOR0206", Determinism,
        "Process-unstable identity in Borough.Core",
        "'{0}' is not stable across processes. 05 §4 and adr/0003: Guid.NewGuid is entropy the " +
        "Input Log does not carry; the default object/ValueType GetHashCode is derived from the " +
        "address or the layout; and string.GetHashCode and System.HashCode are seeded from process " +
        "entropy at start-up. Use a declared id, or a hash function written out in the core — " +
        "Randomness.Mix is one.",
        "The hashes are the subtle half. They compile, they are spelled like hashes, and they " +
        "return a different number for the same logical value in the next process — which turns " +
        "any structure keyed on one into a per-run reordering. Randomised string hashing is the " +
        "same hazard BOR0301 exists to prevent, so letting its direct spelling through would have " +
        "been the rule contradicting itself.");

    // ---- Lint 3: no hash-map enumeration, no System.Random ------------------------------------

    internal static readonly DiagnosticDescriptor HashMapEnumeration = Error(
        "BOR0301", Determinism,
        "Enumerating a hash map in Borough.Core",
        "enumerating a {0}. 05 §4 lint 3: .NET randomises string hashing per process, so this " +
        "order differs between runs of the same binary. Building one and looking up in it is " +
        "legal — walking it is not. Build the ordered structure from the ordered *source* — the " +
        "Ruleset's own document order, or a dense array indexed by handle — and keep the map as a " +
        "lookup index only.",
        "Note the failure is per-*process*, not per-machine, so it will not reproduce for whoever " +
        "is debugging it. SortedDictionary and SortedSet are deliberately absent from the banned " +
        "set: their order is defined by the comparer rather than by the hash. There is deliberately " +
        "no sanctioned route from a hash map to a sorted array — `.Keys.OrderBy(…)` is reported too " +
        "— because sorting only restores determinism when the sort key is total, which an analyser " +
        "cannot check and a reader will not notice is missing. Order that matters comes from the " +
        "source, never from a rescue at the end.");

    internal static readonly DiagnosticDescriptor SystemRandom = Error(
        "BOR0302", Determinism,
        "System.Random in Borough.Core",
        "'{0}' is System.Random. 05 §4 lint 3: its algorithm changed in .NET 6 and is not " +
        "documented as stable. Use Randomness.Draw(world, entityId, tick, purpose).",
        "Ours is a format, not an implementation detail — an Input Log reproduces a run only if " +
        "every draw is bit-identical, so changing the draw function is a save-format-class change " +
        "under 05 §7. A shared mutable stream also makes every draw depend on evaluation order, " +
        "which is the coordination cost counter-based randomness exists to avoid.");

    // ---- Lint 7: no reference types in simulation state ---------------------------------------

    internal static readonly DiagnosticDescriptor ManagedState = Error(
        "BOR0701", SimulationState,
        "Reference type in Borough.Core simulation state",
        "'{0}' holds a reference ({1}), so '{2}' does not satisfy the unmanaged constraint. " +
        "05 §4 lint 7 and adr/0036. Every variable-length collection in the core is an intrusive " +
        "index list — a head index on the owner, a 'next' index on the element, both in flat " +
        "arrays, never a per-entity collection object. If this type is genuinely cold — it runs on " +
        "a click and never inside a Tick — mark it [ColdPath(\"why\")].",
        "The target is not the tables. Arrays of unmanaged structs are opaque to the GC, so the " +
        "tables were never at risk. The risk is the three derived, variable-length, per-entity " +
        "structures — per-Bin wait lists, cached Parking Sheds, Event Wheel buckets — which as " +
        "collection objects are on the order of a million long-lived traced references at the 1M " +
        "target. S4's K6 measured that difference as 6.984 ms against 100.200 ms.");

    // ---- The purpose_tag row of 05 §4's banned-construct table --------------------------------

    internal static readonly DiagnosticDescriptor DuplicatePurposeTag = Error(
        "BOR0801", Determinism,
        "Duplicate purpose_tag value",
        "'{0}' has the same value as '{1}'. 05 §4 and adr/0003: two mechanisms drawing under one " +
        "tag are the same coin flip, always. Give it its own value — append, never renumber.",
        "Nothing at runtime can catch this. The correlation it creates produces no exception, no " +
        "assertion failure and no statistical outlier a playtest would read as a bug — a Citizen " +
        "who chooses a job badly also chooses a shop badly, and the city it produces is plausible. " +
        "A build-time check over the enum is the only possible detector, which is why the corpus " +
        "states it as build-time rather than as a test.");

    internal static readonly DiagnosticDescriptor PurposeTagZeroReserved = Error(
        "BOR0802", Determinism,
        "Zero is reserved for PurposeTag.None",
        "'{0}' has the value 0, which is reserved for PurposeTag.None. A default-initialised " +
        "PurposeTag field must be recognisable as unset rather than silently meaning the first " +
        "purpose anybody declared.",
        "A zeroed struct field is the normal state of a row that has not been written yet. If 0 is " +
        "a real tag, every such row draws under it.");

    internal static readonly DiagnosticDescriptor PurposeTagUnderlyingType = Error(
        "BOR0803", Determinism,
        "PurposeTag must be backed by ulong",
        "PurposeTag is backed by '{0}'. The tag is added into a u64 draw coordinate, so it must be " +
        "declared ': ulong' — a narrower or signed backing type can carry a sign extension into " +
        "that addition.",
        "adr/0003 writes the draw function out normatively over unsigned 64-bit coordinates. The " +
        "backing type is part of that signature, not a storage preference.");
}
