using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lanternmere.Core.Content;

public sealed class ItemDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconPath { get; set; } = "";
}

public sealed class ItemDatabase
{
    public IReadOnlyList<ItemDefinition> Items { get; }
    private readonly Dictionary<string, ItemDefinition> _byId;

    public ItemDatabase(IEnumerable<ItemDefinition> items)
    {
        Items = items.ToList();
        _byId = Items.ToDictionary(i => i.Id);
    }

    public bool TryGet(string id, out ItemDefinition item) => _byId.TryGetValue(id, out item!);

    private sealed class Payload { public List<ItemDefinition> Items { get; set; } = new(); }

    public static ItemDatabase LoadFromJson(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var payload = JsonSerializer.Deserialize<Payload>(json, options) ?? new Payload();
        return new ItemDatabase(payload.Items);
    }
}
