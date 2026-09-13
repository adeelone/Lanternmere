using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Lanternmere.Core.Content;

namespace Lanternmere.Core;

/// <summary>
/// Loads every data-driven content file (regions, dialogue trees, items,
/// discoveries) from disk once at startup. Parsing itself lives in
/// Lanternmere.Core.Content (RegionLoader, DialogueTree, ItemDatabase,
/// DiscoveryDatabase) so it's unit-testable without touching a filesystem;
/// this class is just the disk-reading + validation orchestration for the
/// real MonoGame executable.
/// </summary>
public sealed class GameContent
{
    public Dictionary<string, RegionDescriptor> Regions { get; }
    public Dictionary<string, DialogueTree> DialogueTrees { get; }
    public ItemDatabase Items { get; }
    public DiscoveryDatabase Discoveries { get; }
    public IReadOnlyList<RegionValidationIssue> ValidationIssues { get; }

    private GameContent(
        Dictionary<string, RegionDescriptor> regions,
        Dictionary<string, DialogueTree> dialogueTrees,
        ItemDatabase items,
        DiscoveryDatabase discoveries,
        IReadOnlyList<RegionValidationIssue> validationIssues)
    {
        Regions = regions;
        DialogueTrees = dialogueTrees;
        Items = items;
        Discoveries = discoveries;
        ValidationIssues = validationIssues;
    }

    /// <summary>True if any content required for a clean playthrough is missing/invalid — drives the brief's required "missing-content error" screen instead of a crash or silent bad state.</summary>
    public bool HasFatalIssues => ValidationIssues.Count > 0;

    public static GameContent? TryLoad(string dataRoot, out string? error)
    {
        try
        {
            var regionsDir = Path.Combine(dataRoot, "maps");
            var dialogueDir = Path.Combine(dataRoot, "dialogue");
            var itemsPath = Path.Combine(dataRoot, "items", "items.json");
            var discoveriesPath = Path.Combine(dataRoot, "discoveries", "discoveries.json");

            if (!Directory.Exists(regionsDir) || !Directory.Exists(dialogueDir) || !File.Exists(itemsPath) || !File.Exists(discoveriesPath))
            {
                error = $"Required content directory or file missing under '{dataRoot}'.";
                return null;
            }

            var regions = RegionLoader.LoadAllFromDirectory(regionsDir);
            var validation = RegionLoader.Validate(regions);

            var dialogueTrees = new Dictionary<string, DialogueTree>();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var file in Directory.GetFiles(dialogueDir, "*.json"))
            {
                var tree = JsonSerializer.Deserialize<DialogueTree>(File.ReadAllText(file), jsonOptions);
                if (tree is not null) dialogueTrees[tree.Id] = tree;
            }

            var items = ItemDatabase.LoadFromJson(File.ReadAllText(itemsPath));
            var discoveries = DiscoveryDatabase.LoadFromJson(File.ReadAllText(discoveriesPath));

            error = null;
            return new GameContent(regions, dialogueTrees, items, discoveries, validation);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }
}
