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
CORE="${S2_CORE:-2}"   # a physical core; its SMT sibling is left idle by pinning to one thread

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
LABEL="${CPU}-ddr${MEM_TAG}-${GOVERNOR}-${TURBO}"

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
OUT="${RESULTS}/s2-${LABEL}.md"

# A re-capture must not destroy the run it should be compared against. The label is the configuration,
# so two runs of the canonical configuration collide by design — and the second one is worth taking
# precisely when the first is worth keeping: two runs of one configuration are an error bar, one run is
# an assertion. This was found the hard way, and `docs/spike-results.md` records the spread that had to
# be written down in prose because the file it came from had already been overwritten.
#
# Archived under the previous run's own modification time rather than now, so the suffix says when that
# capture was taken and not when it was displaced.
if [[ -e "${OUT}" ]]; then
    PREVIOUS="${RESULTS}/s2-${LABEL}-$(date -u -r "${OUT}" +%Y%m%dT%H%M%SZ).md"
    mv -n "${OUT}" "${PREVIOUS}"
    echo "kept previous capture as ${PREVIOUS}" >&2
fi

echo "=== S2 [${LABEL}] → ${OUT} ===" >&2

# Pinned to one core, and niced before the privilege drop — an unprivileged process cannot lower its
# own nice value, so `sudo -u user nice` would fail where `nice sudo -u user` succeeds. -10 rather
# than -20 for the same reason S4 gives: the run holds one core of twelve and there is no reason to
# make the box unresponsive for the last increment of an advantage pinning already bought.
if [[ "${EUID}" -eq 0 && -n "${SUDO_USER:-}" ]]; then
    nice -n -10 sudo -u "${SUDO_USER}" -H taskset -c "${CORE}" "${BINARY}" "$@" --out "${OUT}"
else
    taskset -c "${CORE}" "${BINARY}" "$@" --out "${OUT}"
fi

echo "wrote ${OUT}" >&2
