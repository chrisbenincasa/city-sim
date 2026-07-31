#!/usr/bin/env bash
#
# S4 task 9 — K6, the GC tail, across every GC configuration and both heap arms.
#
# plans/0004: "Run it under both GC configurations — ServerGarbageCollection and
# ConcurrentGarbageCollection, each on and off — and report all four. The GC mode is a lever on this
# answer and reporting one number for it would understate what is actually available."
#
# **The GC mode is fixed when the runtime starts**, so the four configurations cannot be a loop inside
# one process the way K5's parameters can. Each is a separate launch with DOTNET_gcServer and
# DOTNET_gcConcurrent set, and the report prints back what GCSettings actually observed so that a
# variable which failed to take cannot be mistaken for a configuration that did nothing.
#
# Two arms per configuration — see K6GcTail's own comment for why the unmanaged arm alone cannot fail,
# and therefore cannot be a test. Eight runs at ten minutes each is eighty minutes; set S4_MINUTES to
# something smaller for a smoke run.
#
#     spikes/S4.Kernels/tools/k6-run.sh                 all eight, ten minutes each
#     S4_MINUTES=1 spikes/S4.Kernels/tools/k6-run.sh    a smoke run, eight minutes total
#     sudo spikes/S4.Kernels/tools/k6-run.sh            ...under the canonical performance+turbo
#
# Unlike the kernels, K6 is deliberately **not** pinned with taskset. Server GC places one heap and one
# collection thread per core, and pinning the process to a single core would measure a server GC that
# was never allowed to be one — which is precisely the configuration under test.

set -euo pipefail

MINUTES="${S4_MINUTES:-10}"

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$(dirname "${HERE}")"
RESULTS="${PROJECT}/results"
BINARY="${PROJECT}/bin/Release/net10.0/S4.Kernels"

ORIGINAL_GOVERNOR="$(cat /sys/devices/system/cpu/cpu0/cpufreq/scaling_governor)"
ORIGINAL_NO_TURBO="$(cat /sys/devices/system/cpu/intel_pstate/no_turbo 2>/dev/null || echo unsupported)"

set_governor() {
    for g in /sys/devices/system/cpu/cpu*/cpufreq/scaling_governor; do
        echo "$1" > "${g}"
    done
}

restore() {
    echo "restoring governor=${ORIGINAL_GOVERNOR} no_turbo=${ORIGINAL_NO_TURBO}" >&2
    set_governor "${ORIGINAL_GOVERNOR}" || true
    if [[ "${ORIGINAL_NO_TURBO}" != "unsupported" ]]; then
        echo "${ORIGINAL_NO_TURBO}" > /sys/devices/system/cpu/intel_pstate/no_turbo || true
    fi
}

if [[ "${EUID}" -eq 0 ]]; then
    trap restore EXIT
    set_governor performance
    if [[ "${ORIGINAL_NO_TURBO}" != "unsupported" ]]; then
        echo 0 > /sys/devices/system/cpu/intel_pstate/no_turbo
    fi
    sleep 5
    GOVERNOR=performance
    TURBO=turbo
else
    echo "not root: measuring under the machine's current configuration, not the canonical one" >&2
    GOVERNOR="${ORIGINAL_GOVERNOR}"
    TURBO=$([[ "${ORIGINAL_NO_TURBO}" == "1" ]] && echo noturbo || echo turbo)
fi

DMIDECODE="${RESULTS}/dmidecode-memory.txt"
MEM_TAG="unknown"
if [[ -r "${DMIDECODE}" ]]; then
    MEM_TAG="$(awk '/Configured Memory Speed:/ && $4 ~ /^[0-9]+$/ { print $4; exit }' "${DMIDECODE}")"
fi
LABEL="ddr${MEM_TAG}-${GOVERNOR}-${TURBO}"

run_as_invoker() {
    if [[ "${EUID}" -eq 0 && -n "${SUDO_USER:-}" ]]; then
        sudo -u "${SUDO_USER}" -H "$@"
    else
        "$@"
    fi
}

echo "building Release" >&2
run_as_invoker dotnet build -c Release "${PROJECT}/S4.Kernels.csproj" --nologo -v quiet

OUT="${RESULTS}/k6-${LABEL}.md"
: > "${OUT}"
{
    echo "# S4 K6 — the GC tail — \`${LABEL}\`"
    echo
    echo "Eight runs of ${MINUTES} minutes: four GC configurations x two heap arms. Not pinned —"
    echo "server GC wants every core, and pinning would measure a server GC never allowed to be one."
    echo
} >> "${OUT}"

for server in 0 1; do
    for concurrent in 0 1; do
        for arm in "" "--managed"; do
            arm_tag=$([[ -n "${arm}" ]] && echo managed || echo unmanaged)
            echo "=== server=${server} concurrent=${concurrent} ${arm_tag}, ${MINUTES}m ===" >&2

            run_as_invoker env \
                DOTNET_gcServer="${server}" \
                DOTNET_gcConcurrent="${concurrent}" \
                "${BINARY}" k6 --minutes "${MINUTES}" ${arm} --label "${LABEL}" \
                | tail -n +2 >> "${OUT}"
            echo >> "${OUT}"
        done
    done
done

[[ -n "${SUDO_USER:-}" ]] && chown "${SUDO_USER}" "${OUT}"
echo "written to ${OUT}" >&2
