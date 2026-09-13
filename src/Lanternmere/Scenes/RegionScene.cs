using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Core.Content;
using Lanternmere.Core.Puzzles;
using Lanternmere.Entities;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// The core loop scene: loads a <see cref="RegionDescriptor"/> from
/// <see cref="GameContent"/> and renders its tiles/interactables/NPCs,
/// drives player movement + collision + camera follow, resolves the
/// single context-sensitive interaction against whatever is nearest, and
/// hosts region-to-region transitions. Replaces the prior placeholder
/// bounded-room GameplayScene.
/// </summary>
public sealed class RegionScene : IScene
{
    private const float InteractionRangePixels = 14f;

    private readonly InputManager _input;
    private readonly GameSettings _settings;
    private readonly GameContent _content;
    private readonly WorldState _world;
    private readonly AudioManager _audio;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    private readonly DebugOverlay _debugOverlay;

    private readonly Player _player = new();
    private readonly CollisionSystem _collision = new();
    private readonly Camera _camera = new();

    private RegionDescriptor _region = null!;
    private readonly string _spawnId;

    private Texture2D _groundTile = null!;
    private Texture2D _wallTile = null!;
    private Texture2D _playerTexture = null!;
    private readonly Dictionary<string, Texture2D> _npcTextures = new();
    private Texture2D _landmarkIcon = null!;
    private Texture2D _itemIcon = null!;
    private readonly Dictionary<string, Texture2D> _itemIconsByItemId = new();

    private string? _toastText;
    private float _toastSeconds;
    private float _footstepTimer;
    private const float FootstepIntervalSeconds = 0.33f;
    private const float FootstepMinSpeedSquared = 100f; // ~10px/s — filters out the near-zero deceleration tail

    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => true;

    public RegionScene(InputManager input, GameSettings settings, GameContent content, WorldState world, AudioManager audio,
        SpriteFont font, Texture2D pixel, string regionId, string spawnId)
    {
        _input = input;
        _settings = settings;
        _content = content;
        _world = world;
        _audio = audio;
        _font = font;
        _pixel = pixel;
        _debugOverlay = new DebugOverlay();
        RegionId = regionId;
        _spawnId = spawnId;
    }

    public string RegionId { get; }

    public void Enter(SceneManager manager)
    {
        _manager = manager;
        _region = _content.Regions[RegionId];

        var device = manager.Game.GraphicsDevice;
        var ground = TextureFactory.FromHex(_region.GroundColorHex);
        var accent = TextureFactory.FromHex(_region.AccentColorHex);
        _groundTile = TextureFactory.CreateGroundTile(device, _region.TileSize, ground, RegionId.GetHashCode());
        _wallTile = TextureFactory.CreateWallTile(device, _region.TileSize, accent);
        _playerTexture = TextureFactory.CreateFigure(device, 16, 22, new Color(0xd9, 0x9a, 0x3e), new Color(0xf2, 0xd9, 0xb3));
        _landmarkIcon = TextureFactory.CreateIcon(device, 12, new Color(0xff, 0xe8, 0xa3), IconShape.Diamond);
        _itemIcon = TextureFactory.CreateIcon(device, 10, new Color(0x9a, 0xd9, 0xe0), IconShape.Circle);

        _itemIconsByItemId.Clear();
        foreach (var itemId in _region.Interactables.Where(i => i.GrantsItemId is not null).Select(i => i.GrantsItemId!).Distinct())
        {
            _itemIconsByItemId[itemId] = TextureFactory.CreateIcon(device, 10, new Color(0x9a, 0xd9, 0xe0), TextureFactory.IconShapeForItem(itemId));
        }

        foreach (var npc in _region.Npcs)
        {
            _npcTextures[npc.Id] = TextureFactory.CreateFigure(device, 16, 22, NpcColor(npc.Id), new Color(0xe6, 0xe6, 0xe6));
        }

        BuildCollision();

        var spawn = _region.SpawnPoints.TryGetValue(_spawnId, out var sp) ? sp : _region.SpawnPoints["default"];
        _player.Position = new Vector2(spawn.X, spawn.Y);
        _player.Velocity = Vector2.Zero;

        _camera.ViewportWidth = device.Viewport.Width;
        _camera.ViewportHeight = device.Viewport.Height;
        _camera.SetRegionBounds(_region.WidthTiles * _region.TileSize, _region.HeightTiles * _region.TileSize);
        _camera.SnapTo(_player.Position);

        _world.CurrentRegionId = _region.Id;
        _world.PlayerX = _player.Position.X;
        _world.PlayerY = _player.Position.Y;
        var isFirstVisit = !_world.VisitedRegionIds.Contains(_region.Id);
        _world.MarkRegionVisited(_region.Id);

        // Puzzle 2's cross-region clue (docs/PUZZLES.md): the wind cliffs
        // beacon puzzle cannot be attempted at all until the rain garden
        // has actually been visited, not merely known about.
        if (_region.Id == "rain_garden")
        {
            _world.SetFlag(WindCliffsPuzzle.RainGardenVisitedFlag);
        }

        _audio.PlayAmbience(_region.AmbienceId);
        _audio.PlayMusic(_region.MusicId);

        if (isFirstVisit)
        {
            _world.AddDiscovery($"journal_arrival_{_region.Id}");
        }

        // Safe to autosave here: the region is fully loaded, no puzzle
        // mutation is in flight, and this is not mid-transition (the
        // transition already completed by the time Enter() runs).
        _world.LastSafeAnchorId = $"{_region.Id}:{_spawnId}";
        Autosave();
    }

    public void Exit() { }

    private void BuildCollision()
    {
        var regions = new List<Rectangle>();
        var size = _region.TileSize;
        for (var ty = 0; ty < _region.HeightTiles; ty++)
        {
            for (var tx = 0; tx < _region.WidthTiles; tx++)
            {
                if (_region.IsSolidAtTile(tx, ty))
                {
                    regions.Add(new Rectangle(tx * size, ty * size, size, size));
                }
            }
        }
        _collision.SetSolidRegions(regions);
    }

    private static Color NpcColor(string npcId) => npcId switch
    {
        "the_cartographer" => new Color(0x6b, 0x8e, 0x9e),
        "the_gardener" => new Color(0x7c, 0xa6, 0x5a),
        "the_watcher" => new Color(0x9e, 0x8e, 0x6b),
        "the_ferryperson" => new Color(0x5a, 0x7c, 0xa6),
        "the_archivist" => new Color(0x8e, 0x6b, 0x9e),
        _ => new Color(0xa0, 0xa0, 0xa0),
    };

    public void Update(GameTime gameTime)
    {
        _input.Update();
        _world.PlaytimeSeconds += gameTime.ElapsedGameTime.TotalSeconds;

        if (_toastSeconds > 0f) _toastSeconds -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        else _toastText = null;

        if (_input.WasPressedThisFrame(GameAction.Pause))
        {
            _manager.Push(new PauseScene(_input, _settings, _audio, _pixel, _font, _world));
            return;
        }
        if (_input.WasPressedThisFrame(GameAction.OpenJournal))
        {
            _manager.Push(new JournalScene(_input, _pixel, _font, _content, _world, _audio));
            return;
        }
        if (_input.WasPressedThisFrame(GameAction.OpenMap))
        {
            _manager.Push(new MapScene(_input, _pixel, _font, _content, _world, _audio));
            return;
        }
        if (_input.WasPressedThisFrame(GameAction.OpenInventory))
        {
            _manager.Push(new InventoryScene(_input, _pixel, _font, _content, _world, _audio));
            return;
        }

        _player.Update(gameTime, _input, _collision);
        _world.PlayerX = _player.Position.X;
        _world.PlayerY = _player.Position.Y;
        _camera.Follow(_player.Position, gameTime);
        UpdateFootsteps(gameTime);

        var nearestInteractable = FindNearestInteractable();
        var nearestNpc = FindNearestNpc();
        var nearestTransition = FindOverlappingTransition();

        if (_input.WasPressedThisFrame(GameAction.Interact))
        {
            if (nearestNpc is not null) InteractWithNpc(nearestNpc);
            else if (nearestInteractable is not null) InteractWith(nearestInteractable);
        }

        if (nearestTransition is not null)
        {
            TryTransition(nearestTransition);
        }

#if DEBUG
        if (Microsoft.Xna.Framework.Input.Keyboard.GetState().GetPressedKeyCount() > 0 &&
            Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1))
        {
            _debugOverlay.Toggle();
        }
#endif
    }

    private InteractableDef? FindNearestInteractable()
    {
        InteractableDef? best = null;
        var bestDist = float.MaxValue;
        foreach (var i in _region.Interactables)
        {
            if (!IsVisible(i)) continue;
            var center = new Vector2(i.X + i.Width / 2f, i.Y + i.Height / 2f);
            var dist = Vector2.Distance(_player.Position, center);
            if (dist < bestDist && dist <= i.Width / 2f + InteractionRangePixels)
            {
                bestDist = dist;
                best = i;
            }
        }
        return best;
    }

    private NpcPlacementDef? FindNearestNpc()
    {
        NpcPlacementDef? best = null;
        var bestDist = float.MaxValue;
        foreach (var npc in _region.Npcs)
        {
            var center = new Vector2(npc.X + 8, npc.Y + 11);
            var dist = Vector2.Distance(_player.Position, center);
            if (dist < bestDist && dist <= 8 + InteractionRangePixels)
            {
                bestDist = dist;
                best = npc;
            }
        }
        return best;
    }

    private TransitionDef? FindOverlappingTransition()
    {
        foreach (var t in _region.Transitions)
        {
            var rect = new Rectangle((int)t.X, (int)t.Y, t.Width, t.Height);
            if (_player.Bounds.Intersects(rect)) return t;
        }
        return null;
    }

    private bool IsVisible(InteractableDef def)
    {
        if (def.RequiresFlag is not null && !_world.GetFlag(def.RequiresFlag)) return false;
        if (def.HiddenAfterFlag is not null && _world.GetFlag(def.HiddenAfterFlag)) return false;
        // A one-shot pickup disappears once collected, rather than staying
        // in the world to be re-examined indefinitely. Puzzle pieces
        // (plots, beacons, altar slots) don't set GrantsItemId, so they're
        // unaffected and stay interactable for repeated puzzle attempts.
        if (def.GrantsItemId is not null && _world.HasItem(def.GrantsItemId)) return false;
        return true;
    }

    private void TryTransition(TransitionDef transition)
    {
        if (transition.RequiresFlag is not null && !_world.GetFlag(transition.RequiresFlag))
        {
            ShowToast(transition.LockedMessage ?? "That way is not open yet.");
            return;
        }

        _manager.Replace(new RegionScene(_input, _settings, _content, _world, _audio, _font, _pixel,
            transition.DestinationRegionId, transition.DestinationSpawnId));
    }

    private void InteractWithNpc(NpcPlacementDef npc)
    {
        if (!_content.DialogueTrees.TryGetValue(npc.DialogueId, out var tree))
        {
            ShowToast($"{npc.DisplayName} has nothing to say right now.");
            return;
        }

        _audio.PlaySfx("sfx_interact");
        _manager.Push(new DialogueScene(_input, _pixel, _font, _settings, _audio, tree, _world));
    }

    private void InteractWith(InteractableDef def)
    {
        if (def.PuzzleId is not null)
        {
            InteractWithPuzzlePiece(def);
            return;
        }

        // Lantern relight is the finale gate: special-cased since it checks
        // all three region fragments rather than a single flag.
        if (def.Id == "village_hub_lantern")
        {
            TryRelightLantern(def);
            return;
        }

        var firstTime = def.DiscoveryId is not null && !_world.HasDiscovery(def.DiscoveryId);
        var newItem = def.GrantsItemId is not null && !_world.HasItem(def.GrantsItemId);

        if (def.SetsFlagOnInteract is not null) _world.SetFlag(def.SetsFlagOnInteract);
        if (def.DiscoveryId is not null) _world.AddDiscovery(def.DiscoveryId);
        if (def.GrantsItemId is not null) _world.AddItem(def.GrantsItemId);
        if (def.IsLandmark) _world.RevealLandmark(def.Id);

        _audio.PlaySfx(newItem ? "sfx_discovery" : "sfx_interact");

        var text = def.ExamineText ?? "There's nothing more to see here.";
        if (newItem && _content.Items.TryGet(def.GrantsItemId!, out var item))
        {
            text += $"\n\n(Picked up: {item.DisplayName})";
        }
        else if (firstTime)
        {
            text += "\n\n(New journal entry.)";
        }

        if (def.DialogueId is not null && _content.DialogueTrees.TryGetValue(def.DialogueId, out var tree))
        {
            _manager.Push(new DialogueScene(_input, _pixel, _font, _settings, _audio, tree, _world));
        }
        else
        {
            _manager.Push(new DialogueScene(_input, _pixel, _font, _settings, _audio, DialogueScene.BuildExamineTree(def.Id, text), _world));
        }
    }

    private void InteractWithPuzzlePiece(InteractableDef def)
    {
        switch (def.PuzzleId)
        {
            case RainGardenPuzzle.PuzzleId:
            {
                var puzzle = new RainGardenPuzzle(_world);
                if (def.PuzzlePieceIndex < 0) { puzzle.ManualReset(); ShowToast("The plots are dry again. Start over."); return; }
                var feedback = puzzle.WaterPlot(def.PuzzlePieceIndex);
                HandlePuzzleFeedback(feedback, "rain_garden_watering_hint");
                if (puzzle.HintAvailable) _world.AddDiscovery("journal_rain_garden_watering_hint");
                if (feedback == PuzzleFeedback.Solved)
                {
                    PuzzleProgression.OnRainGardenSolved(_world);
                    Autosave();
                }
                break;
            }
            case WindCliffsPuzzle.PuzzleId:
            {
                var puzzle = new WindCliffsPuzzle(_world);
                if (def.PuzzlePieceIndex < 0) { puzzle.ManualReset(); ShowToast("The beacons are unlit again. Start over."); return; }
                var feedback = puzzle.LightBeacon(def.PuzzlePieceIndex);
                HandlePuzzleFeedback(feedback, null);
                if (feedback == PuzzleFeedback.Solved)
                {
                    PuzzleProgression.OnWindCliffsSolved(_world);
                    Autosave();
                }
                break;
            }
            case AmberShorePuzzle.PuzzleId:
            {
                var puzzle = new AmberShorePuzzle(_world);
                var slot = def.PuzzlePieceIndex;
                var correctItem = slot >= 0 && slot < AmberShorePuzzle.CorrectSlotItems.Length ? AmberShorePuzzle.CorrectSlotItems[slot] : null;

                if (puzzle.IsLocked)
                {
                    ShowToast("The altar doesn't respond. You need to understand more of this valley first.");
                    return;
                }

                // Simplified single-button placement: try whichever carried
                // shard the player is holding against this slot. A full
                // drag/select item picker is a natural next polish pass —
                // see ROADMAP.md.
                var carriedShard = _world.InventoryItemIds.FirstOrDefault(id => id.StartsWith("tide_glass_shard_") && puzzle.ItemInSlot(slot) != id);
                if (carriedShard is null)
                {
                    ShowToast("You have no tide-glass shard to try here.");
                    return;
                }

                var feedback = puzzle.PlaceGlass(slot, carriedShard);
                HandlePuzzleFeedback(feedback, null);
                if (feedback == PuzzleFeedback.Solved)
                {
                    PuzzleProgression.OnAmberShoreSolved(_world);
                    Autosave();
                }
                break;
            }
        }
    }

    private void HandlePuzzleFeedback(PuzzleFeedback feedback, string? hintDiscoveryId)
    {
        switch (feedback)
        {
            case PuzzleFeedback.Solved:
                _audio.PlaySfx("sfx_puzzle_solved");
                ShowToast("Solved!");
                break;
            case PuzzleFeedback.CorrectStep:
                _audio.PlaySfx("sfx_puzzle_correct");
                break;
            case PuzzleFeedback.ResetWrongOrder:
                _audio.PlaySfx("sfx_puzzle_wrong");
                ShowToast("That's not right — it resets. Watch the order more closely.");
                _camera.Shake(2.5f, 0.2f, _settings.ReducedShake);
                break;
            case PuzzleFeedback.NoOpWrongChoice:
                _audio.PlaySfx("sfx_puzzle_wrong");
                ShowToast("Nothing happens.");
                break;
            case PuzzleFeedback.IncorrectPattern:
                _audio.PlaySfx("sfx_puzzle_wrong");
                ShowToast("It doesn't fit there.");
                _camera.Shake(2.5f, 0.2f, _settings.ReducedShake);
                break;
            case PuzzleFeedback.Locked:
                ShowToast("Not yet.");
                break;
        }
    }

    private void TryRelightLantern(InteractableDef def)
    {
        if (!PuzzleProgression.CanRelightLantern(_world))
        {
            ShowToast("The lantern stays dark. There's more of the valley to learn first.");
            return;
        }

        if (_world.ReachedMainEnding)
        {
            ShowToast("The lantern burns steady now.");
            return;
        }

        _audio.PlaySfx("sfx_lantern_relight");
        PuzzleProgression.OnLanternRelit(_world);
        Autosave();
        _manager.Replace(new EndingScene(_input, _settings, _pixel, _font, _audio, _world));
    }

    /// <summary>A simple timed footstep cadence while the player is actually moving — the "positional environmental effects" the brief asks for, scoped down to "plays at all" rather than true stereo panning, which nothing in this game currently needs.</summary>
    private void UpdateFootsteps(GameTime gameTime)
    {
        if (_player.Velocity.LengthSquared() < FootstepMinSpeedSquared)
        {
            _footstepTimer = 0f;
            return;
        }

        _footstepTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_footstepTimer > 0f) return;

        _footstepTimer = FootstepIntervalSeconds;
        _audio.PlaySfx("sfx_footstep");
    }

    private void ShowToast(string text)
    {
        _toastText = text;
        _toastSeconds = 2.5f;
    }

    private void Autosave() => SaveSystem.Save(0, _world);

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        var ground = TextureFactory.FromHex(_region.GroundColorHex);

        spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);
        DrawTiles(spriteBatch);
        DrawInteractables(spriteBatch);
        DrawNpcs(spriteBatch);
        spriteBatch.Draw(_playerTexture, new Vector2(_player.Bounds.X, _player.Bounds.Y), Color.White);
        spriteBatch.End();

        spriteBatch.Begin();
        DrawPromptAndToast(spriteBatch, viewport);
        DrawRegionLabel(spriteBatch);
        _debugOverlay.Draw(spriteBatch, _font, gameTime, _region.DisplayName, _player.Position);
        spriteBatch.End();
    }

    private void DrawTiles(SpriteBatch spriteBatch)
    {
        var size = _region.TileSize;
        for (var ty = 0; ty < _region.HeightTiles; ty++)
        {
            for (var tx = 0; tx < _region.WidthTiles; tx++)
            {
                var texture = _region.IsSolidAtTile(tx, ty) ? _wallTile : _groundTile;
                spriteBatch.Draw(texture, new Vector2(tx * size, ty * size), Color.White);
            }
        }
    }

    private void DrawInteractables(SpriteBatch spriteBatch)
    {
        foreach (var i in _region.Interactables)
        {
            if (!IsVisible(i)) continue;
            var icon = i.GrantsItemId is not null
                ? _itemIconsByItemId.GetValueOrDefault(i.GrantsItemId, _itemIcon)
                : _landmarkIcon;
            var center = new Vector2(i.X + i.Width / 2f - icon.Width / 2f, i.Y + i.Height / 2f - icon.Height / 2f);
            spriteBatch.Draw(icon, center, Color.White);
        }
    }

    private void DrawNpcs(SpriteBatch spriteBatch)
    {
        foreach (var npc in _region.Npcs)
        {
            if (_npcTextures.TryGetValue(npc.Id, out var tex))
            {
                spriteBatch.Draw(tex, new Vector2(npc.X, npc.Y), Color.White);
            }
        }
    }

    private void DrawPromptAndToast(SpriteBatch spriteBatch, Viewport viewport)
    {
        string? text = null;
        if (_toastText is not null) text = _toastText;
        else
        {
            var npc = FindNearestNpc();
            var interactable = FindNearestInteractable();
            if (npc is not null) text = $"[E] Talk to {npc.DisplayName}";
            else if (interactable is not null) text = $"[E] {interactable.Prompt}";
        }

        if (text is null) return;
        var size = _font.MeasureString(text);
        var pos = new Vector2((viewport.Width - size.X) / 2f, viewport.Height - 56);
        var boxRect = new Rectangle((int)pos.X - 8, (int)pos.Y - 4, (int)size.X + 16, (int)size.Y + 8);

        if (_settings.HighContrastInteractionIndicators)
        {
            spriteBatch.Draw(_pixel, boxRect, Color.Black);
            var border = new Color(0xff, 0xe8, 0x3e);
            spriteBatch.Draw(_pixel, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, 2), border);
            spriteBatch.Draw(_pixel, new Rectangle(boxRect.X, boxRect.Bottom - 2, boxRect.Width, 2), border);
            spriteBatch.Draw(_pixel, new Rectangle(boxRect.X, boxRect.Y, 2, boxRect.Height), border);
            spriteBatch.Draw(_pixel, new Rectangle(boxRect.Right - 2, boxRect.Y, 2, boxRect.Height), border);
            spriteBatch.DrawString(_font, text, pos, Color.White);
        }
        else
        {
            spriteBatch.Draw(_pixel, boxRect, new Color(0, 0, 0, 160));
            spriteBatch.DrawString(_font, text, pos, Color.White);
        }
    }

    private void DrawRegionLabel(SpriteBatch spriteBatch)
    {
        spriteBatch.DrawString(_font, _region.DisplayName, new Vector2(16, 12), Color.White * 0.85f);
    }
}
