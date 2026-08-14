#!/usr/bin/env bash
#
# S5 — the Lane kernel, captured under a stated frequency configuration.
#
# S2's tools/routing-run.sh with the section list changed. The argument for it is recorded there and
# in docs/spike-results.md: the harness printing its own governor is how three machine-state defects
# in this corpus are known rather than assumed, and printing is not the same as preventing.
#
#     sudo spikes/S5.Lanes/tools/lane-run.sh                 every section, plus L4
#     sudo spikes/S5.Lanes/tools/lane-run.sh --network       L2 only
#     spikes/S5.Lanes/tools/lane-run.sh                      no root: whatever the machine is in
#
# Run as root for the canonical capture, which is `performance` with turbo enabled. It also runs
# without root, under whatever configuration the machine is already in, and labels the result with
# that configuration rather than the canonical one. That is a usable measurement and an honest one;
# it is not the one the report should quote if a root capture exists.
#
# The original governor and turbo state are restored on exit, including on Ctrl-C.

set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$(dirname "${HERE}")"
RESULTS="${PROJECT}/results"
BINARY="${PROJECT}/bin/Release/net10.0/S5.Lanes"
CORE="${S5_CORE:-2}"   # a physical core, given as one logical processor of it

# Both threads of that physical core, not one of them — S2's finding, and it is not a hypothesis:
# pinning to a single logical processor starves the tiered JIT's background compilation of anywhere
# to run, and it then shares the measured core with whatever is timed first. The sibling is read
# from the kernel rather than derived by arithmetic, because the numbering is a property of the
# machine.
CPUSET="${S5_CPUSET:-$(cat "/sys/devices/system/cpu/cpu${CORE}/topology/thread_siblings_list" 2>/dev/null || echo "${CORE}")}"

ORIGINAL_GOVERNOR="$(cat /sys/devices/system/cpu/cpu0/cpufreq/scaling_governor)"
ORIGINAL_NO_TURBO="$(cat /sys/devices/system/cpu/intel_pstate/no_turbo 2>/dev/null || echo unsupported)"

set_governor() {
    local governor="$1"
    for g in /sys/devices/system/cpu/cpu*/cpufreq/scaling_governor; do
        echo "${governor}" > "${g}"
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

# The configured transfer rate is a variable of this machine and not a constant of it, so it goes in
# the label. S5's L2 sweep walks past L3 deliberately, so it is bound by this at the top rungs.
DMIDECODE="${PROJECT}/../S4.Kernels/results/dmidecode-memory.txt"
MEM_TAG="unknown"
if [[ -r "${DMIDECODE}" ]]; then
    MEM_TAG="$(awk '/Configured Memory Speed:/ && $4 ~ /^[0-9]+$/ { print $4; exit }' "${DMIDECODE}")"
fi

CPU="$(awk -F': ' '/model name/ { gsub(/\(R\)|\(TM\)|CPU| @.*/, "", $2); gsub(/^ +| +$/, "", $2);
                                  gsub(/ +/, "-", $2); print tolower($2); exit }' /proc/cpuinfo)"

# Which sections ran is part of the identity of a capture. S2 records what it costs when it is not:
# a section re-run displaced a whole-run capture, and the figures a write-up had published existed
# in no retained file. An unknown flag is refused rather than silently labelled `all`.
SECTIONS=""
WANTS_CORES=0
for arg in "$@"; do
    case "${arg}" in
        --denominator) SECTIONS="${SECTIONS}+l0" ;;
        --queue)       SECTIONS="${SECTIONS}+l1" ;;
        --network)     SECTIONS="${SECTIONS}+l2" ;;
        --promotion)   SECTIONS="${SECTIONS}+l3" ;;
        --division)    SECTIONS="${SECTIONS}+l5" ;;
        --threads)     SECTIONS="${SECTIONS}+l6" ; WANTS_CORES=1 ;;
        --out)         ;;
        --*)
            echo "lane-run.sh: unknown section flag ${arg}." >&2
            echo "Add it to the SECTIONS case list before capturing, or the artefact will be" >&2
            echo "named for a scope it does not have." >&2
            exit 2
            ;;
    esac
done
SECTIONS="${SECTIONS:-+all}"
SECTIONS="${SECTIONS#+}"

# L6 measures the kernel at 2 and 4 threads, and the default pin is one *physical* core's two
# hyperthreads — so the default would run the 4-thread rung on one core and report the result as a
# scaling figure. That is the failure session F named in another context: a placeholder whose value
# sits inside the range of legitimate answers cannot announce itself. A 4-thread rung pinned to one
# core comes back near 1.00× and reads as "the kernel does not scale", which is a conclusion about
# the taskset and not about the kernel.
#
# So requesting L6 widens the set to four physical cores and both siblings of each, unless the
# caller has named a set explicitly. The chosen set goes in the filename either way, which is what
# makes the widening reviewable rather than magic.
S5_THREAD_CORES="${S5_THREAD_CORES:-2 3 4 5}"
if [[ "${WANTS_CORES}" -eq 1 && -z "${S5_CPUSET:-}" ]]; then
    WIDE=""
    for c in ${S5_THREAD_CORES}; do
        SIBS="$(cat "/sys/devices/system/cpu/cpu${c}/topology/thread_siblings_list" 2>/dev/null || echo "${c}")"
        WIDE="${WIDE},${SIBS}"
    done
    CPUSET="${WIDE#,}"
    echo "L6 requested: widening the pin to ${CPUSET} (four physical cores and their siblings)." >&2
    echo "The default one-core pin would report a 4-thread rung measured on one core." >&2
fi

# `+` rather than `,` so the filename holds no separator a shell will split on.
LABEL="${SECTIONS}-${CPU}-ddr${MEM_TAG}-${GOVERNOR}-${TURBO}-cpu${CPUSET//,/+}"

run_as_invoker() {
    if [[ "${EUID}" -eq 0 && -n "${SUDO_USER:-}" ]]; then
        sudo -u "${SUDO_USER}" -H "$@"
    else
        "$@"
    fi
}

echo "building Release" >&2
run_as_invoker dotnet build -c Release "${PROJECT}/S5.Lanes.csproj" --nologo -v quiet

mkdir -p "${RESULTS}"

# Every capture is timestamped and nothing is ever overwritten. There is no canonical filename: the
# newest timestamp for a given label is the current capture, and `ls -t` is how you find it. Two
# runs of one configuration are an error bar and one run is an assertion, and that only holds if
# both survive.
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT="${RESULTS}/s5-${LABEL}-${STAMP}.md"

echo "=== S5 [${LABEL}] → ${OUT} ===" >&2

# Niced before the privilege drop: an unprivileged process cannot lower its own nice value, so
# `sudo -u user nice` would fail where `nice sudo -u user` succeeds. -10 rather than -20 because the
# run holds one core of twelve.
if [[ "${EUID}" -eq 0 && -n "${SUDO_USER:-}" ]]; then
    nice -n -10 sudo -u "${SUDO_USER}" -H taskset -c "${CPUSET}" "${BINARY}" "$@" --out "${OUT}"
else
    taskset -c "${CPUSET}" "${BINARY}" "$@" --out "${OUT}"
fi

echo "wrote ${OUT}" >&2
