using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

public enum SaveSlotMode { Load, Save }

/// <summary>
/// The 3-slot picker used by Title's Continue/Load and (in Save mode) a
/// manual save target. Shows honest per-slot metadata (region, playtime,
/// last-saved time) or "Empty", and surfaces a corrupted-and-unrecoverable
/// slot distinctly rather than silently skipping it.
/// </summary>
public sealed class SaveSlotScene : IScene
{
    private readonly InputManager _input;
    private readonly GameSettings _settings;
    private readonly AudioManager _audio;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly Core.GameContent _content;
    private readonly SaveSlotMode _mode;
    private readonly WorldState? _worldToSave;
    private SceneManager _manager = null!;
    private int _selected;

    public bool DrawsOverPreviousScene => true;

    public SaveSlotScene(InputManager input, GameSettings settings, AudioManager audio, Texture2D pixel, SpriteFont font,
        Core.GameContent content, SaveSlotMode mode, WorldState? worldToSave = null)
    {
        _input = input;
        _settings = settings;
        _audio = audio;
        _pixel = pixel;
        _font = font;
        _content = content;
        _mode = mode;
        _worldToSave = worldToSave;
    }

    // Slot labels are read from disk once here rather than every Draw call
    // (60x/second) — the save files can't change while this picker is open
    // except via HandleSelect's own Save branch, which pops the scene
    // immediately afterward, so there's nothing to invalidate.
    private readonly string[] _slotDetails = new string[SaveSystem.SlotCount];

    public void Enter(SceneManager manager)
    {
        _manager = manager;
        for (var slot = 0; slot < SaveSystem.SlotCount; slot++)
        {
            _slotDetails[slot] = DescribeSlot(slot);
        }
    }

    public void Exit() { }

    private static string DescribeSlot(int slot)
    {
        var (result, savedAt, world) = SaveSystem.LoadWithMetadata(slot);
        return result switch
        {
            LoadResult.Success or LoadResult.RecoveredFromBackup =>
                $"{world!.CurrentRegionId} - {(int)(world.PlaytimeSeconds / 60)} min - {savedAt:yyyy-MM-dd HH:mm} UTC" + (world.ReachedMainEnding ? " - Completed" : ""),
            LoadResult.Corrupted => "Corrupted (unrecoverable)",
            _ => "Empty",
        };
    }

    public void Update(GameTime gameTime)
    {
        _input.Update();

        if (_input.WasPressedThisFrame(GameAction.Cancel))
        {
            _manager.Pop();
            return;
        }
        if (_input.WasPressedThisFrame(GameAction.MoveUp)) _selected = (_selected - 1 + SaveSystem.SlotCount) % SaveSystem.SlotCount;
        else if (_input.WasPressedThisFrame(GameAction.MoveDown)) _selected = (_selected + 1) % SaveSystem.SlotCount;
        else if (_input.WasPressedThisFrame(GameAction.Confirm))
        {
            HandleSelect();
        }
    }

    private void HandleSelect()
    {
        if (_mode == SaveSlotMode.Save)
        {
            SaveSystem.Save(_selected, _worldToSave!);
            _audio.PlaySfx("sfx_menu_select");
            _manager.Pop();
            return;
        }

        var (result, _, world) = SaveSystem.LoadWithMetadata(_selected);
        switch (result)
        {
            case LoadResult.Success:
            case LoadResult.RecoveredFromBackup:
                _audio.PlaySfx("sfx_menu_select");
                _manager.ReplaceAll(new RegionScene(_input, _settings, _content, world!, _audio,
                    _font, _pixel, world!.CurrentRegionId, "default"));
                break;
            case LoadResult.Corrupted:
                _manager.Push(new SaveCorruptedScene(_input, _pixel, _font));
                break;
            case LoadResult.NotFound:
                // Empty slot in Load mode: no-op, nothing to load.
                break;
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x10, 0x0c, 0x14, 245));
        spriteBatch.DrawString(_font, _mode == SaveSlotMode.Save ? "Save to which slot?" : "Load which slot?", new Vector2(40, 24), new Color(0xd9, 0x9a, 0x3e));

        var y = 80f;
        for (var slot = 0; slot < SaveSystem.SlotCount; slot++)
        {
            var color = slot == _selected ? new Color(0xd9, 0x9a, 0x3e) : Color.LightGray;
            var prefix = slot == _selected ? "> " : "  ";
            var label = slot == 0 ? $"Slot {slot} (autosave)" : $"Slot {slot}";

            spriteBatch.DrawString(_font, $"{prefix}{label,-18} {_slotDetails[slot]}", new Vector2(40, y), color);
            y += 26;
        }

        spriteBatch.DrawString(_font, "Enter: select   Esc: back", new Vector2(40, viewport.Height - 32), Color.Gray);
        spriteBatch.End();
    }
}
