#!/usr/bin/env bash
#
# S4 task 1 on macOS — a second machine for the baseline.
#
# The Linux sweep script has no counterpart here and deliberately so. macOS exposes no governor and
# no turbo switch, so there is nothing to sweep; and it does not allow a thread to be pinned to a
# core at all, so the scaling curve is measured with the scheduler placing threads and must be read
# as a shape rather than as a set of points. Both facts are recorded in the output.
#
# Needs only the .NET SDK. No root.
#
#     spikes/S4.Kernels/tools/mac-sample.sh
#
# Then copy spikes/S4.Kernels/results/*-apple-*.md back to the machine holding the repository.

set -euo pipefail

SECONDS_PER_WINDOW="${S4_SECONDS:-10}"
SCALING_SECONDS="${S4_SCALING_SECONDS:-5}"

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$(dirname "${HERE}")"
RESULTS="${PROJECT}/results"
BINARY="${PROJECT}/bin/Release/net10.0/S4.Kernels"

mkdir -p "${RESULTS}"

echo "building Release" >&2
dotnet build -c Release "${PROJECT}/S4.Kernels.csproj" --nologo -v quiet

LABEL="$(/usr/sbin/sysctl -n machdep.cpu.brand_string | tr ' ' '-' | tr '[:upper:]' '[:lower:]')"
echo "machine: ${LABEL}" >&2

# Close what you can before this runs. There is no way to ask macOS for an idle core, so a busy
# machine shows up as variance with no way to attribute it.
"${BINARY}" baseline --seconds "${SECONDS_PER_WINDOW}" --label "${LABEL}" --out "${RESULTS}/baseline-${LABEL}.md" > /dev/null
"${BINARY}" scaling  --seconds "${SCALING_SECONDS}"    --label "${LABEL}" --out "${RESULTS}/scaling-${LABEL}.md"  > /dev/null

echo >&2
echo "results:" >&2
ls -1 "${RESULTS}" | grep -- "${LABEL}" >&2
