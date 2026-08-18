using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Parking;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Space;

namespace Borough.Tests.Movement;

/// <summary>
/// The Trip, the Leg and the Traveller: <b>a plan, its spans, and the cursor executing them</b>.
/// </summary>
public sealed class TripTableTests
{
    /// <summary>A graph, and the two tables that hang off its Segments.</summary>
    private static (RoadGraph Graph, TripTable Trips, LegTable Legs) Fixture()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        return (graph, new TripTable(8, graph.Segments), new LegTable(16, graph.Segments));
    }

    private static Address At(int segment, int offset, StreetSide side) =>
        Address.On(segment, new Tiles(offset), side);

    /// <summary>
    /// A fresh Trip is <b>in flight</b>, not completed — which is what makes <i>no Trip without a
    /// Fate</i> a check rather than a tautology.
    /// </summary>
    [Fact]
    public void ANewTripIsInFlightWithNoFailingLeg()
    {
        (RoadGraph graph, TripTable trips, _) = Fixture();

        Handle<Trip> trip = trips.Create(graph.Segments, TripPurpose.Shopping, At(0, 0, StreetSide.Left), At(2, 32, StreetSide.Left));

        int slot = trips.Rows.Resolve(trip);

        Assert.Equal(TripFate.InFlight, (TripFate)trips.Fate[slot]);
        Assert.Equal(Rows.NoSlot, trips.FailingLeg[slot]);
        Assert.Equal(TripPurpose.Shopping, (TripPurpose)trips.Purpose[slot]);
    }

    /// <summary>An Address survives the round trip through its three columns.</summary>
    /// <remarks>
    /// The columns are split because a <see cref="HandleColumn{TTarget}"/> is the only way the
    /// Segment half reaches the State Hash as a <em>value</em>. What this checks is that splitting it
    /// did not lose the offset or the side on the way through.
    /// </remarks>
    [Fact]
    public void AnAddressSurvivesItsThreeColumns()
    {
        (RoadGraph graph, TripTable trips, _) = Fixture();

        Address origin = At(0, 7, StreetSide.Right);
        Address destination = At(2, 19, StreetSide.Left);

        int slot = trips.Rows.Resolve(trips.Create(graph.Segments, TripPurpose.Shopping, origin, destination));

        Assert.Equal(origin, trips.Origin(slot, graph.Segments));
        Assert.Equal(destination, trips.Destination(slot, graph.Segments));
    }

    /// <summary>
    /// <b>A Trip is an <em>ordered</em> sequence of Legs</b>, and <c>walk → drive → walk</c> read
    /// backwards is a different journey.
    /// </summary>
    [Fact]
    public void LegsWalkInTheOrderTheyWereAppended()
    {
        (RoadGraph graph, TripTable trips, LegTable legs) = Fixture();

        Address door = At(0, 0, StreetSide.Left);
        Address kerb = At(0, 32, StreetSide.Left);
        Address park = At(2, 0, StreetSide.Left);
        Address shop = At(2, 32, StreetSide.Left);

        int trip = trips.Rows.Resolve(trips.Create(graph.Segments, TripPurpose.Shopping, door, shop));

        int[] created =
        [
            legs.Rows.Resolve(legs.Create(graph.Segments, TravelMode.Foot, door, kerb, TravelTime.FromTicks(1))),
            legs.Rows.Resolve(legs.Create(graph.Segments, TravelMode.Car, kerb, park, TravelTime.FromTicks(2))),
            legs.Rows.Resolve(legs.Create(graph.Segments, TravelMode.Foot, park, shop, TravelTime.FromTicks(3))),
        ];

        foreach (int leg in created)
        {
            trips.Append(legs, trip, leg);
        }

        List<TravelMode> walked = [];

        foreach (int leg in trips.LegList(legs).Walk(trip))
        {
            walked.Add((TravelMode)legs.Mode[leg]);
        }

        Assert.Equal([TravelMode.Foot, TravelMode.Car, TravelMode.Foot], walked);
    }

    /// <summary>The Fate and the Leg that caused it are recorded together.</summary>
    [Fact]
    public void ResolveRecordsTheFateAndTheFailingLeg()
    {
        (RoadGraph graph, TripTable trips, _) = Fixture();

        int slot = trips.Rows.Resolve(trips.Create(graph.Segments, TripPurpose.Shopping, At(0, 0, StreetSide.Left), At(2, 32, StreetSide.Left)));

        trips.Resolve(slot, TripFate.NoRouteFound, failingLeg: 1);

        Assert.Equal(TripFate.NoRouteFound, (TripFate)trips.Fate[slot]);
        Assert.Equal(1, trips.FailingLeg[slot]);
    }

    /// <summary>
    /// A Trip ends once. <b>The second Fate would overwrite the first, and the first is the true
    /// one.</b>
    /// </summary>
    [Fact]
    public void ATripCannotEndTwice()
    {
        (RoadGraph graph, TripTable trips, _) = Fixture();

        int slot = trips.Rows.Resolve(trips.Create(graph.Segments, TripPurpose.Shopping, At(0, 0, StreetSide.Left), At(2, 32, StreetSide.Left)));

        trips.Resolve(slot, TripFate.Completed);

        Assert.Throws<InvalidOperationException>(() => trips.Resolve(slot, TripFate.Stranded));
    }

    /// <summary>A Trip is resolved to an outcome, never back to in flight.</summary>
    [Fact]
    public void ATripCannotBeResolvedBackToInFlight()
    {
        (RoadGraph graph, TripTable trips, _) = Fixture();

        int slot = trips.Rows.Resolve(trips.Create(graph.Segments, TripPurpose.Shopping, At(0, 0, StreetSide.Left), At(2, 32, StreetSide.Left)));

        Assert.Throws<ArgumentOutOfRangeException>(() => trips.Resolve(slot, TripFate.InFlight));
    }

    /// <summary>
    /// ⚠ <b>A Leg outlives its Segment, and its Address becomes a named absence rather than a stale
    /// reference</b> (<c>adr/0079</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The endpoint columns are <see cref="Reference.Severable"/>, so a road bulldozed under a
    /// planned Leg is state the design models rather than a break in referential integrity — declared
    /// <c>Required</c>, the whole-world handle walk would report every Trip caught by a demolition as
    /// corruption, which is a diagnostic that cries wolf on the commonest event in a city that
    /// condemns Buildings.
    /// </para>
    /// <para>
    /// <b>What the read returns is <see cref="Address.None"/> and not a throw</b>, which is the whole
    /// point of resolving at the boundary: the caller turns it into
    /// <see cref="TripFate.Stranded"/> at the one place that can tell the difference between a road
    /// that never existed and a road that has just gone.
    /// </para>
    /// </remarks>
    [Fact]
    public void ALegWhoseSegmentIsBulldozedReadsAsNoAddress()
    {
        (RoadGraph graph, _, LegTable legs) = Fixture();

        Address doomed = At(0, 4, StreetSide.Left);
        Address elsewhere = At(2, 4, StreetSide.Left);

        int slot = legs.Rows.Resolve(
            legs.Create(graph.Segments, TravelMode.Foot, doomed, elsewhere, TravelTime.FromTicks(1)));

        Assert.True(legs.From(slot, graph.Segments).Exists);

        graph.Segments.Rows.Free(graph.Segments.Rows.At(0));

        Assert.Equal(Address.None, legs.From(slot, graph.Segments));
        Assert.False(legs.From(slot, graph.Segments).Exists);

        // The other end is untouched — a severed Leg is not a destroyed one, and the Trip is still
        // reportable, which is the reason a Trip is an object at all.
        Assert.Equal(elsewhere, legs.To(slot, graph.Segments));
    }

    /// <summary>The cursor walks the plan and reports when there is no more of it.</summary>
    [Fact]
    public void ATravellerAdvancesThroughItsLegsAndThenArrives()
    {
        (RoadGraph graph, TripTable trips, LegTable legs) = Fixture();

        LotTable lots = new(4);
        BuildingTable buildings = new(4, lots);
        HouseholdTable households = new(4, buildings);
        CarParkTable carParks = new(4, buildings, graph.Segments);
        CitizenTable citizens = new(4, households, buildings, carParks);
        TravellerTable travellers = new(4, citizens, trips);

        Address door = At(0, 0, StreetSide.Left);
        Address shop = At(2, 32, StreetSide.Left);

        Handle<Trip> trip = trips.Create(graph.Segments, TripPurpose.Shopping, door, shop);
        int tripSlot = trips.Rows.Resolve(trip);

        int first = legs.Rows.Resolve(legs.Create(graph.Segments, TravelMode.Foot, door, shop, TravelTime.FromTicks(1)));
        int second = legs.Rows.Resolve(legs.Create(graph.Segments, TravelMode.Foot, shop, door, TravelTime.FromTicks(2)));

        trips.Append(legs, tripSlot, first);
        trips.Append(legs, tripSlot, second);

        Handle<Citizen> citizen = citizens.Rows.Allocate();
        int traveller = travellers.Rows.Resolve(
            travellers.Create(citizen, trip, first, new Ticks(10)));

        Assert.True(travellers.Advance(traveller, legs, new Ticks(20)));
        Assert.Equal(second, travellers.CurrentLeg[traveller]);
        Assert.Equal(new Ticks(20), travellers.ArrivesAt[traveller]);

        Assert.False(travellers.Advance(traveller, legs, new Ticks(30)));
    }

    /// <summary>Across the street is one Segment and two sides, and nothing else.</summary>
    [Fact]
    public void AcrossTheStreetIsOneSegmentAndTwoSides()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        Address left = At(0, 4, StreetSide.Left);
        Address right = At(0, 20, StreetSide.Right);
        Address sameSide = At(0, 20, StreetSide.Left);
        Address otherStreet = At(1, 20, StreetSide.Right);

        Assert.True(left.AcrossTheStreetFrom(right));
        Assert.False(left.AcrossTheStreetFrom(sameSide));
        Assert.False(left.AcrossTheStreetFrom(otherStreet));
        Assert.False(Address.None.AcrossTheStreetFrom(right));
    }
}
