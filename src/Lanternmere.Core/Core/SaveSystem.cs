using System;
using System.IO;
using System.Text.Json;

namespace Lanternmere.Core;

/// <summary>Wrapper persisted to disk: schema version + payload, so future saves can migrate old data instead of breaking on it.</summary>
public sealed class SaveFileEnvelope
{
    public int SchemaVersion { get; set; } = SaveSystem.CurrentSchemaVersion;
    public WorldState World { get; set; } = new();
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
}

public enum LoadResult
{
    Success,
    NotFound,
    Corrupted,
    RecoveredFromBackup,
}

/// <summary>
/// Multi-slot save system with atomic writes, a backup copy, corruption
/// detection, and graceful recovery — per the brief's "Save system"
/// section. Never called mid-transition or mid-puzzle-mutation; callers
/// (Scenes) are responsible for only invoking Save() at safe anchors or on
/// explicit manual save.
/// </summary>
public static class SaveSystem
{
    public const int CurrentSchemaVersion = 1;
    private const int SlotCount = 3;

    private static string SlotPath(int slot) => Path.Combine(GameSettings.GetSaveDirectory(), $"save{slot}.json");
    private static string BackupPath(int slot) => Path.Combine(GameSettings.GetSaveDirectory(), $"save{slot}.bak.json");
    private static string TempPath(int slot) => Path.Combine(GameSettings.GetSaveDirectory(), $"save{slot}.tmp.json");

    public static bool SlotExists(int slot) => File.Exists(SlotPath(slot));

    /// <summary>
    /// Atomic-ish save: write to a temp file, then move the current file to
    /// the backup slot, then move the temp file into place. If the process
    /// dies between steps, the previous save or its backup is still
    /// recoverable — there is never a moment where both the primary and
    /// backup files are simultaneously invalid.
    /// </summary>
    public static void Save(int slot, WorldState world)
    {
        if (slot < 0 || slot >= SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));

        var envelope = new SaveFileEnvelope { World = world, SavedAtUtc = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true });

        var tempPath = TempPath(slot);
        File.WriteAllText(tempPath, json);

        // Verify the temp file round-trips before touching the real slot —
        // never let a bad write clobber a good save.
        var verify = TryDeserialize(File.ReadAllText(tempPath));
        if (verify is null)
        {
            File.Delete(tempPath);
            throw new InvalidOperationException("Save payload failed to round-trip; aborting save to protect existing data.");
        }

        var primaryPath = SlotPath(slot);
        if (File.Exists(primaryPath))
        {
            File.Copy(primaryPath, BackupPath(slot), overwrite: true);
        }
        File.Move(tempPath, primaryPath, overwrite: true);
    }

    public static (LoadResult result, WorldState? world) Load(int slot)
    {
        if (slot < 0 || slot >= SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));

        var primaryPath = SlotPath(slot);
        if (!File.Exists(primaryPath)) return (LoadResult.NotFound, null);

        var primary = TryDeserialize(SafeRead(primaryPath));
        if (primary is not null)
        {
            return (LoadResult.Success, Migrate(primary));
        }

        // Primary is corrupted — fall back to the backup copy rather than
        // losing the save outright.
        var backupPath = BackupPath(slot);
        if (File.Exists(backupPath))
        {
            var backup = TryDeserialize(SafeRead(backupPath));
            if (backup is not null)
            {
                return (LoadResult.RecoveredFromBackup, Migrate(backup));
            }
        }

        return (LoadResult.Corrupted, null);
    }

    private static string SafeRead(string path)
    {
        try { return File.ReadAllText(path); }
        catch { return string.Empty; }
    }

    private static SaveFileEnvelope? TryDeserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<SaveFileEnvelope>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static WorldState Migrate(SaveFileEnvelope envelope)
    {
        // No migrations needed yet at schema version 1. Future schema bumps
        // add a switch here, e.g.:
        //   if (envelope.SchemaVersion < 2) { ...upgrade envelope.World...; envelope.SchemaVersion = 2; }
        return envelope.World;
    }
}
