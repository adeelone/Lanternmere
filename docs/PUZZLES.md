# Lanternmere — Puzzle documentation

Per the brief: "Document every puzzle's inputs, state machine, reset behavior, clues, solution, accessibility alternative, and test cases." None of the three required puzzles are implemented yet (see ROADMAP.md) — this file specs them so implementation has a concrete contract to build and test against.

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
- **Test cases**: correct two beacons in correct order solves; correct beacons wrong order does not solve but does not reset; incorrect beacon choice gives feedback and does not solve or reset; solving before visiting the rain garden is impossible by construction (weathervane clue ungated, but testable that the puzzle cannot reach `"solved"` without the rain garden flag if that gating is added — currently NOT implemented, see ROADMAP flag on cross-region gating).

## Puzzle 3 — Amber shore tide-glass array (uses knowledge from all regions, gates the finale)

- **Inputs**: requires all three name-fragments recovered (from rain garden, wind cliffs, and — circularly — this puzzle IS the amber shore fragment, so more precisely: requires the other two fragments plus an item, tide_glass, found earlier at the shore).
- **State machine**: `PuzzleState = "locked" | "unlocked" | "solved"`. Transitions to `"unlocked"` only when `WorldState.Flags["fragment_rain_garden_recovered"]` and `WorldState.Flags["fragment_wind_cliffs_recovered"]` are both true. `"solved"` requires placing 3 tide-glass pieces (only 1 currently modeled in Content/Data/items/items.json — see ROADMAP, more items needed) into a beach altar in the pattern shown by the Ferryperson's account.
- **Reset behavior**: removing a placed piece is always allowed at any time before solved; no punishment for experimentation.
- **Clues**: the Ferryperson's dialogue (gated behind the two prior fragments) describes the correct pattern.
- **Solution**: place tide-glass pieces per the Ferryperson's description; puzzle completion triggers the finale gate back to the village hub lantern.
- **Accessibility alternative**: the pattern is describable in words (not purely spatial/visual) so it doesn't rely on precise mouse/analog-stick placement — exact interaction model still TBD, see ROADMAP.
- **Test cases**: puzzle cannot be attempted (UI should communicate "locked", not silently do nothing) before both prior fragments; correct pattern solves and sets `ReachedMainEnding`-adjacent flags; incorrect pattern gives feedback, no reset of already-placed correct pieces.

## Optional secret (archive)

Not yet specced — flagged in ROADMAP.md as a follow-up once the three required puzzles above are actually implemented and tested.
