#!/usr/bin/env bash
#
# S4 task 1 — the machine baseline, measured under every frequency configuration the box offers.
#
# plans/0004 says "disable or record turbo and frequency-governor state", because a benchmark whose
# variance is the governor's is a benchmark of the governor. This script does both: it runs the
# baseline under all four combinations of {powersave, performance} x {turbo, no turbo} so that the
# governor's actual contribution is a measured number rather than an assumption, and it records the
# state alongside each result.
#
# It also captures `dmidecode -t memory`, which needs root and which supplies the DIMM population
# and channel count that turn the measured copy rate into a fraction of a theoretical ceiling.
#
# It then measures the scaling curve — aggregate bandwidth against thread count — once, under the
# canonical configuration. The curve is a property of the memory system rather than of the governor,
# so running it four times would cost four times as much to say the same thing.
#
# Run once, with sudo:
#
#     sudo spikes/S4.Kernels/tools/baseline-sweep.sh              both
#     sudo spikes/S4.Kernels/tools/baseline-sweep.sh baselines    the four single-threaded captures
#     sudo spikes/S4.Kernels/tools/baseline-sweep.sh scaling      the curve alone
#
# The original governor and turbo state are restored on exit, including on Ctrl-C.

set -euo pipefail

MODE="${1:-all}"
case "${MODE}" in
    all|baselines|scaling) ;;
    *) echo "usage: $0 [all|baselines|scaling]" >&2; exit 2 ;;
esac

SECONDS_PER_WINDOW="${S4_SECONDS:-10}"
SCALING_SECONDS="${S4_SCALING_SECONDS:-5}"
CORE="${S4_CORE:-2}"   # a physical core; its SMT sibling is left idle by pinning to one thread

if [[ "${EUID}" -ne 0 ]]; then
    echo "must run as root: sudo $0" >&2
    exit 1
fi
if [[ -z "${SUDO_USER:-}" ]]; then
    echo "SUDO_USER is not set; run via sudo from your normal account, not as a root login" >&2
    exit 1
fi

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$(dirname "${HERE}")"
RESULTS="${PROJECT}/results"
BINARY="${PROJECT}/bin/Release/net10.0/S4.Kernels"

run_as_user() { sudo -u "${SUDO_USER}" -H "$@"; }

ORIGINAL_GOVERNOR="$(cat /sys/devices/system/cpu/cpu0/cpufreq/scaling_governor)"
ORIGINAL_NO_TURBO="$(cat /sys/devices/system/cpu/intel_pstate/no_turbo 2>/dev/null || echo unsupported)"

restore() {
    echo "restoring governor=${ORIGINAL_GOVERNOR} no_turbo=${ORIGINAL_NO_TURBO}" >&2
    set_governor "${ORIGINAL_GOVERNOR}" || true
    if [[ "${ORIGINAL_NO_TURBO}" != "unsupported" ]]; then
        echo "${ORIGINAL_NO_TURBO}" > /sys/devices/system/cpu/intel_pstate/no_turbo || true
    fi
}
trap restore EXIT

set_governor() {
    local governor="$1"
    for g in /sys/devices/system/cpu/cpu*/cpufreq/scaling_governor; do
        echo "${governor}" > "${g}"
    done
}

mkdir -p "${RESULTS}"
chown "${SUDO_USER}" "${RESULTS}"

echo "capturing DIMM configuration" >&2
DMIDECODE="${RESULTS}/dmidecode-memory.txt"
dmidecode -t memory > "${DMIDECODE}"
chown "${SUDO_USER}" "${DMIDECODE}"

# The configured transfer rate goes in every label. The memory configuration is a variable of this
# machine, not a constant of it — these DIMMs are rated 3200 and were running at 2133 with XMP off —
# and two sweeps taken at different rates must not be able to overwrite each other.
MEM_TAG="$(awk '/Configured Memory Speed:/ && $4 ~ /^[0-9]+$/ { print $4; exit }' "${DMIDECODE}")"
MEM_TAG="ddr${MEM_TAG:-unknown}"
echo "memory configured at ${MEM_TAG}" >&2

echo "building Release" >&2
run_as_user dotnet build -c Release "${PROJECT}/S4.Kernels.csproj" --nologo -v quiet

if [[ "${MODE}" != "scaling" ]]; then
for governor in powersave performance; do
    for no_turbo in 0 1; do
        turbo_label=$([[ "${no_turbo}" == "1" ]] && echo noturbo || echo turbo)
        label="${MEM_TAG}-${governor}-${turbo_label}"

        set_governor "${governor}"
        if [[ "${ORIGINAL_NO_TURBO}" != "unsupported" ]]; then
            echo "${no_turbo}" > /sys/devices/system/cpu/intel_pstate/no_turbo
        elif [[ "${no_turbo}" == "1" ]]; then
            echo "skipping ${label}: no intel_pstate/no_turbo on this machine" >&2
            continue
        fi

        # Let the clock settle into the new configuration before anything is timed.
        sleep 5

        echo "=== ${label} ===" >&2
        run_as_user env "S4_DMIDECODE_FILE=${DMIDECODE}" \
            taskset -c "${CORE}" "${BINARY}" baseline \
            --seconds "${SECONDS_PER_WINDOW}" \
            --label "${label}" \
            --out "${RESULTS}/baseline-${label}.md" > /dev/null
    done
done
fi

# The curve, under the canonical configuration decided by the sweep: performance, turbo enabled.
# Deliberately *not* under taskset — it places its own threads one per physical core, and an
# inherited affinity mask would make sched_setaffinity fail rather than silently mis-measure.
if [[ "${MODE}" != "baselines" ]]; then
    set_governor performance
    if [[ "${ORIGINAL_NO_TURBO}" != "unsupported" ]]; then
        echo 0 > /sys/devices/system/cpu/intel_pstate/no_turbo
    fi
    sleep 5

    label="${MEM_TAG}-performance-turbo"
    echo "=== scaling curve, ${label} ===" >&2
    run_as_user env "S4_DMIDECODE_FILE=${DMIDECODE}" \
        "${BINARY}" scaling \
        --seconds "${SCALING_SECONDS}" \
        --label "${label}" \
        --out "${RESULTS}/scaling-${label}.md" > /dev/null
fi

echo >&2
echo "results in ${RESULTS}:" >&2
ls -1 "${RESULTS}" >&2
