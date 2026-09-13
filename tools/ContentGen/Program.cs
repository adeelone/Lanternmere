using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Lanternmere.Core.Content;
using Lanternmere.Core.Puzzles;

// Generates every Lanternmere.Core.Content data file (regions, dialogue,
// items, discoveries) from strongly-typed C# objects rather than hand-typed
// JSON, so content is schema-correct by construction. Self-validates with
// RegionLoader.Validate before writing. Run with:
//   dotnet run --project tools/ContentGen -- <path-to-src/Lanternmere/Content/Data>
// This is a one-off authoring tool, not part of the shipped game — see
// README.md "Content authoring."

var dataRoot = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Lanternmere", "Content", "Data");
dataRoot = Path.GetFullPath(dataRoot);
Directory.CreateDirectory(Path.Combine(dataRoot, "maps"));
Directory.CreateDirectory(Path.Combine(dataRoot, "dialogue"));
Directory.CreateDirectory(Path.Combine(dataRoot, "items"));
Directory.CreateDirectory(Path.Combine(dataRoot, "discoveries"));

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

// ---------------------------------------------------------------- helpers

static int[] RectRoomTiles(int w, int h, IEnumerable<(int x, int y, int w, int h)>? obstacles = null)
{
    var tiles = new int[w * h];
    for (var y = 0; y < h; y++)
    for (var x = 0; x < w; x++)
        if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
            tiles[y * w + x] = 1;

    foreach (var (ox, oy, ow, oh) in obstacles ?? Enumerable.Empty<(int, int, int, int)>())
    for (var y = oy; y < oy + oh; y++)
    for (var x = ox; x < ox + ow; x++)
        if (x >= 0 && x < w && y >= 0 && y < h)
            tiles[y * w + x] = 1;

    return tiles;
}

static void CarveGapV(int[] tiles, int w, int h, int edgeX, int centerTileY, int halfWidth)
{
    for (var dy = -halfWidth; dy <= halfWidth; dy++)
    {
        var ty = centerTileY + dy;
        if (ty >= 0 && ty < h) tiles[ty * w + edgeX] = 0;
    }
}

static void CarveGapH(int[] tiles, int w, int h, int edgeY, int centerTileX, int halfWidth)
{
    for (var dx = -halfWidth; dx <= halfWidth; dx++)
    {
        var tx = centerTileX + dx;
        if (tx >= 0 && tx < w) tiles[edgeY * w + tx] = 0;
    }
}

static SpawnPointDef Spawn(float x, float y) => new() { X = x, Y = y };

// ------------------------------------------------------------------ regions

const int tile = 20;
var regions = new List<RegionDescriptor>();

// --- village_hub ---------------------------------------------------------
{
    int w = 30, h = 20;
    var tiles = RectRoomTiles(w, h, new[] { (13, 8, 4, 3) }); // a small central plinth block the lantern sits beside
    CarveGapV(tiles, w, h, 0, 10, 2);      // to rain garden (west)
    CarveGapV(tiles, w, h, w - 1, 10, 2);  // to wind cliffs (east)
    CarveGapH(tiles, w, h, h - 1, 15, 2);  // to amber shore (south)

    regions.Add(new RegionDescriptor
    {
        Id = "village_hub",
        DisplayName = "The Village Hub",
        WidthTiles = w,
        HeightTiles = h,
        TileSize = tile,
        GroundColorHex = "3a4a3f",
        AccentColorHex = "20241c",
        SolidTiles = tiles,
        AmbienceId = "ambience_village_hub",
        MusicId = "music_theme",
        SpawnPoints = new Dictionary<string, SpawnPointDef>
        {
            ["default"] = Spawn(300, 220),
            ["from_rain_garden"] = Spawn(60, 200),
            ["from_wind_cliffs"] = Spawn(540, 200),
            ["from_amber_shore"] = Spawn(300, 340),
            ["from_archive"] = Spawn(420, 260),
        },
        Interactables = new List<InteractableDef>
        {
            new()
            {
                Id = "village_hub_lantern", X = 292, Y = 150, Width = 16, Height = 32,
                Prompt = "Examine the lantern", IsLandmark = true, DiscoveryId = "journal_landmark_lantern",
                ExamineText = "The central lantern. Dark, for now. Whatever relights it should probably matter to the whole valley, not just this square.",
            },
            new()
            {
                Id = "village_hub_signpost", X = 100, Y = 100, Width = 16, Height = 24,
                Prompt = "Read the signpost", IsLandmark = true, DiscoveryId = "journal_landmark_signpost",
                ExamineText = "Three roads lead out from here, and the post that once named them has been sanded smooth by weather and, it looks like, on purpose.",
            },
            new()
            {
                Id = "village_hub_archive_hatch", X = 420, Y = 220, Width = 16, Height = 16,
                Prompt = "Open the archive hatch", RequiresFlag = "has_brass_key",
                ExamineText = "The hatch swings open easily now that you have the key.",
            },
        },
        Npcs = new List<NpcPlacementDef>
        {
            new() { Id = "the_cartographer", DisplayName = "The Cartographer", X = 260, Y = 260, DialogueId = "cartographer_intro" },
        },
        Transitions = new List<TransitionDef>
        {
            new() { Id = "to_rain_garden", X = -10, Y = 160, Width = 20, Height = 100, DestinationRegionId = "rain_garden", DestinationSpawnId = "from_village" },
            new() { Id = "to_wind_cliffs", X = w * tile - 10, Y = 160, Width = 20, Height = 100, DestinationRegionId = "wind_cliffs", DestinationSpawnId = "from_village" },
            new() { Id = "to_amber_shore", X = 260, Y = h * tile - 10, Width = 100, Height = 20, DestinationRegionId = "amber_shore", DestinationSpawnId = "from_village" },
            new()
            {
                Id = "to_archive", X = 420, Y = 220, Width = 16, Height = 16, DestinationRegionId = "archive", DestinationSpawnId = "default",
                RequiresFlag = "has_brass_key", LockedMessage = "The archive hatch is locked. Something in the valley must open it.",
            },
        },
    });
}

// --- rain_garden -----------------------------------------------------------
{
    int w = 70, h = 30;
    var tiles = RectRoomTiles(w, h);
    CarveGapV(tiles, w, h, w - 1, 15, 2); // back to village hub (east)

    var plotY = 400;
    regions.Add(new RegionDescriptor
    {
        Id = "rain_garden",
        DisplayName = "The Rain Garden",
        WidthTiles = w,
        HeightTiles = h,
        TileSize = tile,
        GroundColorHex = "2c3e2f",
        AccentColorHex = "17241a",
        SolidTiles = tiles,
        AmbienceId = "ambience_rain_garden",
        MusicId = "music_theme",
        SpawnPoints = new Dictionary<string, SpawnPointDef>
        {
            ["default"] = Spawn(w * tile - 60, 300),
            ["from_village"] = Spawn(w * tile - 60, 300),
        },
        Interactables = new List<InteractableDef>
        {
            new() { Id = "rain_garden_plot_0", X = 200, Y = plotY, Width = 20, Height = 20, Prompt = "Water this plot", PuzzleId = RainGardenPuzzle.PuzzleId, PuzzlePieceIndex = 0 },
            new() { Id = "rain_garden_plot_1", X = 260, Y = plotY, Width = 20, Height = 20, Prompt = "Water this plot", PuzzleId = RainGardenPuzzle.PuzzleId, PuzzlePieceIndex = 1 },
            new() { Id = "rain_garden_plot_2", X = 320, Y = plotY, Width = 20, Height = 20, Prompt = "Water this plot", PuzzleId = RainGardenPuzzle.PuzzleId, PuzzlePieceIndex = 2 },
            new() { Id = "rain_garden_plot_3", X = 380, Y = plotY, Width = 20, Height = 20, Prompt = "Water this plot", PuzzleId = RainGardenPuzzle.PuzzleId, PuzzlePieceIndex = 3 },
            new() { Id = "rain_garden_reset_marker", X = 290, Y = plotY - 40, Width = 16, Height = 16, Prompt = "Start over", PuzzleId = RainGardenPuzzle.PuzzleId, PuzzlePieceIndex = -1 },
            new()
            {
                Id = "rain_garden_dry_fountain", X = 300, Y = 200, Width = 24, Height = 24, Prompt = "Examine the dry fountain",
                IsLandmark = true, DiscoveryId = "journal_landmark_fountain",
                ExamineText = "A fountain that hasn't run in years. Someone still sweeps the leaves out of the basin.",
            },
            new()
            {
                Id = "rain_garden_weathervane", X = 500, Y = 150, Width = 16, Height = 32, Prompt = "Examine the weathervane",
                IsLandmark = true, DiscoveryId = "journal_landmark_weathervane",
                ExamineText = "It still turns true. Whatever it's pointing at, it's been pointing there a long time.",
            },
            new()
            {
                Id = "rain_garden_fern", X = 150, Y = 250, Width = 16, Height = 16, Prompt = "Pick up the pressed fern",
                GrantsItemId = "pressed_fern",
                ExamineText = "A fern, flattened and dried between two flat stones — someone's careful work, left behind.",
            },
        },
        Npcs = new List<NpcPlacementDef>
        {
            new() { Id = "the_gardener", DisplayName = "The Gardener", X = 300, Y = 460, DialogueId = "gardener_intro" },
        },
        Transitions = new List<TransitionDef>
        {
            new() { Id = "to_village", X = w * tile - 10, Y = 200, Width = 20, Height = 100, DestinationRegionId = "village_hub", DestinationSpawnId = "from_rain_garden" },
        },
    });
}

// --- wind_cliffs -------------------------------------------------------------
{
    int w = 80, h = 24;
    var tiles = RectRoomTiles(w, h);
    CarveGapV(tiles, w, h, 0, 12, 2); // back to village hub (west)

    var beaconY = 260;
    regions.Add(new RegionDescriptor
    {
        Id = "wind_cliffs",
        DisplayName = "The Wind Cliffs",
        WidthTiles = w,
        HeightTiles = h,
        TileSize = tile,
        GroundColorHex = "4a5560",
        AccentColorHex = "262c31",
        SolidTiles = tiles,
        AmbienceId = "ambience_wind_cliffs",
        MusicId = "music_theme",
        SpawnPoints = new Dictionary<string, SpawnPointDef>
        {
            ["default"] = Spawn(60, 260),
            ["from_village"] = Spawn(60, 260),
        },
        Interactables = new List<InteractableDef>
        {
            new() { Id = "wind_cliffs_beacon_0", X = 300, Y = beaconY, Width = 20, Height = 32, Prompt = "Light this beacon", PuzzleId = WindCliffsPuzzle.PuzzleId, PuzzlePieceIndex = 0 },
            new() { Id = "wind_cliffs_beacon_1", X = 500, Y = beaconY, Width = 20, Height = 32, Prompt = "Light this beacon", PuzzleId = WindCliffsPuzzle.PuzzleId, PuzzlePieceIndex = 1 },
            new() { Id = "wind_cliffs_beacon_2", X = 700, Y = beaconY, Width = 20, Height = 32, Prompt = "Light this beacon", PuzzleId = WindCliffsPuzzle.PuzzleId, PuzzlePieceIndex = 2 },
            new() { Id = "wind_cliffs_lever", X = 500, Y = beaconY + 60, Width = 16, Height = 16, Prompt = "Pull the reset lever", PuzzleId = WindCliffsPuzzle.PuzzleId, PuzzlePieceIndex = -1 },
            new()
            {
                Id = "wind_cliffs_rope_bridge", X = 400, Y = 150, Width = 40, Height = 16, Prompt = "Examine the rope bridge",
                IsLandmark = true, DiscoveryId = "journal_landmark_bridge",
                ExamineText = "The rope is still lashed tight to the first and third posts. The middle one hangs slack — whatever it held once, it isn't holding it now.",
            },
            new()
            {
                Id = "wind_cliffs_watch_tower", X = 700, Y = 130, Width = 24, Height = 40, Prompt = "Examine the watch tower",
                IsLandmark = true, DiscoveryId = "journal_landmark_tower",
                ExamineText = "A tower built for watching the sea for something that, as far as you can tell, never came back.",
            },
            new()
            {
                Id = "wind_cliffs_cairn", X = 620, Y = 340, Width = 16, Height = 16, Prompt = "Search the cairn",
                RequiresFlag = "wind_cliffs_puzzle_solved", GrantsItemId = "brass_key", SetsFlagOnInteract = "has_brass_key",
                ExamineText = "A brass key, left under a stone stacked deliberately — someone wanted it found, eventually.",
            },
        },
        Npcs = new List<NpcPlacementDef>
        {
            new() { Id = "the_watcher", DisplayName = "The Watcher", X = 760, Y = 260, DialogueId = "watcher_intro" },
        },
        Transitions = new List<TransitionDef>
        {
            new() { Id = "to_village", X = -10, Y = 200, Width = 20, Height = 100, DestinationRegionId = "village_hub", DestinationSpawnId = "from_wind_cliffs" },
        },
    });
}

// --- amber_shore -------------------------------------------------------------
{
    int w = 70, h = 26;
    var tiles = RectRoomTiles(w, h);
    CarveGapH(tiles, w, h, 0, 15, 2); // back to village hub (north)

    var altarY = 380;
    regions.Add(new RegionDescriptor
    {
        Id = "amber_shore",
        DisplayName = "The Amber Shore",
        WidthTiles = w,
        HeightTiles = h,
        TileSize = tile,
        GroundColorHex = "8a6a3f",
        AccentColorHex = "40301a",
        SolidTiles = tiles,
        AmbienceId = "ambience_amber_shore",
        MusicId = "music_theme",
        SpawnPoints = new Dictionary<string, SpawnPointDef>
        {
            ["default"] = Spawn(300, 60),
            ["from_village"] = Spawn(300, 60),
        },
        Interactables = new List<InteractableDef>
        {
            new() { Id = "amber_shore_shard_dawn", X = 150, Y = 300, Width = 16, Height = 16, Prompt = "Pick up the dawn-glass shard", GrantsItemId = "tide_glass_shard_dawn", ExamineText = "Sea-worn glass, warm amber, catching the light like sunrise." },
            new() { Id = "amber_shore_shard_dusk", X = 500, Y = 300, Width = 16, Height = 16, Prompt = "Pick up the dusk-glass shard", GrantsItemId = "tide_glass_shard_dusk", ExamineText = "Sea-worn glass, deep violet-amber, cooler than the other." },
            new() { Id = "amber_shore_shard_deep", X = 330, Y = 440, Width = 16, Height = 16, Prompt = "Pick up the deep-glass shard", GrantsItemId = "tide_glass_shard_deep", ExamineText = "Sea-worn glass, almost clear, heavier than it looks." },
            new()
            {
                Id = "amber_shore_shipwreck", X = 550, Y = 150, Width = 48, Height = 32, Prompt = "Examine the shipwreck",
                IsLandmark = true, DiscoveryId = "journal_landmark_shipwreck",
                ExamineText = "A hull, half-buried, that someone stopped trying to salvage a long time ago.",
            },
            new()
            {
                Id = "amber_shore_altar_slot_0", X = 280, Y = altarY, Width = 16, Height = 16, Prompt = "Place a shard (sunrise post)",
                PuzzleId = AmberShorePuzzle.PuzzleId, PuzzlePieceIndex = 0, IsLandmark = true, DiscoveryId = "journal_landmark_altar",
                ExamineText = "The tide-glass altar. Three empty settings, worn smooth by salt and hands.",
            },
            new() { Id = "amber_shore_altar_slot_1", X = 320, Y = altarY, Width = 16, Height = 16, Prompt = "Place a shard (far post)", PuzzleId = AmberShorePuzzle.PuzzleId, PuzzlePieceIndex = 1 },
            new() { Id = "amber_shore_altar_slot_2", X = 360, Y = altarY, Width = 16, Height = 16, Prompt = "Place a shard (between)", PuzzleId = AmberShorePuzzle.PuzzleId, PuzzlePieceIndex = 2 },
        },
        Npcs = new List<NpcPlacementDef>
        {
            new() { Id = "the_ferryperson", DisplayName = "The Ferryperson", X = 300, Y = 150, DialogueId = "ferryperson_intro" },
        },
        Transitions = new List<TransitionDef>
        {
            new() { Id = "to_village", X = 260, Y = -10, Width = 100, Height = 20, DestinationRegionId = "village_hub", DestinationSpawnId = "from_amber_shore" },
        },
    });
}

// --- archive (optional secret interior) ---------------------------------------
{
    int w = 26, h = 18;
    var tiles = RectRoomTiles(w, h);
    CarveGapH(tiles, w, h, 0, 13, 2); // stairway back up to village hub

    regions.Add(new RegionDescriptor
    {
        Id = "archive",
        DisplayName = "The Archive",
        WidthTiles = w,
        HeightTiles = h,
        TileSize = tile,
        GroundColorHex = "2a2420",
        AccentColorHex = "141210",
        SolidTiles = tiles,
        AmbienceId = "ambience_archive",
        MusicId = null,
        SpawnPoints = new Dictionary<string, SpawnPointDef>
        {
            ["default"] = Spawn(260, 300),
        },
        Interactables = new List<InteractableDef>
        {
            new()
            {
                Id = "archive_final_record", X = 260, Y = 200, Width = 20, Height = 20, Prompt = "Read the final record",
                IsOptionalSecret = true, RequiresFlag = "fragment_amber_shore_recovered", DiscoveryId = "journal_archive_final_record",
                ExamineText = "The last page, in a different hand: the lantern-keeper's own. They didn't leave because they stopped caring. They left because they were the only one left who remembered how to say goodbye properly, and it took every name they knew to do it.",
            },
        },
        Npcs = new List<NpcPlacementDef>
        {
            new() { Id = "the_archivist", DisplayName = "The Archivist", X = 260, Y = 260, DialogueId = "archivist_intro" },
        },
        Transitions = new List<TransitionDef>
        {
            new() { Id = "to_village", X = 220, Y = -10, Width = 80, Height = 20, DestinationRegionId = "village_hub", DestinationSpawnId = "from_archive" },
        },
    });
}

// ---------------------------------------------------------------- dialogue

var dialogueTrees = new List<DialogueTree>
{
    new()
    {
        Id = "cartographer_intro", NpcId = "the_cartographer",
        Nodes = new List<DialogueNode>
        {
            new() { Id = "start", Speaker = "The Cartographer", Text = "The lantern's gone dark, and half my map with it. You're not from here, are you? Good. Maybe you'll notice what we stopped seeing.", Condition = new DialogueCondition { RequiresFlag = "met_the_cartographer", Negate = true }, FallbackNext = "check_ready", SetsFlag = "met_the_cartographer", Next = "offer_task" },
            new() { Id = "check_ready", Speaker = "The Cartographer", Text = "Three names recovered. I think the lantern's just waiting on you now.", Condition = new DialogueCondition { RequiresFlag = "all_fragments_recovered" }, FallbackNext = "repeat_greeting", EndsConversation = true },
            new() { Id = "repeat_greeting", Speaker = "The Cartographer", Text = "Still finding your way around? The rain garden's past the gate, if you haven't been.", Next = "offer_task_repeat" },
            new()
            {
                Id = "offer_task", Speaker = "The Cartographer", Text = "Three places lost their names when the light went out. Bring back what you find, and I'll help you write them down again.",
                Choices = new List<DialogueChoice>
                {
                    new() { Text = "Where should I start?", Next = "hint_rain_garden" },
                    new() { Text = "I'll figure it out myself.", Next = "end" },
                },
            },
            new() { Id = "hint_rain_garden", Speaker = "The Cartographer", Text = "The rain garden's closest, past the gate. Something's still growing there, which is more than I can say for the rest of us.", UnlocksDiscoveryId = "journal_cartographer_note", EndsConversation = true },
            new() { Id = "offer_task_repeat", Speaker = "The Cartographer", Text = "Anything you find out there, bring it back here.", EndsConversation = true },
            new() { Id = "end", Speaker = "", Text = "", EndsConversation = true },
        },
    },
    new()
    {
        Id = "gardener_intro", NpcId = "the_gardener",
        Nodes = new List<DialogueNode>
        {
            new() { Id = "start", Speaker = "The Gardener", Text = "You're new. Careful where you step — the ground remembers who waters it, and who doesn't.", Condition = new DialogueCondition { RequiresFlag = "met_the_gardener", Negate = true }, FallbackNext = "check_solved", SetsFlag = "met_the_gardener", Next = "explain_plots" },
            new() { Id = "check_solved", Speaker = "The Gardener", Text = "The garden found its name again because of you. Amaranth Hollow. Say it enough and maybe it'll stick this time.", Condition = new DialogueCondition { RequiresFlag = "fragment_rain_garden_recovered" }, FallbackNext = "repeat", EndsConversation = true },
            new() { Id = "explain_plots", Speaker = "The Gardener", Text = "Four plots, four different kinds of thirsty. Start with the one that needs it most.", UnlocksDiscoveryId = "journal_gardener_note", EndsConversation = true },
            new() { Id = "repeat", Speaker = "The Gardener", Text = "Water the driest plot first. Always the driest.", EndsConversation = true },
        },
    },
    new()
    {
        Id = "watcher_intro", NpcId = "the_watcher",
        Nodes = new List<DialogueNode>
        {
            new() { Id = "start", Speaker = "The Watcher", Text = "Someone else who thinks the wind means something. Two of these three beacons matter. The rope told me which, ages ago.", Condition = new DialogueCondition { RequiresFlag = "met_the_watcher", Negate = true }, FallbackNext = "check_solved", SetsFlag = "met_the_watcher", Next = "offer_clues" },
            new() { Id = "check_solved", Speaker = "The Watcher", Text = "Lit properly, at last. I can stop watching for a while.", Condition = new DialogueCondition { RequiresFlag = "fragment_wind_cliffs_recovered" }, FallbackNext = "repeat", EndsConversation = true },
            new()
            {
                Id = "offer_clues", Speaker = "The Watcher", Text = "Ask, if you want. I don't mind saying it plainly.",
                Choices = new List<DialogueChoice>
                {
                    new() { Text = "Which two beacons?", Next = "tell_rope_clue" },
                    new() { Text = "Which order?", Next = "tell_wind_clue" },
                    new() { Text = "I'll work it out.", Next = "end" },
                },
            },
            new() { Id = "tell_rope_clue", Speaker = "The Watcher", Text = "Look at the fray on the rope bridge. Whichever posts it's still lashed to — those are load-bearing.", UnlocksDiscoveryId = "journal_watcher_note", Next = "offer_clues" },
            new() { Id = "tell_wind_clue", Speaker = "The Watcher", Text = "The rain garden's weathervane doesn't lie. Light the upwind one first.", Next = "offer_clues" },
            new() { Id = "repeat", Speaker = "The Watcher", Text = "Go on. The beacons won't light themselves.", EndsConversation = true },
            new() { Id = "end", Speaker = "", Text = "", EndsConversation = true },
        },
    },
    new()
    {
        Id = "ferryperson_intro", NpcId = "the_ferryperson",
        Nodes = new List<DialogueNode>
        {
            new() { Id = "start", Speaker = "The Ferryperson", Text = "You picked a strange place to start a conversation. Most people don't, here.", Condition = new DialogueCondition { RequiresFlag = "met_the_ferryperson", Negate = true }, FallbackNext = "gate_ready", SetsFlag = "met_the_ferryperson", Next = "deflect" },
            new() { Id = "deflect", Speaker = "The Ferryperson", Text = "Ask me something else. I don't talk about the landing much. Not yet.", EndsConversation = true },
            new() { Id = "gate_ready", Speaker = "The Ferryperson", Text = "You've seen enough of this valley now. Fine — ask.", Condition = new DialogueCondition { RequiresFlag = "both_prior_fragments_recovered" }, FallbackNext = "deflect_again", Next = "account_offer" },
            new() { Id = "deflect_again", Speaker = "The Ferryperson", Text = "Come back when you understand more of this place.", EndsConversation = true },
            new()
            {
                Id = "account_offer", Speaker = "The Ferryperson", Text = "Go on, then.",
                Choices = new List<DialogueChoice>
                {
                    new() { Text = "Tell me about the scattering.", Next = "scattering_account" },
                    new() { Text = "What's the tide-glass pattern?", Next = "pattern_clue" },
                    new() { Text = "Not now.", Next = "end" },
                },
            },
            new() { Id = "scattering_account", Speaker = "The Ferryperson", Text = "The drought took the garden first. Then the cliffwatch had nothing left to warn about. We didn't leave all at once — we just stopped calling it home, one boat at a time.", UnlocksDiscoveryId = "journal_ferryperson_note", Next = "account_offer" },
            new() { Id = "pattern_clue", Speaker = "The Ferryperson", Text = "Dawn-glass faces the sunrise post. Dusk-glass, the far one. The deep-glass sits between them, same as the water does.", Next = "account_offer" },
            new() { Id = "end", Speaker = "", Text = "", EndsConversation = true },
        },
    },
    new()
    {
        Id = "archivist_intro", NpcId = "the_archivist",
        Nodes = new List<DialogueNode>
        {
            new() { Id = "start", Speaker = "The Archivist", Text = "You found the stairs, then. Not many do, and fewer come back twice.", Condition = new DialogueCondition { RequiresFlag = "met_the_archivist", Negate = true }, FallbackNext = "repeat", SetsFlag = "met_the_archivist", Next = "offer" },
            new()
            {
                Id = "offer", Speaker = "The Archivist", Text = "Ask, if you'd like.",
                Choices = new List<DialogueChoice>
                {
                    new() { Text = "What is this place?", Next = "explain" },
                    new() { Text = "I'll read for myself.", Next = "end" },
                },
            },
            new() { Id = "explain", Speaker = "The Archivist", Text = "Everything anyone here chose to write down before they left. I keep it, even knowing most of it will never be read again. You're reading it. That's enough.", UnlocksDiscoveryId = "journal_archivist_note", EndsConversation = true },
            new() { Id = "repeat", Speaker = "The Archivist", Text = "The pages are still here. So am I.", EndsConversation = true },
            new() { Id = "end", Speaker = "", Text = "", EndsConversation = true },
        },
    },
};

// ------------------------------------------------------------------- items

var items = new List<ItemDefinition>
{
    new() { Id = "brass_key", DisplayName = "Brass Key", Description = "Worn smooth. Opens the archive hatch.", IconPath = "procedural:key" },
    new() { Id = "pressed_fern", DisplayName = "Pressed Fern", Description = "Found in the rain garden. Still faintly green.", IconPath = "procedural:leaf" },
    new() { Id = "tide_glass_shard_dawn", DisplayName = "Dawn-Glass Shard", Description = "Sea-worn glass, warm amber, catching the light like sunrise.", IconPath = "procedural:glass" },
    new() { Id = "tide_glass_shard_dusk", DisplayName = "Dusk-Glass Shard", Description = "Sea-worn glass, deep violet-amber, cooler than the other.", IconPath = "procedural:glass" },
    new() { Id = "tide_glass_shard_deep", DisplayName = "Deep-Glass Shard", Description = "Sea-worn glass, almost clear, heavier than it looks.", IconPath = "procedural:glass" },
};

// ------------------------------------------------------------ discoveries

var discoveries = new List<DiscoveryDefinition>
{
    new() { Id = "journal_arrival_village_hub", Category = "journal", Title = "Arrival", Text = "The valley was quieter than the maps suggested. Even the wind seemed to be listening for something." },
    new() { Id = "journal_arrival_rain_garden", Category = "journal", Title = "The Rain Garden", Text = "Green, against all odds, and tended by someone who hasn't given up on it." },
    new() { Id = "journal_arrival_wind_cliffs", Category = "journal", Title = "The Wind Cliffs", Text = "Beacons in a row, unlit, facing a sea that stopped sending anything worth warning about." },
    new() { Id = "journal_arrival_amber_shore", Category = "journal", Title = "The Amber Shore", Text = "Glass in the sand, warm to the touch even in shade. Someone left in a hurry, or slowly enough not to notice leaving." },
    new() { Id = "journal_arrival_archive", Category = "journal", Title = "The Archive", Text = "Stairs down, into a room that smells like paper and salt." },

    new() { Id = "journal_rain_garden_fragment", Category = "landmark", Title = "The Rain Garden's Name", Text = "Fragment recovered: the garden was once called Amaranth Hollow." },
    new() { Id = "journal_wind_cliffs_fragment", Category = "landmark", Title = "The Wind Cliffs' Name", Text = "Fragment recovered: the cliffs were once called The Cliffwatch." },
    new() { Id = "journal_amber_shore_fragment", Category = "landmark", Title = "The Amber Shore's Name", Text = "Fragment recovered: the shore was once called Tern's Landing." },

    new() { Id = "journal_rain_garden_watering_hint", Category = "journal", Title = "A Pattern in the Wilting", Text = "The driest plot always wants water first. Read the wilt, not the guesswork." },

    new() { Id = "journal_cartographer_note", Category = "npc", Title = "The Cartographer's Task", Text = "The Cartographer wants the map finished before their memory of the old names fades entirely." },
    new() { Id = "journal_gardener_note", Category = "npc", Title = "The Gardener's Patience", Text = "The Gardener stayed for the garden, not despite it. They want it to matter to someone else, too." },
    new() { Id = "journal_watcher_note", Category = "npc", Title = "The Watcher's Vigil", Text = "The Watcher has kept this duty alone for years, waiting to be relieved of it by someone who understands why it mattered." },
    new() { Id = "journal_ferryperson_note", Category = "npc", Title = "The Scattering", Text = "The valley didn't empty all at once. The drought took the garden first, then the cliffwatch lost its purpose, and people left one boat at a time." },
    new() { Id = "journal_archivist_note", Category = "npc", Title = "The Archivist's Duty", Text = "The Archivist keeps the full record even believing most of it will never be read again — until now." },

    new() { Id = "journal_archive_final_record", Category = "journal", Title = "The Last Page", Text = "The lantern-keeper's own hand: they didn't leave because they stopped caring. They left because they were the only one left who remembered how to say goodbye properly, and it took every name they knew to do it." },

    new() { Id = "journal_landmark_lantern", Category = "landmark", Title = "The Central Lantern", Text = "Dark, for now. The valley's namesake, waiting." },
    new() { Id = "journal_landmark_signpost", Category = "landmark", Title = "The Sanded Signpost", Text = "Three roads out of the hub, and a post sanded smooth of names — deliberately, it looks like." },
    new() { Id = "journal_landmark_fountain", Category = "landmark", Title = "The Dry Fountain", Text = "Hasn't run in years. Still, someone sweeps the leaves from the basin." },
    new() { Id = "journal_landmark_weathervane", Category = "landmark", Title = "The Weathervane", Text = "Turns true. It's been pointing at something for a long time." },
    new() { Id = "journal_landmark_bridge", Category = "landmark", Title = "The Rope Bridge", Text = "Tight on two posts, slack on the middle one. Whatever it held once isn't there anymore." },
    new() { Id = "journal_landmark_tower", Category = "landmark", Title = "The Watch Tower", Text = "Built for watching the sea for something that, as far as anyone can tell, never came back." },
    new() { Id = "journal_landmark_shipwreck", Category = "landmark", Title = "The Shipwreck", Text = "Half-buried, and long since given up on." },
    new() { Id = "journal_landmark_altar", Category = "landmark", Title = "The Tide-Glass Altar", Text = "Three empty settings, worn smooth by salt and hands." },
};

// -------------------------------------------------------------- validate

var regionsById = regions.ToDictionary(r => r.Id);
var issues = RegionLoader.Validate(regionsById);
if (issues.Count > 0)
{
    Console.Error.WriteLine("Region validation FAILED:");
    foreach (var issue in issues) Console.Error.WriteLine("  " + issue);
    Environment.Exit(1);
}

var dialogueIds = new HashSet<string>();
foreach (var tree in dialogueTrees)
{
    if (!dialogueIds.Add(tree.Id)) { Console.Error.WriteLine($"Duplicate dialogue tree id '{tree.Id}'"); Environment.Exit(1); }
    var nodeIds = new HashSet<string>();
    foreach (var node in tree.Nodes)
    {
        if (!nodeIds.Add(node.Id)) { Console.Error.WriteLine($"Dialogue '{tree.Id}' has duplicate node id '{node.Id}'"); Environment.Exit(1); }
    }
    foreach (var npc in regions.SelectMany(r => r.Npcs))
    {
        if (npc.DialogueId == tree.Id) { /* referenced, fine */ }
    }
}
foreach (var npc in regions.SelectMany(r => r.Npcs))
{
    if (!dialogueIds.Contains(npc.DialogueId))
    {
        Console.Error.WriteLine($"NPC '{npc.Id}' references missing dialogue tree '{npc.DialogueId}'");
        Environment.Exit(1);
    }
}

// ---------------------------------------------------------------- write

foreach (var region in regions)
{
    var path = Path.Combine(dataRoot, "maps", region.Id + ".json");
    File.WriteAllText(path, JsonSerializer.Serialize(region, jsonOptions));
}
foreach (var tree in dialogueTrees)
{
    var path = Path.Combine(dataRoot, "dialogue", tree.Id + ".json");
    File.WriteAllText(path, JsonSerializer.Serialize(tree, jsonOptions));
}
File.WriteAllText(Path.Combine(dataRoot, "items", "items.json"), JsonSerializer.Serialize(new { items }, jsonOptions));
File.WriteAllText(Path.Combine(dataRoot, "discoveries", "discoveries.json"), JsonSerializer.Serialize(new { discoveries }, jsonOptions));

Console.WriteLine($"Wrote {regions.Count} regions, {dialogueTrees.Count} dialogue trees, {items.Count} items, {discoveries.Count} discoveries to {dataRoot}");
Console.WriteLine("Region validation: OK (0 issues)");
