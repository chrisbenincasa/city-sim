"""Generate a deliberate authoring omission and its complete change; no simulation edits.

Run from the repository root, then load each /tmp/viability-authoring-*.toml with:
  dotnet src/Borough.Headless/bin/Debug/net10.0/Borough.Headless.dll --school \
    --ruleset PATH --citizens 1000 --ticks 1 --schools 0
One Tick checks acceptance only, not balance or human usability.
"""
from pathlib import Path

source = Path(__file__).with_name("connected.toml").read_text()
consume = ('rate    = 256\napply   = { min = 1, max = 1 }\n'
           'inputs  = [ { scope = "local", resource = "sundries", amount = 4 } ]')
assert source.count(consume) == 1
partial = source.replace(consume, consume.replace("amount = 4", "amount = 6"))
complete = partial
for before, after in (
    ('capacity = 128, owner = "occupant"', 'capacity = 192, owner = "occupant"'),
    ('capacity = 1024, owner = "business"', 'capacity = 1536, owner = "business"'),
    ('outputs = [ { scope = "local", resource = "sundries", amount = 8 } ]',
     'outputs = [ { scope = "local", resource = "sundries", amount = 12 } ]'),
):
    assert complete.count(before) == 1
    complete = complete.replace(before, after)
Path("/tmp/viability-authoring-consumption-only.toml").write_text(partial)
Path("/tmp/viability-authoring-consumption.toml").write_text(complete)
