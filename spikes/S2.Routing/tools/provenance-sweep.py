#!/usr/bin/env python3
"""Provenance sweep: does every figure the corpus quotes trace to a capture, and to a table?

Run from the repository root:  python3 spikes/S2.Routing/tools/provenance-sweep.py

Every figure in S2's sections of docs/spike-results.md is matched against every capture in
spikes/S2.Routing/results/ and classified three ways: TABLE-BACKED (the line it matches is a
markdown table row), PROSE-ONLY (it matches, but only inside a paragraph -- real, but not
checkable), NOT FOUND.

This matches QUANTITIES, not rendered strings. The first version of this sweep compared strings
and produced a false accusation against 10.37 ms, because spike-results writes milliseconds where
the harness prints microseconds. So: every figure is parsed with its unit, normalised to a
canonical dimension, and compared at the precision the corpus displays it to.

Two rules that are load-bearing and were both learned by getting them wrong:

  * Tokenise on number boundaries. Otherwise `1814.08 us` satisfies a hunt for `14.08%`, and the
    sweep reports coincidental substrings as provenance.
  * Trailing zeros are a precision claim. `151,000 ns` means +/-500, not +/-0.5; reading its zeros
    as significant digits manufactures absences that are not there.

A NOT FOUND result is a question, not a verdict. Most of them resolve to corpus arithmetic over
operands that are themselves table-backed -- 223.92 KiB is exactly 453.37 - 229.45 -- to figures
belonging to another spike, or to a superseded harness state whose capture is deliberately not
retained. Read the row before publishing the accusation.
"""
import re, os, sys, glob, signal
from collections import defaultdict

CORPUS = "docs/spike-results.md"
CAPTURES = "spikes/S2.Routing/results/*.md"
START = "## S2 — the routing ceiling"
END = "## S0a"

# Sections that are ABOUT provenance, and so quote unbacked figures as their subject matter.
# Sweeping them would count the write-up's own examples as findings.
SKIP_SECTIONS = ("### The provenance sweep", "### The sweep as a quantity match")

# unit -> (dimension, factor to canonical)
UNITS = {
    "ns": ("time", 1.0), "µs": ("time", 1e3), "us": ("time", 1e3),
    "ms": ("time", 1e6), "s": ("time", 1e9),
    "B": ("bytes", 1.0), "KiB": ("bytes", 1024.0),
    "MiB": ("bytes", 1024.0**2), "GiB": ("bytes", 1024.0**3),
    "%": ("percent", 1.0), "×": ("ratio", 1.0), "x": ("ratio", 1.0),
}
BARE = "--bare" in sys.argv   # include undimensioned numbers; noisy, see parse()
NUM = r"(\d[\d,]*(?:\.\d+)?)"
UNIT = r"\s*(ns|µs|us|ms|s|B|KiB|MiB|GiB|%|×)"
TOKEN = re.compile(NUM + r"(?:" + UNIT + r"(?![A-Za-z])" + r")?")


def parse(text):
    """Yield (value, dimension, decimals, raw) for every numeric token."""
    for m in TOKEN.finditer(text):
        raw, unit = m.group(1), m.group(2)
        # reject a token glued to a word character on either side (ids, hashes, section refs)
        s, e = m.span()
        if s > 0 and text[s - 1] in "_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ":
            continue
        digits = raw.replace(",", "")
        val = float(digits)
        if "." in digits:
            dec = len(digits.split(".")[1])
        else:
            # Trailing zeros are a precision claim: 151,000 ns means +/-500, not +/-0.5.
            stripped = digits.rstrip("0")
            dec = -(len(digits) - len(stripped)) if stripped else 0
        if unit:
            dim, f = UNITS[unit]
            yield (val * f, dim, dec, unit, val)
        elif BARE:
            # Undimensioned numbers, off by default: a bare 121 is a District count, a row index,
            # a section reference and a measurement, and the sweep cannot tell which.
            if dec > 0 or val >= 1000:
                yield (val, "bare", dec, "", val)


def load_corpus():
    lines = open(CORPUS, encoding="utf-8").read().split("\n")
    lo = next(i for i, l in enumerate(lines) if l.startswith(START))
    hi = next(i for i, l in enumerate(lines) if l.startswith(END) and i > lo)
    out = []
    skipping = False
    for i in range(lo, hi):
        line = lines[i]
        if line.startswith("###"):
            skipping = line.startswith(SKIP_SECTIONS)
        if skipping:
            continue
        if line.lstrip().startswith(">"):        # amendment/retraction blockquotes
            continue
        for (v, dim, dec, unit, disp) in parse(line):
            out.append((v, dim, dec, unit, disp, i + 1, line.strip()))
    return out


def load_captures():
    """dim -> list of (canonical value, kind, file, lineno)."""
    idx = defaultdict(list)
    for path in sorted(glob.glob(CAPTURES)):
        base = os.path.basename(path)
        for n, line in enumerate(open(path, encoding="utf-8"), 1):
            kind = "TABLE" if line.lstrip().startswith("|") else "PROSE"
            for (v, dim, dec, unit, disp) in parse(line):
                idx[dim].append((v, kind, base, n))
    return idx


def match(v, dim, dec, unit, idx):
    """Capture values agreeing with v when rounded to the corpus's displayed precision."""
    if dim == "time" and unit:
        scale = UNITS[unit][1]
    elif dim == "bytes" and unit:
        scale = UNITS[unit][1]
    else:
        scale = 1.0
    tol = 0.5 * (10 ** -dec) * scale
    hits = [h for h in idx.get(dim, []) if abs(h[0] - v) <= tol]
    return hits


def main():
    signal.signal(signal.SIGPIPE, signal.SIG_DFL)   # tolerate | head
    corpus = load_corpus()
    idx = load_captures()
    table, prose, missing = [], [], []
    for (v, dim, dec, unit, disp, ln, ctx) in corpus:
        hits = match(v, dim, dec, unit, idx)
        if any(h[1] == "TABLE" for h in hits):
            table.append((disp, unit, ln))
        elif hits:
            prose.append((disp, unit, ln, ctx, hits[0]))
        else:
            missing.append((disp, unit, ln, ctx))

    seen = set()
    def uniq(rows, key=lambda r: (r[0], r[1])):
        out = []
        for r in rows:
            k = key(r)
            if k not in seen:
                seen.add(k); out.append(r)
        return out

    print(f"TABLE-BACKED : {len(table)} tokens")
    print(f"PROSE-ONLY   : {len(prose)} tokens")
    print(f"NOT FOUND    : {len(missing)} tokens")
    print()
    print("=" * 100)
    print("PROSE-ONLY (distinct figures)")
    print("=" * 100)
    for (disp, unit, ln, ctx, hit) in uniq(prose):
        print(f"  {disp}{unit:<4} corpus:{ln:<5} <- {hit[2]}:{hit[3]}")
        print(f"      {ctx[:150]}")
    print()
    print("=" * 100)
    print("NOT FOUND (distinct figures)")
    print("=" * 100)
    for (disp, unit, ln, ctx) in uniq(missing):
        print(f"  {disp}{unit:<4} corpus:{ln}")
        print(f"      {ctx[:150]}")


main()
