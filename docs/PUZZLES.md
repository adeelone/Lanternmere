# Lanternmere — Puzzle documentation

Per the brief: "Document every puzzle's inputs, state machine, reset behavior, clues, solution, accessibility alternative, and test cases." All three required puzzles are implemented (`src/Lanternmere.Core/Puzzles/`) and tested (`test/Lanternmere.Tests/PuzzleTests.cs`, `FullPlaythroughSimulationTests.cs`) against this exact contract.

## Puzzle 1 — Rain garden watering sequence (teaches observation + interaction)

- **Inputs**: player interacts with 4 dormant plant plots in the rain garden, each of which has a distinct visual "thirst" cue (wilted angle, color) rather than a text label.
- **State machine**: `Unsolved -> {plot watered in correct order} -> Solved`. Each plot has its own `Dry | Watered` state; the puzzle overall is `PuzzleState = "0_watered" | "1_watered" | "2_watered" | "3_watered" | "solved"`. Watering a plot out of the correct order resets all plots to `Dry` and the puzzle state to `"0_watered"` — no failure state beyond a reset, per "no punishment" requirement.
- **Reset behavior**: automatic on incorrect order (see above); also resettable manually via a "start over" prompt if the player interacts with the puzzle's central marker.
- **Clues**: the correct order is visually implied by which plants look "most" wilted (water the driest first) — no text required. A journal entry becomes available after 2 failed attempts that states this rule in words, for players who don't pick up on the visual cue.
- **Solution**: water plots in order of visual dryness, driest to least-dry.
- **Accessibility alternative**: each plot's dryness is also exposed via a non-color cue (a wilt-angle silhouette, readable independent of color vision) and, on interact, a screen-reader-friendly text description ("badly wilted" / "slightly wilted" / "healthy") rather than relying on color alone.
- **Test cases**: correct order solves; any incorrect order resets to `0_watered` and does not soft-lock; repeated failures still allow eventual success; puzzle state persists correctly across save/reload mid-attempt (reload with `"2_watered"` resumes at `"2_watered"`, not solved or reset).

## Puzzle 2 — Wind cliffs beacon triangulation (combines clues from two locations)

- **Inputs**: three beacon towers around the wind cliffs region; a clue in the rain garden region (a weathervane) indicates true wind direction; a clue at the cliffs themselves (a rope's fray pattern) indicates which two of the three beacons are load-bearing.
- **State machine**: `PuzzleState = "0_of_3_lit" | "1_of_3_lit" | "2_of_3_lit" | "solved"`. Lighting the wrong beacon (the non-load-bearing one indicated by the rope clue) does not reset progress — it's a no-op with feedback, not a punishment, since the brief requires clues after repeated attempts without immediately giving the answer, not a hard fail.
- **Reset behavior**: manual reset only (a lever at the beacon base), since progress can't be "broken" by wrong interactions.
- **Clues**: weathervane (rain garden, cross-region) + rope fray pattern (wind cliffs, local). Requires the player to have visited the rain garden first to fully solve — the brief's "combines clues from two nearby locations" requirement.
- **Solution**: light the two beacons indicated by the rope fray pattern, in the direction indicated by the weathervane (order matters: upwind beacon first).
- **Accessibility alternative**: The Watcher NPC will restate both clues in dialogue if asked directly, for players who prefer verbal over environmental clues.
- **Test cases**: correct two beacons in correct order solves; correct beacons wrong order does not solve but does not reset; incorrect beacon choice gives feedback and does not solve or reset; solving before visiting the rain garden is **actually enforced** — `WindCliffsPuzzle.LightBeacon` returns `PuzzleFeedback.Locked` and makes no state change unless `WorldState.Flags["visited_rain_garden"]` is true, which `RegionScene` sets the moment the player enters the rain garden region. See `WindCliffsPuzzleTests.LightBeacon_WithoutRainGardenVisit_ReturnsLocked`.

## Puzzle 3 — Amber shore tide-glass array (uses knowledge from all regions, gates the finale)

- **Inputs**: requires the other two fragments (rain garden + wind cliffs) plus three distinct items — `tide_glass_shard_dawn`, `tide_glass_shard_dusk`, `tide_glass_shard_deep` — found as pickups scattered around the amber shore.
- **State machine**: `PuzzleState = "locked" | "unlocked" | "solved"`. Transitions to `"unlocked"` only when `WorldState.Flags["fragment_rain_garden_recovered"]` and `WorldState.Flags["fragment_wind_cliffs_recovered"]` are both true (`AmberShorePuzzle.RefreshLockState`). `"solved"` requires each of the three altar slots to hold its correct shard: slot 0 (sunrise post) = dawn, slot 1 (far post) = dusk, slot 2 (between) = deep — per the Ferryperson's pattern clue.
- **Reset behavior**: removing a placed piece (`AmberShorePuzzle.RemoveGlass`) is always allowed at any time before solved; no punishment for experimentation.
- **Clues**: the Ferryperson's `pattern_clue` dialogue node (gated behind `both_prior_fragments_recovered`) describes the correct pattern in words: "Dawn-glass faces the sunrise post. Dusk-glass, the far one. The deep-glass sits between them, same as the water does."
- **Solution**: place each shard in its matching slot; the third correct placement triggers `PuzzleFeedback.Solved`, which `RegionScene` turns into `fragment_amber_shore_recovered` + the finale becoming reachable at the village hub lantern.
- **Accessibility alternative**: the pattern is fully describable in words (see the clue text above) rather than relying on purely spatial/visual matching — the in-world interaction is a simplified "try whichever shard you're carrying against this slot" single-button affordance rather than a full drag-and-drop item picker (see ROADMAP.md); the puzzle *logic* itself (which this doc specs and `AmberShorePuzzleTests` covers) does not depend on that simplification.
- **Test cases**: puzzle cannot be attempted before both prior fragments (`PlaceGlass_WhileLocked_ReturnsLocked`); correct pattern solves (`PlaceGlass_AllCorrect_Solves`); incorrect pattern gives feedback and does not reset already-placed correct pieces (`PlaceGlass_IncorrectPattern_DoesNotResetPriorCorrectSlots`); removal always allowed pre-solve (`RemoveGlass_AlwaysAllowedBeforeSolved`).

## Optional secret (the archive)

- **Inputs**: the brass key (found at the wind cliffs cairn, which itself requires `wind_cliffs_puzzle_solved`) opens the village hub's archive hatch transition. Inside, one interactable — `archive_final_record` — is the secret's payload.
- **State machine**: binary. The interactable is hidden (`RequiresFlag = "fragment_amber_shore_recovered"`) until that flag is set, then examining it unlocks `journal_archive_final_record` and, since `WorldState.HasDiscovery` is checked at lantern-relight time, sets `ReachedOptionalEndingVariation` alongside `ReachedMainEnding`.
- **Reset behavior**: none needed — reading it is a one-way, repeatable (idempotent) discovery, not a puzzle with failure states.
- **Clues**: none required by design; finding the brass key and the hatch itself is the "puzzle" — a curiosity reward per the brief's "should reward curiosity, not random wall-touching," since the key's location is signposted by the Watcher's cairn only appearing after the wind cliffs puzzle is solved, not by random searching.
- **Solution**: solve the wind cliffs puzzle → find the cairn → take the key → open the hatch → (later) recover the amber shore fragment → read the final record.
- **Accessibility alternative**: not applicable — it's a single flat text reveal, already screen-reader-friendly by construction (all dialogue/examine text renders as plain drawn text, no image-only content).
- **Test cases**: covered indirectly by `FullPlaythroughSimulationTests.CleanSave_WithArchiveSecretFirst_ReachesOptionalEndingVariation`, which confirms the optional variation is reachable, and `CleanSave_RequiredOrder_ReachesEndingWithNoSoftlock`, which confirms the *main* ending is still reachable without ever finding the secret.
