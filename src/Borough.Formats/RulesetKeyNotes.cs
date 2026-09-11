namespace Borough.Formats;

/// <summary>
/// One authored sentence per Ruleset key, saying what the key <em>does</em>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The one thing in the Ruleset surface that has to be written by a person, and the reason it is
/// a table rather than a document.</b> <c>SchemaDump</c>'s own remark predicted this file and named
/// the trap it walks into: <em>"No key carries a description, and that is a gap rather than a
/// decision. What a key means lives in the Ruleset headers and the loader's doc comments, and
/// neither is attributable to a key mechanically. A description would have to be authored — at which
/// point it is a second copy again."</em>
/// </para>
/// <para>
/// 🔴 <b>What makes it not a second copy is <c>RulesetKeyNoteTests</c> and nothing else.</b> That
/// test builds the key set from <see cref="RulesetLoader.KeySurface"/> and fails in <em>both</em>
/// directions — a key with no note, and a note naming no key. So a reader added to the loader
/// reddens the build until somebody says what it is for, and a reader deleted reddens it until the
/// sentence goes too. ***A hand-authored list that a test holds against the code is not the thing
/// <c>plans/0012</c> Cause 1 is about***: Cause 1 is a copy nothing compares, and every drift it
/// records was invisible at the moment it happened.
/// </para>
/// <para>
/// ⚠ <b>A note says what the key does, never what a good value is.</b> The loader's refusal text
/// already carries the range and says it better, at the moment an author is wrong; the Ruleset
/// headers carry the demonstration; <c>plans/0002</c> §D carries whether a number is ratified. A
/// note that repeated any of those would be the copy this file is trying not to be. Where a value
/// genuinely is the point — a stated zero that is refused, an absence that is a city — the note says
/// what the value <em>means</em> and never what to write.
/// </para>
/// <para>
/// ⚠ <b>Absence is load-bearing and is stated wherever it is not obvious.</b> Most of this surface
/// is optional, and in this loader an omitted table is usually a real city rather than a default —
/// no roads, no Trip model, nobody drives, every trade clears at the ceiling for ever. A reference
/// that listed keys and said nothing about omitting them would describe a language nobody writes.
/// </para>
/// <para>
/// ⚠ <b>The retired keys are deliberately absent</b> — <c>condemn_after</c>, <c>sample</c>,
/// <c>wage</c>, <c>storage</c>, <c>sealing_decay_tau</c> in <c>[layers]</c>, the three
/// <c>adr/0148</c> moved to <c>[[business]]</c>, <c>occupants</c>/<c>jobs</c>/<c>parking</c>/
/// <c>footprint_tiles</c> from <c>plans/0052</c> and <c>plans/0053</c>, and <c>tenanted</c> from
/// <c>plans/0054</c>. They are known to the loader only so it can refuse them by name with a
/// sentence saying where they went. ⚠ <b>Count the <c>RefuseRetired</c> calls rather than trusting a
/// total here</b>: this sentence said <em>eight</em> and named five, which was two slices stale
/// before <c>plans/0054</c> touched it. They are permitted and never offered
/// (<c>RulesetLoader.Reader._retired</c>), they are not in the surface, and a note here would
/// advertise them.
/// </para>
/// </remarks>
public static class RulesetKeyNotes
{
    /// <summary>Every authored note, keyed <c>"&lt;context&gt; &lt;key&gt;"</c>.</summary>
    /// <remarks>
    /// <b>The context is spelled exactly as <c>RulesetLoader.ContextOf</c> spells it</b> — with the
    /// brackets, doubled for an array of tables, and an inline table written as its holder followed
    /// by the key that holds it (<c>[[rule]] inputs</c>). That is what lets the test compare these
    /// against the surface without a translation step, and a translation step is a place for the two
    /// to disagree.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> All => Notes;

    /// <summary>The note for one key, or <c>null</c> where none is authored.</summary>
    public static string? For(string context, string key) =>
        Notes.TryGetValue($"{context} {key}", out string? note) ? note : null;

    private static readonly Dictionary<string, string> Notes = new(StringComparer.Ordinal)
    {
        ["[care] report_days"] = "Reporting window for deaths and missed work; independent of detailed trace retention.",
        ["[care] interval"] = "How often appointments are assigned and providers are discovered.",
        ["[care] routine_days"] = "Time between completed routine visits.",
        ["[care] illness_per_thousand"] = "Daily baseline chance of a generic illness episode.",
        ["[care] health_risk_per_thousand"] = "Additional daily illness chance per point of Household Health deficit.",
        ["[care] initial_severity"] = "Severity at illness onset.",
        ["[care] serious_severity"] = "Severity that prevents work and shopping.",
        ["[care] admission_severity"] = "Severity at which inpatient care is needed.",
        ["[care] recovery_per_day"] = "Natural improvement on a day without deterioration.",
        ["[care] treated_recovery_per_day"] = "Improvement under treatment.",
        ["[care] deterioration_per_day"] = "Severity gained on a day of deterioration.",
        ["[care] deterioration_percent"] = "Daily chance of deterioration without treatment.",
        ["[care] death_severity"] = "Severity at which untreated illness can become fatal.",
        ["[care] death_per_thousand"] = "Daily fatality risk at critical severity, multiplied by illness duration.",
        ["[care] visit_minutes"] = "Duration of an outpatient consultation.",
        ["[care] wait_days"] = "Acceptable routine wait at the habitual clinic.",
        ["[care] urgent_wait_days"] = "Acceptable wait at the habitual clinic for serious illness.",
        ["[care] priority_every_days"] = "Waiting duration that adds one point to appointment priority.",
        ["[care] switch_after_waits"] = "Excessive-wait days before successful fallback can change the habitual clinic.",
        ["[care] known_clinics"] = "Maximum clinics retained in a Household provider list.",
        ["[care] search_candidates"] = "Maximum Buildings examined per clinic discovery pass.",
        ["[care] history_keeps"] = "Maximum care events retained city-wide, including events for deceased Citizens.",
        ["[care] booking_days"] = "The forward appointment calendar horizon.",
        ["[care] floor_tiles_per_treatment"] = "Floor area needed for one simultaneous outpatient treatment.",
        ["[care] floor_tiles_per_bed"] = "Floor area needed for one inpatient bed.",
        ["[school] days"] = "Weekdays on which children attend school.",
        ["[school] bell_earliest_minute"] = "Earliest civil minute for a school bell.",
        ["[school] bell_latest_minute"] = "Latest civil minute for a school bell.",
        ["[school] dismiss_earliest_minute"] = "Earliest civil minute for school dismissal.",
        ["[school] dismiss_latest_minute"] = "Latest civil minute for school dismissal.",
        ["[school] retry_ticks"] = "Delay before retrying an interrupted service return journey.",
        ["[school] history_keeps"] = "Maximum care events retained city-wide, including events for deceased Citizens.",
        ["[[building]] care_days"] = "Weekdays offering outpatient appointments.",
        ["[[building]] care_opens_hour"] = "Civil opening hour for outpatient appointments.",
        ["[[building]] care_closes_hour"] = "Civil closing hour for outpatient appointments.",
        ["[[building]] bed_percent"] = "Share of facility floor area reserved for inpatient beds; the rest supports outpatient treatment.",
        ["[[building]] level"] =
            "Which school level a kind serving education teaches: 1 primary, 2 secondary, 3 "
            + "university. Refused on any kind that does not serve education. Required of an "
            + "education kind exactly where the file also states [schooling]; absent otherwise "
            + "means an undifferentiated school every family matches.",
        // ---- [[resource]] ---------------------------------------------------------------------
        ["[[resource]] name"] =
            "What this Resource is called. Every other table refers to it by this name, and the "
            + "engine never sees the string.",
        ["[[resource]] family"] =
            "Which of the three kinds of thing this is: a good moves as a Shipment on the Road "
            + "Graph and shows up in the traffic, a utility flows along the District adjacency "
            + "graph, and money is conserved and does not move at all. The family decides transport "
            + "and whether a Bin holding it has a ceiling, so there is no default.",
        ["[[resource]] need"] =
            "Which Household Need this Resource feeds when it is consumed. Only sustenance and "
            + "satisfaction may be named: the other two Needs are fed by travelling to a service "
            + "Building rather than by buying, and are declared with [[building]] serves instead. "
            + "Absent means the Resource feeds no Need, which is the ordinary case.",

        // ---- [[building]] ---------------------------------------------------------------------
        ["[[building]] name"] =
            "What this kind of Building is called. [[zone_rule]] kind and [[rule]] kind refer to it "
            + "by this name.",
        ["[[building]] houses"] =
            "Whether a HOUSEHOLD may take a tenancy in a Building of this kind. It says whether and "
            + "never how many: the count is the Building's own floor area over [capacity] "
            + "floor_tiles_per_occupant, so two Buildings of one kind on differently-sized ground "
            + "hold different numbers. Households and Businesses share the one ceiling, and this is "
            + "one of the two permissions over it — see premises, which does not follow from this "
            + "one. Absent means no Household may live here, which is what most kinds are.",
        ["[[building]] premises"] =
            "Whether a BUSINESS may take a tenancy in a Building of this kind. houses' other half, "
            + "over the same single ceiling, and it governs both ways a trade arrives: the one this "
            + "kind comes with and one premised out of the unpremised pool. A kind declaring "
            + "business and not this is refused, because the trade would have nowhere to sit. A "
            + "kind declaring both is mixed use — the flats above the shop, competing for the same "
            + "tenancies. Absent means no Business may take premises here.",
        ["[[building]] parked"] =
            "Whether Buildings of this kind carry parking at all. Whether, and never how many: the "
            + "count is the Building's floor area over [capacity] floor_tiles_per_parking_space. It "
            + "exists so that a detached house may carry a driveway where a tower may not — a "
            + "parking minimum is a property of the city, and an exemption from it is a property of "
            + "the kind. Absent means the kind provides none.",
        ["[[building]] rent"] =
            "What a Household pays per Day to live in a Building of this kind. Zero or absent "
            + "means the dwelling is free. A Household that cannot afford the rent skips the "
            + "dwelling during placement.",
        ["[[building]] arrivals_per_day"] =
            "How many Households a Building of this kind admits from the Outside each Day. Stating "
            + "this is what makes the kind an Outside Connection, so absence means the kind is not "
            + "a gate; a stated zero is refused, because a door that never opens loads clean and "
            + "does nothing.",
        ["[[building]] serves"] =
            "Which Attended Need a Household travelling here satisfies — education or health. "
            + "Stating it is what makes the kind a service Building; absence means it is not one.",
        ["[[building]] business"] =
            "The trade a Building of this kind comes with when it is raised, naming a [[business]]. "
            + "Absent means it comes with none, which is almost every kind that has ever shipped.",
        ["[[building]] condemn_after_days"] =
            "How many Days the premises' own Rules may starve continuously before the Building is "
            + "condemned. Absent means this kind never declines.",
        ["[[building]] tenancy_ends_after_days"] =
            "How many Days a tenant's own Rules may starve continuously before the tenancy ends and "
            + "the tenant is put out. Independent of the premises' threshold: a kind may state "
            + "either, both or neither. Absent means tenancies here never end.",
        ["[[building]] sheds_occupant_after_days"] =
            "How many Days the premises may starve before the Building sheds one Occupant — the "
            + "first rung of decline, below condemnation. Requires condemn_after_days and must be "
            + "strictly shorter than it, or the Building would be abandoned before it could shed "
            + "anything. Absent means the kind sheds nobody on its way down.",
        ["[[building]] abandoned_when_empty_after_days"] =
            "How many Days a Building of this kind may house nobody before the city gives up on it. "
            + "This reads occupancy where condemn_after_days reads failure, so it kills surplus "
            + "stock rather than failing stock. It counts Households and not tenants of any kind, "
            + "so a shop sitting in a dwelling does not keep the clock from starting. Absent means "
            + "a Building of this kind stands empty for ever.",
        ["[[building]] collapses_after_days"] =
            "How many Days an abandoned Building stands as a shell before it collapses and the Lot "
            + "is clear again. Required of any kind that can be abandoned, because shells with no "
            + "sink are a collection that grows with elapsed time.",
        ["[[building]] bins"] =
            "The stores a Building of this kind carries, one entry per Resource. A Rule's local "
            + "scope draws on these.",
        ["[[building]] bins resource"] =
            "Which Resource this Bin holds, naming a [[resource]]. One Bin per Resource per kind.",
        ["[[building]] bins capacity"] =
            "How much of that Resource the Bin holds when full. Refused on a money Bin, which has no "
            + "physical ceiling — a finite one would mean a seller too rich to be paid.",
        ["[[building]] bins owner"] =
            "Whose Bin this is: the premises', or the tenant's. It is what makes a Rule over this "
            + "Bin the tenant's Rule rather than the Building's, and a tenant's Bins live exactly as "
            + "long as the tenancy. Absent means the premises own it.",

        // ---- [[business]] ---------------------------------------------------------------------
        ["[[business]] name"] =
            "What this trade is called. [[building]] business names it.",
        ["[[business]] shift_start_earliest_hour"] =
            "The earliest hour of the Day a Business of this trade may open, as a whole in-world "
            + "hour. A Workplace draws one start hour and its whole staff share it, so a Citizen "
            + "stores no start hour at all. Required exactly when the trade employs somebody.",
        ["[[business]] shift_start_latest_hour"] =
            "The latest hour a Business of this trade may open. Hour 23 is the ceiling, because this "
            + "is an hour of the Day rather than a duration and hour 24 is the next Day's hour 0.",
        ["[[business]] wage_per_day"] =
            "What one job at this trade pays for one Day worked, as a flat rate. Paired with "
            + "pay_period_days: each is half a mechanism alone. Absent means the trade pays nothing.",
        ["[[business]] work_days"] = "Workdays as a Monday-first weekly bit mask. Absent preserves daily work.",
        ["[[business]] open_days"] = "Shop operating days as a Monday-first weekly bit mask, independent of staffing.",
        ["[[business]] opens_hour"] = "The daily opening hour for shopping purchases.",
        ["[[business]] closes_hour"] = "The daily closing hour; purchases arriving at or after closing fail.",
        ["[shopping] interval"] = "How often each Household considers replenishment, staggered across Ticks.",
        ["[shopping] low_days"] = "Supplies below this many Days of consumption prompt an outing.",
        ["[shopping] target_days"] = "The Days of consumption an outing tries to bring home, bounded by storage and affordability.",
        ["[shopping] severe_need"] = "Sustenance depth at which shopping can delay work and try one extra known shop.",
        ["[shopping] known_shops"] = "The maximum number of remembered providers per Household.",
        ["[shopping] search_candidates"] = "The number of nearby premises sampled when discovering shops, without inspecting stock.",
        ["[shopping] retry_ticks"] = "The delay before retrying an unsuccessful outing or an interrupted return.",
        ["[[business]] pay_period_days"] =
            "How many Days pass between paydays at this trade. One is daily; zero would be a payday "
            + "that never comes round.",

        ["[[business]] goes_bankrupt_after_short_paydays"] =
            "How many paydays running this trade may fail to pay its workers in full before it is "
            + "wound up: the staff are dismissed and the premises are left standing and empty. A "
            + "payroll met in full resets the count. Absent means it never goes bankrupt.",
        ["[[business]] requires_tier"] =
            "The lowest Skill Tier this trade will hire, a minimum rather than a band — a Citizen "
            + "above it may still take the post. Absent means it hires anybody.",
        ["[[business]] tuition_per_day"] =
            "What this trade charges a Household In Education, per Day — the private-university "
            + "route to a degree, rationed by what a Household can pay rather than by places. "
            + "Refused unless the file declares a [[building]] serving education at level 3. "
            + "Absent means the trade is not a private university.",

        // ---- [[rule]] -------------------------------------------------------------------------
        ["[[rule]] name"] =
            "What this Rule is called. on_fail names another Rule by this.",
        ["[[rule]] kind"] =
            "Which [[building]] kind this Rule runs on. Whether it is the premises' Rule or its "
            + "tenant's is derived from the Bins its terms reach, never authored.",
        ["[[rule]] rate"] =
            "How often a Rule Instance re-arms, in Ticks — its reschedule interval. Distinct from "
            + "the sweep interval a Zone Rule or a Policy states: a Bin Rule is armed per Building "
            + "on the Event Wheel, not swept over the city.",
        ["[[rule]] apply"] =
            "How many times the Rule's terms are applied on one firing: either a band, or a count "
            + "derived from a Readout. Never both — below a band's minimum is a failure and a "
            + "derived zero is a success, so the two have colliding failure semantics.",
        ["[[rule]] apply min"] =
            "The fewest applications one firing may make. Falling below this is what makes the "
            + "firing a failure.",
        ["[[rule]] apply max"] =
            "The most applications one firing may make. Equal to min is the fixed case rather than a "
            + "second form.",
        ["[[rule]] apply derived"] =
            "The name of a Readout whose value sets the application count, instead of a band. The "
            + "readable set is declared in the simulation rather than in the Ruleset, and a Readout "
            + "must be readable against the entity this Rule hangs off.",
        ["[[rule]] apply percent"] =
            "How many applications one unit of the derived Readout is worth, as a percentage — 100 "
            + "is one application per unit. Only meaningful beside derived.",
        ["[[rule]] inputs"] =
            "What the Rule consumes on each application. A term that cannot be drawn is what makes "
            + "the firing fail, which is what arms the Failure Pressure the decline thresholds read.",
        ["[[rule]] inputs resource"] =
            "Which Resource this term draws, naming a [[resource]].",
        ["[[rule]] inputs amount"] =
            "How much of it one application draws. A Rule that moves nothing is a Rule with no term "
            + "rather than a term of zero.",
        ["[[rule]] inputs scope"] =
            "Where the term draws from: local is this Building's own Bin, pool is the District "
            + "market (a purchase, whose payment is implicit at the prevailing price), and global is "
            + "the treasury, which holds conserved Resources only. A term reaching the tenant's Bins "
            + "from a premises Rule is refused, because crossing an ownership boundary is a trade.",
        ["[[rule]] outputs"] =
            "What the Rule produces on each application, into a Bin or onto a Map Layer.",
        ["[[rule]] outputs resource"] =
            "Which Resource this term deposits, naming a [[resource]].",
        ["[[rule]] outputs amount"] =
            "How much of it one application deposits.",
        ["[[rule]] outputs scope"] =
            "Where the term deposits: local, pool, global, or map. The map scope is write-only, so a "
            + "Rule may emit to a Layer and can never read or wait on one.",
        ["[[rule]] outputs layer"] =
            "Which Map Layer a map-scoped term emits into. A Layer cell has no capacity, so such a "
            + "term never fails.",
        ["[[rule]] on_fail"] =
            "Another Rule to try when this one's inputs cannot be met — a source ladder over one "
            + "Bin. A Rule named here is a link rather than a head, so it is reached by walking a "
            + "failed chain and is never armed on its own rate.",
        ["[[rule]] fills"] =
            "Which Bin a fallback link is relieving, so a chain can be answered as one mechanism. "
            + "The map scope is refused: nothing ever waits on a Layer, so nothing can rescue a wait "
            + "on one.",
        ["[[rule]] reports"] =
            "The name of the Condition this Rule's failure reports under, which is what the "
            + "diagnosis panel groups by. Absent means the failure is not reported by name.",

        // ---- [[zone_rule]] --------------------------------------------------------------------
        ["[[zone_rule]] name"] =
            "What this Zone Rule is called.",
        ["[[zone_rule]] kind"] =
            "Which [[building]] kind this Rule raises on a Lot it accepts.",
        ["[[zone_rule]] zone"] =
            "Which permission bit a Lot must carry for this Rule to build on it — a bit index, not a "
            + "mask. A bit no zone command can paint is refused, because the Rule would sample Lots "
            + "for ever and build nothing.",
        ["[[zone_rule]] interval"] =
            "How many Ticks between sweeps of this Rule over the city. Distinct from a Bin Rule's "
            + "rate: a Zone Rule sweeps, it is not armed per Lot.",
        ["[[zone_rule]] revisit_ticks"] =
            "How long this Rule takes to look at every Lot once, in Ticks. The sample per trigger is "
            + "derived from this duration rather than authored, so the fraction of the city covered "
            + "per cycle does not shrink as the city grows. The draw is with replacement, so this is "
            + "a rate and not coverage.",
        ["[[zone_rule]] build_threshold_days"] =
            "How much unmet demand, in household-Days, must accumulate in a District before this "
            + "Rule raises a Building there. It is the entry cost for a trade, and today it is the "
            + "only brake on birth. Absent keeps the older predicate, which reads no demand at all.",
        ["[[zone_rule]] cooldown_days"] =
            "How many Days a District waits after raising a Building of this kind before it may "
            + "raise another — what damps the response to the demand signal. Requires "
            + "build_threshold_days, since a Rule reading no demand has nothing to damp.",

        // ---- [[band]] -------------------------------------------------------------------------
        ["[[band]] name"] =
            "What this density band is called.",
        ["[[band]] admits"] =
            "Which permission bits a Lot in this band may keep -- a list of bit indices, not a mask. "
            + "It is a CAP applied by intersection against the Lot's own permission set, so a band "
            + "can only ever take a permission away. Bands are ordered by declaration, least intense "
            + "first, and a band nobody declared admits everything.",

        // ---- [[life_stage]] -------------------------------------------------------------------
        ["[[life_stage]] name"] =
            "What this stage of a Household's life is called. next, childless and children_become "
            + "name a stage by this.",
        ["[[life_stage]] duration_days"] =
            "The fewest Days a Household spends in this stage before its countdown comes due.",
        ["[[life_stage]] spread_days"] =
            "The width of the window the countdown is drawn over, uniform on [duration, duration + "
            + "spread). Zero is a real answer and means every Household in this stage leaves it on "
            + "the same Day — which, for a city whose whole population was created at Tick 0, makes "
            + "the founding generation's echo run for the whole game.",
        ["[[life_stage]] next"] =
            "The stage a Household moves to when its countdown comes due. Absent means the stage is "
            + "terminal, which is the only way to spell that — a stage naming itself is refused.",
        ["[[life_stage]] childless"] =
            "The stage a Household goes to instead when it draws zero children on leaving this one. "
            + "Paired with the child band: each half alone loads clean and does nothing.",
        ["[[life_stage]] children_min"] =
            "The fewest children a Household bears on leaving this stage. Zero is the answer that "
            + "matters, because drawing zero is what routes a Household to the childless stage.",
        ["[[life_stage]] children_max"] =
            "The most it bears. The band is inclusive at both ends; equal ends mean every Household "
            + "bears the same number.",
        ["[[life_stage]] children_become"] =
            "The stage each child forms its own Household in when it leaves home. This is a door "
            + "into the Unplaced Pool, so a Ruleset stating it owes [placement] gives_up_after_days.",
        ["[[life_stage]] adult_age_min_days"] =
            "The youngest age a Citizen carries on becoming an adult in this stage. An age is drawn "
            + "on formation and never advances, so there is no default that is not a guess about a "
            + "population. Zero is unavailable: it is what marks a child.",
        ["[[life_stage]] adult_age_max_days"] =
            "The oldest such age. Required together with the minimum, of any stage a children_become "
            + "names.",
        ["[[life_stage]] centrality_base_percent"] =
            "Where this stage sits on the space-against-centrality axis when it looks for a home — 0 "
            + "wants room, 100 wants the middle of the city. Absent is the neutral value, which is "
            + "the placement this build had before preferences existed.",
        ["[[life_stage]] centrality_spread_percent"] =
            "The width of the band a Household of this stage draws its own position from, so that a "
            + "stage is a distribution rather than one opinion. Zero means the stage agrees with "
            + "itself completely. The band must fit inside the axis.",
        ["[[life_stage]] school_level"] =
            "Which school level this stage's children attend: 1 primary, 2 secondary. A university "
            + "is refused here — it is attended by a Household In Education, a state and not a "
            + "Life Stage. Absent means this stage's children attend no school.",

        // ---- [[policy]] -----------------------------------------------------------------------
        ["[[policy]] name"] =
            "What this Policy is called. The player's governing panel addresses a Policy by this and "
            + "by nothing else, so an unnamed one is unaddressable rather than addressable by "
            + "position.",
        ["[[policy]] sweeps"] =
            "Which population the Policy runs over: household or business. A third, building, is "
            + "declared in the design and refused here, because a Building population is whichever "
            + "rows a predicate picks and there is no predicate.",
        ["[[policy]] interval"] =
            "How many Ticks between sweeps of this Policy over that population.",
        ["[[policy]] apply"] =
            "How many times the transfer is applied to each member on one sweep: a band, or a count "
            + "derived from a Readout.",
        ["[[policy]] apply min"] =
            "The fewest applications per member.",
        ["[[policy]] apply max"] =
            "The most. Equal to min is the fixed case.",
        ["[[policy]] apply derived"] =
            "The name of a Readout whose value sets the count. It must be readable against the "
            + "population this Policy sweeps — a Readout that only hangs off a Building has no row "
            + "here to read.",
        ["[[policy]] apply percent"] =
            "How many applications one unit of that Readout is worth, as a percentage.",
        ["[[policy]] transfer"] =
            "The movement of money the Policy makes, stated as both ends at once. That is what makes "
            + "it a transfer rather than a term list: the two sides balance inside one atomic Rule.",
        ["[[policy]] transfer from"] =
            "Which side the money leaves — typically local, the swept member's own balance. The pool "
            + "and map scopes are refused, because a pool term is a purchase whose payment is "
            + "implicit and a map cell has no capacity to draw from.",
        ["[[policy]] transfer to"] =
            "Which side it arrives at — typically global, the treasury.",
        ["[[policy]] transfer resource"] =
            "Which Resource moves, naming a [[resource]]. The treasury holds conserved Resources "
            + "only, so a global end means money.",
        ["[[policy]] transfer amount"] =
            "How much moves per application.",

        // ---- [[terrain]] ----------------------------------------------------------------------
        ["[[terrain]] name"] =
            "Which of the five terrain types this table prices: ordinary, rock, floodplain, marsh or "
            + "thin_soil. The set is closed — this selects a type the generator already places "
            + "rather than declaring one. A file prices all five or none.",
        ["[[terrain]] base_fertility_percent"] =
            "The ceiling this ground's Fertility starts at, as a percentage of fully fertile. 100 is "
            + "the top of the scale rather than a tuning choice.",
        ["[[terrain]] sealing_decay_tau"] =
            "How many scheduled updates this ground takes to shed its Sealing once it is no longer "
            + "built on. Zero means never, which is a real answer and is rock's.",

        // ---- [[hinterland]] -------------------------------------------------------------------
        ["[[hinterland]] edge"] =
            "Which side of the map this Hinterland sits behind — north, south, east or west — shared "
            + "by every Outside Connection on that edge. The edge is what a Hinterland is, so there "
            + "is no default and two Hinterlands may not share one.",
        ["[[hinterland]] emigrant_balance_min"] =
            "The least money a Household arriving from this Hinterland brings with it.",
        ["[[hinterland]] emigrant_balance_max"] =
            "The most. Stated as a band rather than one figure, because a single figure gives every "
            + "arrival the same means.",
        ["[[hinterland]] rent"] =
            "What a home costs per Day out here, in the money a [[building]] rent is in. It is what "
            + "a prospective Household compares against the cost of living in the city, and what a "
            + "Household waiting for somewhere to live compares against every home it is shown. "
            + "Required beside a choice model and refused without one.",
        ["[[hinterland]] centrality_tiles"] =
            "How far from a centre life out here is, in the Tiles the map is measured in. It puts "
            + "the Outside on the same scale a dwelling's walk to the centre is on, so that a "
            + "Household weighing one against the other is measuring both from the same place. "
            + "Required beside a choice model and refused without one.",
        ["[[hinterland]] prices"] =
            "What this Hinterland will sell each Good for, one entry per Good. These are the only "
            + "authored anchor under every price in the design: a District's Pool may charge up to "
            + "the cheapest declared Hinterland price and no more.",
        ["[[hinterland]] prices resource"] =
            "Which Good is being priced, naming a [[resource]]. Only a good may be — a utility is "
            + "not stocked, and money is what a price is denominated in rather than a thing that has "
            + "one.",
        ["[[hinterland]] prices price"] =
            "The import price, which becomes the ceiling on what that Good can cost inside the city. "
            + "Delete the entry to leave a Good unpriced; a price of zero would make it free "
            + "everywhere for ever instead.",

        // ---- [[lattice]] ----------------------------------------------------------------------
        ["[[lattice]] origin_east_tiles"] =
            "How far east of the map's corner this Street lattice starts, in Tiles, on the block "
            + "grid. What a file authors is where a settlement is; its extent and its share of the "
            + "population are derived.",
        ["[[lattice]] origin_north_tiles"] =
            "How far north, on the same grid. Absent [[lattice]] tables altogether mean one lattice "
            + "at the origin corner, and the gap between two origins is the entire content of a "
            + "two-settlement world.",

        // ---- [layers] -------------------------------------------------------------------------
        ["[layers] pollution_period"] =
            "How many Ticks between recomputations of the pollution field.",
        ["[layers] pollution_offset"] =
            "Which Tick of that cycle pollution fires on. It has to sit inside the period, or the "
            + "Layer would never be recomputed at all.",
        ["[layers] pollution_decay_ticks"] =
            "How long a plume takes to fade, as a duration in Ticks. Zero means it never fades. A "
            + "decay shorter than the period it runs at is refused, because it would round to zero "
            + "updates and behave as never while reading as fast.",
        ["[layers] kernel_metres"] =
            "How far an industrial pollution source reaches, in metres — the width of the diffusion "
            + "kernel. Baked into the world at creation and refused on reload.",
        ["[layers] land_value_period"] =
            "How many Ticks between recomputations of the land value field.",
        ["[layers] land_value_offset"] =
            "Which Tick of that cycle land value fires on.",
        ["[layers] land_value_tau"] =
            "The time constant land value moves on — how much momentum it has. A period of 1 is land "
            + "value with no momentum; a tau of 0 is not a time constant, since it divides.",
        ["[layers] sealing_decay_period"] =
            "How many Ticks between recomputations of the Sealing field.",
        ["[layers] sealing_decay_offset"] =
            "Which Tick of that cycle Sealing fires on. How fast each ground actually sheds Sealing "
            + "is per terrain type, on [[terrain]] sealing_decay_tau.",
        ["[layers] woodland_regrowth_period"] =
            "How many Ticks between recomputations of the woodland field.",
        ["[layers] woodland_regrowth_offset"] =
            "Which Tick of that cycle woodland regrowth fires on.",
        ["[layers] woodland_regrowth_days"] =
            "How many Days a Cell cleared of all its forest takes to grow back to what the seed "
            + "laid. Omit the key for a world where forest never returns; zero is refused, because "
            + "it would mean instantly rather than never.",
        ["[layers] noise_range_metres"] =
            "How far a road's noise reaches from it, in metres.",
        ["[layers] noise_intensity_percent"] =
            "How loudly one Vehicle per Tick radiates at one Tile, as a percent. Not a scale: the "
            + "level is logarithmic above unity and linear below it, so this decides which regime "
            + "the city sits in.",
        ["[layers] shoreline_range_metres"] =
            "How far a fouled Water Body reaches from its edge, in metres. A world with no water is "
            + "spelled by omitting [water], not by writing zero here.",
        ["[layers] shoreline_intensity_percent"] =
            "How strongly a completely fouled body radiates at one Tile from its edge, as a percent. "
            + "Its multiplicand is a fill fraction where noise's is an unbounded flow, so the two "
            + "intensities are not comparable.",
        ["[layers] desirability_pollution_percent"] =
            "How much pollution subtracts from desirability, as a weight. Zero removes the term "
            + "rather than defaulting it.",
        ["[layers] desirability_noise_percent"] =
            "How much noise subtracts from desirability. Zero removes the term.",
        ["[layers] desirability_shoreline_percent"] =
            "How much a fouled shoreline subtracts from desirability. Zero removes the term.",
        ["[layers] fertility_pollution_percent"] =
            "How much fully fertile ground a unit of pollution costs, as a percent. It weighs the "
            + "pollution term for every Cell alike rather than per terrain type, which is why it "
            + "lives here and not on [[terrain]]. There is deliberately no sealing counterpart: a "
            + "fully built Cell has no farmland, which pins that coefficient.",

        // ---- [placement] ----------------------------------------------------------------------
        ["[placement] interval"] =
            "How many Ticks between passes that drain the Unplaced Pool into standing dwellings. "
            + "Omitting the whole [placement] table means the pass does not run and nobody is ever "
            + "housed, which is loud rather than quiet — the Pool grows and the Census says so.",
        ["[placement] revisit_ticks"] =
            "How long the placement pass takes to look at everybody waiting in the Pool once, in "
            + "Ticks. The sample per trigger is derived from this, so the fraction of a housing "
            + "queue cleared per cycle does not shrink as the queue grows.",
        ["[placement] candidates"] =
            "How many dwellings one Household looks at on one occasion before waiting for the next. "
            + "A behaviour model rather than a budget: a family that sees three flats and takes the "
            + "first with room is not an optimiser being approximated.",
        ["[placement] move_at_need"] =
            "The Sustenance or Satisfaction deficit at which a continuing shortage can prompt "
            + "a move. Requires reassessment and Needs; each tenancy must endure its own shortage "
            + "long enough to reach this depth. Absent means shortages never prompt moves.",
        ["[placement] reconsider_ticks"] =
            "The duration used to size a sample of housed Households for reassessment, in Ticks; "
            + "sampling is with replacement and does not guarantee coverage. Unaffordable rent "
            + "or an enabled shortage criterion returns a Household to the Unplaced Pool, so the Ruleset "
            + "owes gives_up_after_days. Absent means no reassessment.",
        ["[placement] gives_up_after_days"] =
            "How long a Household keeps looking for a home before it gives up and leaves. Required "
            + "of any Ruleset with a door into the Pool — a gate kind, a reassessment sweep, or a "
            + "life stage whose children leave home — because a Pool with an inflow and no sink "
            + "grows without bound. Absent means nobody ever gives up, which is only coherent in a "
            + "world with no door in it.",

        ["[placement] mu_percent"] =
            "How sharply a Household acts on what it prefers, as a percent, where 100 is the scale "
            + "the choice model is written around. Higher is more decisive and also narrower: an "
            + "option far enough below the best stops being possible rather than becoming "
            + "unlikely. Absent means the best-scoring candidate is taken outright.",
        ["[placement] centrality_tiles_per_unit"] =
            "How far a Household would walk to trade one unit of what it prefers, in Tiles. It is "
            + "what makes distance comparable with rent. Required beside mu_percent and refused "
            + "without it.",
        ["[placement] rent_per_unit"] =
            "The difference in daily rent worth one unit of what a Household prefers. It is what "
            + "makes rent comparable with distance, and it is a soft trade-off rather than the "
            + "affordability test, which stays a filter. Required beside mu_percent and refused "
            + "without it.",

        ["[placement] moving_costs_rent"] =
            "The daily rent a Household would pay to stay where it is rather than move. It is what "
            + "makes a home it already has worth more to it than the same home would be to a "
            + "stranger, so a marginal improvement elsewhere does not empty the city every time the "
            + "reassessment sweep runs. Zero means moving costs a family nothing. Required beside "
            + "mu_percent and refused without it.",

        // ---- [jobs] ---------------------------------------------------------------------------
        ["[jobs] interval"] =
            "How many Ticks between passes that assign work to Citizens who have none. Omitting the "
            + "whole [jobs] table means nobody is ever assigned work. The table is refused without a "
            + "Commute Budget above it, because the pass has no search radius of its own.",
        ["[jobs] revisit_ticks"] =
            "How long the assignment pass takes to look at every Citizen once, in Ticks.",
        ["[jobs] candidates"] =
            "How many workplaces one Citizen looks at on one occasion. A person who sees three "
            + "places with a vacancy and takes the first one near enough to walk to is satisficing, "
            + "not optimising.",
        ["[jobs] shift_hours_min"] =
            "The shortest working day in this city, in whole in-world hours. It doubles as the gap "
            + "between a Citizen's two journeys, so it must exceed the Commute Budget or somebody "
            + "could still be travelling to work when the roster says they leave it.",
        ["[jobs] shift_hours_max"] =
            "The longest working day. Staff share a start hour and so arrive together; they leave "
            + "spread over this band, which makes it the evening peak's whole width. Equal bounds "
            + "are a control rather than a mistake.",
        ["[jobs] arrive_early_max_minutes"] =
            "How far ahead of their Shift a Citizen may aim to arrive, in whole in-world minutes. "
            + "Start hours are on the hour, so without this the whole city departs on a handful of "
            + "Ticks and the morning comes out a plateau of equal bars. Zero means everybody cuts it "
            + "fine.",
        ["[jobs] wage_tier_percent"] =
            "What each of the three Skill Tiers is paid, as a percent of the trade's posted rate — "
            + "exactly three entries, the first of which restates the posted rate and can only be "
            + "100. Absent means every tier is paid the same.",
        ["[jobs] experience_per_day"] =
            "What one Day worked is worth toward promotion. Required together with "
            + "tier2_experience: a rate with no ceiling is half a mechanism.",
        ["[jobs] tier2_experience"] =
            "How much accumulated experience promotes a Citizen to Tier 2. Required together with "
            + "experience_per_day.",
        ["[jobs] unschooled_experience_percent"] =
            "What one Day worked is worth to a Citizen who missed schooling, as a percent of the "
            + "schooled rate. Absent means a missed childhood costs nothing on this axis.",
        ["[jobs] experience_premium_percent"] =
            "The premium a Citizen has earned inside their own band at its ceiling, as a percent "
            + "added to their pay — the design's one source of productivity growth within a tier. "
            + "Absent means experience never adds to pay.",

        // ---- [schooling] ------------------------------------------------------------------------
        ["[schooling] attendance_weight_percent"] =
            "How much of the childhood score the attendance term carries; the depth term carries "
            + "the rest.",
        ["[schooling] full_attendance_days"] =
            "Secondary Days that score a full 100 on the attendance term.",
        ["[schooling] primary_gate_days"] =
            "Primary Days below which secondary attendance counts for nothing at all — the gate.",
        ["[schooling] tier2_score"] =
            "The childhood score at or above which an adult forms at Tier 2. Also the university "
            + "qualification, because the two are the same claim about the same childhood.",
        ["[schooling] university_days"] =
            "How many Days In Education confers Tier 3. Required exactly of a file declaring a "
            + "[[building]] serving education; refused of one that declares none.",

        // ---- [roads] --------------------------------------------------------------------------
        ["[roads] block_tiles"] =
            "The Street grid spacing in Tiles — the block. It decides Segment count and therefore "
            + "every routing figure downstream. Omitting the whole [roads] table means the world has "
            + "no roads at all.",
        ["[roads] block_spread_tiles"] =
            "How much wider the wide line of each four-line period is, and how much narrower the "
            + "narrow one — so the lattice comes out with a hierarchy of streets instead of one "
            + "block size everywhere. Absent means the lattice is uniform, which is what every "
            + "other shipped file states. It moves no mean: every period sums to four nominal "
            + "blocks, so the network's grain is unchanged and only its uniformity moves. A world "
            + "stating it may declare only one [[lattice]], at origin 0.",
        ["[roads] arterial_count"] =
            "How many freeform Arterials cross the map. Zero is a city of Streets alone, and "
            + "therefore a city where Severance cannot happen — which is what every city-modelling "
            + "Ruleset states, because an Arterial is a player tool that does not belong in a "
            + "generator.",
        ["[roads] arterial_junction_tiles"] =
            "How many Tiles of Arterial lie between two authored Junction pieces, which is that "
            + "Arterial's Segment length.",
        ["[roads] foot_crossing_every"] =
            "Keep a pedestrian crossing at every nth Street an Arterial severs. Zero means no "
            + "crossings and a network cut in two for anybody on foot.",
        ["[roads] foot_paths_per_thousand_blocks"] =
            "How many blocks in a thousand get a cut-through for pedestrians. The stronger Severance "
            + "lever of the two.",
        ["[roads] street_speed_kph"] =
            "Free-flow speed on a Street, in km/h, converted exactly at load because the library "
            + "holds no metres and no seconds.",
        ["[roads] arterial_speed_kph"] =
            "Free-flow speed on an Arterial.",
        ["[roads] walk_speed_kph"] =
            "Walking speed on foot. It is also what turns the Commute Budget into the box the job "
            + "search draws candidates from.",
        ["[roads] street_capacity_per_hour"] =
            "How many Vehicles an hour a Street carries before the volume-delay function starts "
            + "slowing it.",
        ["[roads] arterial_capacity_per_hour"] =
            "The same for an Arterial.",
        ["[roads] foot_path_capacity_per_hour"] =
            "The same for a foot path.",

        // ---- [capacity] -----------------------------------------------------------------------
        ["[capacity] floor_tiles_per_occupant"] =
            "How much floor one tenancy takes, in Tiles. A Building's occupancy is its floor area — "
            + "its footprint on every storey — divided by this, so it varies with the ground the "
            + "Building stands on rather than with its kind. Omitting the whole [capacity] table "
            + "means no Building in the city holds anybody.",
        ["[capacity] floor_tiles_per_job"] =
            "How much floor one job takes, in Tiles. A Business employs its share of its premises' "
            + "floor area divided by this, its share being one of the Building's tenancies — so one "
            + "trade employs differently in a terrace and in a slab. Absent means nobody in this "
            + "city is employed anywhere.",
        ["[capacity] floor_tiles_per_parking_space"] =
            "How much floor a Building must have per parking space it is given, in Tiles. It is a "
            + "parking minimum, which is a property of a city rather than of a building. Absent "
            + "means the city has no parking at all.",
        ["[capacity] floor_tiles_per_place"] =
            "How much floor one attendance a Day takes at a service Building, in Tiles. A school's "
            + "places are its floor area divided by this, so a bigger school teaches more children "
            + "and where the schools stand matters as much as how many there are. It is the one rate "
            + "in this table whose absence means the OPPOSITE of the other three: it is a ceiling "
            + "rather than a supply, so omitting it means no service Building is ever full. It is "
            + "refused in a Ruleset where no kind declares serves, because a ceiling nothing can "
            + "reach would sit there reading as a mechanism this world has.",

        // ---- [lots] ---------------------------------------------------------------------------
        ["[lots] lots_per_segment"] =
            "Number of Addresses per Street Segment used by block-based subdivision and form ranking. Explicit residential frontage controls Detached and Perimeter parcel counts instead.",

        ["[lots] residential_frontage_tiles"] =
            "Target parcel frontage in Tiles for Detached and Perimeter forms. Longer Streets carry more parcels. Fixed at world creation; absence uses the demonstration subdivision.",
        ["[lots] residential_depth_tiles"] =
            "Depth of residential parcels in Tiles, bounded by the available block interior. Fixed at world creation.",
        ["[lots] house_width_tiles"] =
            "Detached house frontage in Tiles, centred inside its parcel. Fixed at world creation.",
        ["[lots] house_depth_tiles"] =
            "Detached house depth in Tiles, centred inside its parcel. Fixed at world creation.",
        ["[lots] house_storeys"] =
            "Detached house height in storeys; the starting height for residential forms with explicit parcel dimensions. Fixed at world creation.",

        ["[lots] street_half_width_tiles"] =
            "Ground reserved on each side of a Street centreline, in Tiles. Footprints clear this "
            + "strip on all four block edges; the shell draws the same Street width. Fixed at world creation.",

        ["[lots] setback_tiles"] =
            "Maximum independent random inset in Tiles for block-based footprints and explicit Perimeter parcels. Explicit Detached houses use centred house dimensions instead. Reserved Street ground is always excluded.",

        ["[lots] storeys_per_rung"] =
            "Step between density rungs. Block-based forms use it in their plot-ratio target; explicit Perimeter parcels add it per rung to house_storeys. Detached houses keep house_storeys.",

        ["[lots] pattern_spread"] =
            "How many rungs either side of its band's own rung a block's pattern may be drawn, so a "
            + "density is a mix of forms rather than one form. The band's rung stays the centre of "
            + "what the band means; the draw is on the block's own ground, so a block cleared and "
            + "zoned again is re-carved the same way and a re-plat can still never move a block down "
            + "the ladder. Omitting it means every block takes its band's form exactly.",

        // ---- [trips] --------------------------------------------------------------------------
        ["[trips] crossing_seconds"] =
            "What it costs on foot to reach the other side of a Segment, in in-world seconds. Zero "
            + "is legitimate and means a shop opposite is the shop next door. A road you wait longer "
            + "than an hour to cross is not a Street at all — it is an Arterial, which carries no "
            + "pedestrian Arcs between its Junction pieces.",
        ["[trips] commute_fast_minutes"] =
            "The ceiling on a commute counted as fast, in in-world clock minutes. It grades a "
            + "commute that happens anyway and refuses nothing.",
        ["[trips] commute_moderate_minutes"] =
            "The ceiling on a moderate commute. Above it a commute is unsavoury but still made.",
        ["[trips] commute_budget_minutes"] =
            "How long a journey may take before the person making it gives up — the only rung that "
            + "refuses a Trip. All three rungs are one decision: state every one or none. Omitting "
            + "them is a city whose commutes are never refused for their length and never graded, "
            + "which is the only city whose cost distribution is uncensored.",

        // ---- [households] ---------------------------------------------------------------------
        ["[households] car_ownership_percent"] =
            "What share of Households keeps a car. A Citizen travels in their Household's mode, so "
            + "this is drive everywhere or walk everywhere; mode choice per Trip is a different "
            + "question nothing has designed. Omitting the whole [households] table means nobody "
            + "drives.",
        ["[households] opening_balance_min"] =
            "The least money a Household is created with at world creation.",
        ["[households] opening_balance_max"] =
            "The most. Both ends or neither — one end alone reads either as a fixed endowment or as "
            + "a band reaching down to destitute, and those are different cities. Omitting both "
            + "endows nobody.",

        // ---- [traffic] ------------------------------------------------------------------------
        ["[traffic] alpha_percent"] =
            "How much slower a Segment is at exactly its capacity, as a percentage — the alpha of "
            + "the volume-delay function. Omitting the whole [traffic] table means roads never slow "
            + "down. Note that congestion here is a loop rather than a formula: a slower Vehicle "
            + "dwells longer, and longer dwell is higher volume.",
        ["[traffic] beta"] =
            "The exponent of the volume-delay curve, a small whole number. Below 1 the function is "
            + "not increasing in volume; above 6 a clamped ratio overflows the fixed-point "
            + "arithmetic it is computed in.",
        ["[traffic] clamp_percent"] =
            "The largest volume-over-capacity ratio the function will read, as a percentage. Below "
            + "100 the clamp binds before a Segment is even full; far above it the router is "
            + "comparing noise.",

        // ---- [parking] ------------------------------------------------------------------------
        ["[parking] radius_metres"] =
            "How far a driver will walk from a Car Park to where they were going. Omitting the whole "
            + "[parking] table is a city with no Parking Shed at all; a radius of zero would be a "
            + "city whose Car Parks all exist and none can be reached.",
        ["[parking] shed_keeps"] =
            "How many Car Parks a Building's Parking Shed holds, and therefore how far a query walks "
            + "before it stops. Not redundant with the radius: the cap bounds the work and the "
            + "radius bounds the walk, and they bind in different worlds. A shed is stored for every "
            + "Building at this width, so it is a per-city cost rather than a per-query one.",

        // ---- [water] --------------------------------------------------------------------------
        ["[water] sea_level_percent"] =
            "How high the sea stands, as a fraction of the height range this world realised. Delete "
            + "the [water] table for an inland world with no coast; zero would put the sea at the "
            + "lowest Cell, which is a second spelling of the same thing.",
        ["[water] flood_level_percent"] =
            "How high a flood reaches on the same scale, which is what generates the Hazard Region. "
            + "It must stand above the sea — at or below it would describe ground already under "
            + "water. Absent means no floodplain, which is a steep coast and a world.",
        ["[water] carries"] =
            "Which Resource a Water Body's Bin holds, naming a [[resource]]. It must be a utility: a "
            + "body moves its contents along an edge of the water graph with no Vehicle, and a good "
            + "is by definition a Resource whose movement between Districts needs one.",
        ["[water] capacity_per_cell"] =
            "How much one wet Cell of a body holds. A body that held nothing would be an infinite "
            + "sink wearing the opposite spelling.",
        ["[water] outflow_per_exit_per_day"] =
            "How much leaves a body through one exit in a Day — the way out.",
        ["[water] runoff_per_sealed_cell_per_day"] =
            "What a fully sealed Cell sheds into the body it drains to in a Day — the way in. These "
            + "four keys stand or fall together; omit all four for water with no level.",

        // ---- [disasters] ----------------------------------------------------------------------
        ["[disasters] flood_every_days"] =
            "How many Days between floods. All three keys are required together, and the table is "
            + "refused without a [water] flood_level_percent above it — a flood happens on the "
            + "Hazard Region, so a schedule without one is an event with nowhere to occur. No "
            + "severity constant is authored anywhere: the only constants are durations.",
        ["[disasters] flood_rises_over_days"] =
            "How many Days a flood takes to reach its full extent. Without it a flood would inundate "
            + "its whole reach on a single Tick.",
        ["[disasters] flood_recedes_over_days"] =
            "How many Days it takes to drain away again. A lifetime longer than the interval is "
            + "overlapping floods, which is a world and is not refused.",

        // ---- [districts] ----------------------------------------------------------------------
        ["[districts] prominence_percent"] =
            "How far a peak in the field must stand above its saddle before it is a centre of its "
            + "own, as a percentage of its own height. Zero makes every bump a District and the city "
            + "fragments into as many as it has Cells; 100 is a peak rising from nothing. Delete the "
            + "table for a city with no Districts at all.",
        ["[districts] revisit_ticks"] =
            "How often the Districts are re-derived. There is no spelling here for finding them once "
            + "and freezing them.",
        ["[districts] hysteresis_percent"] =
            "How decisively the field must favour a new District before a Cell changes hands, as a "
            + "percentage of the level its own basin reaches it at. Zero would move a Cell on a tie, "
            + "where the answer is a scan order rather than a finding.",
        ["[districts] migrate_cells"] =
            "The most Cells that may change District in one re-evaluation. A bound of none would "
            + "compute the new boundary and then refuse to move to it; a large value is how you say "
            + "undamped.",

        // ---- [market] -------------------------------------------------------------------------
        ["[market] decay_percent"] =
            "How much of the standing consumption rate survives one Day — the smoothing under a "
            + "Pool's price. Zero is allowed and means no smoothing, which is a twitchy market and a "
            + "real one. Omitting the whole [market] table means every trade clears at the import "
            + "ceiling for ever. Neither key here is a price: a Pool opens at the ceiling and moves "
            + "from there, so no seed exists and none is needed.",
        ["[market] move_cap_percent"] =
            "The furthest a price may travel in one Day, as a percentage of the import ceiling. Zero "
            + "would mean it never moves, which is what deleting the table already says.",

        // ---- [income_tax] ---------------------------------------------------------------------
        ["[income_tax] allowance_per_day"] =
            "What a Citizen may earn in one Day before anything is withheld. It reads earnings for "
            + "a Day and never a balance, so a payment covering several Days is attributed across "
            + "them before any band is consulted. Omitting the whole [income_tax] table means no "
            + "income tax is levied at all; a stated zero means the first unit earned is taxed.",
        ["[income_tax] upper_threshold_per_day"] =
            "The Day's earnings at which the upper band starts. Below it the middle rate applies to "
            + "everything above the allowance; at or above it the upper rate applies to the excess. "
            + "Equal to the allowance is a two-band schedule and is legitimate.",
        ["[income_tax] middle_rate_percent"] =
            "The share withheld from earnings between the allowance and the threshold. Marginal, so "
            + "it never reprices what was earned below the allowance.",
        ["[income_tax] upper_rate_percent"] =
            "The share withheld from earnings above the threshold. It may not be below the middle "
            + "rate: both are marginal, so a rate that fell as earnings rose would make take-home "
            + "income step downward at the threshold and a Citizen would keep less for having "
            + "earned more. All four keys here are one decision — state every one or delete the "
            + "table.",

        // ---- [business_tax] -------------------------------------------------------------------
        ["[business_tax] threshold_per_day"] =
            "The Day's profit at which the upper marginal band starts. Profit below it faces the "
            + "lower rate and profit above it the upper rate, so the two bands meet here. It reads "
            + "profit for a Day and never a Business's balance: a Day that made a loss is simply "
            + "untaxed, and nothing is carried forward to the next one. Omitting the whole "
            + "[business_tax] table means no profit is taxed at all.",
        ["[business_tax] lower_rate_percent"] =
            "The share taken from profit below the threshold. There is no separate tax-free band in "
            + "this schedule and no key for one -- writing zero here is how an author gets one, and "
            + "the threshold then acts as the allowance.",
        ["[business_tax] upper_rate_percent"] =
            "The share taken from profit above the threshold. It may not be below the lower rate: "
            + "both are marginal, so a rate that fell as profit rose would make post-tax profit step "
            + "downward at the threshold and a trade would keep less for having earned more. All "
            + "three keys here are one decision \u2014 state every one or delete the table.",

        // ---- [needs] --------------------------------------------------------------------------
        ["[needs] sustenance_degrade"] =
            "How far Sustenance falls on one occasion. Stated as a positive step whose direction "
            + "belongs to the mechanism: a degrade always falls and a recover always rises. Omitting "
            + "the whole [needs] table means no Household in this city has a Need at all.",
        ["[needs] sustenance_recover"] =
            "How far Sustenance rises when a Household is fed.",
        ["[needs] satisfaction_degrade"] =
            "How far Satisfaction falls on one occasion.",
        ["[needs] satisfaction_recover"] =
            "How far Satisfaction rises when the Household buys what feeds it.",
        ["[needs] education_degrade"] =
            "How far Education falls on one occasion. Attended rather than bought, so this pair is "
            + "required exactly of a file declaring a [[building]] that serves education, and "
            + "refused of one that does not — a rate nothing can reach reads on the page as a "
            + "mechanism the world has.",
        ["[needs] education_recover"] =
            "How far Education rises when a Household attends a school.",
        ["[needs] health_degrade"] =
            "How far Health falls on one occasion. Conditional on a kind serving health, exactly as "
            + "education's pair is.",
        ["[needs] health_recover"] =
            "How far Health rises when a Household attends a health service.",
        ["[needs] floor"] =
            "The deepest deficit any Need may reach. A Need is a relative scalar where 0 is ideal, "
            + "so this is negative. It is required because an unbounded magnitude is the thing a "
            + "long run exists to catch.",

        // ---- [founding] -----------------------------------------------------------------------
        ["[founding] founding_band"] =
            "What a Household spends to capitalise a shop, moved from its balance into the "
            + "Business's. At zero every Household could always afford one, so the means test that "
            + "is the whole of the trigger would stop discriminating. There is deliberately no "
            + "demand key: a Household founds on its own means, and a key reading shop count or "
            + "vacancy would be the RCI meter this design refuses.",
        ["[founding] reconsider_ticks"] =
            "How long every Household takes to consider founding once. The founding pass rides "
            + "[placement]'s trigger rather than owning a cadence of its own, so this must be at "
            + "least one such interval long.",
    };
}
