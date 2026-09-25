using Engine.Audio;
using Engine.Core.Math;
using Engine.Core.Time;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Graphics;
using Engine.Graphics.Cameras;
using Engine.Graphics.Resources;
using Engine.Graphics.Sprites;
using Engine.Graphics.Tilemaps;
using Engine.Input;
using Engine.Physics.Components;
using Engine.Runtime;
using Engine.Simulations;
using Engine.Tooling.Debugging;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.Worlds;
using Game.Sandbox.Camera;
using Game.Sandbox.Gameplay;
using Game.Sandbox.UI;

namespace Game.Sandbox.States;

public sealed class GameplayState :
    SimulationState
{
    private readonly IInput _input;
    private readonly Engine.Graphics.Cameras.Camera _camera;
    private readonly IGraphicsDevice _graphics;
    private readonly World _world;
    private readonly DebugConsoleOverlay? _consoleOverlay;
    private readonly TilemapRenderer _tilemapRenderer;
    private readonly CameraController _cameraController;
    private readonly Engine.ECS.World _ecsWorld;
    private readonly EngineRuntime _runtime;
    private FixedVector2 _previousPlayerPosition;
    private FixedVector2 _currentPlayerPosition;
    private readonly SpriteRenderer _spriteRenderer;
    private readonly Sprite _playerSprite;
    private readonly Sprite _wallSprite;
    private bool _hasPlayerInterpolationState;
    private readonly IReadOnlyList<EntityId> _walls;
    private readonly EntityId _player;

    private readonly InteractionResolver _interactionResolver;
    private readonly InputAction _interact;

    private readonly InputAction _moveUp;
    private readonly InputAction _moveDown;
    private readonly InputAction _moveLeft;
    private readonly InputAction _moveRight;

    private readonly UiShowcaseWindow _testWindow;

    private readonly IAudioManager _audio;
    private readonly IAudioBuffer _playerMoveSound;
    private readonly IAudioSource _playerMoveSource;

    private readonly UiSystem _ui;
    private readonly ITextInput _textInput;

    private readonly Action _pauseGame;
    private readonly Action _resumeGame;
    private readonly GameplayHud _hud;
    private bool _pauseMenuOpen;

    private bool _wasMoving;

    public GameplayState(
        EngineRuntime runtime,
        Simulation simulation,
        IInput input,
        Engine.Graphics.Cameras.Camera camera,
        IGraphicsDevice graphics,
        Engine.ECS.World ecsWorld,
        World world,
        TextureAtlas tileAtlas,
        InputAction moveUp,
        InputAction moveDown,
        InputAction moveLeft,
        InputAction moveRight,
        InputAction zoomIn,
        InputAction zoomOut,
        InputAction interact,
        EntityId player,
        IReadOnlyList<EntityId> walls,
        IReadOnlyList<EntityId> interactionTargets,
        IAudioManager audio,
        IAudioBuffer playerMoveSound,
        UiSystem ui,
        ITextInput textInput,
        Action pauseGame,
        Action resumeGame,
        DebugConsoleOverlay? consoleOverlay = null)
        : base(simulation)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(ecsWorld);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(tileAtlas);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(walls);
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(textInput);
        ArgumentNullException.ThrowIfNull(pauseGame);
        ArgumentNullException.ThrowIfNull(resumeGame);

        _runtime = runtime;
        _walls = walls;
        _input = input;
        _camera = camera;
        _graphics = graphics;
        _ecsWorld = ecsWorld;
        _world = world;
        _player = player;
        _interact =
    interact;

        _interactionResolver =
            new InteractionResolver(
                _ecsWorld,
                interactionTargets);
        _ui = ui;
        _textInput = textInput;
        _pauseGame = pauseGame;
        _resumeGame = resumeGame;

        _hud =
            new GameplayHud();

        _ui.Root.AddChild(
            _hud);

        _hud.SetHealth(
            1.0f);

        _hud.TestWindowRequested +=
            OpenTestWindow;

        _testWindow =
            new UiShowcaseWindow(
                _ui,
                tileAtlas.Texture);

        if (_ecsWorld.Exists(_player) &&
            _ecsWorld.Has<Transform2D>(_player))
        {
            var position =
                _ecsWorld
                    .Get<Transform2D>(_player)
                    .Position;

            _previousPlayerPosition =
                position;

            _currentPlayerPosition =
                position;

            _hasPlayerInterpolationState =
                true;
        }

        _audio = audio;

        _playerMoveSound =
            playerMoveSound;

        _playerMoveSource =
            audio.CreateSource(
                playerMoveSound);

        _moveUp = moveUp;
        _moveDown = moveDown;
        _moveLeft = moveLeft;
        _moveRight = moveRight;

        _tilemapRenderer =
            new TilemapRenderer(
                graphics,
                camera,
                tileAtlas,
                1.0f);

        _spriteRenderer =
            new SpriteRenderer(
                graphics);

        var playerRegion =
            tileAtlas.GetRegion(
                0);

        _playerSprite =
            new Sprite(
                tileAtlas.Texture,
                playerRegion.UV,
                new Vector2(
                    1.0f,
                    1.0f),
                10);

        _wallSprite =
            new Sprite(
                tileAtlas.Texture,
                tileAtlas.GetRegion(0).UV,
                new Vector2(
                    1.0f,
                    1.0f),
                5);

        _cameraController =
            new CameraController(
                camera,
                input,
                moveUp,
                moveDown,
                moveLeft,
                moveRight,
                zoomIn,
                zoomOut,
                10.0f,
                20.0f);

        _cameraController.AllowMovement =
            false;

        _consoleOverlay =
            consoleOverlay;
    }

    private void OpenTestWindow()
    {
        _testWindow.Show(
            new Vector2(
                380.0f,
                170.0f));
    }

    public override void Update(
        TimeSnapshot time)
    {
        _input.Update();

        _consoleOverlay?.Update();

        if (_consoleOverlay?.IsOpen == true)
        {
            base.Update(time);
            return;
        }

        _ui.Update(
            time.Delta.TotalSeconds);

        var interactionTarget =
    _interactionResolver.FindTarget(
        _player);

        if (interactionTarget.HasValue)
        {
            _hud.SetInteractionPrompt(
                "F — INTERACT");

            if (_input.IsPressed(
                    _interact))
            {
                Simulation.Submit(
                    new InteractCommand(
                        _player,
                        interactionTarget.Value));
            }
        }
        else
        {
            _hud.SetInteractionPrompt(
                null);
        }

        if (_textInput.IsPressed(
                TextInputKey.Escape))
        {
            if (_ui.Screens.CurrentScreen is SettingsScreen)
            {
                _ui.Screens.Pop();
            }
            else if (_pauseMenuOpen)
            {
                ResumeGame();
            }
            else
            {
                OpenPauseMenu();
            }

            return;
        }

        var direction =
            FixedVector2.Zero;

        if (!_pauseMenuOpen &&
            !_ui.ConsumesKeyboardInput)
        {
            if (_input.IsDown(
                    _moveUp))
            {
                direction +=
                    new FixedVector2(
                        Fixed32.Zero,
                        -Fixed32.One);
            }

            if (_input.IsDown(
                    _moveDown))
            {
                direction +=
                    new FixedVector2(
                        Fixed32.Zero,
                        Fixed32.One);
            }

            if (_input.IsDown(
                    _moveLeft))
            {
                direction +=
                    new FixedVector2(
                        -Fixed32.One,
                        Fixed32.Zero);
            }

            if (_input.IsDown(
                    _moveRight))
            {
                direction +=
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.Zero);
            }

            _cameraController.Update(
                time);
        }

        var isMoving =
            direction.LengthSquared() >
            Fixed32.Zero;

        if (isMoving &&
            !_wasMoving)
        {
            _playerMoveSource.Play();
        }

        _wasMoving =
            isMoving;

        Simulation.Submit(
            new PlayerMoveCommand(
                _player,
                direction));

        base.Update(
            time);

        var playerPosition =
            TryGetPlayerPosition();

        if (playerPosition.HasValue)
        {
            _hud.SetPlayerPosition(
                playerPosition.Value);
        }
    }

    public override void FixedUpdate(
        SimulationTime time)
    {
        var previousPosition =
            TryGetPlayerPosition();

        base.FixedUpdate(
            time);

        var currentPosition =
            TryGetPlayerPosition();

        if (previousPosition.HasValue &&
            currentPosition.HasValue)
        {
            _previousPlayerPosition =
                previousPosition.Value;

            _currentPlayerPosition =
                currentPosition.Value;

            _hasPlayerInterpolationState =
                true;
        }
    }

    private FixedVector2? TryGetPlayerPosition()
    {
        if (!_ecsWorld.Exists(_player) ||
            !_ecsWorld.Has<Transform2D>(_player))
        {
            return null;
        }

        return _ecsWorld
            .Get<Transform2D>(_player)
            .Position;
    }

    public override void Render(
        double interpolationAlpha)
    {
        var position =
            GetRenderPlayerPosition(
                interpolationAlpha);

        if (position.HasValue)
        {
            var renderPosition =
                position.Value;

            _spriteRenderer.DrawWorld(
                _playerSprite,
                renderPosition -
                new Vector2(
                    _playerSprite.Size.X * 0.5f,
                    _playerSprite.Size.Y * 0.5f));

            _camera.Position =
                renderPosition;
        }

        foreach (var chunk in _world.GetChunks())
        {
            _tilemapRenderer.RenderChunk(
                chunk.Position.X,
                chunk.Position.Y,
                chunk.Tiles.Width,
                chunk.Tiles.Height,
                chunk.Tiles.AsValueReadOnlySpan());
        }

        foreach (var wall in _walls)
        {
            RenderWall(wall);
        }

        _runtime.RenderDebugVisualization();

        _ui.Render();

        _consoleOverlay?.Render();
    }

    private void OpenPauseMenu()
    {
        if (_pauseMenuOpen)
        {
            return;
        }

        _ui.Screens.Replace(
            new PauseScreen(
                _ui.Overlays,
                ResumeGame,
                OpenSettings));

        _pauseMenuOpen = true;

        _pauseGame();
    }

    private void ResumeGame()
    {
        if (!_pauseMenuOpen)
        {
            return;
        }

        _ui.Screens.Clear();

        _pauseMenuOpen = false;

        _resumeGame();
    }

    private void OpenSettings()
    {
        _ui.Screens.Push(
            new SettingsScreen(
                _ui.Overlays,
                () => _ui.Screens.Pop()));
    }

    private void RenderWall(
        EntityId wall)
    {
        if (!_ecsWorld.Exists(wall) ||
            !_ecsWorld.Has<Transform2D>(wall) ||
            !_ecsWorld.Has<Collider2D>(wall))
        {
            return;
        }

        ref var transform =
            ref _ecsWorld.Get<Transform2D>(
                wall);

        ref var collider =
            ref _ecsWorld.Get<Collider2D>(
                wall);

        var size =
            collider.Shape.Size;

        var startX =
            transform.Position.X.ToFloat() -
            size.X.ToFloat() * 0.5f;

        var startY =
            transform.Position.Y.ToFloat() -
            size.Y.ToFloat() * 0.5f;

        var width =
            size.X.FloorToInt();

        var height =
            size.Y.FloorToInt();

        for (var y = 0;
             y < height;
             y++)
        {
            for (var x = 0;
                 x < width;
                 x++)
            {
                _spriteRenderer.DrawWorld(
                    _wallSprite,
                    new Vector2(
                        startX + x,
                        startY + y));
            }
        }
    }

    private Vector2? GetRenderPlayerPosition(
        double interpolationAlpha)
    {
        if (!_hasPlayerInterpolationState)
        {
            var position =
                TryGetPlayerPosition();

            if (!position.HasValue)
            {
                return null;
            }

            return new Vector2(
                position.Value.X.ToFloat(),
                position.Value.Y.ToFloat());
        }

        var alpha =
            (float)System.Math.Clamp(
                interpolationAlpha,
                0.0,
                1.0);

        var previousX =
            _previousPlayerPosition.X.ToFloat();

        var previousY =
            _previousPlayerPosition.Y.ToFloat();

        var currentX =
            _currentPlayerPosition.X.ToFloat();

        var currentY =
            _currentPlayerPosition.Y.ToFloat();

        return new Vector2(
            previousX +
            (currentX - previousX) *
            alpha,

            previousY +
            (currentY - previousY) *
            alpha);
    }

    public override void Shutdown()
    {
        _playerMoveSource.Dispose();

        base.Shutdown();
    }
}