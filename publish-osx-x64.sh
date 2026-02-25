#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

RID="osx-x64"
OUTDIR="$SCRIPT_DIR/dist/$RID"
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

echo "Publishing Excel2DBSharp for $RID..."
dotnet publish -c Release -r "$RID" \
  /p:PublishSingleFile=true \
  /p:SelfContained=true \
  /p:PublishTrimmed=true \
  "${DEBUG_ARGS[@]}" \
  -o "$OUTDIR"

echo
echo "Publish complete:"
echo "$OUTDIR/Excel2DBSharp"
