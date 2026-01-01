#!/usr/bin/env bash
set -euo pipefail

MGCB="$HOME/.dotnet/tools/mgcb"

strip() {
  local s="$1"
  s="${s#\"}"
  s="${s%\"}"
  echo "$s"
}

build=""
args=()

for a in "$@"; do
  case "$a" in
    /@:* )
      build="$(strip "${a#/@:}")"
      ;;
    /platform:* )
      args+=( "-t=${a#/platform:}" )
      ;;
    /outputDir:* )
      args+=( "-o=$(strip "${a#/outputDir:}")" )
      ;;
    /intermediateDir:* )
      args+=( "-n=$(strip "${a#/intermediateDir:}")" )
      ;;
    /workingDir:* )
      args+=( "-w=$(strip "${a#/workingDir:}")" )
      ;;
    /quiet )
      args+=( "-q" )
      ;;
  esac
done

if [[ -z "$build" ]]; then
  echo "mgcb-compat: missing Content.mgcb" >&2
  exit 2
fi

exec "$MGCB" -b "$build" "${args[@]}"
