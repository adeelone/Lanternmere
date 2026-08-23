# Lanternmere — asset & license manifest

Per the brief: "All third-party and generated assets need a license/provenance manifest" and "If final custom assets cannot all be produced, create a cohesive legally usable placeholder set and label it honestly in the asset manifest; do not pull unlicensed images from search results."

## Current status: no art or audio assets have been produced yet

This scaffolding pass built architecture, systems, and data structure only. `Content/Content.mgcb` currently references no textures, fonts, or audio files, and `Game1.cs` draws a single solid-color placeholder rectangle for the player and walls (see `Scenes/GameplayScene.cs`) rather than any sprite.

When art/audio production starts, every asset added here must get a row below before it ships in a build players run:

| Asset | Source | License | Notes |
|---|---|---|---|
| *(none yet)* | | | |

Do not add image or audio files to `Content/` without a corresponding manifest row. Placeholder programmer-art (solid rectangles, generated tones) does not need a manifest row as long as it's clearly generated in code (as the current player/wall rectangles are) rather than sourced from an external file.
