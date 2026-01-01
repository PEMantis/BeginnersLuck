#!/usr/bin/env bash
set -euo pipefail

MGCB="src/BeginnersLuck.Game/Content/Content.mgcb"
OUT="src/BeginnersLuck.Game/Content/bin/DesktopGL/Content"
INT="src/BeginnersLuck.Game/Content/obj/DesktopGL/net9.0/Content"
WD="src/BeginnersLuck.Game/Content"

# IMPORTANT:
# - dotnet-mgcb shim REQUIRES /@:
# - DO NOT quote the /@: path (your paths have no spaces, so this is safe)
dotnet mgcb /verbose /@:$MGCB \
  /platform:DesktopGL \
  /outputDir:$OUT \
  /intermediateDir:$INT \
  /workingDir:$WD
