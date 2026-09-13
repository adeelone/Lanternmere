# Lanternmere

A quiet 2D exploration adventure about restoring the names of forgotten places. See `BRIEF.md` for the full design brief this build follows, `docs/NARRATIVE_BIBLE.md` and `docs/PUZZLES.md` for design specs, `docs/REGION_FORMAT.md` for the data-driven map format, `ROADMAP.md` for what's built vs. not, and `AUDIT.md` for an honest requirement-by-requirement status.

**Working title only.** Complete title, storefront, trademark, and domain clearance before any public release. Do not imitate the art, characters, maps, writing, or music of an existing game or living artist.

## Status

A complete, playable first pass: title through credits, all three required regions plus the village hub plus the optional archive secret, five NPCs with dialogue, all three required puzzles, journal/map/inventory UI, save/load/settings, and both endings — see `AUDIT.md` for the honest, requirement-by-requirement breakdown of what's solid vs. still placeholder. Art and audio are procedurally generated placeholders (see `ASSET_MANIFEST.md`), not final assets.

## Stack

- C# / .NET 8
- MonoGame 3.8.5 DesktopGL
- xUnit for unit tests

## Project layout

```
src/Lanternmere.Core/   Pure game-logic library (no rendering): settings, save system,
                        world state/flags, collision math, input abstraction, player
                        movement, region/dialogue/item/discovery data models + loaders,
                        and the three puzzle state machines. No GraphicsDevice
                        dependency anywhere in this project — fully unit-testable and
                        builds on any OS.
src/Lanternmere/        The MonoGame executable: Game1, the full scene stack (title,
                        regions, dialogue, journal/map/inventory, pause, settings,
                        save slots, ending, credits, error states), procedural
                        placeholder art/audio generation, and Content/ (the SpriteFont
                        pipeline assets + data-driven JSON for maps/dialogue/items/
                        discoveries).
test/Lanternmere.Tests/ xUnit tests — Core logic only (see below).
tools/ContentGen/       Generates every Content/Data/*.json file from typed C# objects
                        and self-validates before writing. See docs/REGION_FORMAT.md.
docs/                   Narrative bible, puzzle specs, region format, art/audio notes.
```

`Lanternmere.Core` and `Lanternmere` are separate projects specifically so unit tests don't need MonoGame's native libraries or a content-pipeline font build — `test/Lanternmere.Tests` only references `Lanternmere.Core`, so `dotnet test` runs on any OS/CI runner (see `.github/workflows/ci.yml`, which runs the test suite on both Ubuntu and Windows, and additionally builds + publishes the full game on Windows).

## Requirements

- .NET 8 SDK
- A font named "Segoe UI" installed on the build machine (Windows has this by default) — see "Font portability" below

## Build & run

```bash
dotnet build Lanternmere.sln
dotnet run --project src/Lanternmere
```

Or use the clean-build script, which wipes all build output first:

```powershell
# Windows
powershell -File scripts/clean-build.ps1 -Publish   # -Publish also produces publish/win-x64
```
```bash
# macOS/Linux — builds & tests Lanternmere.Core + the test suite only;
# see the script's own comments for why the full game isn't built here.
./scripts/clean-build.sh
```

## Run tests

```bash
dotnet test test/Lanternmere.Tests/Lanternmere.Tests.csproj
```

53 tests currently pass: save/load round-tripping and corrupted-save-with-backup-recovery, AABB collision, world-state flags, region loading + cross-region validation (missing spawn points, bad transition targets, duplicate ids, unreachable-on-solid-tile interactables) against both synthetic fixtures and the actual shipped content, dialogue branching/conditions/history tracking, all three puzzle state machines (including out-of-order attempts, no-softlock resets, and save/reload mid-attempt), item/discovery databases, and a full clean-save playthrough simulated at the logic level (title → all three puzzles solved in the required order → lantern relit → main ending, plus the optional-ending-variation path and two out-of-order-blocked-not-broken cases).

## Content authoring

Region/dialogue/item/discovery JSON under `src/Lanternmere/Content/Data/` is generated, not hand-written — see `docs/REGION_FORMAT.md` and `tools/ContentGen/Program.cs`. To change content, edit the generator and re-run:

```bash
dotnet run --project tools/ContentGen -- src/Lanternmere/Content/Data
```

It self-validates (`RegionLoader.Validate`) and refuses to write invalid content.

## Dev launch flags (Debug builds only)

Compiled out of Release entirely (`src/Lanternmere/Core/DevLaunchOptions.cs`, `#if DEBUG`). Useful for visual QA and performance testing without navigating there by hand:

```bash
Lanternmere.exe --region wind_cliffs                       # jump straight into a region
Lanternmere.exe --region village_hub --scene journal        # jump into a UI overlay scene
                                                              # (journal|map|inventory|pause|
                                                              #  settings|dialogue|credits|
                                                              #  creditsroll|ending)
Lanternmere.exe --region wind_cliffs --perflog               # log min/avg/max FPS over 5s, then exit
```

## Save data location

`Core.GameSettings.GetSaveDirectory()` uses `Environment.SpecialFolder.ApplicationData`, which resolves to `%AppData%/Lanternmere` on Windows and an equivalent per-user config location on macOS/Linux. Settings (`settings.json`) and 3 save slots (`save0.json`..`save2.json`, slot 0 is the autosave slot, each with a `.bak.json` backup) live there.

## Packaged release

```bash
dotnet publish src/Lanternmere/Lanternmere.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64
```

Produces `publish/win-x64/Lanternmere.exe` plus MonoGame's native dependencies (`SDL2.dll`, `openal.dll`) and the `Content/` tree (built font `.xnb`s + data JSON), verified in this repo by launching the produced `.exe` directly (not `dotnet run`) from outside the source tree. A macOS build (`-r osx-x64` or `osx-arm64`) has **not** been produced or tested — see `AUDIT.md`.

`--self-contained false` requires the .NET 8 runtime on the target machine. A fully standalone build (no runtime install needed on the target machine at all) has also been produced and smoke-tested in this repo:

```bash
dotnet publish src/Lanternmere/Lanternmere.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64-standalone
```

## Font portability

`Content/Fonts/*.spritefont` reference "Segoe UI" by name — the MonoGame content pipeline rasterizes it into a bitmap font at **build time**, so the shipped game doesn't need the font installed, but *building* the game does need it available on the build machine. This works out of the box on Windows; a Linux/macOS build machine (or CI runner) would need an equivalent font installed and the `.spritefont` `FontName` updated, or the content pipeline font build will fail. See `ASSET_MANIFEST.md`.

## What's deliberately out of scope

See `ROADMAP.md`. Highlights: final (non-placeholder) art and audio, a macOS packaged-build test, and trademark/store/domain clearance (a legal/business step, not a coding task).
