# References

Annotated bibliography for the project. Entries are tagged:

- `USE` — directly applicable to what we are building
- `SCOPE` — read as a scoping document; tells us where a cliff is, not how to build
- `FREEFORM-ONLY` — relevant only if we abandon grid-snapped roads
- `HISTORICAL` — context, not guidance

Where a work was cited by another project, that is noted. Where we are supplying the canonical reference for a technique someone used *without* attribution, that is noted too — the distinction matters when judging how carefully a design was reasoned.

---

## 1. Land use and emergent growth

This is the literature underpinning [`02-simulation-model.md` §5](02-simulation-model.md). Our "households seeking housing" model is, it turns out, a game-shaped version of an established academic field: **integrated land-use and transport (LUTI) microsimulation**. Worth knowing before we reinvent it badly.

All of the following are **cited by Citybound** on its project page.

**Weidner, T., Moeckel, R. & Brinckerhoff, P. (2011). "SILO: A Land-Use Model For Integrated Modeling."** CUPUM 2011. [PDF](https://silo.zone/doc/silo_cupum_2011.pdf) · [Summary PDF](https://www.umdsmartgrowth.org/wp-content/uploads/2020/07/SILO-for-Website.pdf) `USE` — **read this first**
Closer to our design than UrbanSim is. SILO *explicitly rejects* full-information utility maximisation: agents "will not know information on all of the vacant dwellings in the region when looking to move. Rather than maximizing utility, agents look to satisfy requirements, within time and money constraints, that may be biased or based in habit." That is satisficing under constraints — our `BOUNDED KNOWLEDGE` tag, arrived at independently.

Two mechanisms worth stealing: **Cobb-Douglas utility aggregation** (components multiply, so zero on any one component yields zero total — expressing "no amount of cheapness compensates for zero reachable jobs" structurally); and **vacancy-rate-driven development**, which is simpler than UrbanSim's pro-forma and directly displayable. Open source (Java) at [silo.zone](https://silo.zone).

**Waddell, P. et al. (2003). "Microsimulation of Urban Development and Location Choices: Design and Implementation of UrbanSim."** *Networks and Spatial Economics* 3(1):43–67. `USE`
The architectural reference — agent queue → sampled choice set → logit → capacity consumption → price adjustment → developer response. Superseded and paywalled; **read these free ones instead**: [Waddell 2018, "Architecture for Modular Microsimulation of Real Estate Markets and Transportation"](https://arxiv.org/abs/1807.01148) and [Waddell et al. 2018, "An Integrated Pipeline Architecture"](https://arxiv.org/abs/1802.09335). Code at [github.com/UDST/urbansim](https://github.com/UDST/urbansim) — the real documentation; `models/supplydemand.py` and `urbanchoice/interaction.py` are the two files that matter.

**McFadden, D. — random utility maximisation, the multinomial logit model, and sampling of alternatives.** `USE` — *the underlying tool; not cited by Citybound.*
Best free introduction: [Ben-Akiva's MIT OCW notes on MNL, IIA, and nested logit](https://ocw.mit.edu/courses/1-201j-transportation-systems-analysis-demand-and-economics-fall-2008/1fe5a6531216e44dbe94543535a1ddd3_MIT1_201JF08_lec04.pdf). Train's *Discrete Choice Methods with Simulation* is the standard reference and is free from the author.

**The finding that matters most:** under *uniform* random sampling of alternatives, no correction term is needed — the sampling probability is identical across alternatives and cancels out of the choice probability. Further, McFadden's correction is an **estimation** artifact, needed only when fitting coefficients to observed data. We author coefficients, so it does not apply to us at all. Sampling `N` alternatives is therefore not an approximation of a full-enumeration model — it *is* a bounded-rationality behaviour model, and `N` is a gameplay dial. See [`02-simulation-model.md` §5.3](02-simulation-model.md).

**Kim, D. (2012). "Modelling Urban Growth: Towards an Agent Based Microeconomic Approach to Urban Dynamics and Spatial Policy Simulation."** UCL PhD thesis. [Free full text](https://core.ac.uk/download/pdf/16247712.pdf) `USE`
Theory-first and calibration-light — the regime a game lives in. Asks our question: what is the minimal set of microeconomic rules that produces recognisable urban form? Its greenbelt and transit policy experiments are directly analogous to player actions. Skim the model and experiment chapters; skip the literature review.

**Koomen, E. et al. (2015). "A utility-based suitability framework for integrated local-scale land-use modelling."** `SCOPE`
One good idea, not worth the paper: **express every sector's desire for a parcel as a bid price in a single currency, then let the highest bidder win.** That is a unified mechanism for resolving residential-vs-commercial-vs-industrial competition for the same Lot, and it is legible to the player. The rest is calibration against Dutch land-use statistics.

**Park, I. K. & von Rabenau, B. (2011). "Disentangling agglomeration economies."** `SCOPE`
An econometrics paper, not a runnable model. Take one concept: agglomeration is *several distinct forces* — productivity (firms near firms), amenity (density brings variety, benefits households), and congestion (hurts everyone). Give each its own term and agglomeration *and its limits* both emerge. Its Ohio-county coefficients are meaningless to us.

**Beckmann, K. J. et al. (2007). "ILUMASS."** [Post-mortem PDF](http://www.spiekermann-wegener.com/pub/pdf/ILUMASS_IJUS.pdf) `HISTORICAL` — **read the post-mortem, not the model**
Seven German institutes, six years, a fully microscopic land-use + transport + environment simulation that **never became fully operational**. Documented causes: integration complexity across independently-developed subsystems, very long run times, and **file-based data transfer between modules**.

It died of integration overhead, not bad theory. The architectural lesson is direct: if we build housing, jobs, traffic, and economy as separate systems exchanging state across a serialisation boundary, we get ILUMASS. Keep everything in one address space with shared typed in-memory state.

**Tiglao, N. C. C. (2005). "Modeling Households and Location Choices in Metro Manila."** `HISTORICAL` — **skip**
Primarily about synthesising a population from census marginals when disaggregate microdata is unavailable. A real problem for researchers matching a real city; we *author* our population and have no census to match.

> **Caveat for this entire section.** These are calibration-heavy models fitted against census and travel-survey data. We have no ground truth and player legibility matters more than statistical fidelity. Take the *structure*; reject the calibration apparatus. The deviations we make deliberately are tabulated in [`02-simulation-model.md` §5.8](02-simulation-model.md).

---

## 2. Routing

The live decision, tracked in the plan's pathfinding section. Nothing here was cited by Citybound in code; the first entry is the algorithm it implemented without attribution.

**Tsuchiya, P. F. (1988). "The Landmark Hierarchy: A New Hierarchy for Routing in Very Large Networks."** ACM SIGCOMM '88, CCR 18(4):35–42. [PDF](https://pdfs.semanticscholar.org/43f6/6f4834b4412b7ed1d6e3e86960ac54e7abf6.pdf) `USE`
Unmistakably Citybound's routing design, down to the radius-bounded visibility rule, explained by its inventor. Its selling point is exactly our constraint: landmark hierarchies get area-hierarchy path lengths and table sizes while being *"easier to dynamically configure using a distributed algorithm, allowing for very large, dynamic networks"* — no global preprocessing when the player bulldozes.

**Perkins, C. E. & Bhagwat, P. (1994). "Highly Dynamic Destination-Sequenced Distance-Vector Routing (DSDV) for Mobile Computers."** ACM SIGCOMM '94. [PDF](https://www.cse.iitb.ac.in/~mythili/teaching/cs653_spring2014/references/dsdv.pdf) `USE`
**Read this as a bug report on Citybound's implementation.** DSDV's contribution is per-destination sequence numbers guaranteeing loop freedom under churn. Citybound's routing entries have no sequence numbers — only distance comparison and `learned_from`. Classical distance-vector without them suffers **count-to-infinity**: on link deletion, stale routes circulate and distances creep upward one hop at a time instead of being withdrawn. Its `routing_timeout` (15 ticks) and `forget_routes` are ad-hoc mitigations.

In a normal network, link deletion is a rare fault. **In a city builder it is the core verb.** If we adopt distance-vector routing, we take DSDV's version, not Citybound's.

**Botea, A., Müller, M. & Schaeffer, J. (2004). "Near Optimal Hierarchical Path-Finding" (HPA\*).** *Journal of Game Development* 1(1):7–28. [PDF](http://webdocs.cs.ualberta.ca/~mmueller/ps/2004/hpastar.pdf) `USE`
The other side of the decision. Clusters with entrance nodes; up to 10× faster than optimised A\* at within 1% of optimal. Two properties matter for us: a complete path is not required — it returns a sequence of sub-problems, so the first can be solved and movement begun; and topology changes invalidate only the affected cluster and its borders.

**Sturtevant, N. & Buro (2005). "Partial Pathfinding Using Map Abstraction and Refinement" (PRA\*).** AAAI. [PDF](https://webdocs.cs.ualberta.ca/~nathanst/papers/partialpathfinding.pdf) `HISTORICAL` — **evaluated and rejected.** Cited by Citybound (which dates it 2004; the paper is 2005 — an error we inherited).

On static maps PRA\* and HPA\* are equivalent in speed (~10×) and quality (~1% above optimal). The differentiator is *partial refinement* — returning a truncated path so an agent starts moving before planning finishes, on a 1 ms/frame budget. **That solves an RTS problem we do not have**: our agents have long, stable routes and arrivals are not latency-critical, so planning can be amortised freely.

Meanwhile the property we *do* need is the one PRA\* does not deliver. Its dynamic-update story is a single sentence — *"it is possible to repair abstractions in O(log n) time per update. A full description of these methods, however, is beyond the scope of this paper"* — and the follow-up paper was never written. Worse, the reason is structural: **PRA\*'s abstraction is derived from connectivity (bottom-up 4-cliques), so a topology edit changes the partition itself and can cascade sideways.** HPA\*'s clusters are a fixed spatial partition we chose, so invalidation is bounded by construction.

Deriving abstraction from structure is what makes PRA\* elegant on static maps and exactly what makes it wrong when the structure is the thing being edited.

**Jansen & Buro, "HPA\* Enhancements"** [PDF](https://cdn.aaai.org/ojs/18791/18791-52-22495-1-10-20210929.pdf) and **"DHPA\* and SHPA\*: Efficient Hierarchical Pathfinding in Dynamic Environments"** [PDF](https://ojs.aaai.org/index.php/AIIDE/article/download/12397/12256/15925) `USE`
The follow-up literature PRA\* lacks. DHPA\* is named for our exact problem. Reference implementation: [hugoscurti/hierarchical-pathfinding](https://github.com/hugoscurti/hierarchical-pathfinding).

**Bast, H. et al. (2016). "Route Planning in Transportation Networks."** [arXiv:1504.05140](https://arxiv.org/pdf/1504.05140) `SCOPE`
The definitive survey, with an explicit axis for preprocessing cost vs. query time vs. update cost under dynamic edge weights. That axis *is* our decision. Read the taxonomy; skip the continental-scale benchmarks.

**Geisberger, R. et al. (2008). "Contraction Hierarchies."** WEA 2008. `SCOPE`
Included so we can rule it out deliberately. Microsecond queries, but heavy preprocessing and a static-graph assumption — wrong shape for a network the player rewrites mid-game. Check the customizable/dynamic CH follow-ups before dismissing entirely.

### Current standing of the decision

**PRA\* is out** (above). Between HPA\* and distance-vector, the reading has complicated rather than settled things, and the reason is worth stating because it reframes the question.

**Our dominant query is not "shortest path from A to B."** The household location-choice loop asks *"what is the commute from this candidate dwelling to any job?"* — many-to-many, evaluated tens of thousands of times per cycle. Accessibility fields ask *"how many jobs are within 30 minutes of here?"* Only vehicle steering asks for a next hop.

The first two are **routing-table queries**, and a distance-vector protocol answers all three in O(1) after convergence while being **incremental by construction** — "recompute the affected region" *is* the algorithm, with no abstraction to rebuild and no invalidation logic to write.

Two counter-considerations keep this open:

1. **Our design already answers the many-to-many query another way** — the zone-to-zone travel-time matrix serving the Statistical tier. If that matrix carries the choice loop (and it should; §5.8 makes "never resolve a route inside the choice loop" a rule), then the detailed-tier router only handles vehicle steering, and the many-to-many argument for distance-vector largely evaporates.
2. **Distance-vector still needs DSDV sequence numbers** or an equivalent loop-freedom mechanism, per the entry above.

Meanwhile the argument for HPA\* stands: Citybound's landmark election exists partly to *impose* structure on an irregular freeform graph, and our grid already provides the regular tiling HPA\*'s clusters assume.

**Suggested resolution for the spike:** build the travel-time matrix first and see how much work is left over. If the matrix carries accessibility and commute queries, the residual problem is narrow and HPA\* is the low-risk answer. If the matrix proves too coarse or too stale, distance-vector's unified answer becomes attractive — with sequence numbers.

Note as a prior, not proof: Eickhoff cited the hierarchical-abstraction literature and chose distance-vector anyway, for a game with our constraint.

---

## 3. Traffic

**Treiber, M., Hennecke, A. & Helbing, D. (2000). "Congested traffic states in empirical observations and microscopic simulations."** *Phys. Rev. E* 62(2):1805–1824. [arXiv preprint](https://arxiv.org/pdf/cond-mat/0002177) `USE`
The Intelligent Driver Model. Citybound implements it (with hardcoded constants: acceleration 0.4, max deceleration 5.0, exponent 4, minimum spacing 4.0, car length 4.0) and cites *only the Wikipedia article*. Car-following is orthogonal to road geometry, so this transfers to a grid unchanged.

**Kesting, A., Treiber, M. & Helbing, D. (2007). "General Lane-Changing Model MOBIL for Car-Following Models."** *TRR* 1999:86–94. [Free TGF'07 version](https://www.akesting.de/download/MOBIL_TGF07.pdf) `USE`
The companion lane-changing model, defined in terms of the car-following model's own acceleration function — so it drops directly onto IDM.

**Worth knowing: Citybound's lane changing is much weaker than its reputation.** Switching is triggered purely by longitudinal position — a car begins switching when within ~300 units of the end of a switchable stretch. It is *mandatory, routing-driven only*. There is no incentive criterion and no politeness term, so there is **no discretionary lane changing at all** — no overtaking a slow truck, no keep-right. If we want lanes chosen for reasons other than the next turn, MOBIL is the missing half.

**Blatnig, S. (2008). "Microscopic Traffic Simulation with Intelligent Agents."** `HISTORICAL` — cited by Citybound.

---

## 4. Incremental computation

**Hammer, M. A. et al. (2015). "Incremental Computation with Names."** OOPSLA. [arXiv](https://arxiv.org/abs/1503.07792) · original: ["Adapton: Composable, Demand-Driven Incremental Computation"](https://www.cs.tufts.edu/~jfoster/papers/cs-tr-5027.pdf), PLDI 2014 `SCOPE` — **cited by Citybound. Evaluated and rejected as a dependency.**

The problem is structurally ours — the player edits the road network, and travel times, accessibility, land values, and coverage fields all need updating. Adapton solves it generally via a demanded computation graph with dirtying and propagation phases.

**Do not adopt it.** The measured overhead is 12–20× wall-clock and 3–6× memory versus a non-incremental baseline, recouped only when demanding a *tiny* fraction of a large output after a *small* input change. We are in the opposite regime: the accessibility field is read by every Household in the choice loop, every cycle. The benchmarks show Adapton *losing to from-scratch* above roughly 5% demand. Additional blockers: its guarantees rest on inner-layer purity, which our mutable RNG-consuming loop violates; the Rust crate is a research artifact; and getting cache *names* right is itself a subtle ongoing burden — precisely what adopting a library was meant to avoid.

Also: Adapton's value is *automatically discovering* dependencies in arbitrary code. Ours fit on a napkin — `road network → travel times → accessibility → choice utilities → prices → development feasibility`. Six nodes.

**What to steal is the vocabulary and one trick.** Read §2 of the tech report (six pages, genuinely illuminating), then close it and hand-roll. The trick worth taking: **stop propagating when a recomputed value is unchanged.** If accessibility at a node recomputes to the same number, do not dirty land value. Hand-rolled dirty-flag systems routinely forget this.

**And the bigger lever is simply recomputing less often.** UrbanSim refreshes accessibility once per simulated *year*. Most of the value people seek from incremental computation is available by not recomputing every tick.

---

## 5. Architecture and data layout

None of these are cited by Citybound in any citable form; its `kay` README name-drops "Data-Oriented Game Development", "Erlang", and "Object-Oriented Programming", plus a Wikipedia link for Alan Kay.

**Armstrong, J. (2003). "Making reliable distributed systems in the presence of software errors."** PhD thesis, KTH. [PDF](https://erlang.org/download/armstrong_thesis_2003.pdf) `HISTORICAL`
Isolated processes, no shared state, "let it crash" plus supervision. Explains Citybound's design of essential message types surviving actor panics — which is the ancestor of our Past/Future crash-forensics idea.

**Acton, M. (2014). "Data-Oriented Design and C++."** CppCon keynote. [Video](https://www.youtube.com/watch?v=rX0ItVEVjHc) `USE`
The canonical statement of "the purpose of a program is to transform data". Source of the cache-locality arguments behind our typed-tables decision.

**Fabian, R. (2018). *Data-oriented Design*.** [Free full text](https://www.dataorienteddesign.com/dodbook/) `USE`
Book-length treatment. The chapters on existential processing and component relations bridge from Acton's talk to an actual entity store.

**Kay, A. (1998). Squeak-dev mailing list, 10 October 1998.** [Archived](http://wiki.c2.com/?AlanKayOnMessaging) `HISTORICAL`
*"The big idea is 'messaging'."*

---

## 6. Procedural architecture

Relevant only when we get to building visuals; buildings are independent of road topology so all of this transfers to a grid intact.

**Müller, P., Wonka, P., Haegler, S., Ulmer, A. & Van Gool, L. (2006). "Procedural Modeling of Buildings."** SIGGRAPH / *ACM TOG* 25(3). `USE`
CGA shape — context-sensitive rules over a hierarchy of scopes, with the `split`/`repeat`/`comp` operators every subsequent building generator reimplements, including Citybound's. Rectangular lots are the easy case.

**Wonka, P., Wimmer, M., Sillion, F. & Ribarsky, W. (2003). "Instant Architecture."** *ACM TOG* 22(3). [TU Wien page](https://www.cg.tuwien.ac.at/research/publications/2003/Wonka-2003-Ins/) `USE`
Split grammars plus a separate **control grammar** distributing style attributes spatially — the clean answer to "make this neighbourhood consistent and the next one different" without hand-authoring.

**Kelly, T. & Wonka, P. (2011). "Interactive architectural modeling with procedural extrusions."** `FREEFORM-ONLY` — cited by Citybound.

**Kelly, T. (2013). "Unwritten Procedural Modeling with Skeletons."** PhD thesis. `FREEFORM-ONLY` — cited by Citybound.

---

## 7. Computational geometry

**Entire section is `FREEFORM-ONLY` and exists to document what our road decision bought us.**

Citybound's `descartes` library chose the *opposite* of robust exact predicates: hand-tuned epsilons, "thick" primitives, and a `RoughEq` trait absorbing both floating-point error and sloppy user input. Grid snapping gives exact integer coordinates and makes both approaches unnecessary. This is a large bug class designed away rather than solved.

- **Shewchuk, J. R. (1997). "Adaptive Precision Floating-Point Arithmetic and Fast Robust Geometric Predicates."** [PDF](https://people.eecs.berkeley.edu/~jrs/papers/robust-predicates.pdf)
- **Aichholzer, O. & Aurenhammer, F. (1996). "Straight Skeletons for General Polygonal Figures in the Plane."** — the usual basis for carving irregular lots out of a block. With grid-snapped blocks, lots are axis-aligned rectangles and this collapses to arithmetic.
- **Chen, X. & McMains, S. (2005). "Polygon Offsetting by Computing Winding Numbers."** [PDF](https://mcmains.me.berkeley.edu/pubs/DAC05OffsetPolygon.pdf) — centerline → road surface → sidewalk → frontage. Pre-authored junction pieces are the design move that lets us not implement this.

---

## 8. Scoping — problems to recognise and retreat from

**Gunawan, A., Lau, H. C. & Vansteenwegen, P. (2016). "Orienteering Problem: A survey of recent variants, solution approaches and applications."** *EJOR* 255(2):315–332. `SCOPE` — **cited by Citybound**, in "Down the Rabbit Hole", where he identifies his household activity-planning problem as a MOTDAOPTW (Multiobjective Time-dependent Arc Orienteering Problem with Time Windows), *"NP-hard and requires on the order of seconds of CPU time"* per route.

Read as a **negative result**, not a build plan. Its value is that it tells you in ten pages that per-household activity scheduling is NP-hard and second-scale — which is why Citybound never shipped a working economy, and why our design uses bounded satisficing households instead. Knowing where that cliff sits is the most transferable thing in it.

> The exact survey he linked (`mysmu.edu/faculty/hclau/EJOR - Orienteering Survey.pdf`) is unreachable. This is the most likely candidate — the URL sits on Hoong Chuin Lau's own faculty page and he co-authored it. The 2011 Vansteenwegen/Souffriau/Van Oudheusden EJOR survey is the alternative.

---

## 9. Genre prior art — zoning, density, and where demand comes from

Absent from this bibliography until [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md) and [`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md), which is a gap worth naming: the academic literature above tells us how to *model* land use, and these tell us what players have already been taught to expect. Both ADRs turn on the difference.

Primary sourcing is harder here than for papers. `wiki.sc4devotion.com` and `community.simtropolis.com` — the two most technical sources — sit behind bot protection and are reachable only via search snippets. Where a fact below is secondhand, it says so.

**SimCity 4 (Maxis, 2003) — density is a cap.** `USE`
[StrategyWiki: Zoning and Demand](https://strategywiki.org/wiki/SimCity_4/Zoning_and_Demand) · [Simtropolis: "Overcoming the Peanut Butter Point"](https://community.simtropolis.com/forums/topic/46826-overcoming-the-peanut-butter-point/)

Residential and Commercial carry Low/Med/High density; Industrial instead carries a *type* ladder (Agriculture → Dirty → Manufacturing → High-Tech), which is not a density axis. **Zoning high density is permission, not instruction** — *"they will then slowly upgrade if demand is there"* — so a high-density zone on cheap land grows small, low-stage buildings. The asymmetry is deliberate and worth stealing: **upzoning is permissive, downzoning is a command** that demolishes over-tall buildings immediately.

Wealth (§/§§/§§§) is orthogonal to density and to Stage — three axes, not one ladder. Stage advancement is gated by **regional population thresholds** (widely repeated as ~1,114 for mid-rise and ~25,952 for high-rise, attributed to the Prima guide; *not primary-source confirmed*). **That gate is the part to reject**: a hidden global scalar deciding local outcomes, structurally identical to the RCI meter `00-vision.md` pillar 1 forbids.

**The Peanut Butter Point** is the single most instructive failure in the genre for us. Mid-game the city stalls — nobody arrives, nobody leaves, zoning more does nothing — and the community remedy was *place parks to raise the residential demand cap*, with neighbour connections raising the commercial one. **The stall had no cause to surface**, because nothing true was happening; it was a constant, and the counterplay was an out-of-world fact learned from a forum. [`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md) is designed against this specifically: our equivalent stall is a stock being drawn down, and the counterplay is visible in the thing draining.

**SimCity (Maxis, 2013) / GlassBox — density derived from road tier.** `SCOPE`
[Parsimonious: Zoning — About Density](http://www.parsimonious.org/simcity5/zoning-density.html) · [Roads & Mass Transit: Upgrading & Zone Density](http://www.parsimonious.org/simcity5/roads-upgrading.html)

**There is no density brush at all.** You zone undifferentiated R/C/I and the *adjacent road tier* sets the ceiling (dirt track → street → medium street → high-density avenue → streetcar avenue). Also a cap, and the source is emphatic: *"High density roads does NOT always equal high density buildings… you can just as easily have a network filled with high density roads that only have small low density buildings."*

Two mechanics worth remembering. Redevelopment upward requires **every building inside the footprint of the prospective larger building** to have maxed out first — density change is slow and needs lot merging, where *wealth* change is near-instant. And road geometry bites back: widening a street to an avenue consumes the depth between parallel roads and can permanently strand a block below high-density viability.

**Read as the tempting wrong answer.** Road-derived density is seductive for us because it makes density a condition read off the map. [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md) rejects it on the grounds that SC2013 needed the gate *because its traffic was fake* — ours is not, so the gate would pre-empt the lesson our engine exists to teach.

No developer rationale for the SC4 → SC2013 shift could be found. Quigley's GDC 2013 talk, Librande's BLDGBLOG interview, and Willmott's *Inside GlassBox* were all checked; none discusses it. Treat this as an open gap rather than an absence of reasons.

**Cities: Skylines 1 (Colossal Order, 2015) — density is a command.** `SCOPE`
[Zoning](https://skylines.paradoxwikis.com/Zoning) · [Residential](https://skylines.paradoxwikis.com/Residential)

Residential and Commercial split Low/High; Office and Industrial have no density axis, only specialisations. Growable prefabs are authored into **mutually exclusive asset pools**, so a high-density cell can never spawn a house — poor conditions yield a level-1 apartment, not a bungalow. (Inferred from the asset pipeline and modding documentation rather than a quoted developer statement.) Building **level 1–5** is the emergent axis, driven by land value and education; density is the painted one. No DLC added a middle tier.

**Cities: Skylines 2 (2023)** extends residential to six archetypes — Low, Medium Row Housing, Medium, Mixed, Low Rent, High — with documented households-per-tile ranges and roughly +25% capacity per level. **Row housing as a distinct band is direct support for [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s subdivide-versus-stack split**: CO shipped it as its own thing because it genuinely is one. The cap-versus-command model is unchanged from CS1, and no developer commentary on that choice was found for either title.

**The convergence worth noting:** SC4 separates density from wealth; CS separates density from level. Two lineages that agree on almost nothing else both refuse to collapse capacity and quality into one ladder. `adr/0025` adopts that and rejects everything else about both.

**RollerCoaster Tycoon (Chris Sawyer, 1999) — arrival rate, not a demand meter.** `USE`

Cited for structure rather than documentation. Park rating does not *cause* attendance; it sets an **arrival rate**, and every guest that arrives is an individual with their own money, happiness, and decisions, who walks in through the gate and leaves through it. This is the model [`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md) takes for immigration, and the distinction that makes it not-RCI: **a scalar that causes people is not a scalar that causes buildings.**

---

## 10. Genre prior art — how waste is carried, and why nobody carries it one way

Absent until milestone 24's task 6 needed it, and the gap is the same shape as §9's. The question that
sent me here is narrow and structural: [`adr/0031`](adr/0031-one-resource-abstraction-and-depth-not-count.md)
distinguishes Resource families on **one axis — whether moving it between Districts requires a Vehicle**
— and the corpus answers that question about Waste four different ways.
[`CONTEXT.md`](../CONTEXT.md) → Water Body calls it *"the Waste family"*; `CONTEXT.md` → Resource puts it
in the **Good** row, moving as a Shipment; [`04 §1`](04-economy-and-goods.md) calls it *"service capacity
… coverage rather than a Good"*; and `adr/0031`'s own table lists it among the four escapees, quoting
[`03 §1`](03-agent-architecture.md)'s *"production → flow → treatment"*. The build has no `Waste` member
in `ResourceFamily` at all.

**The finding is that the question is malformed, and the genre has known it for twenty-five years.**
Every title below that models waste seriously ships **two** mechanisms, and the seam falls exactly on
`adr/0031`'s axis: solid refuse moves in a **vehicle on the road network**, liquid waste moves through a
**pipe or flow network with no vehicle**. No surveyed title unifies them.

Sourcing is the same difficulty §9 records — community wikis rather than developer statements, and no
design rationale was found for the split in any title. It is treated below as **convergent practice**,
which is evidence about what works and not a reason. `adr/0043` types the disposition question as
*arguable*; nothing here decides it.

**Cities: Skylines 1 (Colossal Order, 2015) — refuse is a Vehicle, and the road network is the
constraint.** `USE`
[Garbage disposal](https://skylines.paradoxwikis.com/Garbage_disposal) · [Water and sewage](https://skylines.paradoxwikis.com/Water_and_sewage)

Every building accumulates garbage in its own buffer, and the only way to clear it is a truck: *"Garbage
collection is completely dependent on the road network, as the only way to clear out garbage is for a
garbage truck to drive by and collect it, meaning traffic levels and road design have a big impact on how
much garbage accumulates in buildings."* Landfills fill and then need **outbound** trucks to empty
themselves into incinerators — so the disposal site is a stock with an inflow and an outflow, both
vehicular. ***This is the purest "waste is a Good" implementation in the genre***: a Shipment, in a
Vehicle, in the jam.

The same game runs sewage the other way, and the contrast inside one title is the useful part. *"Drain
pipes simply dump raw sewage into the water and can quickly cause massive amounts of water pollution"*,
and the counterplay is positional: *"You want to place your pumping station upstream of any sewage drains
so that it will not be contaminated by water pollution."* **No vehicle appears anywhere in that
sentence.**

⚠ **Read the water half as `SCOPE` rather than `USE`.** CS1's water is a simulated fluid with enough
fidelity that a pump's draw can reverse a river's flow locally — a widely reported behaviour, and
secondhand here. `CONTEXT.md` → Water Body is a **graph with an outflow rate to the next body
downstream**, which is deliberately several orders coarser. The lesson to take is that *contamination is
carried by the water's own direction*; the lesson to refuse is the fluid simulation that makes it true.

**Workers & Resources: Soviet Republic (3Division, 2019) — the split, and the exception that proves the
axis.** `USE`
[Waste management](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Waste_management) · [Sewage](https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Sewage)

The most instructive title here, because it ships both mechanisms at full detail and lets one of them
break the rule. Refuse is produced by dwellings and workplaces, collected by **garbage trucks**, stored
in dumps or containers, and burned in incinerators that may generate power or heat. Wastewater is
collected *"through small, medium, big sewage pipes"* to a treatment plant, and *"wastewater can be dumped
in a river or lake with the help of a sewage discharge."*

🔴 **And wastewater may also be moved by a sewage truck**, which is the detail worth keeping. A pipe is
the *cheap* way to move liquid waste, not the only way — so the family axis is about what the mechanism
**is denominated in**, never about what a desperate player can physically do. A design that made
"Utility" mean *cannot be trucked* would be contradicted by a shipped game; one that makes it mean *flows
on a graph by default* is not.

**SimCity 4 (Maxis, 2003) — two systems, two vocabularies, one city.** `USE`
[StrategyWiki: Sanitation](https://strategywiki.org/wiki/SimCity_4/Sanitation)

Garbage is a landfill **zone** you paint, plus a Recycling Center and a Waste-to-Energy plant; sewage is a
**pipe network** with treatment plants, and the treatment plants *"don't pollute the ground so can be
plopped close to water"*. The garbage half is notably softer than CS1's — capacity and coverage rather
than a completed trip — which is worth noting against §9's finding that SC4 and CS diverge on almost
everything: **on this question they agree about the seam and disagree about the fidelity either side of
it.**

**SimCity 3000 (Maxis, 1999) — waste crosses an ownership boundary, and transport is what gates it.**
`USE`
[Simtropolis: To export, or dispose of garbage on your own](https://community.simtropolis.com/omnibus/other-games/to-export-or-dispose-of-garbage-on-your-own-in-simcity-3000-r660/) · [SimCity Wiki: Garbage](https://simcity.fandom.com/wiki/Garbage)

Neighbouring mayors buy and sell garbage disposal, and a city with spare capacity **earns** by importing
it. The gate is the part to steal: *"Garbage deals are only available if you have built road, rail or
seaport connections to a neighboring city."* ***Waste is a tradeable commodity whose trade is
conditioned on a transport link***, which is
[`adr/0050`](adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)'s
shape arriving from an unexpected direction, and a direct precedent for a Hinterland that accepts
outflow. Both citations are secondhand community documentation.

**Timberborn (Mechanistry, 2021) — contamination as a property of the water graph.** `SCOPE`

Cited for structure rather than documentation, as §9 cites RollerCoaster Tycoon. Badwater enters the
river network and travels **downstream**, contaminating what it reaches; the counterplay is damming,
diverting and outlasting it rather than collecting it. **It is the clearest statement in any of these
titles that pollution can be a property of a flow network and not of a place** — which is what a Water
Body with a capacity and a downstream edge is trying to be.

### What this says about the disposition question, and what it does not

**Convergent practice across four independent lineages** — Maxis 1999 and 2003, Colossal Order 2015,
3Division 2019 — is that solid and liquid waste are **different mechanisms with different transport**,
and every one of them puts the seam on the axis `adr/0031` already uses. That is corroboration of a
strong kind: not one designer's taste, but four teams who agreed about nothing else.

⚠ **It does not settle what `CONTEXT.md` should say, and it must not be read as having done so.**
[`0043`](adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) types the
question *arguable*, so it is settled by argument in a sitting, and this section is an input to that
sitting rather than its conclusion. What it does establish is that ***the corpus's four answers are not
four positions***: `CONTEXT.md` → Resource is describing refuse, `CONTEXT.md` → Water Body is describing
effluent, and both are right about the thing they are looking at. The defect is that one word is doing
two jobs.

⚠ **One thing here is a genuine warning rather than a precedent.** CS1's water pollution is legible to a
player because the water is *visibly* flowing and the pollution is *visibly* on it. A graph with an
outflow rate has neither property, and
[`00-vision.md`](00-vision.md)'s honesty pillar is what would be spent if a city were poisoned by an edge
nobody can see. **Whatever family Waste lands in, a Water Body that fouls needs something to look at** —
which is `CLAUDE.md`'s Definition of done arriving at this subject early rather than late.

---


## Notes on sourcing

Citybound's **project page carries a real bibliography** (the source of most citations above), but its **codebase contains exactly one technical reference** — a Wikipedia link to the Intelligent Driver Model in `intelligent_acceleration.rs`. A repo-wide search for `paper`, `thesis`, `algorithm`, `inspired`, `based on`, `grammar`, and `orienteering` returns nothing else.

One source remains unretrieved: the Notion database **"Citybound Inspiration and References"** (`notion.so/aeplay/d82db2a604334f4581d050cc7d96ca0d`), which "The Research That Goes Into Citybound" defers to for the geometry reading behind *"hundreds of papers, master and PhD theses and books"*. Notion redirect-loops for non-browser fetchers. Given our grid decision it is likely low-value, but it is the one gap.
