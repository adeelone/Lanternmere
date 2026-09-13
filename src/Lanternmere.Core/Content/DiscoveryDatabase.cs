using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Lanternmere.Core.Content;

/// <summary>category: "journal" (general lore), "landmark" (sketch entry), "npc" (character note).</summary>
public sealed class DiscoveryDefinition
{
    public string Id { get; set; } = "";
    public string Category { get; set; } = "journal";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class DiscoveryDatabase
{
    public IReadOnlyList<DiscoveryDefinition> Discoveries { get; }

    public DiscoveryDatabase(IEnumerable<DiscoveryDefinition> discoveries)
    {
        Discoveries = discoveries.ToList();
        // ToDictionary here is a deliberate fail-fast duplicate-id check
        // (throws on a repeated Id) — the resulting dictionary itself
        // isn't retained, since every real lookup goes through UnlockedFor.
        _ = Discoveries.ToDictionary(d => d.Id);
    }

    /// <summary>Discoveries currently present in the player's journal, in the database's canonical (authoring) order rather than acquisition order — reads better as a journal.</summary>
    public IEnumerable<DiscoveryDefinition> UnlockedFor(WorldState world) =>
        Discoveries.Where(d => world.HasDiscovery(d.Id));

    private sealed class Payload { public List<DiscoveryDefinition> Discoveries { get; set; } = new(); }

    public static DiscoveryDatabase LoadFromJson(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var payload = JsonSerializer.Deserialize<Payload>(json, options) ?? new Payload();
        return new DiscoveryDatabase(payload.Discoveries);
    }
}
