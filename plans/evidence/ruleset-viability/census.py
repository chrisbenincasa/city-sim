"""Semantic shared-table duplication; run from the repository root with Python 3.11+."""
import collections
import json
from pathlib import Path
import tomllib

files = sorted(Path("rulesets").glob("*.toml"))
parsed = [tomllib.loads(path.read_text()) for path in files]
report = {"files": len(files), "sections": {}}
for section in ("roads", "layers", "lots", "capacity", "placement", "jobs"):
    values = [json.dumps(t[section], sort_keys=True) for t in parsed if section in t]
    report["sections"][section] = {
        "files": len(values),
        "distinct": len(set(values)),
        "largest_identical_group": collections.Counter(values).most_common(1)[0][1],
    }
result = Path(__file__).parent / "results" / "duplication.json"
result.parent.mkdir(exist_ok=True)
result.write_text(json.dumps(report, indent=2) + "\n")
