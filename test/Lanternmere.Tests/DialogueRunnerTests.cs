using System.Collections.Generic;
using Lanternmere.Core;
using Lanternmere.Core.Content;
using Xunit;

namespace Lanternmere.Tests;

public class DialogueRunnerTests
{
    private static DialogueTree BuildTree() => new()
    {
        Id = "test_tree",
        NpcId = "npc",
        Nodes = new List<DialogueNode>
        {
            new() { Id = "start", Speaker = "A", Text = "Hello.", Next = "offer" },
            new()
            {
                Id = "offer", Speaker = "A", Text = "Choose.",
                Choices = new List<DialogueChoice>
                {
                    new() { Text = "Locked option", Next = "locked_reply", Condition = new DialogueCondition { RequiresFlag = "has_key" } },
                    new() { Text = "Always available", Next = "end" },
                },
            },
            new() { Id = "locked_reply", Speaker = "A", Text = "You had the key.", SetsFlag = "heard_locked_reply", EndsConversation = true },
            new() { Id = "end", Speaker = "", Text = "", EndsConversation = true },
        },
    };

    [Fact]
    public void Advance_LinearNode_MovesToNext()
    {
        var world = new WorldState();
        var runner = new DialogueRunner(BuildTree(), world);

        Assert.Equal("start", runner.CurrentNode.Id);
        runner.Advance();

        Assert.Equal("offer", runner.CurrentNode.Id);
    }

    [Fact]
    public void AvailableChoices_FiltersOutUnsatisfiedConditions()
    {
        var world = new WorldState();
        var runner = new DialogueRunner(BuildTree(), world);
        runner.Advance();

        Assert.Single(runner.AvailableChoices);
        Assert.Equal("Always available", runner.AvailableChoices[0].Text);
    }

    [Fact]
    public void AvailableChoices_IncludesConditionalChoiceOnceFlagSet()
    {
        var world = new WorldState();
        world.SetFlag("has_key");
        var runner = new DialogueRunner(BuildTree(), world);
        runner.Advance();

        Assert.Equal(2, runner.AvailableChoices.Count);
    }

    [Fact]
    public void Choose_AppliesSideEffectsAndFinishes()
    {
        var world = new WorldState();
        world.SetFlag("has_key");
        var runner = new DialogueRunner(BuildTree(), world);
        runner.Advance();

        runner.Choose(0); // "Locked option" -> locked_reply

        Assert.Equal("locked_reply", runner.CurrentNode.Id);
        Assert.True(world.GetFlag("heard_locked_reply"));
        Assert.True(runner.IsFinished);
    }

    [Fact]
    public void ConditionalNodeWithFallback_RedirectsWhenConditionFails()
    {
        var tree = new DialogueTree
        {
            Id = "gated_tree",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "start", Condition = new DialogueCondition { RequiresFlag = "seen_intro" }, FallbackNext = "intro", Text = "Welcome back.", EndsConversation = true },
                new() { Id = "intro", Text = "First time here.", SetsFlag = "seen_intro", EndsConversation = true },
            },
        };
        var world = new WorldState();

        var runner = new DialogueRunner(tree, world);

        Assert.Equal("intro", runner.CurrentNode.Id);
    }
}
