using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

public readonly record struct ShoppingReading(int Outings, int Searches, int Purchases, long Bought, long Delivered, long Lost);

public sealed class ShoppingEngine
{
    private readonly World _world;
    private readonly TripEngine _trips;
    private readonly CommuteEngine _commutes;
    private readonly Dictionary<ulong, int> _byHousehold = new();
    private readonly WalkScratch _walk = new();
    private ulong[] _order = [];
    private int[] _active = [];
    private ShoppingReading _reading;
    public ShoppingReading Last => _reading;
    private ShoppingTable State => _world.Shopping;
    private KnownShopTable Known => _world.KnownShops;

    public ShoppingEngine(World world, TripEngine trips, CommuteEngine commutes)
    {
        _world = world;
        _trips = trips;
        _commutes = commutes;
        for (int row = 0; row < State.Rows.SlotCount; row++)
        {
            if (State.Rows.IsLive(row) && world.Households.Rows.TryResolve(State.Household[row], out int hh))
            { _byHousehold[world.Households.Rows.IdAt(hh)] = row; }
        }
    }

    public static bool IsReplenishment(World world, int instance)
    {
        if (!world.Rules.Shopping.Runs || !world.Households.Rows.TryResolve(world.RuleInstances.Household[instance], out _))
        { return false; }
        foreach (Term term in world.Rules.Inputs(world.RuleInstances.Rule[instance]))
        { if (term.Bin.Scope == Scope.Pool) { return true; } }
        return false;
    }

    public void Step(Ticks tick)
    {
        _reading = default;
        int count = State.Rows.SlotCount;
        if (_active.Length < count) { _active = new int[count]; _order = new ulong[count]; }
        int active = 0;
        for (int row = 0; row < count; row++)
        {
            if (!State.Rows.IsLive(row)) { continue; }
            if (!_world.Households.Rows.TryResolve(State.Household[row], out _))
            { Retire(row); continue; }
            if (State.Stage[row] == 0) { continue; }
            _active[active] = row;
            _order[active++] = Randomness.Draw(_world.Key, State.Rows.IdAt(row), tick, PurposeTag.ShoppingSettleOrder);
        }
        _order.AsSpan(0, active).Sort(_active.AsSpan(0, active));
        for (int i = 0; i < active; i++) { Continue(_active[i], tick); }
        if (!_world.Rules.Shopping.Runs) { return; }
        int interval = _world.Rules.Shopping.Interval;
        for (int hh = (int)(tick.Raw % (ulong)interval); hh < _world.Households.Rows.SlotCount; hh += interval)
        {
            if (!_world.Households.Rows.IsLive(hh)
                || !_world.Buildings.Rows.TryResolve(_world.Households.Dwelling[hh], out int home)) { continue; }
            ulong id = _world.Households.Rows.IdAt(hh);
            if (!_byHousehold.TryGetValue(id, out int row))
            {
                row = State.Rows.Resolve(State.Rows.Allocate());
                State.Household[row] = _world.Households.Rows.At(hh);
                _byHousehold[id] = row;
            }
            if (State.Stage[row] != 0 || tick < State.NextAt[row]) { continue; }
            Consider(row, hh, home, tick);
        }
    }

    private void Consider(int row, int hh, int home, Ticks tick)
    {
        ShoppingRuleset rules = _world.Rules.Shopping;
        int chosen = -1;
        long chosenDaily = 0;
        long chosenLevel = 0;
        foreach (int instance in _world.BuildingRules.Walk(home))
        {
            if (_world.RuleInstances.Household[instance] != State.Household[row] || !IsReplenishment(_world, instance)) { continue; }
            RuleId rule = _world.RuleInstances.Rule[instance];
            ResourceId good = _world.Rules.Inputs(rule)[0].Bin.Resource;
            int bin = _world.FindLocalBin(instance, good);
            long daily = DailyUse(hh, home, good);
            if (bin < 0 || daily <= 0) { continue; }
            long level = _world.Bins.LevelAt(bin);
            if (level >= daily * rules.LowDays || level >= _world.Bins.Capacity[bin]) { continue; }
            if (chosen >= 0 && level * chosenDaily >= chosenLevel * daily) { continue; }
            chosen = bin; chosenDaily = daily; chosenLevel = level;
        }
        if (chosen < 0) { State.UnservedSince[row] = default; return; }
        State.Good[row] = _world.Bins.Resource[chosen];
        if (State.UnservedSince[row].Raw == 0) { State.UnservedSince[row] = tick + new Ticks(1); }
        bool severe = _world.Households.Sustenance[hh] <= rules.SevereNeed;
        int citizen = -1;
        foreach (int member in _world.Members.Walk(hh))
        {
            if ((_world.Rules.DeclaresLifeStages && _world.Citizens.Age[member] == 0)
                || (CitizenActivity)_world.Citizens.Activity[member] != CitizenActivity.AtHome) { continue; }
            if (!severe && WorkSchedule.OnDuty(_world, member, tick)) { continue; }
            citizen = member; break;
        }
        if (citizen < 0) { return; }
        State.Good[row] = _world.Bins.Resource[chosen];
        long target = chosenDaily * rules.TargetDays;
        target = Min(target, _world.Bins.Capacity[chosen]);
        State.Wanted[row] = target - chosenLevel;
        State.Severe[row] = severe ? (byte)1 : (byte)0;
        State.Citizen[row] = _world.Citizens.Rows.At(citizen);
        Discover(row, home, citizen, tick);
        int provider = Choose(row, tick, -1);
        if (provider < 0)
        {
            Failure(row, ShoppingFailure.NoKnownShop); CountFailure(row);
            State.NextAt[row] = tick + new Ticks((ulong)rules.RetryTicks);
            return;
        }
        int shop = _world.Businesses.Rows.Resolve(Known.Business[provider]);
        int building = _world.Buildings.Rows.Resolve(_world.Businesses.Building[shop]);
        TravelTime cost = Cost(home, building, citizen);
        if (cost.IsImpassable || !_world.Rules.Trips.WithinBudget(cost))
        { Failure(row, ShoppingFailure.Unreachable); CountFailure(row); AdvanceCursor(row, provider); return; }
        if (!severe && !HasTime(citizen, tick, cost)) { return; }
        State.Attempts[row] = 1;
        State.Seller[row] = Known.Business[provider];
        State.Cursor[row] = provider + 1;
        State.Place[row] = _world.Buildings.Rows.At(home);
        State.Stage[row] = 1;
        _reading = _reading with { Outings = _reading.Outings + 1 };
        Travel(row, home, building, tick);
    }

    private long DailyUse(int hh, int home, ResourceId good)
    {
        long daily = 0;
        foreach (int instance in _world.BuildingRules.Walk(home))
        {
            if (_world.RuleInstances.Household[instance] != _world.Households.Rows.At(hh)) { continue; }
            RuleId id = _world.RuleInstances.Rule[instance];
            RuleDefinition rule = _world.Rules.Rule(id);
            if (rule.IsTerminal || rule.Rate == 0 || IsReplenishment(_world, instance)) { continue; }
            long net = 0;
            foreach (Term term in _world.Rules.Inputs(id))
            { if (term.Bin.Scope == Scope.Local && term.Bin.Resource == good) { net += term.Amount; } }
            foreach (Term term in _world.Rules.Outputs(id))
            { if (term.Bin.Scope == Scope.Local && term.Bin.Resource == good) { net -= term.Amount; } }
            if (net > 0) { daily += IntegerMath.CeilDiv(net * rule.Apply.Min * Ticks.PerDay, rule.Rate); }
        }
        return daily;
    }

    private bool HasTime(int citizen, Ticks tick, TravelTime cost)
    {
        if (!_world.Businesses.Rows.TryResolve(_world.Citizens.Workplace[citizen], out _)) { return true; }
        if (!CommuteRoster.TryPhasesOf(_world.Citizens, _world.Buildings, _world.Businesses, _world.Rules,
                _world.Key, citizen, out int departure, out _)) { return true; }
        ulong phase = tick.Raw % Ticks.PerDay;
        ulong until = ((ulong)departure + Ticks.PerDay - phase) % Ticks.PerDay;
        Ticks next = tick + new Ticks(until);
        return !WorkSchedule.DepartsToday(_world, citizen, next) || until > cost.ToTicksFloor().Raw * 2 + 2;
    }

    private void Discover(int row, int home, int citizen, Ticks tick)
    {
        int previous = -1;
        int count = 0;
        for (int p = State.ProviderHead[row] - 1; p >= 0;)
        {
            int next = Known.Next[p] - 1;
            if (!_world.Businesses.Rows.TryResolve(Known.Business[p], out _)
                || count >= _world.Rules.Shopping.KnownShops)
            {
                if (previous < 0) { State.ProviderHead[row] = next + 1; } else { Known.Next[previous] = next + 1; }
                if (State.Cursor[row] == p + 1) { State.Cursor[row] = 0; }
                Known.Rows.Free(Known.Rows.At(p));
            }
            else { count++; previous = p; }
            p = next;
        }
        // A full list that repeatedly failed makes room for one newly heard-of shop.
        if (count >= _world.Rules.Shopping.KnownShops && State.Failed[row] >= count && previous >= 0)
        {
            int head = State.ProviderHead[row] - 1;
            State.ProviderHead[row] = Known.Next[head];
            if (State.Cursor[row] == head + 1) { State.Cursor[row] = 0; }
            Known.Rows.Free(Known.Rows.At(head)); count--;
        }
        if (count >= _world.Rules.Shopping.KnownShops || !_world.Lots.Rows.TryResolve(_world.Buildings.Lot[home], out int lot)) { return; }
        Cells radius = EmploymentEngine.Radius(_world.Rules.Trips.CommuteBudget, _world.Rules.Roads.WalkSpeed);
        CellRect box = CellRect.At(CellGrid.ToCells(_world.Lots.East[lot]), CellGrid.ToCells(_world.Lots.North[lot])).Dilate(radius).Clamp();
        int buildings = _world.BuildingsInCells.CountIn(box);
        if (buildings == 0) { return; }
        for (int attempt = 0; attempt < _world.Rules.Shopping.SearchCandidates && count < _world.Rules.Shopping.KnownShops; attempt++)
        {
            ulong entity = Randomness.Mix(State.Rows.IdAt(row) ^ ((ulong)(uint)attempt << 32));
            ulong draw = Randomness.Draw(_world.Key, entity, tick, PurposeTag.ShoppingDiscovery);
            int building = _world.BuildingsInCells.NthIn(box, _world.Buildings, (int)(draw % (ulong)buildings));
            if (building < 0) { continue; }
            foreach (int business in _world.BuildingBusinesses.Walk(building))
            {
                if (StockBin(business, State.Good[row]) < 0 || Knows(row, business)) { continue; }
                TravelTime cost = Cost(home, building, citizen);
                if (cost.IsImpassable || !_world.Rules.Trips.WithinBudget(cost)) { continue; }
                int entry = Known.Rows.Resolve(Known.Rows.Allocate());
                Known.Business[entry] = _world.Businesses.Rows.At(business);
                if (count == 0) { State.ProviderHead[row] = entry + 1; }
                else
                {
                    int tail = State.ProviderHead[row] - 1;
                    while (Known.Next[tail] != 0) { tail = Known.Next[tail] - 1; }
                    Known.Next[tail] = entry + 1;
                }
                count++;
                if (count >= _world.Rules.Shopping.KnownShops) { break; }
            }
        }
    }

    private bool Knows(int row, int business)
    {
        for (int p = State.ProviderHead[row] - 1; p >= 0; p = Known.Next[p] - 1)
        { if (Known.Business[p] == _world.Businesses.Rows.At(business)) { return true; } }
        return false;
    }

    private int Choose(int row, Ticks tick, int excluded)
    {
        int head = State.ProviderHead[row] - 1;
        int start = State.Cursor[row] > 0 ? State.Cursor[row] - 1 : head;
        if (start < 0) { return -1; }
        int p = start;
        do
        {
            if (_world.Businesses.Rows.TryResolve(Known.Business[p], out int business) && business != excluded
                && _world.Rules.DeclaresBusiness(_world.Businesses.Kind[business])
                && _world.Rules.BusinessKind(_world.Businesses.Kind[business]).ShopHours.IsOpen(tick)
                && StockBin(business, State.Good[row]) >= 0
                && _world.Buildings.Rows.TryResolve(_world.Businesses.Building[business], out _)) { return p; }
            p = Known.Next[p] != 0 ? Known.Next[p] - 1 : head;
        } while (p >= 0 && p != start);
        return -1;
    }

    private void Continue(int row, Ticks tick)
    {
        if (!_world.Citizens.Rows.TryResolve(State.Citizen[row], out int citizen))
        {
            _reading = _reading with { Lost = _reading.Lost + State.Cargo[row] };
            State.Cargo[row] = 0; State.Stage[row] = 0;
            Failure(row, ShoppingFailure.LostCargo); return;
        }
        if ((CitizenActivity)_world.Citizens.Activity[citizen] == CitizenActivity.ShoppingTravelling) { return; }
        if (tick < State.NextAt[row]) { return; }
        int hh = _world.Households.Rows.Resolve(State.Household[row]);
        bool arrived = (TripFate)_world.Citizens.LastTripFate[citizen] == TripFate.Completed;
        if (State.Stage[row] == 1)
        {
            int business = _world.Businesses.Rows.TryResolve(State.Seller[row], out int b) ? b : -1;
            if (arrived) { State.Place[row] = State.Destination[row]; }
            if (arrived && business >= 0 && _world.Buildings.Rows.TryResolve(_world.Businesses.Building[business], out int shop))
            {
                State.Place[row] = _world.Buildings.Rows.At(shop);
                Purchase(row, hh, business, tick);
            }
            else { Failure(row, ShoppingFailure.Unreachable); }
            if (State.Cargo[row] == 0 && State.Severe[row] != 0 && State.Attempts[row] < 2)
            {
                int next = Choose(row, tick, business);
                if (next >= 0 && _world.Buildings.Rows.TryResolve(State.Place[row], out int from))
                {
                    int seller = _world.Businesses.Rows.Resolve(Known.Business[next]);
                    int to = _world.Buildings.Rows.Resolve(_world.Businesses.Building[seller]);
                    State.Attempts[row]++;
                    State.Cursor[row] = next + 1;
                    State.Seller[row] = Known.Business[next];
                    Travel(row, from, to, tick); return;
                }
            }
            if (State.Cargo[row] == 0) { CountFailure(row); AdvanceCursor(row, State.Cursor[row] - 1); }
            State.Stage[row] = 3;
        }
        else if (State.Stage[row] == 2)
        {
            if (arrived) { State.Place[row] = State.Destination[row]; State.Stage[row] = 4; }
            else
            {
                Failure(row, ShoppingFailure.Unreachable); State.Stage[row] = 3;
                State.NextAt[row] = tick + new Ticks((ulong)RetryTicks()); return;
            }
        }
        if (!_world.Buildings.Rows.TryResolve(_world.Households.Dwelling[hh], out int home))
        { State.NextAt[row] = tick + new Ticks((ulong)RetryTicks()); return; }
        if (State.Stage[row] == 4)
        {
            // If the Household moved during the return, carry on to its new home.
            if (State.Place[row] != _world.Buildings.Rows.At(home)) { State.Stage[row] = 3; }
            else
            {
                int bin = OwnerBin(_world.Households.BinHead[hh], State.Good[row]);
                if (State.Cargo[row] > 0 && bin < 0)
                {
                    _reading = _reading with { Lost = _reading.Lost + State.Cargo[row] };
                    State.Cargo[row] = 0; Failure(row, ShoppingFailure.LostCargo);
                }
                if (State.Cargo[row] > 0 && bin >= 0)
                {
                    long amount = Min(State.Cargo[row], _world.Bins.Capacity[bin] - _world.Bins.LevelAt(bin));
                    if (amount > 0)
                    {
                        _world.Deposit(_world.Bins.Rows.At(bin), amount, tick);
                        State.Cargo[row] -= amount;
                        _reading = _reading with { Delivered = _reading.Delivered + amount };
                    }
                }
                if (State.Cargo[row] > 0) { State.NextAt[row] = tick + new Ticks((ulong)RetryTicks()); return; }
                State.Stage[row] = 0;
                State.NextAt[row] = tick + new Ticks((ulong)RetryTicks());
                _world.Citizens.Activity[citizen] = (byte)CitizenActivity.AtHome;
                _commutes.Resume(citizen, tick); return;
            }
        }
        if (State.Stage[row] == 3 && !_world.Buildings.Rows.TryResolve(State.Place[row], out _))
        {
            _reading = _reading with { Lost = _reading.Lost + State.Cargo[row] };
            State.Cargo[row] = 0; State.Stage[row] = 0;
            Failure(row, ShoppingFailure.LostCargo);
            State.NextAt[row] = tick + new Ticks((ulong)RetryTicks());
            _world.RecordTripFate(citizen, TripFate.Stranded);
            _world.Citizens.Activity[citizen] = (byte)CitizenActivity.AtHome;
            return;
        }
        if (State.Stage[row] == 3 && _world.Buildings.Rows.TryResolve(State.Place[row], out int place))
        {
            State.Stage[row] = 2;
            State.Destination[row] = _world.Buildings.Rows.At(home);
            Travel(row, place, home, tick);
        }
    }

    private void Purchase(int row, int hh, int business, Ticks tick)
    {
        if (!_world.Rules.BusinessKind(_world.Businesses.Kind[business]).ShopHours.IsOpen(tick))
        { Failure(row, ShoppingFailure.Closed); return; }
        int bin = StockBin(business, State.Good[row]);
        if (bin < 0 || _world.Bins.LevelAt(bin) == 0) { Failure(row, ShoppingFailure.Empty); return; }
        int market = _world.Markets.MarketOf(_world, bin);
        if (market < 0 || !_world.Bins.Rows.TryResolve(_world.Households.Balance[hh], out int purse)
            || !_world.Bins.Rows.TryResolve(_world.Businesses.Balance[business], out int till))
        { Failure(row, ShoppingFailure.Unaffordable); return; }
        long price = _world.DistrictPools.Price[market].Raw;
        long amount = Min(State.Wanted[row], _world.Bins.LevelAt(bin));
        if (price > 0)
        {
            amount = Min(amount, IntegerMath.FloorDiv(_world.Bins.LevelAt(purse), price));
            amount = Min(amount, IntegerMath.FloorDiv(_world.Bins.Capacity[till] - _world.Bins.LevelAt(till), price));
        }
        if (amount <= 0) { Failure(row, ShoppingFailure.Unaffordable); return; }
        _world.Withdraw(_world.Bins.Rows.At(bin), amount, tick);
        if (price > 0)
        {
            _world.Withdraw(_world.Bins.Rows.At(purse), amount * price, tick);
            _world.Deposit(_world.Bins.Rows.At(till), amount * price, tick);
        }
        _world.DistrictPools.Consumed[market] += amount;
        State.Cargo[row] = amount;
        State.UnservedSince[row] = default;
        State.Failed[row] = 0;
        State.Reason[row] = (byte)ShoppingFailure.None;
        State.Cursor[row] = 0;
        _reading = _reading with { Purchases = _reading.Purchases + 1, Bought = _reading.Bought + amount };
    }

    private void Travel(int row, int from, int to, Ticks tick)
    {
        int citizen = _world.Citizens.Rows.Resolve(State.Citizen[row]);
        State.NextAt[row] = tick + new Ticks(1);
        State.Destination[row] = _world.Buildings.Rows.At(to);
        _world.Citizens.Activity[citizen] = (byte)CitizenActivity.ShoppingTravelling;
        _world.Citizens.LastTripFate[citizen] = (byte)TripFate.InFlight;
        _reading = _reading with { Searches = _reading.Searches + 1 };
        _trips.Start(citizen, from, to, _world.ModeOf(citizen), TripPurpose.Shopping, tick);
    }

    private TravelTime Cost(int from, int to, int citizen)
    {
        _reading = _reading with { Searches = _reading.Searches + 1 };
        TravelMode mode = _world.ModeOf(citizen);
        return WalkRouting.Cost(_world.Roads, mode, _world.AccessPoint(from, mode), _world.AccessPoint(to, mode),
            _world.Rules.Trips.CrossingCost, _walk);
    }

    private int StockBin(int business, ResourceId good)
    {
        if (!_world.Businesses.Rows.IsLive(business)) { return -1; }
        int bin = OwnerBin(_world.Businesses.BinHead[business], good);
        return bin >= 0 && _world.Markets.MarketOf(_world, bin) >= 0 ? bin : -1;
    }

    private int OwnerBin(Handle<Bin> head, ResourceId good)
    {
        for (Handle<Bin> at = head; _world.Bins.Rows.TryResolve(at, out int bin); at = _world.Bins.OwnerNext[bin])
        { if (_world.Bins.Resource[bin] == good) { return bin; } }
        return -1;
    }

    private void AdvanceCursor(int row, int provider)
    { State.Cursor[row] = provider >= 0 && Known.Rows.IsLive(provider) ? Known.Next[provider] : 0; }

    private void Failure(int row, ShoppingFailure reason)
    {
        State.Reason[row] = (byte)reason;
    }

    private void CountFailure(int row)
    { if (State.Failed[row] < ushort.MaxValue) { State.Failed[row]++; } }

    private int RetryTicks() => _world.Rules.Shopping.Runs ? _world.Rules.Shopping.RetryTicks : Ticks.PerDay;
    private static long Min(long a, long b) => a < b ? a : b;

    private void Retire(int row)
    {
        _reading = _reading with { Lost = _reading.Lost + State.Cargo[row] };
        for (int p = State.ProviderHead[row] - 1; p >= 0;)
        { int next = Known.Next[p] - 1; Known.Rows.Free(Known.Rows.At(p)); p = next; }
        // Dead Household handles retain their id only in the lookup key; rebuild avoids enumeration.
        _byHousehold.Clear();
        State.Rows.Free(State.Rows.At(row));
        for (int i = 0; i < State.Rows.SlotCount; i++)
        {
            if (State.Rows.IsLive(i) && _world.Households.Rows.TryResolve(State.Household[i], out int hh))
            { _byHousehold[_world.Households.Rows.IdAt(hh)] = i; }
        }
    }
}
