using System.Collections.Generic;

namespace Lanternmere.Core.Content;

public sealed class DialogueCondition
{
    /// <summary>World flag name this choice/node requires to be shown/taken. Null = always available.</summary>
    public string? RequiresFlag { get; set; }

    /// <summary>If true, the flag must be false (not true) for this to be available.</summary>
    public bool Negate { get; set; }

    public bool IsSatisfiedBy(WorldState world) =>
        RequiresFlag is null || (world.GetFlag(RequiresFlag) != Negate);
}

public sealed class DialogueChoice
{
    public string Text { get; set; } = "";
    public string Next { get; set; } = "";
    public DialogueCondition? Condition { get; set; }

    /// <summary>Optional flag set the moment this choice is taken (independent of whatever the destination node does).</summary>
    public string? SetsFlag { get; set; }
}

public sealed class DialogueNode
{
    public string Id { get; set; } = "";
    public string Speaker { get; set; } = "";
    public string Text { get; set; } = "";

    /// <summary>Straight-line continuation when there are no choices. Null if this node ends the conversation or only has Choices.</summary>
    public string? Next { get; set; }

    public List<DialogueChoice> Choices { get; set; } = new();
    public bool EndsConversation { get; set; }

    /// <summary>Optional flag set the moment this node is shown.</summary>
    public string? SetsFlag { get; set; }

    /// <summary>Optional discovery unlocked the moment this node is shown.</summary>
    public string? UnlocksDiscoveryId { get; set; }

    /// <summary>Only shown if this condition passes; otherwise the runner falls through to FallbackNext.</summary>
    public DialogueCondition? Condition { get; set; }
    public string? FallbackNext { get; set; }
}

public sealed class DialogueTree
{
    public string Id { get; set; } = "";
    public string NpcId { get; set; } = "";
    public List<DialogueNode> Nodes { get; set; } = new();

    private Dictionary<string, DialogueNode>? _byId;

    public DialogueNode GetNode(string id)
    {
        _byId ??= Nodes.ToDictionaryChecked();
        if (!_byId.TryGetValue(id, out var node))
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Dialogue '{Id}' has no node '{id}'.");
        }
        return node;
    }

    public DialogueNode Start => GetNode("start");
}

internal static class DialogueTreeExtensions
{
    public static Dictionary<string, DialogueNode> ToDictionaryChecked(this List<DialogueNode> nodes)
    {
        var dict = new Dictionary<string, DialogueNode>();
        foreach (var node in nodes)
        {
            dict[node.Id] = node;
        }
        return dict;
    }
}
