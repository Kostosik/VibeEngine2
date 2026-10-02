# VibeEngine2 API Guide

Практическое руководство по основным API VibeEngine2.

Архитектурное описание: [ENGINE.md](ENGINE.md).

Главный принцип:

> Engine предоставляет механизмы, Game определяет смысл.

---

# 1. Базовая runtime-модель

Типичная composition выглядит так:

~~~text
Application
    ↓
EngineRuntime
    ├── ECS World
    ├── Simulation
    ├── Physics
    └── Engine services
         ├── Graphics
         ├── Input
         ├── Camera
         ├── Audio
         └── Content
~~~

Runtime получает application services через EngineRuntimeServices:

~~~csharp
var services =
    new EngineRuntimeServices(
        graphics,
        input,
        camera,
        audio,
        content);

var runtime =
    new EngineRuntime(
        new EngineRuntimeOptions(),
        services);
~~~

Lifecycle:

~~~csharp
runtime.Initialize();

runtime.Update(context);
runtime.FixedUpdate(fixedContext);

// ...

runtime.Shutdown();
runtime.Dispose();
~~~

---

# 2. ECS

Основной тип:

~~~csharp
Engine.ECS.World
~~~

Создание:

~~~csharp
var world =
    new Engine.ECS.World();
~~~

## Entity

~~~csharp
var entity =
    world.CreateEntity();

if (world.Exists(entity))
{
    // entity существует
}

world.DestroyEntity(entity);
~~~

## Components

Component — обычный struct.

~~~csharp
public struct Health
{
    public int Value;
}
~~~

Добавление:

~~~csharp
world.Add(
    entity,
    new Health
    {
        Value = 100
    });
~~~

Получение:

~~~csharp
if (world.Has<Health>(entity))
{
    ref var health =
        ref world.Get<Health>(entity);

    health.Value -= 10;
}
~~~

Удаление:

~~~csharp
world.Remove<Health>(entity);
~~~

## Queries

Для массовой обработки:

~~~csharp
foreach (var item in world.QueryReadOnly<Health>())
{
    var entity =
        item.Entity;

    ref readonly var health =
        ref item.Component;

    // обработка
}
~~~

Для массовой read-only обработки также доступны parallel APIs через Job System.

## Commands

Для structural changes используется command buffer:

~~~csharp
var commands =
    world.Commands;
~~~

На runtime-уровне доступен также runtime.Commands.

---

# 3. Simulation и Systems

Simulation:

~~~csharp
var simulation =
    runtime.Simulation;
~~~

Команда:

~~~csharp
runtime.Simulation.Submit(
    command);
~~~

Fixed update:

~~~csharp
runtime.FixedUpdate(
    fixedContext);
~~~

Типичный fixed system:

~~~csharp
public sealed class MySystem :
    IFixedUpdateSystem
{
    public void FixedUpdate(
        FixedSystemContext context)
    {
        // simulation logic
    }
}
~~~

Регистрация:

~~~csharp
runtime.Scheduler.Add(
    mySystem,
    SystemPhase.FixedUpdate);
~~~

Зависимость:

~~~csharp
runtime.Scheduler.Add(
    systemB,
    SystemPhase.FixedUpdate)
    .After<SystemA>();
~~~

Deterministic state:

~~~csharp
runtime.Simulation.RegisterDeterministicState(
    world);

var hash =
    runtime.GetStateHash();
~~~

---

# 4. Transform

Transform вынесен в отдельный модуль Engine.Transform.

Для 2D используется Transform2D.

~~~csharp
var transform =
    new Transform2D(
        new Vector2(
            100.0f,
            50.0f));
~~~

Основные свойства:

~~~text
Position
Rotation
Scale
~~~

В ECS:

~~~csharp
ref var transform =
    ref world.Get<Transform2D>(
        entity);

transform.Position =
    new Vector2(
        200.0f,
        100.0f);

transform.Rotation =
    0.5f;

transform.Scale =
    new Vector2(
        2.0f,
        2.0f);
~~~

Transform не ограничен rendering. Он может использоваться physics, audio, gameplay и editor.

---

# 5. Camera2D

Camera находится в Engine.Camera.

Создание:

~~~csharp
var camera =
    new Camera2D(
        new Vector2(
            1280.0f,
            720.0f));
~~~

Положение и zoom:

~~~csharp
camera.Position =
    new Vector2(
        100.0f,
        50.0f);

camera.Zoom =
    2.0f;
~~~

World → screen:

~~~csharp
var screen =
    camera.WorldToScreen(
        worldPosition);
~~~

Screen → world:

~~~csharp
var world =
    camera.ScreenToWorld(
        screenPosition);
~~~

Culling:

~~~csharp
if (camera.IsVisible(bounds))
{
    // объект может быть отрисован
}
~~~

При resize:

~~~csharp
camera.SetViewportSize(
    viewportSize);
~~~

---

# 6. Graphics

Главная abstraction:

~~~csharp
IGraphicsDevice graphics
~~~

Она предоставляет:

~~~text
Textures
Fonts
Buffers
Shaders
RenderTargets
Pipeline
~~~

Frame lifecycle:

~~~csharp
graphics.BeginFrame();

// submit render commands

graphics.EndFrame();
~~~

Команда:

~~~csharp
graphics.Submit(
    command);
~~~

Render command содержит layer. Pipeline выбирает commands для каждого pass по layer range.

---

# 7. Render Pipeline

Pipeline:

~~~csharp
graphics.Pipeline
~~~

Очистка:

~~~csharp
graphics.Pipeline.Clear();
~~~

Добавление pass:

~~~csharp
graphics.Pipeline.AddPass(
    new RenderPass(
        "World",
        renderTarget,
        RenderState.Default,
        true,
        layers));
~~~

Pass выполняются последовательно:

~~~text
Render Queue
    ↓
Pass 1
    ↓
Pass 2
    ↓
Pass 3
    ↓
Backbuffer
~~~

Render target можно использовать как промежуточный результат следующего pass.

---

# 8. Graphics2D

2D-specific rendering находится в assembly Engine.Graphics.2D.

Общие graphics contracts остаются в Engine.Graphics.

Готовые 2D passes:

~~~csharp
RenderPass2D.Default
RenderPass2D.World
RenderPass2D.Ui
RenderPass2D.Debug
RenderPass2D.Present
~~~

2D layer содержит mechanisms для sprites, tilemaps и world textures.

---

# 9. Physics

Physics доступен через:

~~~csharp
runtime.Physics
~~~

Обычный игровой объект может объединять:

~~~text
Transform2D
PhysicsBody2D
Collider2D
~~~

Physics работает в fixed update.

Доступны:

- collision detection;
- collision resolution;
- joints;
- sleeping;
- contact events;
- physics queries.

Contacts:

~~~csharp
var contacts =
    runtime.Physics.Contacts;
~~~

Contact events:

~~~csharp
var events =
    runtime.Physics.ContactEvents;
~~~

Разовые пространственные запросы выполняются через PhysicsQuery2D.

---

# 10. Audio

Audio находится в Engine.Audio.

В runtime:

~~~csharp
var audio =
    runtime.Services.Audio;
~~~

Загрузка:

~~~csharp
var buffer =
    audio.Load(
        new AssetPath(
            "Sounds/explosion.wav"));
~~~

Source:

~~~csharp
using var source =
    audio.CreateSource(
        buffer);

source.SetGain(
    0.8f);

source.SetLooping(
    true);

source.Play();
~~~

Shortcut:

~~~csharp
using var source =
    audio.Play(
        buffer);
~~~

Returned IAudioSource является ресурсом, которым управляет вызывающая сторона.

Listener:

~~~csharp
using var listener =
    audio.CreateListener();

listener.SetPosition(
    listenerPosition);

listener.SetOrientation(
    forward,
    up);
~~~

OpenAL является backend и находится в Engine.Audio.OpenAL.

---

# 11. Input

Главный интерфейс:

~~~csharp
IInput
~~~

В runtime:

~~~csharp
var input =
    runtime.Services.Input;
~~~

Состояние action:

~~~csharp
if (input.IsDown(move))
{
    // action активен
}

if (input.IsPressed(jump))
{
    // нажатие в текущем frame
}

if (input.IsReleased(jump))
{
    // отпускание в текущем frame
}
~~~

Analog value:

~~~csharp
var value =
    input.GetValue(
        moveHorizontal);
~~~

Actions связываются с physical input через InputActionMap:

~~~csharp
var actions =
    new InputActionMap();

actions.Bind(
    moveLeft,
    leftBinding);

actions.Bind(
    moveRight,
    rightBinding);
~~~

Gameplay system работает с semantic actions, а не с конкретным backend input API.

---

# 12. Content

Content находится в Engine.Content.

Через runtime:

~~~csharp
var content =
    runtime.Services.Content;
~~~

Загрузка:

~~~csharp
var asset =
    content.Load<MyAsset>(
        path);
~~~

Проверка cache:

~~~csharp
if (content.IsLoaded<MyAsset>(path))
{
    // asset loaded
}
~~~

Unload:

~~~csharp
content.Unload<MyAsset>(
    path);
~~~

Reload:

~~~csharp
content.Reload<MyAsset>(
    path);
~~~

Async:

~~~csharp
var asset =
    await content.LoadAsync<MyAsset>(
        path,
        cancellationToken);
~~~

Для собственного типа можно зарегистрировать IContentLoader<T>.

---

# 13. World и Chunks

World доступен через:

~~~csharp
runtime.World
~~~

Создание chunk:

~~~csharp
var chunk =
    world.CreateChunk(
        chunkPosition);
~~~

Получение:

~~~csharp
if (world.TryGetChunk(
        chunkPosition,
        out var chunk))
{
    // use chunk
}
~~~

Или:

~~~csharp
var chunk =
    world.GetOrCreateChunk(
        chunkPosition);
~~~

World также предоставляет преобразование world coordinates в chunk/local coordinates.

Важное правило: spatial interest и simulation activity — независимые понятия. Далёкий chunk не обязан останавливаться только из-за отсутствия игрока рядом.

---

# 14. Networking

Networking находится в Engine.Networking.

Основные уровни:

~~~text
Transport
Session
Channels
Topology
Lockstep
Replication
~~~

Gameplay code должен работать с networking mechanisms и contracts, а не напрямую зависеть от конкретной transport implementation.

Для state-driven multiplayer используются deterministic simulation и state hashes.

Replication вынесена в Engine.Networking.Replication.

---

# 15. UI

UI находится в Engine.UI.

Базовая модель:

~~~text
UiRoot
  └── UiContainer
       ├── UiLabel
       ├── UiButton
       └── ...
~~~

Основной base class:

~~~csharp
UiWidget
~~~

Основные свойства:

~~~csharp
widget.Visible = true;
widget.Enabled = true;
widget.Width = 200.0f;
widget.Height = 40.0f;
~~~

UI использует retained-mode hierarchy и layout measurement/arrangement.

Конкретный экран или gameplay UI создаётся приложением или игрой.

---

# 16. Tooling и Validation

При включённом tooling runtime может предоставлять:

~~~text
Inspection
Debug Console
Profiler
Validation
Debug Visualization
Debug Execution
~~~

Например:

~~~csharp
runtime.Validation
runtime.Inspection
runtime.Console
runtime.Profiler
~~~

Некоторые сервисы optional, потому что tooling может быть отключён через EngineRuntimeOptions.

---

# 17. Resource Lifetime

Большинство runtime resources имеют явный lifecycle.

Пример:

~~~csharp
using var source =
    audio.CreateSource(
        buffer);
~~~

Важно различать:

~~~text
C# reference
resource ownership
backend lifetime
~~~

Перед Dispose или Unload необходимо понимать ownership конкретного объекта.

Особенно это важно для graphics и audio resources.

---

# 18. Backend Independence

Игровой код должен по возможности работать с:

~~~text
IGraphicsDevice
IInput
IAudioManager
IContentManager
~~~

а не с:

~~~text
OpenGL*
OpenAL*
Silk.NET*
~~~

Concrete backend создаётся на composition boundary приложения.

Например:

~~~text
Application composition
        ↓
OpenGL / OpenAL / Silk.NET
        ↓
Engine abstractions
        ↓
Game systems
~~~

---

# 19. What belongs to Game

Game владеет:

- components;
- gameplay systems;
- game commands;
- game events;
- game rules;
- game state;
- game-specific assets;
- game-specific networking policy.

Engine владеет:

- mechanisms;
- infrastructure;
- generic algorithms;
- backend contracts;
- runtime services;
- lifecycle infrastructure.

---

# 20. Main Rule

При добавлении нового поведения задавай вопрос:

> **Это механизм, полезный разным играм, или смысл конкретной игры?**

Механизм может относиться к Engine.

Смысл конкретной игры должен оставаться в Game.

Это основной критерий сохранения VibeEngine2 универсальным и предотвращения game-specific API в Engine.
