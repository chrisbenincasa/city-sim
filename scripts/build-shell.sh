#!/usr/bin/env bash
#
# Build the shell's assemblies as Debug or optimized, and report which one Godot will load.
#
# Usage:
#   scripts/build-shell.sh             # Debug, JIT optimizer off: the debugger sees every local
#   scripts/build-shell.sh --optimized # optimized: for timings and large cities
#   scripts/build-shell.sh --check     # build nothing, report what is there
#
# Godot always loads .godot/mono/temp/bin/Debug, whichever mode built it. An incremental build
# ignores a change to -p:Optimize and keeps the old assemblies, so every build here is a full
# rebuild, and the mode is read back from the assemblies rather than assumed.

set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
project="$root/src/Borough.Godot"
bin="$project/.godot/mono/temp/bin/Debug"

case "${1:-}" in
  '' | --debug) optimize=false ;;
  --optimized)  optimize=true ;;
  --check)      optimize='' ;;
  *) echo "usage: $0 [--debug | --optimized | --check]" >&2; exit 2 ;;
esac

if [ -n "$optimize" ]; then
  dotnet build "$project" --no-incremental -p:Optimize="$optimize" -v:q -nologo
fi

probe="$(mktemp -d)/probe.fsx"
trap 'rm -rf "$(dirname "$probe")"' EXIT
cat > "$probe" <<'FSX'
open System.IO
open System.Reflection.Metadata
open System.Reflection.PortableExecutable

let disableOptimizations = 0x100

let mode (path: string) =
  use pe = new PEReader(File.OpenRead path)
  let md = pe.GetMetadataReader()
  md.GetAssemblyDefinition().GetCustomAttributes()
  |> Seq.map md.GetCustomAttribute
  |> Seq.tryPick (fun a ->
      let parent =
        match a.Constructor.Kind with
        | HandleKind.MemberReference -> (md.GetMemberReference(MemberReferenceHandle.op_Explicit a.Constructor)).Parent
        | _ -> Unchecked.defaultof<_>
      if parent.Kind <> HandleKind.TypeReference then None else
      let t = md.GetTypeReference(TypeReferenceHandle.op_Explicit parent)
      if md.GetString t.Name <> "DebuggableAttribute" then None else
      let blob = md.GetBlobReader a.Value
      blob.ReadUInt16() |> ignore
      Some (if blob.ReadInt32() &&& disableOptimizations <> 0 then "debug" else "optimized"))
  |> Option.defaultValue "optimized"

let modes =
  [ for name in ["Borough.Core"; "Borough.Formats"; "Borough.Appearance"; "Borough.Godot"] ->
      let path = Path.Combine(fsi.CommandLineArgs[1], name + ".dll")
      let m = if File.Exists path then mode path else "missing"
      printfn "%-20s %s" name m
      m ]
exit (if modes |> List.distinct |> List.length = 1 && modes.Head <> "missing" then 0 else 1)
FSX

if dotnet fsi --quiet "$probe" "$bin"; then
  echo "Godot will load these from $bin"
else
  echo "The assemblies in $bin are mixed or missing. Rebuild with this script." >&2
  exit 1
fi
