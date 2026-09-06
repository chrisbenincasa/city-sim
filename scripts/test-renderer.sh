#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build tests/Borough.Renderer.Tests --disable-build-servers
"${GODOT_BIN:-godot}" --path tests/Borough.Renderer.Tests "$@"
