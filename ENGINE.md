# VibeEngine2

## 1. Назначение

VibeEngine2 — универсальный игровой движок на C#/.NET, предназначенный для создания различных типов игр без привязки ядра движка к конкретной игровой модели.

Основной архитектурный принцип:

> **Движок предоставляет механизмы, игра определяет смысл.**

Engine отвечает за предоставление инфраструктуры, систем, API и runtime-механизмов.

Game отвечает за конкретные правила, сущности, игровые состояния и смысл происходящего.

Поэтому в Engine не должны появляться понятия, специфичные для конкретной игры.

Например, Engine не должен содержать:

* `Player`
* `Enemy`
* `Building`
* `Server`
* `ProductionMachine`
* `ResourceChain`
* `NetworkCable`

Такие понятия относятся к игре или игровому проекту.

---

# 2. Основные архитектурные принципы

## 2.1. Domain Agnostic Engine

Engine не знает, какую именно игру он запускает.

Одна и та же инфраструктура должна позволять создавать разные типы игр:

* 2D;
* 3D в будущем;
* simulation-heavy games;
* networked games;
* single-player games;
* editor-driven projects.

Конкретная игровая логика находится за пределами Engine.

---

## 2.2. Mechanisms, not Meaning

Engine предоставляет механизмы:

* ECS;
* simulation;
* deterministic execution;
* networking;
* rendering;
* input;
* physics;
* audio;
* content loading;
* serialization;
* tooling;
* editor infrastructure.

Game определяет, как эти механизмы используются.

Например, Engine предоставляет `Entity`, `Component` и `System`.

Игра может использовать их для:

* персонажа;
* транспорта;
* здания;
* робота;
* частицы;
* производственной машины.

Сам Engine не должен знать, что такое «здание» или «робот».

---

## 2.3. Stable Boundaries

Основные архитектурные границы важнее количества абстракций.

Не следует создавать интерфейс или дополнительный слой только потому, что подобное решение существует в другом движке.

Новая абстракция должна решать конкретную проблему:

* разделять ответственность;
* предотвращать нежелательную зависимость;
* предоставлять реальную точку расширения;
* изолировать backend;
* обеспечивать требуемую производительность или детерминизм.

---

## 2.4. Hybrid Architecture

VibeEngine2 использует гибридный подход.

Обычный OOP применяется там, где важны:

* простота API;
* читаемость;
* управление ресурсами;
* backend implementations;
* tooling;
* editor systems.

Data-oriented подход применяется там, где он действительно необходим для производительности:

* ECS storage;
* queries;
* jobs;
* массовая обработка simulation data.

---

# 3. Архитектурные слои

Основная структура движка:

```text
Engine.Core
    ↓
Engine.ECS
Engine.Simulations
Engine.Transform
Engine.Physics
Engine.World
Engine.Content
Engine.Serialization
Engine.Input
Engine.Audio
Engine.Graphics
Engine.Camera
Engine.UI
Engine.Networking
Engine.Tooling
Engine.Editor
    ↓
Concrete Backends / Applications
    ↓
Game / Sandbox / Editor.Sandbox
```

Это логическая схема, а не требование, чтобы каждый проект напрямую зависел от каждого другого.

Главный принцип — зависимости должны идти от более фундаментальных механизмов к более специализированным слоям.

---

# 4. Engine.Core

`Engine.Core` содержит наиболее фундаментальные и независимые механизмы движка.

В Core находятся:

* базовая математика;
* идентификаторы;
* время;
* deterministic utilities;
* commands;
* events;
* replays;
* assets contracts;
* logging contracts;
* diagnostics;
* metrics.

Core не должен зависеть от:

* ECS;
* Graphics;
* UI;
* Networking;
* Physics;
* Editor;
* игровых концепций.

Core должен оставаться небольшим и стабильным.

---

# 5. ECS

ECS предоставляет entity/component модель.

Основные элементы:

* `EntityId`;
* `EntityStore`;
* `ComponentStorage<T>`;
* `World`;
* queries;
* snapshots;
* deterministic state hashing.

ECS отвечает за хранение и доступ к state data.

ECS не определяет смысл компонентов.

Например:

```csharp
public struct Health
{
    public int Value;
}
```

может существовать в игре, но сам Engine не обязан иметь компонент `Health`.

---

# 6. Simulation

Simulation отвечает за выполнение игровой логики в фиксированном времени.

Основные механизмы:

* `Simulation`;
* `SystemScheduler`;
* `FixedSystemContext`;
* `SystemPhase`;
* `CommandQueue`;
* `EventBus`;
* fixed tick;
* system dependencies.

Systems могут задавать зависимости выполнения:

```text
System A
    ↓
System B
    ↓
System C
```

Simulation не определяет содержание игровых систем.

Она только предоставляет механизм их выполнения.

---

# 7. Determinism

Детерминированность является отдельной архитектурной частью движка.

Для неё существуют:

* `DeterministicRandom`;
* `DeterministicStateHasher`;
* `DeterministicStateHash`;
* `IDeterministicState`;
* deterministic ECS hashing;
* deterministic state processing.

Детерминированное состояние необходимо для механизмов, которым требуется воспроизводимость simulation.

В частности, это позволяет использовать:

* lockstep networking;
* replay;
* state comparison;
* desync detection.

---

# 8. Networking

Networking разделён на низкоуровневую транспортную инфраструктуру и более специализированные механизмы.

Основной слой:

```text
Engine.Networking
```

Он должен оставаться topology-neutral.

В нём находятся механизмы:

* network session;
* connections;
* packets;
* channels;
* reliable / unreliable delivery;
* command exchange;
* state hash exchange;
* lockstep;
* desync detection;
* network topologies.

Конкретная архитектура сетевой игры не должна заставлять `Simulation` зависеть от конкретной topology.

Например:

```text
Simulation
     ↑
Networking abstraction
     ↑
P2P / Client-Server / Other topology
```

а не:

```text
Simulation
     ↓
Server
```

`Server` и другие игровые роли являются частью конкретной игры или network architecture layer, а не универсального simulation API.

---

# 9. Replication

Состояние ECS может реплицироваться отдельным слоем:

```text
Engine.Networking.Replication
```

Replication зависит от ECS и networking.

Она отвечает за передачу и применение состояния, но не определяет значение этого состояния для конкретной игры.

---

# 10. Graphics

`Engine.Graphics` содержит backend-neutral graphics abstraction.

Он отвечает за общие механизмы:

* graphics device abstraction;
* render commands;
* render queue;
* render pipeline;
* render passes;
* render states;
* render targets;
* shaders;
* textures;
* buffers;
* resource handles;
* render layers.

Graphics не должен зависеть от OpenGL API.

---

# 11. Graphics backends

Конкретная graphics implementation находится отдельно.

Например:

```text
Engine.Graphics
        ↑
Engine.Graphics.OpenGL
```

Backend знает о конкретном graphics API.

Backend-neutral layer — нет.

Такой подход позволяет в будущем добавить другую реализацию без изменения общей graphics API.

---

# 12. Graphics2D

2D rendering является отдельным специализированным слоем:

```text
Engine.Graphics2D
```

Он использует общие Graphics mechanisms, но содержит 2D-specific functionality:

* sprites;
* sprite rendering;
* sprite animation rendering;
* tilemap rendering;
* world texture drawing;
* 2D render presets;
* 2D-specific rendering helpers.

При этом существующие публичные namespaces могут сохраняться ради API stability даже после разделения assemblies.

---

# 13. Camera

Camera является отдельной системой, а не частью Graphics.

Текущая архитектура:

```text
Engine.Camera
    ├── Camera2D
    └── future Camera3D
```

Camera отвечает за преобразование между пространствами и camera-specific operations.

Например:

```text
World Space
    ↓
Camera
    ↓
Screen Space
```

Graphics может использовать Camera implementation в конкретном backend или renderer, но Camera не должна зависеть от Graphics.

---

# 14. Transform and Hierarchy

Transform является отдельной универсальной частью движка:

```text
Engine.Transform
```

Transform отвечает за spatial state и hierarchy.

Основная идея:

```text
Local Transform
       ↓
Parent
       ↓
World Transform
```

Transform не является исключительно graphics concept.

Он может использоваться:

* rendering;
* physics;
* audio;
* gameplay;
* editor;
* tools.

---

# 15. Physics

Physics является отдельным механизмом simulation.

Текущая реализация содержит 2D physics.

Основные механизмы:

* bodies;
* colliders;
* shapes;
* collision detection;
* collision resolution;
* physics queries;
* collision events;
* materials;
* physics settings.

Physics не знает, что именно представляет собой объект.

Collider может принадлежать:

* игроку;
* стене;
* предмету;
* зданию;
* двери;
* любому другому игровому объекту.

Эти значения определяются игрой.

---

# 16. Audio

Audio разделён на backend-neutral API и backend implementation.

```text
Engine.Audio
      ↑
Engine.Audio.OpenAL
```

`Engine.Audio` предоставляет:

* audio device;
* audio buffer;
* audio source;
* audio listener;
* audio data;
* audio loading;
* audio resource management.

OpenAL implementation находится отдельно.

Важная граница:

```text
Engine.Audio
    не знает об OpenAL

Engine.Audio.OpenAL
    знает об Engine.Audio
    и OpenAL
```

Audio resources управляются отдельно от gameplay meaning.

---

# 17. Input

Input предоставляет абстракцию пользовательского ввода.

Конкретный window/input backend может быть реализован отдельно.

Общая архитектура должна позволять игре работать с input mechanisms без жёсткой зависимости от конкретной библиотеки.

---

# 18. UI

`Engine.UI` представляет retained-mode UI.

Это не игровой HUD framework, а общий UI mechanism.

Основные элементы включают:

* widgets;
* layout;
* focus;
* overlays;
* windows;
* modal dialogs;
* dropdowns;
* lists;
* scroll views;
* tooltips;
* context menus;
* text input;
* buttons;
* sliders;
* toggles.

UI предоставляет механизм взаимодействия и отображения.

Конкретное содержание UI определяется приложением или игрой.

---

# 19. Content

`Engine.Content` отвечает за управление игровыми и engine assets.

Основные задачи:

* loading;
* catalogs;
* loaders;
* caching;
* dependency handling;
* cyclic dependency detection;
* serialization-related content operations.

Content layer отделён от конкретных subsystems.

Например:

```text
Content
   ↓
Audio Loader
   ↓
AudioData
```

или:

```text
Content
   ↓
Texture Loader
   ↓
Texture resource
```

---

# 20. Serialization

Serialization предоставляет механизм преобразования engine/game state в сохраняемое представление.

Она используется, в частности, для:

* save data;
* snapshots;
* persistent state;
* network data;
* replay-related data.

Serialization не должна определять игровую семантику.

---

# 21. World

World layer отвечает за организацию большого пространственного состояния.

Важное архитектурное правило:

> Spatial interest и simulation state — разные понятия.

То, что объект или chunk находится далеко от текущего игрока, не означает, что его simulation должна останавливаться.

Следовательно:

```text
Rendering / Spatial Interest
          ≠
Simulation Activity
```

Это позволяет поддерживать large-world и simulation-heavy сценарии.

Конкретная политика активности определяется приложением.

---

# 22. Tooling

`Engine.Tooling` предназначен для инструментов разработки.

Он включает инфраструктуру для:

* inspection;
* debugging;
* validation;
* engine console;
* profiling;
* debug visualization;
* editor foundation.

Tooling не должен содержать игровые правила.

Например, validation может проверять:

```text
runtime invariants
configuration
authoring data
system consistency
```

но не должен предполагать существование конкретных игровых сущностей.

---

# 23. Editor

Editor является отдельным приложением/слоем над engine infrastructure.

Главная идея:

```text
Engine
    ↓
Editor infrastructure
    ↓
Editor.Sandbox
```

Editor использует engine mechanisms, но не определяет их.

`Editor.Sandbox` является запускаемым editor application / integration environment.

---

# 24. Game.Sandbox

`Game.Sandbox` предназначен для проверки движка в реальном сценарии.

Это не финальная игра.

Sandbox используется для:

* integration testing;
* демонстрации API;
* проверки взаимодействия систем;
* ручной проверки rendering/input/audio;
* проверки runtime integration.

Игровые элементы внутри Sandbox не являются частью Engine architecture.

---

# 25. Resource Ownership

Большинство runtime resources имеют явный lifecycle.

Например:

```text
Device
 ├── Buffer
 ├── Source
 └── Listener
```

Владение должно быть понятным.

Resource manager отвечает за ресурсы, которыми он владеет.

Object, который получил независимый runtime object, отвечает за его lifecycle согласно контракту API.

Автоматизация lifetime не должна добавляться без реальной необходимости.

---

# 26. Backend Isolation

Backend-specific API должен находиться в отдельном assembly.

Пример:

```text
Engine.Audio
        ↑
Engine.Audio.OpenAL
```

```text
Engine.Graphics
        ↑
Engine.Graphics.OpenGL
```

Backend-neutral layer определяет contracts.

Backend implementation реализует эти contracts.

Это предотвращает распространение конкретных технологий по всему движку.

---

# 27. Performance Philosophy

Производительность является важной частью архитектуры, но оптимизация не должна происходить преждевременно.

Сначала определяется корректная граница системы.

После этого производительность оптимизируется там, где есть реальная потребность.

Особенно это относится к:

* ECS;
* queries;
* jobs;
* memory;
* rendering;
* physics;
* networking.

Не следует добавлять сложные generic abstractions ради гипотетической производительности.

---

# 28. Testing Philosophy

Тесты должны проверять прежде всего архитектурные гарантии и важное поведение.

Предпочтение отдаётся небольшому количеству содержательных тестов вместо большого количества тестов каждого trivial API accessor.

Особенно важны тесты на:

* determinism;
* serialization;
* resource lifetime;
* backend boundaries;
* rendering pipeline;
* networking behavior;
* physics invariants;
* data validation.

Sandbox дополняет автоматические тесты интеграционными и ручными проверками.

---

# 29. What Engine Intentionally Does Not Contain

Engine не должен превращаться в game framework, который заранее диктует структуру игры.

В частности, Engine не должен содержать универсальные игровые понятия вроде:

```text
Player
Enemy
Building
Quest
Inventory
Weapon
ProductionMachine
ResourceChain
Server
Faction
Economy
```

если для них нет действительно domain-agnostic механизма.

Вместо этого Engine предоставляет инструменты, из которых игра может построить такие понятия самостоятельно.

---

# 30. Overall Architecture

Упрощённая модель VibeEngine2:

```text
                    ┌─────────────────────┐
                    │        Game         │
                    │  game meaning/state │
                    └──────────┬──────────┘
                               │
                         uses engine
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
        ▼                      ▼                      ▼
      Runtime               Tooling                Editor
        │                      │                      │
        ├── ECS                ├── Validation         ├── Editor UI
        ├── Simulation         ├── Debugging          └── Editor tools
        ├── Transform          ├── Profiling
        ├── Physics            └── Inspection
        ├── Networking
        ├── Graphics
        ├── Camera
        ├── Audio
        ├── Input
        ├── UI
        ├── Content
        ├── Serialization
        └── World
                │
                ▼
         Backend implementations
                │
        ┌───────┴────────┐
        ▼                ▼
      OpenGL            OpenAL
```

---

# 31. Architectural Goal

VibeEngine2 должен оставаться engine-first системой.

Его задача — не предсказать конкретную игру, а предоставить устойчивый набор механизмов, на котором разные игры могут строить собственную архитектуру.

Итоговый принцип:

> **Engine предоставляет возможности.
> Game определяет правила.
> Sandbox проверяет интеграцию.
> Editor предоставляет инструменты создания и отладки.**
