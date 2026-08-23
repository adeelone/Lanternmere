using System;
using System.IO;
using System.Text.Json;

namespace Lanternmere.Core;

/// <summary>
/// Persisted player settings: rebinds, accessibility options, and audio
/// levels, per the brief's "Accessibility and settings" section. Stored as
/// plain JSON next to save data (see <see cref="SaveSystem"/> for the more
/// carefully-guarded save-slot format).
/// </summary>
public sealed class GameSettings
{
    public int SchemaVersion { get; set; } = 1;

    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float AmbienceVolume { get; set; } = 0.8f;
    public float EffectsVolume { get; set; } = 1.0f;

    public bool ReducedMotion { get; set; } = false;
    public bool ReducedShake { get; set; } = false;
    public bool ReducedFlash { get; set; } = false;
    public bool HighContrastInteractionIndicators { get; set; } = false;

    public float TextSpeed { get; set; } = 1.0f; // multiplier; see also InstantText
    public bool InstantText { get; set; } = false;

    public bool Fullscreen { get; set; } = false;
    public int ResolutionWidth { get; set; } = 1280;
    public int ResolutionHeight { get; set; } = 720;
    public float UiScale { get; set; } = 1.0f;

    /// <summary>Action name -> bound key/button, e.g. "MoveUp" -> "W". Populated by InputManager defaults if empty.</summary>
    public System.Collections.Generic.Dictionary<string, string> KeyBindings { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, string> GamepadBindings { get; set; } = new();

    private static string GetSettingsPath() =>
        Path.Combine(GetSaveDirectory(), "settings.json");

    public static string GetSaveDirectory()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        // Windows: %AppData%/Lanternmere. macOS/Linux under MonoGame DesktopGL
        // typically resolve ApplicationData to ~/.config — both are
        // documented in README.md "Save data location".
        var dir = Path.Combine(baseDir, "Lanternmere");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static GameSettings LoadOrDefault()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path)) return new GameSettings();
        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<GameSettings>(json);
            return loaded ?? new GameSettings();
        }
        catch (Exception)
        {
            // Corrupt settings file: fall back to defaults rather than crash.
            return new GameSettings();
        }
    }

    public void Save()
    {
        var path = GetSettingsPath();
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        var tmpPath = path + ".tmp";
        File.WriteAllText(tmpPath, json);
        File.Move(tmpPath, path, overwrite: true); // atomic-ish replace
    }
}
