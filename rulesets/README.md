# Choosing a Ruleset

Start with **`neighbourhood.toml` for everyday visual inspection**, **`platted.toml` to
compare building forms**, or **`base/ruleset.toml` to try founding a city yourself**.
For a short playtest with a specific intervention and visible result, follow the
[urban neighbourhood walkthrough](../examples/UrbanNeighbourhood/README.md).

These are demonstrations and development fixtures with provisional tuning. None is a
balanced general-purpose city. A Ruleset enables particular mechanisms; it does not
necessarily supply the Streets, Buildings, services or commands needed to exercise them.

## Quick picks

| I want to… | Choose | What to look for |
|---|---|---|
| Inspect houses, parcels and Streets | [neighbourhood.toml](neighbourhood.toml) | Small houses and independently sized residential parcels; the shell's default. |
| Compare density and building forms | [platted.toml](platted.toml) | Five bands showing detached, perimeter, terrace, courtyard and slab patterns. |
| Inspect attached houses | [rowhouses.toml](rowhouses.toml) | Narrow plots with shared side walls and gardens. The central bands deliberately stay empty. |
| Photograph a varied scene | [pictured.toml](pictured.toml) | Coast, shops, school content, traffic and ruins in one fixture. Individual activities still depend on placement and simulation time. |
| Build from an empty map | [base/ruleset.toml](base/ruleset.toml) | Lay Streets, zone housing, place an edge gate, attract families and place a public school. The treasury has an opening balance but no income. |
| Try one small housing decision | [urban-neighbourhood.toml](urban-neighbourhood.toml) | Permit an addition after Households fail to find acceptable homes, then watch them move in. **Use the prepared save in the walkthrough below.** |
| Watch work and shopping journeys | [shopping.toml](shopping.toml) | Weekly work and physical purchases carried home. Shop supply is synthetic. |
| Try budget controls | [funded.toml](funded.toml), [taxing.toml](taxing.toml) | Public school funding, or income/business taxes and an emissions charge, respectively. Place a school in the funded fixture. |

## Launching

Run these commands from the repository root, using the .NET-enabled Godot installation.
Build the shell first if needed:

```sh
dotnet build src/Borough.Godot
```

For a generated scene, substitute any ordinary single-file Ruleset:

```sh
godot --path src/Borough.Godot -- --ruleset rulesets/platted.toml --citizens 1000
```

With no Ruleset or population arguments the shell selects `neighbourhood.toml` and
1,000 Citizens. Population affects the generated scene's extent and activity; the same
Ruleset at a different population need not show the same outcome.

For manual founding, start empty with the base package:

```sh
godot --path src/Borough.Godot -- --ruleset rulesets/base/ruleset.toml --empty
```

For the small housing playtest, follow the
[UrbanNeighbourhood instructions](../examples/UrbanNeighbourhood/README.md). They export
a saved city containing the street, neighbours and arriving Households, then explain
what to paint and what should happen. Selecting its TOML in an ordinary generated run
does not reproduce that setup. City saves embed their Ruleset; when launching with
`--load`, omit `--ruleset`, `--citizens` and `--empty`.

Service fixtures need facilities placed with the shell's Service tool. Headless flags
mentioned in the catalogue, such as `--school`, `--care` and `--profile-services`, belong
to `Borough.Headless`, not Godot. See its available modes with:

```sh
dotnet run --project src/Borough.Headless -- --help
```

## Full catalogue

Every top-level TOML file and both package entry points are listed below. Package
members are parts of their package, not separate choices. Descriptions identify the
purpose of each fixture, not a guarantee that its outcome occurs immediately or at
every population. File headers contain tuning and experiment details, but some retain
historical implementation claims; use current code and tests to establish behaviour.

### Visuals, layout and terrain

| Ruleset | Demonstrates / when to choose it |
|---|---|
| [neighbourhood.toml](neighbourhood.toml) | Street-scale houses and residential parcel dimensions; everyday visual inspection. |
| [rowhouses.toml](rowhouses.toml) | Attached houses on narrow perimeter plots, with space behind for gardens. |
| [platted.toml](platted.toml) | The complete five-pattern block ladder in concentric bands. |
| [banded.toml](banded.toml) | Density-band admission restrictions: zoning alone does not permit every Building kind. |
| [gridded.toml](gridded.toml) | A street grid with varying block spacing and a repeating hierarchy. |
| [pictured.toml](pictured.toml) | Combined coastal city scene assembled from several mechanism demonstrations; useful for pictures, not attributing an economic result to one cause. |
| [varied.toml](varied.toml) | Terrain types with different Base Fertility, plus woodland and pollution-related tuning. |
| [coastal.toml](coastal.toml) | Water Bodies and a city sited to meet the coast. |
| [flooded.toml](flooded.toml) | Weather-driven flooding, with the city positioned in exposed ground. |
| [fouled.toml](fouled.toml) | Pollution emissions alongside traffic noise, giving land value adverse conditions to respond to. |
| [twinned.toml](twinned.toml) | Two generated centres for observing District derivation. |

### Founding, housing and arrivals

| Ruleset | Demonstrates / required setup |
|---|---|
| [base/ruleset.toml](base/ruleset.toml) | Manual founding, Outside arrivals, housing and a funded public school. Launch with `--empty`; [founding.borough](base/founding.borough) is the accompanying headless Input Log. |
| [urban-neighbourhood.toml](urban-neighbourhood.toml) | Small playable housing intervention; use the [exported-save walkthrough](../examples/UrbanNeighbourhood/README.md). |
| [urban-housing.toml](urban-housing.toml) | Core redevelopment fixture: narrow terraces compete with a two-Lot courtyard. Tests supply neighbours, seekers and geographic permissions; loading the file alone is not the walkthrough. |
| [bordered.toml](bordered.toml) | Outside Connections and four Hinterlands, with roads and cars for reaching distant gates. Arrival commands exercise admission. |
| [crowded.toml](crowded.toml) | Command-driven arrivals outpacing housing, with a short give-up period to expose Unplaced Pool pressure. |
| [welcomed.toml](welcomed.toml) | Prospects accepting or refusing arrival by comparing city homes with Outside rents; supply arrival commands. |
| [attracted.toml](attracted.toml) | Autonomous arrivals from counted Hinterland populations. Admitted Households leave that Outside stock. |
| [attracted-declining.toml](attracted-declining.toml) | Arrivals with fast housing decline and rebuilding, sustaining opportunities to reconsider the city in a longer run. |
| [attracted-outside-cheaper.toml](attracted-outside-cheaper.toml) | Cheaper Outside rents; a comparison/reload partner for `attracted.toml`. Compare continuations from the same saved city. |
| [priced-out.toml](priced-out.toml) | Rent affordability reassessment moving Households back into the Unplaced Pool. |
| [choosy.toml](choosy.toml) | Individual centrality preferences across Life Stages; declining housing gives Households reasons to search. |
| [chosen.toml](chosen.toml) | Probabilistic housing choice: similar Households need not choose the same candidate. |
| [restless.toml](restless.toml) | Supply shortages causing Households to move without a rent or tenancy-expiry trigger. |
| [restless-fed.toml](restless-fed.toml) | Faster restocking as the reload recovery control for `restless.toml`. |

### Traffic and daily journeys

| Ruleset | Demonstrates / when to choose it |
|---|---|
| [severance.toml](severance.toml) | Arterials cutting walking connections on a coarse lattice; useful for inspecting crossings and street ends. |
| [congested.toml](congested.toml) | Cars on constrained Streets; inspect congestion at the chosen population and time. |
| [scarce.toml](scarce.toml) | Parking scarcity with car ownership. |
| [shopping.toml](shopping.toml) | Weekly work, shopping trips and Goods carried home; synthetic shop supply. |
| [stress-shopping.toml](stress-shopping.toml) | Shopping and work under traffic load across two centres; a profiling fixture, not the first choice for light play. |
| [profile-services.toml](profile-services.toml) | Combined shopping, wages, traffic, Life Stages, education and care workload. Headless `--profile --profile-services` distributes facilities. |

### People, needs and services

| Ruleset | Demonstrates / required setup |
|---|---|
| [aged.toml](aged.toml) | Life Stage progression, Household formation and dissolution, working-age eligibility and retirement of empty housing. |
| [raised.toml](raised.toml) | Life Stages combined with Business founding, exercising working-age eligibility for founders. |
| [hungry.toml](hungry.toml) | Sustenance deficit accumulating when consumption cannot be supplied. |
| [stocked.toml](stocked.toml) | Shared baskets, reserves and recipes governing consumption, Bin capacity and production. |
| [schooled.toml](schooled.toml) | Education as an attended Need. Place schools, or use the headless `--school` instrument. |
| [scheduled-school.toml](scheduled-school.toml) | Weekday school attendance, stays and independent return journeys; requires schools. |
| [schooling.toml](schooling.toml) | Childhood education, further education and Skill Tiers with economic consequences; place the relevant education facilities. |
| [care.toml](care.toml) | Generic illness, clinics, inpatient beds and school journeys. Place facilities, or use headless `--care`. |

### Business, money and government

| Ruleset | Demonstrates / when to choose it |
|---|---|
| [tenanted.toml](tenanted.toml) | Business kind declarations and premises-linked bakery content; a small content fixture. Use `founded.toml` for the founding mechanism. |
| [founded.toml](founded.toml) | Households capitalising Businesses and Businesses taking premises. **Business founding**, distinct from starting a city with `base/`. |
| [provisioned.toml](provisioned.toml) | Providers selling Goods through District markets, with demand-responsive shop construction. |
| [oversupplied.toml](oversupplied.toml) | Excess shop construction and competitive failure; compare with `provisioned.toml`. |
| [waged.toml](waged.toml) | Businesses paying wages on staggered paydays. |
| [insolvent.toml](insolvent.toml) | Repeated short payrolls winding up a Business while leaving its premises standing. |
| [taxed.toml](taxed.toml) | Household money and policy transfers, making taxation and treasury spending observable. |
| [levied.toml](levied.toml) | A policy collecting money from Businesses. |
| [funded.toml](funded.toml) | A public school paid for by the treasury, including funding its teaching jobs; place a school. |
| [taxing.toml](taxing.toml) | Income tax, business tax, public works and an emissions charge in a shopping economy. |

### Failure, maintenance and technical controls

| Ruleset | Demonstrates / when to choose it |
|---|---|
| [minimal.toml](minimal.toml) | Basic Bins and Rules plumbing, with placeholder content. Its name does not mean “starter city.” |
| [minimal-tuned.toml](minimal-tuned.toml) | A restocking change for reload comparisons with `minimal.toml`. |
| [split/ruleset.toml](split/ruleset.toml) | The minimal content split into a package; explicit membership, cross-member references and Rule order. |
| [declining.toml](declining.toml) | Premises failure, condemnation, collapse and rebuilding; also used by golden replay fixtures. |
| [declining-tuned.toml](declining-tuned.toml) | The restocking-change reload partner for `declining.toml`, used by the golden session. |
| [diagnosed.toml](diagnosed.toml) | Failure conditions attached to condemnation, so the evidence can explain why a Building failed. |
| [maintained.toml](maintained.toml) | Repair supply as the maintenance control against `declining.toml`. |
| [thinned.toml](thinned.toml) | Occupant shedding under premises pressure before condemnation, with upkeep linked to occupancy. |
| [evicted.toml](evicted.toml) | Tenancies ending from failed consumption while the premises remain standing. |

## Authoring and maintenance

See the [authoring guide](../docs/ruleset-authoring.md) for the content format and the
[generated key reference](../docs/ruleset-reference.md) for individual settings. Read a
fixture's header before changing its tuning. For golden fixtures, follow the
[re-record procedure](../tests/Borough.Tests/Golden/README.md); comments affect content hashes too.

When adding or removing a runnable Ruleset, update this catalogue. State what it
demonstrates and any setup needed to make that visible. Add it to the quick picks only
when it is useful for ordinary playtesting or visual inspection.
