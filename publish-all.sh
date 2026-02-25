#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
INCLUDE_PDB=false

for arg in "$@"; do
  case "$arg" in
    --include-pdb) INCLUDE_PDB=true ;;
    *)
      echo "Unknown option: $arg" >&2
      echo "Usage: $0 [--include-pdb]" >&2
      exit 1
      ;;
  esac
done

DEBUG_ARGS=(/p:DebugSymbols=false /p:DebugType=None)
if [[ "$INCLUDE_PDB" == true ]]; then
  DEBUG_ARGS=()
fi

echo "Running platform publish scripts from: $SCRIPT_DIR"

publish_rid() {
  local rid="$1"
  local outdir="$SCRIPT_DIR/dist/$rid"

  echo "Publishing Excel2DBSharp for $rid..."
  dotnet publish Excel2DBSharp.csproj -c Release -r "$rid" \
    /p:PublishSingleFile=true \
    /p:SelfContained=true \
    /p:PublishTrimmed=true \
    "${DEBUG_ARGS[@]}" \
    -o "$outdir"

  echo
  echo "Publish complete:"
  if [[ "$rid" == "win-x64" ]]; then
    echo "$outdir/Excel2DBSharp.exe"
  else
    echo "$outdir/Excel2DBSharp"
  fi
  echo
}

publish_rid "win-x64"
publish_rid "osx-x64"
publish_rid "linux-x64"
