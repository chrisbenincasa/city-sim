using System.Reflection;

using Borough.Core.Instruments;

namespace Borough.Tests.Instruments;

/// <summary>
/// Every census family's slot count matches the enum whose members it reserves room for.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>It exists because milestone 25 task 5 added a fourth <see cref="PlacementCounter"/> and left
/// <c>PlacementCounters</c> at 3.</b> The families are laid out end to end in one array — each base
/// is the previous base plus that family's metric count — so a counter written past its family's
/// region ***lands in the NEXT family and is read back as that family's data.*** The symptom was a
/// census reporting <c>shops emigrated 111</c> on a world where nothing creates a shop, and the 111
/// was a trip counter.
/// </para>
/// <para>
/// <b>The failure is silent in both directions and neither is a crash.</b> Too small overwrites a
/// neighbour; too large leaves a permanent hole that reads as a counter stuck at zero — ***and a
/// counter stuck at zero is indistinguishable from a mechanism that never fires***, which is exactly
/// the reading somebody would take from a census.
/// </para>
/// <para>
/// ⚠ <b>The same shape as <c>TableRegistrationTests</c>, found in the same afternoon.</b> A
/// declaration and a hand-maintained number that must agree, with nothing checking that they do. The
/// enum is the source of truth in both cases; the constant is a copy, and ***a copy of a fact is the
/// copy that drifts.***
/// </para>
/// <para>
/// ⚠ <b><c>TripCostCounters</c> is deliberately absent.</b> It sizes histogram <em>buckets</em> rather
/// than reserving room for an enum's members, so there is no declaration for it to disagree with —
/// checking it against something would mean inventing the something.
/// </para>
/// </remarks>
public sealed class CensusFamilySizeTests
{
    public static TheoryData<string, Type> Families =>
        new()
        {
            { "RuleCounters", typeof(RuleCounter) },
            { "ZoneCounters", typeof(ZoneCounter) },
            { "PlacementCounters", typeof(PlacementCounter) },
            { "TripCounters", typeof(TripCounter) },
            { "JobCounters", typeof(JobCounter) },
            { "PolicyCounters", typeof(PolicyCounter) },
            { "MoneyCounters", typeof(MoneyCounter) },
            { "MoneyFlowCounters", typeof(MoneyFlowCounter) },
        };

    [Theory]
    [MemberData(nameof(Families))]
    public void A_familys_slot_count_is_its_enums_member_count(string constant, Type counter)
    {
        FieldInfo field = typeof(Census).GetField(
            constant, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"Census has no constant named {constant}. If it was renamed, rename it here too; if "
                + "the family was removed, remove this row.");

        int reserved = (int)field.GetRawConstantValue()!;
        int declared = Enum.GetValues(counter).Length;

        Assert.True(
            reserved == declared,
            $"Census.{constant} is {reserved} and {counter.Name} has {declared} members. The families "
            + "are laid out end to end in one array, so too small writes into the NEXT family and is "
            + "read back as that family's data, and too large leaves a hole that reads as a counter "
            + "stuck at zero. Set the constant to the member count.");
    }
}
