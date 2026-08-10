#!/usr/bin/env bash
#
# S2 — the routing spike, captured under a stated frequency configuration.
#
# R0 was captured under `powersave`, unpinned, and `docs/spike-results.md` records that as the third
# machine-state defect in this corpus. The harness printed its own governor, which is how the defect
# is known rather than assumed — and printing it is not the same as preventing it. This script is the
# preventing half, and it is S4's `tools/kernel-run.sh` with the BenchmarkDotNet machinery removed:
# S2's harness times its own loops, so there is nothing to filter and no generated project to own.
#
#     sudo spikes/S2.Routing/tools/routing-run.sh                 every section
#     sudo spikes/S2.Routing/tools/routing-run.sh --matrix        R1 only
#     sudo spikes/S2.Routing/tools/routing-run.sh --cluster       R3 only
#     sudo spikes/S2.Routing/tools/routing-run.sh --vector        R4 only
#     sudo spikes/S2.Routing/tools/routing-run.sh --storm         R5 only
#     sudo spikes/S2.Routing/tools/routing-run.sh --path-source   R5.5 only
#     sudo spikes/S2.Routing/tools/routing-run.sh --habit         R6.4 only
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
BINARY="${PROJECT}/bin/Release/net10.0/S2.Routing"
CORE="${S2_CORE:-2}"   # a physical core, given as one logical processor of it

# Both threads of that physical core, not one of them. Pinning to a single logical processor starves
# the .NET tiered-JIT's background compilation of anywhere to run, and it then shares the measured
# core with whatever is timed first. That is not a hypothesis: captured `taskset -c 2`, R5's abstract
# graph rebuild read 214.94 ms measured first against 43.99 ms measured last — 4.88× apart — where the
# same pair on the same machine reads 0.92× under `-c 2,8`. It inflated the first-timed half of R5.2's
# table by ~3× and flipped the 8-versus-16 cluster verdict on its face. The sibling is read from the
# kernel rather than derived by arithmetic, because the numbering is a property of the machine.
CPUSET="${S2_CPUSET:-$(cat "/sys/devices/system/cpu/cpu${CORE}/topology/thread_siblings_list" 2>/dev/null || echo "${CORE}")}"

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
# the label — the same argument S4's baseline makes, and S2's read benchmark is bound by it at every
# rung past L3.
DMIDECODE="${PROJECT}/../S4.Kernels/results/dmidecode-memory.txt"
MEM_TAG="unknown"
if [[ -r "${DMIDECODE}" ]]; then
    MEM_TAG="$(awk '/Configured Memory Speed:/ && $4 ~ /^[0-9]+$/ { print $4; exit }' "${DMIDECODE}")"
fi

CPU="$(awk -F': ' '/model name/ { gsub(/\(R\)|\(TM\)|CPU| @.*/, "", $2); gsub(/^ +| +$/, "", $2);
                                  gsub(/ +/, "-", $2); print tolower($2); exit }' /proc/cpuinfo)"
# Which sections ran is part of the identity of a capture, not a detail of how it was invoked. Until
# this was in the name, `--storm` and a full run and `--cluster` all wrote to one filename keyed only
# by the machine configuration, so a section re-run displaced a whole-run capture and vice versa. The
# cost is recorded in `docs/spike-results.md` §S2 R5: the figures its first write-up published exist in
# no retained file on disk, and the numbers had to be re-derived from a fresh capture.
# A section flag the runner accepts and this case list does not know about falls through to `all`, so
# the artefact is named for a scope the run did not have — the retention defect above, wearing the
# label instead of the filename's configuration half. It happened: `--key` produced an `s2-all-…`
# file holding one section. Unknown flags are now refused rather than silently mislabelled.
SECTIONS=""
for arg in "$@"; do
    case "${arg}" in
        --graph)   SECTIONS="${SECTIONS}+r0" ;;
        --denominator) SECTIONS="${SECTIONS}+r0d" ;;
        --matrix)  SECTIONS="${SECTIONS}+r1" ;;
        --traffic) SECTIONS="${SECTIONS}+r2" ;;
        --cluster) SECTIONS="${SECTIONS}+r3" ;;
        --vector)  SECTIONS="${SECTIONS}+r4" ;;
        --storm)   SECTIONS="${SECTIONS}+r5" ;;
        --path-source) SECTIONS="${SECTIONS}+r55" ;;
        --key)     SECTIONS="${SECTIONS}+r61" ;;
        --eviction) SECTIONS="${SECTIONS}+r62" ;;
        --budget)  SECTIONS="${SECTIONS}+r63" ;;
        --shed)    SECTIONS="${SECTIONS}+r56" ;;
        --habit)   SECTIONS="${SECTIONS}+r64" ;;
        --loop)    SECTIONS="${SECTIONS}+r8" ;;
        --out)     ;;
        --*)
            echo "routing-run.sh: unknown section flag ${arg}." >&2
            echo "Add it to the SECTIONS case list before capturing, or the artefact will be" >&2
            echo "named for a scope it does not have." >&2
            exit 2
            ;;
    esac
done
SECTIONS="${SECTIONS:-+all}"
SECTIONS="${SECTIONS#+}"

# The CPU set goes in the name for the same reason the governor does: it is a configuration that moves
# the numbers, and a capture that does not say which one it ran under cannot be compared with one that
# does. `+` rather than `,` so the filename holds no separator that a shell will split on.
LABEL="${SECTIONS}-${CPU}-ddr${MEM_TAG}-${GOVERNOR}-${TURBO}-cpu${CPUSET//,/+}"

run_as_invoker() {
    if [[ "${EUID}" -eq 0 && -n "${SUDO_USER:-}" ]]; then
        sudo -u "${SUDO_USER}" -H "$@"
    else
        "$@"
    fi
}

echo "building Release" >&2
run_as_invoker dotnet build -c Release "${PROJECT}/S2.Routing.csproj" --nologo -v quiet

mkdir -p "${RESULTS}"

# Every capture is timestamped and nothing is ever overwritten. The previous spelling wrote to a stable
# filename and archived the file it displaced, which is the right instinct and was not enough: the
# archive was keyed on the machine configuration alone, so a run of a different *section* collided with
# it silently. Two runs of one configuration are an error bar and one run is an assertion, and that only
# holds if both survive. There is no canonical filename any more — the newest timestamp for a given
# label is the current capture, and `ls -t` is how you find it.
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT="${RESULTS}/s2-${LABEL}-${STAMP}.md"

echo "=== S2 [${LABEL}] → ${OUT} ===" >&2

# Pinned to both threads of one physical core, and niced before the privilege drop — an unprivileged
# process cannot lower its own nice value, so `sudo -u user nice` would fail where `nice sudo -u user`
# succeeds. -10 rather than -20 for the same reason S4 gives: the run holds one core of twelve and
# there is no reason to make the box unresponsive for the last increment of an advantage pinning
# already bought. The harness prints `Environment.ProcessorCount` into its own machine block, so a
# reader can check the affinity actually took rather than trusting this line.
if [[ "${EUID}" -eq 0 && -n "${SUDO_USER:-}" ]]; then
    nice -n -10 sudo -u "${SUDO_USER}" -H taskset -c "${CPUSET}" "${BINARY}" "$@" --out "${OUT}"
else
    taskset -c "${CPUSET}" "${BINARY}" "$@" --out "${OUT}"
fi

echo "wrote ${OUT}" >&2
