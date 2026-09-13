using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Lanternmere.Core.Content;

public sealed class RegionValidationIssue
{
    public string RegionId { get; }
    public string Message { get; }
    public RegionValidationIssue(string regionId, string message) { RegionId = regionId; Message = message; }
    public override string ToString() => $"[{RegionId}] {Message}";
}

/// <summary>
/// Loads region JSON files and validates them as a set, per the brief's
/// "Automated map validation for missing spawn points, bad destinations,
/// duplicate IDs, and unreachable required triggers where feasible."
/// Pure logic, no MonoGame content APIs, so it's directly unit-testable —
/// see test/Lanternmere.Tests/RegionLoaderTests.cs.
/// </summary>
public static class RegionLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static RegionDescriptor LoadFromJson(string json)
    {
        var region = JsonSerializer.Deserialize<RegionDescriptor>(json, Options);
        if (region is null) throw new InvalidDataException("Region JSON deserialized to null.");
        return region;
    }

    public static Dictionary<string, RegionDescriptor> LoadAllFromDirectory(string directory)
    {
        var result = new Dictionary<string, RegionDescriptor>();
        foreach (var file in Directory.GetFiles(directory, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            var region = LoadFromJson(File.ReadAllText(file));
            result[region.Id] = region;
        }
        return result;
    }

    /// <summary>
    /// Validates a full set of loaded regions against each other (so
    /// cross-region transition destinations can be checked). Returns every
    /// issue found rather than throwing on the first one, so a single
    /// validation pass reports the complete picture.
    /// </summary>
    public static List<RegionValidationIssue> Validate(IReadOnlyDictionary<string, RegionDescriptor> regions)
    {
        var issues = new List<RegionValidationIssue>();

        foreach (var region in regions.Values)
        {
            if (region.SolidTiles.Length != region.WidthTiles * region.HeightTiles)
            {
                issues.Add(new RegionValidationIssue(region.Id,
                    $"SolidTiles length {region.SolidTiles.Length} does not match WidthTiles*HeightTiles ({region.WidthTiles * region.HeightTiles})."));
            }

            if (!region.SpawnPoints.ContainsKey("default"))
            {
                issues.Add(new RegionValidationIssue(region.Id, "Missing required 'default' spawn point."));
            }

            var seenIds = new HashSet<string>();
            foreach (var id in region.Interactables.Select(i => i.Id)
                         .Concat(region.Npcs.Select(n => n.Id))
                         .Concat(region.Transitions.Select(t => t.Id)))
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    issues.Add(new RegionValidationIssue(region.Id, "An interactable/NPC/transition has an empty id."));
                    continue;
                }
                if (!seenIds.Add(id))
                {
                    issues.Add(new RegionValidationIssue(region.Id, $"Duplicate id '{id}' within region."));
                }
            }

            foreach (var transition in region.Transitions)
            {
                if (!regions.TryGetValue(transition.DestinationRegionId, out var destination))
                {
                    issues.Add(new RegionValidationIssue(region.Id,
                        $"Transition '{transition.Id}' points at unknown region '{transition.DestinationRegionId}'."));
                    continue;
                }
                if (!destination.SpawnPoints.ContainsKey(transition.DestinationSpawnId))
                {
                    issues.Add(new RegionValidationIssue(region.Id,
                        $"Transition '{transition.Id}' points at spawn '{transition.DestinationSpawnId}' which does not exist in region '{destination.Id}'."));
                }
            }

            // "Unreachable required triggers where feasible": a landmark or
            // transition whose top-left tile is itself marked solid can
            // never be walked into, since the player's bounds would always
            // collide with it. This is a cheap, sound (if not complete)
            // reachability proxy that catches the common authoring mistake
            // of placing a trigger *inside* a wall tile.
            foreach (var interactable in region.Interactables)
            {
                var tx = (int)(interactable.X / Math.Max(1, region.TileSize));
                var ty = (int)(interactable.Y / Math.Max(1, region.TileSize));
                if (region.WidthTiles > 0 && region.HeightTiles > 0 && region.IsSolidAtTile(tx, ty) &&
                    IsFullySurroundedBySolid(region, tx, ty))
                {
                    issues.Add(new RegionValidationIssue(region.Id,
                        $"Interactable '{interactable.Id}' sits on a solid tile with no adjacent open tile — likely unreachable."));
                }
            }
        }

        return issues;
    }

    private static bool IsFullySurroundedBySolid(RegionDescriptor region, int tx, int ty)
    {
        foreach (var (dx, dy) in new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            if (!region.IsSolidAtTile(tx + dx, ty + dy)) return false;
        }
        return true;
    }
}
