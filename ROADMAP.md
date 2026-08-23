# Lanternmere roadmap

Tracks what this scaffold actually built vs. what `BRIEF.md` requires. Keep this honest — it's the input to `AUDIT.md`.

## Done in this pass

- [x] Project structure split into `Lanternmere.Core` (pure logic, unit-testable) and `Lanternmere` (MonoGame executable) — see README "Project layout" for why
- [x] Scene/state stack (`Core/IScene.cs`, `Core/SceneManager.cs`) with Title → Gameplay → Pause wired and working; Pause overlays Gameplay without discarding its state
- [x] Fixed-timestep game loop (`IsFixedTimeStep = true`, 60Hz) explicitly set in `Game1`
- [x] Eight-direction movement with acceleration/deceleration (`Entities/Player.cs`)
- [x] AABB tile/object collision with per-axis slide resolution, no slopes (`Systems/CollisionSystem.cs`), unit tested
- [x] Rebindable keyboard + gamepad input abstraction (`Systems/InputManager.cs`) with defaults, persisted to settings
- [x] Settings persistence (`Core/GameSettings.cs`): volume levels, accessibility toggles (reduced motion/shake/flash, high contrast, text speed/instant text), resolution/fullscreen, UI scale, key/gamepad bindings
- [x] Versioned, atomic, backup-and-recover save system (`Core/SaveSystem.cs`) — 3 slots, corruption detection, backup fallback — with passing round-trip and corruption-recovery tests
- [x] World state model (`Core/WorldState.cs`): flags, puzzle states, journal/inventory/shortcut lists, playtime, ending flags
- [x] Developer-only debug overlay, compiled out entirely in non-DEBUG builds (`Core/DebugOverlay.cs`)
- [x] Data-driven content stubs: one map descriptor, one dialogue tree, an items file, a discoveries file (`Content/Data/`)
- [x] Asset manifest (`ASSET_MANIFEST.md`) — currently empty/honest since no art or audio has been produced
- [x] Narrative bible draft and full puzzle specs for all 3 required puzzles (`docs/NARRATIVE_BIBLE.md`, `docs/PUZZLES.md`)
- [x] 10 passing xUnit tests; `dotnet build` succeeds with 0 warnings/0 errors; a 15-second headless run under Xvfb started without crashing

## Not built yet (ordered roughly by suggested priority)

1. **Real region/map loading.** Gameplay currently runs in one placeholder bounded room (`Scenes/GameplayScene.cs` hardcodes wall rectangles from the viewport). No Tiled (.tmx) loading exists yet despite `Content/Data/maps/village_hub.json` referencing a `tiledMapPath`.
2. **The three required puzzles.** Fully specced in `docs/PUZZLES.md` (state machines, reset behavior, accessibility alternatives, test cases) but none are implemented.
3. **NPCs and dialogue.** Only one sample dialogue tree exists (`cartographer_intro.json`) and there's no dialogue-rendering scene, no branching-choice UI, and no dialogue history.
4. **Journal, map, and inventory UI.** `WorldState` tracks the underlying lists; there's no screen to view them.
5. **Sprite/tile art and animation.** Gameplay draws solid-color rectangles for the player and walls. No SpriteFont is loaded yet either (`DebugOverlay.Draw` takes a nullable font and no-ops without one).
6. **Audio.** No music, ambience, or effects; no volume mixing wired to the `GameSettings` volume fields yet (the settings exist but nothing reads them into an actual `SoundEffectInstance`/`Song` volume).
7. **Camera follow, room transitions, screen shake (with disable option).** Not implemented — the placeholder room is small enough that camera follow hasn't been needed yet.
8. **Title/Pause/Settings menu UI.** Input handling for Confirm/Cancel/Pause exists; there's no actual menu rendering, so these scenes currently just draw solid overlays.
9. **Save slot picker UI, Continue/Load from Title.** `SaveSystem` supports 3 slots; `TitleScene` currently only supports New Game.
10. **Controller-disconnected, missing-content-error, and other required error states.**
11. **Windows/macOS packaged-build smoke tests.** Only built/run on Linux (this scaffold's dev environment) so far, including one headless Xvfb smoke run — real Windows/macOS packaging is unverified.
12. **Automated map validation** (missing spawn points, bad destinations, duplicate IDs) — not applicable yet since there's no real map loader.
13. **CI.** Not set up.
14. **Cross-region puzzle gating** (e.g. Puzzle 2 requiring a rain-garden visit first) — flagged as a TODO directly in `docs/PUZZLES.md`; not yet implemented in code.
15. **More tide-glass items** — `docs/PUZZLES.md` puzzle 3 needs 3 tide-glass pieces; only 1 is defined in `Content/Data/items/items.json`.

## Explicitly deferred per the brief (not bugs)

- Full 30–60 minute playable content (this pass is architecture only)
- Final custom art/audio — placeholder programmer art only, honestly labeled in `ASSET_MANIFEST.md`
- Store/trademark/domain clearance
