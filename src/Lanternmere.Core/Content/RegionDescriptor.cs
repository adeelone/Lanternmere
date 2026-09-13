using System.Collections.Generic;

namespace Lanternmere.Core.Content;

/// <summary>Pixel-space spawn point a player lands at when entering a region.</summary>
public sealed class SpawnPointDef
{
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>
/// A static, interactable thing in a region: a landmark, an item pickup, a
/// puzzle piece, an NPC-less prop with a dialogue-less examine text, etc.
/// One shape covers all of these because the brief asks for a single
/// context-sensitive interaction, not per-type handlers.
/// </summary>
public sealed class InteractableDef
{
    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public int Width { get; set; } = 16;
    public int Height { get; set; } = 16;

    /// <summary>Shown in the context-sensitive interaction prompt, e.g. "Examine the lantern".</summary>
    public string Prompt { get; set; } = "Examine";

    /// <summary>Optional flat text shown on interact when there's no dialogue tree (a sign, a landmark plaque).</summary>
    public string? ExamineText { get; set; }

    /// <summary>Optional dialogue tree id to run instead of/after ExamineText.</summary>
    public string? DialogueId { get; set; }

    /// <summary>Optional discovery id unlocked the first time this is examined.</summary>
    public string? DiscoveryId { get; set; }

    /// <summary>Optional item id granted the first time this is examined (a pickup).</summary>
    public string? GrantsItemId { get; set; }

    /// <summary>Optional world flag set the first time this is examined.</summary>
    public string? SetsFlagOnInteract { get; set; }

    /// <summary>If set, this interactable is hidden unless the named flag is true (e.g. a puzzle-solved shortcut prop).</summary>
    public string? RequiresFlag { get; set; }

    /// <summary>If set, this interactable is hidden once the named flag becomes true (e.g. a plot that's been resolved).</summary>
    public string? HiddenAfterFlag { get; set; }

    /// <summary>Marks this as one of the brief's "8 major landmarks" — surfaces in the journal's landmark-sketch list and on the map once revealed.</summary>
    public bool IsLandmark { get; set; }

    /// <summary>Marks this as part of the one required optional secret, for audit/traceability only.</summary>
    public bool IsOptionalSecret { get; set; }

    /// <summary>Id of a puzzle this interactable is part of (a plot, a beacon, a glass slot). The puzzle controller owns the actual logic; this just identifies which puzzle + which piece index.</summary>
    public string? PuzzleId { get; set; }
    public int PuzzlePieceIndex { get; set; } = -1;
}

public sealed class NpcPlacementDef
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public string DialogueId { get; set; } = "";
}

/// <summary>A doorway/path edge to another region, or to the same region's different area.</summary>
public sealed class TransitionDef
{
    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public int Width { get; set; } = 16;
    public int Height { get; set; } = 32;
    public string DestinationRegionId { get; set; } = "";
    public string DestinationSpawnId { get; set; } = "default";

    /// <summary>If set, the transition is blocked (with feedback, not a silent wall) unless this flag is true.</summary>
    public string? RequiresFlag { get; set; }
    public string? LockedMessage { get; set; }
}

/// <summary>
/// A full region: a tile grid (ground + collision), spawn points,
/// interactables, NPC placements, and transitions to other regions. This is
/// Lanternmere's data-driven map format — the brief's sanctioned
/// alternative to a Tiled importer ("a maintained importer or a documented
/// data-driven loader"). See docs/REGION_FORMAT.md for the full field
/// reference.
/// </summary>
public sealed class RegionDescriptor
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int WidthTiles { get; set; }
    public int HeightTiles { get; set; }
    public int TileSize { get; set; } = 16;

    /// <summary>RRGGBB hex, no leading '#'. The base ground color the region's placeholder tiles are generated from.</summary>
    public string GroundColorHex { get; set; } = "2c3e2f";
    public string AccentColorHex { get; set; } = "1a1a1a";

    /// <summary>Row-major, length WidthTiles*HeightTiles. Non-zero = solid (collidable).</summary>
    public int[] SolidTiles { get; set; } = System.Array.Empty<int>();

    public Dictionary<string, SpawnPointDef> SpawnPoints { get; set; } = new();
    public List<InteractableDef> Interactables { get; set; } = new();
    public List<NpcPlacementDef> Npcs { get; set; } = new();
    public List<TransitionDef> Transitions { get; set; } = new();

    /// <summary>Ambient audio track id to loop while this region is active (see docs/AUDIO.md).</summary>
    public string? AmbienceId { get; set; }
    public string? MusicId { get; set; }

    public bool IsSolidAtTile(int tx, int ty)
    {
        if (tx < 0 || ty < 0 || tx >= WidthTiles || ty >= HeightTiles) return true; // out of bounds = solid, keeps the player boxed in
        return SolidTiles[ty * WidthTiles + tx] != 0;
    }
}
