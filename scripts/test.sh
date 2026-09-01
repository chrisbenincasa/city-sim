#!/usr/bin/env bash
#
# Run a test lane and never lose the failure list.
#
# A failing `dotnet test` prints each failure inline, then thousands of lines of
# stack traces, then the totals. Anyone who pipes that through `head`/`tail` --
# or reads it in a terminal with a scrollback limit -- loses the one thing they
# ran it for, and pays the full wall-clock again to get it back. This keeps the
# whole run on disk and re-prints the failed test names LAST, after the noise.
#
# Usage:
#   scripts/test.sh                 # the working lane: tier!=instrument
#   scripts/test.sh Policy          # that lane, narrowed to FullyQualifiedName~Policy
#   scripts/test.sh --all           # the whole suite, instruments included (~36m)
#   scripts/test.sh --filter 'EXPR' # an explicit --filter expression
#   scripts/test.sh ... -- <args>   # anything after -- goes to `dotnet test`
#
# Exit status is `dotnet test`'s own, so this is safe in a gate.
#
# It also names the TREE it gated and counts what it collected, which is
# plans/0045 row 15f. A green run is a claim about a tree, and a run that does
# not say which tree cannot be checked against the one that was pushed -- 15f's
# incident was a rebase producing a tree neither parent's gate had ever run on.
# The count lives here rather than in Borough.Tests on purpose: a test cannot
# report that it did not run, because the report would not have been collected
# either. Only something outside the run can count the run.

set -uo pipefail

lane='tier!=instrument'
narrow=''
passthrough=()

while [ $# -gt 0 ]; do
  case "$1" in
    --all)    lane=''; shift ;;
    --filter) lane="$2"; shift 2 ;;
    --)       shift; passthrough=("$@"); break ;;
    *)        narrow="$1"; shift ;;
  esac
done

filter="$lane"
if [ -n "$narrow" ]; then
  if [ -n "$filter" ]; then
    filter="$filter&FullyQualifiedName~$narrow"
  else
    filter="FullyQualifiedName~$narrow"
  fi
fi

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
stamp="$(date +%Y%m%d-%H%M%S)"
log="${BOROUGH_TEST_LOG:-/tmp/borough-test-$stamp.log}"

args=(test -c Release)
[ -n "$filter" ] && args+=(--filter "$filter")
args+=("${passthrough[@]+"${passthrough[@]}"}")

# The tree this run is a claim about. A rebase replays your work onto a parent
# you never gated, so a green with no tree on it cannot be matched to the commit
# that was actually pushed -- which is exactly how row 15f's red tree got there.
# Dirty is reported bluntly rather than cleverly: filtering it down to "paths
# that could have affected this lane" is a guess, and a wrong guess here says
# "clean" about a tree that was not.
tree="$(git -C "$root" rev-parse --short HEAD 2>/dev/null || echo unknown)"
[ -n "$(git -C "$root" status --porcelain 2>/dev/null)" ] && tree="$tree-dirty"
branch="$(git -C "$root" rev-parse --abbrev-ref HEAD 2>/dev/null || echo unknown)"

# Per-clone, inside .git, so it is never committed and never becomes a corpus
# artefact somebody has to keep true.
ledger="$(git -C "$root" rev-parse --git-dir 2>/dev/null)/borough-test-lanes"

echo "lane:   ${filter:-<everything>}"
echo "tree:   $tree on $branch"
echo "log:    $log"
echo

( cd "$root" && dotnet "${args[@]}" ) 2>&1 | tee "$log"
status=${PIPESTATUS[0]}

echo
echo "───────────────────────────────────────────────────────────"
echo "full output: $log"
echo "tree:        $tree on $branch"

# How much of the suite ran. `TierBudgetTests` times the tests it was handed and
# `TierDeclarationTests` counts the instrument share among them; neither can see
# a class that was never handed over, because both reason about the tests they
# got. This compares the collected total against the last run of the SAME lane
# expression, so a narrowed run is compared with narrowed runs and never with
# the whole lane.
key="${filter:-<everything>}"
collected="$(grep -oE 'Total: *[0-9,]+' "$log" | tail -1 | tr -cd '0-9')"

if [ -n "$collected" ]; then
  echo "collected:   $collected tests"

  before="$(awk -F'\t' -v k="$key" '$4 == k { c = $1; t = $2; d = $3 } END { if (c != "") print c "\t" t "\t" d }' "$ledger" 2>/dev/null)"
  was="$(printf '%s' "$before" | cut -f1)"
  wastree="$(printf '%s' "$before" | cut -f2)"
  wasdate="$(printf '%s' "$before" | cut -f3)"

  if [ -n "$was" ] && [ "$collected" -lt "$was" ]; then
    echo
    echo "FEWER TESTS RAN THAN LAST TIME ON THIS LANE:"
    echo "  $collected now on $tree"
    echo "  $was on $wastree at $wasdate"
    echo
    echo "A green run over fewer tests is not the same green. Either tests were"
    echo "deleted -- in which case this is the record of it -- or a class did not"
    echo "reach the assembly, and nothing inside the run can tell you which."
  fi

  # Appended rather than rewritten: the file is the history of a lane, and a
  # count that fell and came back is the shape worth being able to see.
  printf '%s\t%s\t%s\t%s\n' "$collected" "$tree" "$stamp" "$key" >> "$ledger" 2>/dev/null || true
fi

# xUnit's VSTest logger prints one `  Failed <fully.qualified.name> [N ms]` line
# per failure. That is the list, deduped -- theory cases keep their arguments so
# two rows of one theory stay distinguishable.
failures="$(grep -E '^[[:space:]]+Failed ' "$log" \
            | sed -E 's/^[[:space:]]+Failed //; s/ \[[0-9.,]+ [munsh]+\]$//' \
            | sort -u)"

if [ -n "$failures" ]; then
  count="$(printf '%s\n' "$failures" | wc -l | tr -d ' ')"
  echo
  echo "FAILED ($count):"
  printf '%s\n' "$failures" | sed 's/^/  /'
  echo
  echo "for the message behind one:  grep -A6 -F '<name>' $log"
elif [ "$status" -ne 0 ]; then
  echo
  echo "non-zero exit ($status) with no test failures parsed -- build or host error."
  echo "last 20 lines:"
  tail -20 "$log" | sed 's/^/  /'
fi

exit "$status"
