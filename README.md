# Lanternmere

Working name for the former **Wanderer** concept — a quiet 2D exploration adventure about restoring the names of forgotten places. See `BRIEF.md` for the full design brief this build follows, `docs/NARRATIVE_BIBLE.md` and `docs/PUZZLES.md` for design specs, `ROADMAP.md` for what's built vs. not, and `AUDIT.md` for an honest requirement-by-requirement status.

**Working title only.** Complete title, storefront, trademark, and domain clearance before any public release. Do not imitate the art, characters, maps, writing, or music of an existing game or living artist.

## Status

This scaffold implements the core architecture end to end and runnable — scene stack, fixed-timestep game loop, eight-direction movement with acceleration/deceleration, AABB collision, rebindable input, and a versioned save system with atomic writes/backup/corruption-recovery — all covered by real, passing unit tests. **No actual level content, art, audio, dialogue, or puzzles are implemented yet.** See `AUDIT.md`. Treat this as a correct foundation to build the actual game on top of, not a playable slice of Lanternmere itself.

## Stack

- C# / .NET 8 (the brief recommends "the current supported .NET version"; this scaffold pins to 8.0 LTS since that's what the available MonoGame templates and SDK here support — bump when you have a reason to)
- MonoGame 3.8.5 DesktopGL
- xUnit for unit tests

## Project layout

```
src/Lanternmere.Core/   Pure game-logic library (no rendering): settings, save system,
                        world state/flags, collision math, input abstraction, player
                        movement. Depends only on MonoGame's math types (Vector2,
                        Rectangle) — no GraphicsDevice — so it's fully unit-testable.
src/Lanternmere/        The actual MonoGame executable: Game1, the scene stack
                        (Title/Gameplay/Pause), debug overlay, and Content/ (assets +
                        data-driven JSON for maps/dialogue/items/discoveries).
test/Lanternmere.Tests/ xUnit tests against Lanternmere.Core.
docs/                   Narrative bible and puzzle specs.
```

This split exists specifically so unit tests don't need to reference the WinExe game
project (referencing an executable from a test project caused xUnit's test discovery
to silently find zero tests during this scaffold's own verification — see git history
/ AUDIT.md if that regresses).

## Requirements

- .NET 8 SDK
- MonoGame templates: `dotnet new install MonoGame.Templates.CSharp` (already reflected in the committed project files; only needed again if you regenerate a project from scratch)

## Build & run

```bash
cd src/Lanternmere
dotnet build
dotnet run
```

## Run tests

```bash
cd test/Lanternmere.Tests
dotnet test
```

10 tests currently pass, covering save/load round-tripping, corrupted-save recovery via backup, and AABB collision resolution (`SaveSystemTests.cs`, `CollisionSystemTests.cs`, `WorldStateTests.cs`).

## Save data location

`Core.GameSettings.GetSaveDirectory()` uses `Environment.SpecialFolder.ApplicationData`, which resolves to `%AppData%/Lanternmere` on Windows and an equivalent per-user config location on macOS/Linux. Settings (`settings.json`) and up to 3 save slots (`save0.json`..`save2.json`, each with a `.bak.json` backup) live there.

## What's deliberately out of scope for this pass

See `ROADMAP.md` for the full list. Highlights: all three required puzzles, the five NPCs and their dialogue trees beyond one sample file, journal/map/inventory UI, real Tiled maps (gameplay currently runs in a placeholder bounded room), sprite/tile art, audio, the title/pause menu UI beyond input handling, controller testing, and Windows/macOS packaged-build smoke tests.
