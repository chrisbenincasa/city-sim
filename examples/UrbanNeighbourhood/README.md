# One street, two Households, one addition

This small saved city makes the urban-fabric housing decision visible. It has six Citizens:
two existing residents and two arriving Households of two people each. Two vacancies charge
50 money units per Day, compared with their Outside option's 25. Both arriving Households
carry 100, so these vacancies are affordable; they simply prefer their Outside option.
A new terrace charges zero in this deliberately isolated housing fixture.

From the repository root:

```sh
dotnet run --project examples/UrbanNeighbourhood -- rulesets/urban-neighbourhood.toml /tmp/urban-neighbourhood.borough-city
dotnet build src/Borough.Godot
godot --path src/Borough.Godot -- --empty --drive examples/UrbanNeighbourhood/start.drive
```

The script pauses the game and opens **Load city**. Loading frames the neighbourhood. Enter
`/tmp/urban-neighbourhood.borough-city` in the File field and open it. Close the menu.
The save embeds its Ruleset; no content selection is needed.

1. Open **Evidence**, then **Household 3** or **Household 4**. Their inspector explains why
   they are looking for a home. Press Space to run. Repeated failed searches must span
   256 Ticks (three in-world hours). The inspector shows elapsed observations; simply
   leaving the game paused adds none.
2. Open **Zoning**, choose **housing**, and paint the empty frontage between the existing
   homes. The existing parcel brush is sufficient; painting more than one vacant parcel
   also demonstrates that permission alone does not create extra Buildings. At the supplied
   camera angle the street runs diagonally down and right.
3. Resume if paused. One two-Household terrace appears once the wait and permission both
   qualify. The same Households move into it. Their inspector changes to **Home: Building …**;
   **Evidence → Refresh Evidence** shows zero Households looking for a home.
4. Leave the remaining frontage permitted and run longer: no second addition is needed.
   Reload the starting save to try granting permission before or after the waiting period.

The fixture uses ordinary placement, construction, zoning, save/load and gate admission. It
seeds only the initial street, Lots and neighbours. It has no jobs, production, consumption or
rent collection and is not a balanced city. High choice sensitivity makes the price contrast
repeatable; the timing and rents are demonstration tuning. The gate has no visible Building
geometry in this fixture. Numeric intensity caps and new form controls are outside this example.

The regression test `UrbanNeighbourhoodTests` checks both intervention orders, population,
construction stopping, invariants and saved continuation with different route worker counts.
