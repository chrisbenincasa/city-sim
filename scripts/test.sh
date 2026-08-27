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

echo "lane:   ${filter:-<everything>}"
echo "log:    $log"
echo

( cd "$root" && dotnet "${args[@]}" ) 2>&1 | tee "$log"
status=${PIPESTATUS[0]}

echo
echo "───────────────────────────────────────────────────────────"
echo "full output: $log"

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
