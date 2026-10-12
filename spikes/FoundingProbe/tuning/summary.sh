#!/usr/bin/env bash
# Prints sampled Days and payroll totals for tune.sh runs.
#
# usage: summary.sh NAME...   (reads $TUNING_OUT/NAME/run.tsv, default ./out/NAME)
#   EVERY=N samples every N Days (default 20).
set -euo pipefail

here=$(cd "$(dirname "$0")" && pwd)

for name in "$@"; do
  run=${TUNING_OUT:-$here/out}/$name/run.tsv
  echo "== $name: $(cat "$(dirname "$run")/vary.txt")"
  awk -F'\t' -v every="${EVERY:-20}" '
    NR == 1 { for (i = 1; i <= NF; i++) h[$i] = i
              print "day treasury tills households$ households employed businesses grocer$ mill$ starving"
              next }
    { paid += $(h["paid"]); short += $(h["shortfall"]); quit += $(h["quit"]); bankrupt += $(h["bankrupted"]); drawn += $(h["drawn"])
      if ($1 > 60) { latePaid += $(h["paid"]); lateShort += $(h["shortfall"]) } }
    $1 % every == 0 { print $1, $(h["treasury"]), $(h["tills"]), $(h["households$"]), $(h["households"]),
                      $(h["employed"]), $(h["businesses"]), $(h["grocer$"]), $(h["mill$"]), $(h["starving"]) }
    END { printf("paid %d, unpaid %d, quit %d, bankrupt %d, drawn %d, unpaid share after Day 60 %.3f\n",
                 paid, short, quit, bankrupt, drawn,
                 (latePaid + lateShort) ? lateShort / (latePaid + lateShort) : 0) > "/dev/stderr" }
  ' "$run" | column -t
  tail -1 "$(dirname "$run")/err.txt"
done
