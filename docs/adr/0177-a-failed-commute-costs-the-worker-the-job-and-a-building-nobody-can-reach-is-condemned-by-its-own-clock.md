# A failed commute costs the worker the job, and a Building nobody can reach is condemned by its own clock

Status: accepted, 10/13/2026.

## Decision

- A run of failed outbound commutes is timed on the commuting Citizen. At a Ruleset deadline the
  Citizen is dismissed and the post opens to job search.
  - Both plan-time failures count: *no route found* and *exceeded commute budget*.
  - The clock measures elapsed Ticks from the first failure. It never tallies missed shifts.
  - The next completed outbound commute clears it. No other Trip Purpose can clear it.
  - A failed homeward commute starts nothing. That Citizen stays at work and keeps working.
- A failed commute adds nothing to the Household, the Business or the premises. The Business
  already loses that worker's labour each missed shift.
- A Building nobody can reach has a cut-off clock of its own, separate from Rule starvation.
  - A Building is cut off when its Access Point is `Address.None`, or when its Segment shares no
    foot or car component with any Outside Connection.
  - The clock runs from the road edit that cuts the Building off. The edit that reconnects it
    clears the clock.
  - At its own Ruleset deadline the clock condemns the Building.
  - The clock never runs in a world with no Outside Connection.
- A reachable shop that loses trade declines through money. The arrears clock bankrupts a staffed
  Business with no sales.

## Why

- Losing the job is what happens to a person who cannot get to work. The decision stays with the
  individual (`adr/0005`), and dismissal and job search already exist.
- Charging the Household made two workers commuting to different places ambiguous. Moving home
  also fixes nothing while housing choice ignores commutes.
- Charging the Business would reverse the rule that a labour shortage is a closed shift and not
  starvation. Charging the premises would reintroduce the cross-subject leak `adr/0141` removed.
- A workplace nobody can reach needed a Building consequence. Its workers are dismissed, its posts
  reopen, and no applicant can reach them, so nothing ends the state. The cause belongs to the
  Building, so condemning it charges nobody twice.
- `adr/0079` requires any road-shaped pressure to be built once, named and given its own
  threshold, and never bolted onto `adr/0053`'s Rule-starvation predicate.
- The cut-off clock measures access and money measures sales, so neither counts the other's failure.

## Consequences

- `CONTEXT.md`'s Failure Pressure and Commute Budget entries said a Building declines when its
  Trips fail. They now name the worker's commute clock and the premises' cut-off clock.
- Counting *exceeded commute budget* is provisional. `plans/decline-recovery.md` measures how often
  it dismisses workers in a congested city that has no broken connection.
- Both clocks are saved state, so they move the State Hash.
