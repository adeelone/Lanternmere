using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lanternmere.Core;
using Lanternmere.Systems;
using Lanternmere.Scenes;

namespace Lanternmere;

public class Game1 : Game
{
    /// <summary>
    /// A small pragmatic singleton: several leaf scenes (Title, Credits)
    /// need read-only access to shared systems (content, fonts, audio)
    /// that would otherwise have to be threaded through every scene
    /// constructor in the stack. Set once in the constructor; never null
    /// after that.
    /// </summary>
    public static Game1? Instance { get; private set; }

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    private GameSettings _settings = null!;
    private InputManager _input = null!;
    private SceneManager _sceneManager = null!;
    private AudioLibrary _audioLibrary = null!;
    private AudioManager _audioManager = null!;

    public GameContent Content2 { get; private set; } = null!;
    public SpriteFont Font { get; private set; } = null!;
    public SpriteFont LargeFont { get; private set; } = null!;

    private bool _wasGamepadConnected;

#if DEBUG
    private readonly string[] _args;
    private DevLaunchOptions? _devOptions;
    private readonly System.Collections.Generic.List<double> _perfFrameMs = new();
    private double _perfElapsedSeconds;
#endif

    public Game1(string[]? args = null)
    {
        Instance = this;
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Fixed timestep drives deterministic puzzle/physics state (see
        // brief: "Use a fixed-timestep update and deterministic state
        // changes where practical"). This is the MonoGame default, made
        // explicit here so it isn't accidentally disabled later.
        IsFixedTimeStep = true;
        TargetElapsedTime = System.TimeSpan.FromSeconds(1.0 / 60.0);

#if DEBUG
        _args = args ?? System.Array.Empty<string>();
#endif
    }

    protected override void Initialize()
    {
        _settings = GameSettings.LoadOrDefault();

        _graphics.PreferredBackBufferWidth = _settings.ResolutionWidth;
        _graphics.PreferredBackBufferHeight = _settings.ResolutionHeight;
        _graphics.IsFullScreen = _settings.Fullscreen;
        _graphics.ApplyChanges();

        _input = new InputManager(_settings);
        _sceneManager = new SceneManager(this);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = TextureFactory.CreateSolid(GraphicsDevice, Color.White);

        Font = Content.Load<SpriteFont>("Fonts/UIFont");
        LargeFont = Content.Load<SpriteFont>("Fonts/UIFontLarge");

        _audioLibrary = new AudioLibrary();
        _audioManager = new AudioManager(_audioLibrary, _settings);

        var dataRoot = Path.Combine(AppContext.BaseDirectory, "Content", "Data");
        var content = GameContent.TryLoad(dataRoot, out var error);

        if (content is null)
        {
            _sceneManager.Push(new MissingContentScene(_input, _pixel, Font, new[] { error ?? "Unknown content load error." }));
            return;
        }
        if (content.HasFatalIssues)
        {
            var issues = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(content.ValidationIssues, i => i.ToString()));
            _sceneManager.Push(new MissingContentScene(_input, _pixel, Font, issues));
            return;
        }

        Content2 = content;

#if DEBUG
        _devOptions = DevLaunchOptions.Parse(_args);
        if (_devOptions.HasAnything && TryEnterDevLaunch(_devOptions))
        {
            return;
        }
#endif

        _sceneManager.Push(new TitleScene(_input, _settings, _pixel, Font, _audioManager));
    }

#if DEBUG
    /// <summary>
    /// Jumps straight into a region or UI overlay scene per DevLaunchOptions,
    /// instead of navigating there through Title -> New Game -> walking ->
    /// interacting. Exists specifically so a screenshot/perf tool (or a
    /// developer) can visually verify any given scene without needing to
    /// script real keyboard/gamepad input into the game window — see
    /// DevLaunchOptions.cs. Compiled out of Release entirely.
    /// </summary>
    private bool TryEnterDevLaunch(DevLaunchOptions options)
    {
        var regionId = options.RegionId ?? "village_hub";
        if (!Content2.Regions.ContainsKey(regionId))
        {
            _sceneManager.Push(new MissingContentScene(_input, _pixel, Font, new[] { $"--region '{regionId}' does not exist." }));
            return true;
        }

        var world = DevLaunchOptions.BuildPreviewWorldState();
        var region = new RegionScene(_input, _settings, Content2, world, _audioManager, Font, _pixel, regionId, options.SpawnId);
        _sceneManager.Push(region);

        switch (options.SceneName)
        {
            case null:
                break; // just the region itself
            case "journal":
                _sceneManager.Push(new JournalScene(_input, _pixel, Font, Content2, world, _audioManager));
                break;
            case "map":
                _sceneManager.Push(new MapScene(_input, _pixel, Font, Content2, world, _audioManager));
                break;
            case "inventory":
                _sceneManager.Push(new InventoryScene(_input, _pixel, Font, Content2, world, _audioManager));
                break;
            case "pause":
                _sceneManager.Push(new PauseScene(_input, _settings, _audioManager, _pixel, Font, world));
                break;
            case "settings":
                _sceneManager.Push(new SettingsScene(_input, _settings, _audioManager, _pixel, Font));
                break;
            case "dialogue":
                if (Content2.DialogueTrees.TryGetValue("cartographer_intro", out var tree))
                {
                    _sceneManager.Push(new DialogueScene(_input, _pixel, Font, _settings, _audioManager, tree, world));
                }
                break;
            case "credits":
                _sceneManager.Push(new CreditsScene(_input, _settings, _pixel, Font, _audioManager, world));
                break;
            case "creditsroll":
                _sceneManager.Push(new CreditsRollScene(_input, _pixel, Font));
                break;
            case "ending":
                world.SetFlag("fragment_amber_shore_recovered");
                _sceneManager.Push(new EndingScene(_input, _settings, _pixel, Font, _audioManager, world));
                break;
            default:
                _sceneManager.Push(new MissingContentScene(_input, _pixel, Font, new[] { $"--scene '{options.SceneName}' is not a recognized dev scene name." }));
                break;
        }

        return true;
    }
#endif

    /// <summary>Used by CreditsScene's "Continue exploring" post-game option, which doesn't otherwise have a clean path back to a fully-wired RegionScene.</summary>
    public IScene BuildRegionScene(string regionId, string spawnId, WorldState world) =>
        new RegionScene(_input, _settings, Content2, world, _audioManager, Font, _pixel, regionId, spawnId);

    protected override void Update(GameTime gameTime)
    {
        CheckGamepadConnection();
        _audioManager?.Update(gameTime);
        _sceneManager.Update(gameTime);
#if DEBUG
        UpdatePerfLog(gameTime);
#endif
        base.Update(gameTime);
    }

#if DEBUG
    /// <summary>
    /// Per the brief's "Performance check in the densest scene at the
    /// target resolution/frame rate": logs min/avg/max FPS to the console
    /// over --perfseconds (default 5) real seconds, then exits — meant to
    /// be combined with --region to target the densest scene, e.g.
    /// `Lanternmere.exe --region wind_cliffs --perflog`.
    /// </summary>
    private void UpdatePerfLog(GameTime gameTime)
    {
        if (_devOptions is not { PerfLog: true }) return;

        var dt = gameTime.ElapsedGameTime.TotalMilliseconds;
        if (dt > 0) _perfFrameMs.Add(dt);
        _perfElapsedSeconds += gameTime.ElapsedGameTime.TotalSeconds;

        if (_perfElapsedSeconds < _devOptions.PerfLogSeconds || _perfFrameMs.Count == 0) return;

        var fpsValues = _perfFrameMs.ConvertAll(ms => 1000.0 / ms);
        fpsValues.Sort();
        var min = fpsValues[0];
        var max = fpsValues[^1];
        var avg = System.Linq.Enumerable.Average(fpsValues);
        var region = _devOptions.RegionId ?? "village_hub";
        System.Console.WriteLine($"PERFLOG region={region} frames={_perfFrameMs.Count} minFps={min:0.0} avgFps={avg:0.0} maxFps={max:0.0} resolution={_settings.ResolutionWidth}x{_settings.ResolutionHeight}");
        Exit();
    }
#endif

    private void CheckGamepadConnection()
    {
        var connected = GamePad.GetState(PlayerIndex.One).IsConnected;
        if (_wasGamepadConnected && !connected && Content2 is not null && _sceneManager.Current is RegionScene)
        {
            _sceneManager.Push(new ControllerDisconnectedScene(_input, _pixel, Font));
        }
        _wasGamepadConnected = connected;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _sceneManager.Draw(gameTime, _spriteBatch);
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _input.SaveBindingsInto(_settings);
        _settings.Save();
        base.UnloadContent();
    }
}
