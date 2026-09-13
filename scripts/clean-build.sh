#!/usr/bin/env bash
# Clean build script (macOS/Linux) — per the brief's "clean-build scripts" requirement.
# Builds and tests Lanternmere.Core + the test suite, which have no MonoGame
# content-pipeline dependency and so build on any OS. Building/publishing the
# full MonoGame executable (src/Lanternmere) requires a font available for
# the SpriteFont content build — see docs/REGION_FORMAT.md and
# ASSET_MANIFEST.md for why that's a known portability gap, not attempted
# here automatically.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "== Cleaning bin/obj/publish =="
find "$ROOT" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
rm -rf "$ROOT/publish"

echo "== Restoring =="
dotnet restore "$ROOT/Lanternmere.sln"

echo "== Building test project (Release) =="
dotnet build "$ROOT/test/Lanternmere.Tests/Lanternmere.Tests.csproj" --configuration Release --no-restore

echo "== Running tests =="
dotnet test "$ROOT/test/Lanternmere.Tests/Lanternmere.Tests.csproj" --configuration Release --no-build

echo "Done. To build/run the full game on this machine, see README.md 'Build & run'."
