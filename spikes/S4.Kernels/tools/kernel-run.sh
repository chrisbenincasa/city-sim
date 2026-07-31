#!/usr/bin/env bash
#
# S4 tasks 4-8 — K1 through K5 under BenchmarkDotNet, captured against the machine baseline.
#
# The kernels are only meaningful divided by a denominator, and the denominator was measured under a
# stated frequency configuration by tools/baseline-sweep.sh. A kernel run under a different governor
# than its denominator is a ratio between two machines. This script exists so the two cannot drift:
# it sets the same configuration, pins to the same core, and puts the configuration in the label.
#
# Run as root for the canonical capture, which is `performance` with turbo enabled:
#
#     sudo spikes/S4.Kernels/tools/kernel-run.sh                  every kernel
#     sudo spikes/S4.Kernels/tools/kernel-run.sh '*K2*'           one of them
#
# It also runs without root, under whatever configuration the machine is already in, and labels the
# result with that configuration rather than the canonical one. That is a usable measurement and an
# honest one; it is not the one the report should quote if a root capture exists.
#
# The original governor and turbo state are restored on exit, including on Ctrl-C.

set -euo pipefail

FILTER="${1:-*}"
CORE="${S4_CORE:-2}"   # a physical core; its SMT sibling is left idle by pinning to one thread

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$(dirname "${HERE}")"
RESULTS="${PROJECT}/results"
BDN="${RESULTS}/bdn/results"
BINARY="${PROJECT}/bin/Release/net10.0/S4.Kernels"

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

# The configured transfer rate is a variable of this machine, not a constant of it — these DIMMs are
# rated 3200 and were running at 2133 with XMP off — so it goes in the label exactly as it does in
# the baseline's, and two captures taken at different rates cannot overwrite each other.
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

rm -f "${BDN}"/*-report-github.md
echo "=== kernels ${FILTER} [${LABEL}] ===" >&2
run_as_invoker taskset -c "${CORE}" "${BINARY}" bench --filter "${FILTER}"

# BenchmarkDotNet's own artifacts directory is gitignored, and a `--disasm` export is evidence
# rather than an artifact: K1's result is a claim about which instructions the JIT emitted, and a
# claim like that is worth nothing if the listing it rests on is not in the repository.
for asm in "${BDN}"/*-asm.md; do
    [[ -e "${asm}" ]] || continue
    kept="${RESULTS}/asm-${LABEL}-$(basename "${asm}" -asm.md | sed 's/^S4\.Kernels\.Kernels\.//').md"
    cp "${asm}" "${kept}"
    [[ -n "${SUDO_USER:-}" ]] && chown "${SUDO_USER}" "${kept}"
    echo "kept disassembly in ${kept}" >&2
done

# A filtered run holds a subset of the kernels, and must not be able to overwrite a full capture
# with it. The filter goes in the filename whenever there is one.
SUFFIX=""
if [[ "${FILTER}" != "*" ]]; then
    SUFFIX="-$(echo "${FILTER}" | tr -cd '[:alnum:]')"
fi

OUT="${RESULTS}/kernels-${LABEL}${SUFFIX}.md"
{
    echo "## S4 K1-K5 — \`${LABEL}\`"
    echo
    echo "Recorded $(date -u '+%Y-%m-%d %H:%M') UTC, pinned to core ${CORE} with its SMT sibling idle."
    echo "Divide against \`baseline-${LABEL}.md\`, which was measured under the same configuration."
    echo
    for report in "${BDN}"/*-report-github.md; do
        [[ -e "${report}" ]] || continue
        cat "${report}"
        echo
    done
} > "${OUT}"
[[ -n "${SUDO_USER:-}" ]] && chown "${SUDO_USER}" "${OUT}"

echo "written to ${OUT}" >&2
