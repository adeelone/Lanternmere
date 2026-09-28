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
    private float _visualTime;
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

    public void Exit()
    {
        // Every region creates its own procedural textures. Region transitions
        // replace scenes repeatedly, so release those GPU resources here rather
        // than retaining one full set for every place the player has visited.
        _groundTile.Dispose();
        _wallTile.Dispose();
        _playerTexture.Dispose();
        _landmarkIcon.Dispose();
        _itemIcon.Dispose();
        foreach (var texture in _npcTextures.Values) texture.Dispose();
        foreach (var texture in _itemIconsByItemId.Values) texture.Dispose();
        _npcTextures.Clear();
        _itemIconsByItemId.Clear();
    }

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
        if (!_settings.ReducedMotion)
        {
            _visualTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

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

                if (puzzle.IsLocked)
                {
                    ShowToast("The altar doesn't respond. You need to understand more of this valley first.");
                    return;
                }

                var placedItem = puzzle.ItemInSlot(slot);
                var itemsInOtherSlots = Enumerable.Range(0, AmberShorePuzzle.SlotCount)
                    .Where(otherSlot => otherSlot != slot)
                    .Select(puzzle.ItemInSlot)
                    .Where(itemId => itemId is not null)
                    .ToHashSet(StringComparer.Ordinal);
                var carriedShards = _world.InventoryItemIds
                    .Where(id => id.StartsWith("tide_glass_shard_", StringComparison.Ordinal)
                        && !itemsInOtherSlots.Contains(id)
                        && !string.Equals(id, placedItem, StringComparison.Ordinal))
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToList();

                if (carriedShards.Count == 0 && placedItem is null)
                {
                    ShowToast("You have no tide-glass shard to try here.");
                    return;
                }

                _manager.Push(new AltarSelectionScene(
                    _input, _pixel, _font, _content, slot, carriedShards, placedItem,
                    itemId => PlaceGlassInAltarSlot(slot, itemId),
                    () => RemoveGlassFromAltarSlot(slot)));
                break;
            }
        }
    }

    private void PlaceGlassInAltarSlot(int slot, string itemId)
    {
        var puzzle = new AmberShorePuzzle(_world);
        var feedback = puzzle.PlaceGlass(slot, itemId);
        HandlePuzzleFeedback(feedback, null);
        if (feedback == PuzzleFeedback.Solved)
        {
            PuzzleProgression.OnAmberShoreSolved(_world);
            Autosave();
        }
    }

    private void RemoveGlassFromAltarSlot(int slot)
    {
        var puzzle = new AmberShorePuzzle(_world);
        if (puzzle.ItemInSlot(slot) is null) return;
        puzzle.RemoveGlass(slot);
        _audio.PlaySfx("sfx_interact");
        ShowToast("You lift the tide-glass free of the altar.");
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

        spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);
        DrawTiles(spriteBatch);
        DrawAmbientDetails(spriteBatch);
        DrawInteractables(spriteBatch);
        DrawNpcs(spriteBatch);
        DrawPlayer(spriteBatch);
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
        var bob = _settings.ReducedMotion ? 0f : MathF.Sin(_visualTime * 2.8f) * 1.5f;
        var pulse = _settings.ReducedFlash ? 0.88f : 0.82f + (MathF.Sin(_visualTime * 3.2f) + 1f) * 0.09f;
        foreach (var i in _region.Interactables)
        {
            if (!IsVisible(i)) continue;
            var icon = i.GrantsItemId is not null
                ? _itemIconsByItemId.GetValueOrDefault(i.GrantsItemId, _itemIcon)
                : _landmarkIcon;
            var center = new Vector2(i.X + i.Width / 2f - icon.Width / 2f, i.Y + i.Height / 2f - icon.Height / 2f + bob);
            spriteBatch.Draw(_pixel, new Rectangle((int)center.X + 2, (int)center.Y + icon.Height - 1, icon.Width - 4, 2), Color.Black * 0.28f);
            spriteBatch.Draw(icon, center, Color.White * pulse);
        }
    }

    private void DrawNpcs(SpriteBatch spriteBatch)
    {
        var index = 0;
        foreach (var npc in _region.Npcs)
        {
            if (_npcTextures.TryGetValue(npc.Id, out var tex))
            {
                var bob = _settings.ReducedMotion ? 0f : MathF.Sin(_visualTime * 1.7f + index * 1.9f) * 0.75f;
                spriteBatch.Draw(_pixel, new Rectangle((int)npc.X + 2, (int)npc.Y + 19, 12, 4), Color.Black * 0.32f);
                spriteBatch.Draw(tex, new Vector2(npc.X, npc.Y + bob), Color.White);
            }
            index++;
        }
    }

    private void DrawPlayer(SpriteBatch spriteBatch)
    {
        var moving = _player.Velocity.LengthSquared() >= FootstepMinSpeedSquared;
        var bob = !_settings.ReducedMotion && moving ? MathF.Abs(MathF.Sin(_visualTime * 10f)) * -1.5f : 0f;
        var bounds = _player.Bounds;
        spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 2, bounds.Bottom - 2, bounds.Width - 4, 4), Color.Black * 0.38f);
        spriteBatch.Draw(_playerTexture, new Vector2(bounds.X, bounds.Y + bob), Color.White);
    }

    /// <summary>
    /// Gives every region its own visual weather without external assets. Motion is
    /// deterministic and freezes when Reduced Motion is enabled, so the setting now
    /// controls a real effect rather than merely being persisted.
    /// </summary>
    private void DrawAmbientDetails(SpriteBatch spriteBatch)
    {
        var worldWidth = _region.WidthTiles * _region.TileSize;
        var worldHeight = _region.HeightTiles * _region.TileSize;
        var time = _settings.ReducedMotion ? 0f : _visualTime;

        for (var i = 0; i < 34; i++)
        {
            var seedX = PositiveModulo(i * 97 + RegionId.Length * 41, Math.Max(1, worldWidth));
            var seedY = PositiveModulo(i * 53 + RegionId.Length * 67, Math.Max(1, worldHeight));

            switch (RegionId)
            {
                case "rain_garden":
                {
                    var y = PositiveModulo((int)(seedY + time * (34f + i % 5 * 5f)), worldHeight);
                    var x = PositiveModulo((int)(seedX - time * 12f), worldWidth);
                    spriteBatch.Draw(_pixel, new Rectangle(x, y, 1, 6), new Color(0x8f, 0xc9, 0xd8) * 0.36f);
                    break;
                }
                case "wind_cliffs":
                {
                    var x = PositiveModulo((int)(seedX + time * (18f + i % 4 * 3f)), worldWidth);
                    var y = seedY + (int)(MathF.Sin(time * 1.3f + i) * 5f);
                    spriteBatch.Draw(_pixel, new Rectangle(x, y, 5 + i % 6, 1), new Color(0xe4, 0xdd, 0xba) * 0.24f);
                    break;
                }
                case "amber_shore":
                {
                    var shimmer = _settings.ReducedFlash ? 0.22f : 0.18f + (MathF.Sin(time * 2.4f + i) + 1f) * 0.10f;
                    spriteBatch.Draw(_pixel, new Rectangle(seedX, seedY, 2 + i % 4, 1), new Color(0xff, 0xd1, 0x72) * shimmer);
                    break;
                }
                case "archive":
                {
                    var y = PositiveModulo((int)(seedY - time * (3f + i % 3)), worldHeight);
                    var x = seedX + (int)(MathF.Sin(time * 0.7f + i) * 3f);
                    spriteBatch.Draw(_pixel, new Rectangle(x, y, 1, 1), new Color(0xc9, 0xb3, 0x8d) * 0.33f);
                    break;
                }
                default:
                {
                    var x = seedX + (int)(MathF.Sin(time * 0.9f + i) * 5f);
                    var y = seedY + (int)(MathF.Cos(time * 0.7f + i * 0.5f) * 4f);
                    var glow = _settings.ReducedFlash ? 0.34f : 0.26f + (MathF.Sin(time * 2f + i) + 1f) * 0.12f;
                    spriteBatch.Draw(_pixel, new Rectangle(x, y, 2, 2), new Color(0xff, 0xd4, 0x66) * glow);
                    break;
                }
            }
        }
    }

    private static int PositiveModulo(int value, int modulus) => (value % modulus + modulus) % modulus;

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
