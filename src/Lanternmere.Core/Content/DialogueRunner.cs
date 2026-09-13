using System.Collections.Generic;
using System.Linq;

namespace Lanternmere.Core.Content;

/// <summary>
/// Drives a <see cref="DialogueTree"/> against a <see cref="WorldState"/>:
/// resolves conditional nodes/choices, applies flag/discovery side effects,
/// and tracks which node is current. Pure logic — no rendering — so
/// branching and conditions are unit-testable without a graphics device.
/// The owning scene is responsible for rendering CurrentNode and calling
/// Advance()/Choose() in response to input.
/// </summary>
public sealed class DialogueRunner
{
    private readonly DialogueTree _tree;
    private readonly WorldState _world;

    public DialogueNode CurrentNode { get; private set; }
    public bool IsFinished { get; private set; }

    public DialogueRunner(DialogueTree tree, WorldState world)
    {
        _tree = tree;
        _world = world;
        CurrentNode = ResolveConditional(tree.Start);
        ApplyEnterEffects(CurrentNode);
    }

    /// <summary>Choices available on the current node, filtered to only those whose condition currently passes.</summary>
    public IReadOnlyList<DialogueChoice> AvailableChoices =>
        CurrentNode.Choices.Where(c => c.Condition is null || c.Condition.IsSatisfiedBy(_world)).ToList();

    /// <summary>For a node with no choices: move to Next (or finish if there is none).</summary>
    public void Advance()
    {
        if (IsFinished) return;
        if (CurrentNode.Choices.Count > 0) return; // must use Choose() when choices are present

        if (CurrentNode.EndsConversation || CurrentNode.Next is null)
        {
            IsFinished = true;
            return;
        }

        GoTo(CurrentNode.Next);
    }

    /// <summary>Take a choice by index into AvailableChoices.</summary>
    public void Choose(int availableChoiceIndex)
    {
        if (IsFinished) return;
        var choices = AvailableChoices;
        if (availableChoiceIndex < 0 || availableChoiceIndex >= choices.Count) return;

        var choice = choices[availableChoiceIndex];
        if (choice.SetsFlag is not null) _world.SetFlag(choice.SetsFlag);
        GoTo(choice.Next);
    }

    private void GoTo(string nodeId)
    {
        var next = ResolveConditional(_tree.GetNode(nodeId));
        CurrentNode = next;
        ApplyEnterEffects(next);

        if (next.EndsConversation)
        {
            IsFinished = true;
        }
    }

    /// <summary>
    /// A node may declare a Condition + FallbackNext: if the condition
    /// fails, transparently redirect to the fallback (and re-check that
    /// one too), so authors can gate content without the caller needing to
    /// know about conditional branching at all.
    /// </summary>
    private DialogueNode ResolveConditional(DialogueNode node)
    {
        var guard = 0;
        while (node.Condition is not null && !node.Condition.IsSatisfiedBy(_world))
        {
            if (node.FallbackNext is null)
            {
                break; // no fallback specified: show the node as-is rather than crash
            }
            node = _tree.GetNode(node.FallbackNext);
            if (++guard > 64) break; // defensive: guards against an authoring cycle
        }
        return node;
    }

    private void ApplyEnterEffects(DialogueNode node)
    {
        if (node.SetsFlag is not null) _world.SetFlag(node.SetsFlag);
        if (node.UnlocksDiscoveryId is not null) _world.AddDiscovery(node.UnlocksDiscoveryId);
    }
}
