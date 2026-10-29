# Индекс проектов VibeEngine2

Алфавитный справочник по C#-проектам репозитория: где расположен механизм, какие типы служат отправной точкой для изучения кода и от каких проектов он непосредственно зависит.

> **Важно:** запись в индексе подтверждает расположение исходного кода, а не готовность каждой возможности к production. Степень готовности и проверенные ограничения следует фиксировать отдельно по результатам тестов и практических проверок. Ссылки на проектные зависимости ниже отражают прямые `ProjectReference` из соответствующего `.csproj`, а не полный транзитивный граф.

## Как читать индекс

- **Проект** — имя в решении; **файл проекта** — точный путь к `.csproj`.
- **Основные ориентиры** — несколько каталогов или типов, с которых удобно начать изучение; это не полный список публичного API.
- **Прямые зависимости** — другие проекты решения, на которые ссылается данный `.csproj`.
- Общие архитектурные решения описаны в [ENGINE.md](ENGINE.md), практическое использование API — в [API.md](API.md), частые проблемы запуска и сборки — в [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

## Проекты по алфавиту

| Проект | Файл проекта | Назначение и основные ориентиры | Прямые зависимости |
|---|---|---|---|
| **Editor.Sandbox** | `Editor.Sandbox/Editor.Sandbox.csproj` | Исполняемая оболочка редактора. `Program.cs`, `WindowsClipboardService`, `WindowsFileDialogService`. Целевой TFM: `net8.0-windows`. | `Engine.Content`, `Engine.Core`, `Engine.ECS`, `Engine.Editor`, `Engine.Editor.UI`, `Engine.Graphics`, `Engine.Graphics.OpenGL`, `Engine.Input`, `Engine.Networking.Replication`, `Engine.Physics`, `Engine.Serialization`, `Engine.Tooling`, `Engine.UI`, `Engine.Worlds` |
| **Engine.Animation** | `Engine.Animation/Engine.Animation.csproj` | Анимационные клипы, дорожки и ключевые кадры, интерполяторы и воспроизведение. Типы: `AnimationClip`, `AnimationPlayer`, `AnimationTrack`; файловая и JSON-сериализация клипов. | `Engine.Core` |
| **Engine.Audio** | `Engine.Audio/Engine.Audio.csproj` | Общая аудио-модель, аудиоменеджер, устройства, буферы, источники, слушатель, ECS-компоненты и данные аудио. Типы: `AudioManager`, `IAudioDevice`, `IAudioSource`. | `Engine.Core` |
| **Engine.Audio.OpenAL** | `Engine.Audio.OpenAL/Engine.Audio.OpenAL.csproj` | Аудиобэкенд OpenAL. Типы: `OpenALAudioDevice`, `OpenALAudioSource`, `OpenALAudioBuffer`. | `Engine.Audio` |
| **Engine.Benchmarks** | `Engine.Benchmarks/Engine.Benchmarks.csproj` | Измерения производительности Runtime, графики, физики, рендеринга тайловых карт и иерархии трансформаций. Файлы: `PhysicsSystemBenchmark`, `PhysicsBroadPhaseBenchmark`, `TilemapRenderingBenchmark`, `TransformHierarchyBenchmark` и другие бенчмарки. | `Engine.Camera`, `Engine.Content`, `Engine.Core`, `Engine.ECS`, `Engine.Graphics2D`, `Engine.Graphics.OpenGL`, `Engine.Graphics`, `Engine.Jobs`, `Engine.Runtime` |
| **Engine.Camera** | `Engine.Camera/Engine.Camera.csproj` | Данные и параметры 2D-камеры. Основной тип: `Camera2D`. | `Engine.Core` |
| **Engine.Content** | `Engine.Content/Engine.Content.csproj` | Управление ресурсами, дескрипторы, файловые каталоги, загрузчики, конвейер контента и hot reload. Основные ориентиры: `ContentManager`, `ContentHandle`, каталоги `Assets`, `Loading`, `HotReload`. | `Engine.Core` |
| **Engine.Core** | `Engine.Core/Engine.Core.csproj` | Фундаментальная инфраструктура: приложение и игровой цикл, время, математика, события, системы, диагностика, логирование, ресурсы, пути к ассетам, детерминизм и общие механизмы симуляции. Основные каталоги: `Application`, `Assets`, `Collections`, `Determinism`, `Diagnostics`, `Events`, `Logging`, `Math`, `Resources`, `Systems`, `Time`. | Нет |
| **Engine.ECS** | `Engine.ECS/Engine.ECS.csproj` | ECS-мир, сущности и компоненты, хранилища, запросы, системы, команды, снимки и инспекция. Типы: `World`, `WorldSnapshot`, `CommandBuffer`; каталоги `Components`, `Entities`, `Queries`, `Systems`, `Persistence`. | `Engine.Core`, `Engine.Jobs`, `Engine.Memory`, `Engine.Transform` |
| **Engine.Editor** | `Engine.Editor/Engine.Editor.csproj` | Предметная логика редактора: документы, действия и команды, ассеты, проекты, сущности, иерархия, инспекция, панели, выделение, viewport и состояние рабочего пространства. Основной контекст: `EditorContext`; каталоги `Actions`, `Assets`, `Documents`, `Hierarchy`, `Inspection`, `Projects`, `Viewport`, `Workspace`. | `Engine.Core`, `Engine.ECS`, `Engine.Serialization`, `Engine.Tooling`, `Engine.UI`, `Engine.Worlds` |
| **Engine.Editor.UI** | `Engine.Editor.UI/Engine.Editor.UI.csproj` | UI-оболочка редактора, панели, представления authoring, диалоги, сервисы интерфейса и команды редакторского UI. Типы: `EditorApplication`, `EditorUiHost`; каталоги `Authoring`, `Dialogs`, `Panels`, `Services`, `Shell`, `Workspace`. | `Engine.Editor`, `Engine.Physics`, `Engine.Tooling`, `Engine.UI` |
| **Engine.Graphics** | `Engine.Graphics/Engine.Graphics.csproj` | Общие графические контракты и ресурсы: устройство графики, команды отрисовки, render targets, шрифты и графические ресурсы. Ориентиры: `IGraphics`, `IGraphicsDevice`, `IGraphicsApplication`; каталоги `Commands`, `Rendering`, `Resources`, `Fonts`. | `Engine.Camera`, `Engine.Content`, `Engine.Core` |
| **Engine.Graphics2D** | `Engine.Graphics.2D/Engine.Graphics2D.csproj` | 2D-отрисовка, спрайты, анимация спрайтов, рендеринг частиц и тайловых карт. Папка в репозитории называется `Engine.Graphics.2D`, а имя файла проекта — `Engine.Graphics2D.csproj`; namespace: `Engine.Graphics2D`. Ориентиры: `SpriteComponent`, каталог `Sprites`, каталоги `Rendering`, `Particles`, `Tilemaps`. | `Engine.Animation`, `Engine.Camera`, `Engine.ECS`, `Engine.Graphics`, `Engine.Particles` |
| **Engine.Graphics.OpenGL** | `Engine.Graphics.OpenGL/Engine.Graphics.OpenGL.csproj` | Реализация графического бэкенда на OpenGL. Типы: `OpenGLGraphicsDevice`, `OpenGLWindow`; каталоги `Rendering`, `Resources`, `Fonts`. Использует Silk.NET. | `Engine.Core`, `Engine.Graphics2D`, `Engine.Graphics`, `Engine.Input.SilkNet`, `Engine.Input` |
| **Engine.Hosting** | `Engine.Hosting/Engine.Hosting.csproj` | Общий контракт игрового хоста. Основной тип: `IGameHost`. | `Engine.Core`, `Engine.Camera`, `Engine.Graphics`, `Engine.Input`, `Engine.Audio` |
| **Engine.Hosting.Desktop** | `Engine.Hosting.Desktop/Engine.Hosting.Desktop.csproj` | Настольный хост игры, параметры запуска и контекст хоста. Типы: `DesktopGameHost`, `DesktopGameHostContext`, `DesktopGameHostOptions`. Текущая реализация напрямую связывает хост с OpenGL- и OpenAL-проектами. | `Engine.Audio`, `Engine.Audio.OpenAL`, `Engine.Content`, `Engine.Core`, `Engine.Graphics`, `Engine.Graphics.OpenGL`, `Engine.Input`, `Engine.Runtime` |
| **Engine.Input** | `Engine.Input/Engine.Input.csproj` | Общие контракты ввода, снимки состояния, устройства, действия и привязки, указатель, текстовый ввод и курсор. Типы: `IInput`, `IInputBackend`, `InputAction`, `InputActionMap`, `InputSnapshot`. | `Engine.Core` |
| **Engine.Input.SilkNet** | `Engine.Input.SilkNet/Engine.Input.SilkNet.csproj` | Реализация ввода и курсора через Silk.NET. Типы: `SilkNetInputBackend`, `SilkNetCursorService`. | `Engine.Input` |
| **Engine.Jobs** | `Engine.Jobs/Engine.Jobs.csproj` | Система задач, планировщик, зависимости и ожидание завершения. Типы: `JobScheduler`, `JobHandle`, `JobScope`; каталоги `Jobs`, `Scheduling`, `Integration`. | `Engine.Core` |
| **Engine.Localization** | `Engine.Localization/Engine.Localization.csproj` | Механизмы локализации: языки, ключи, записи, таблицы и менеджер переводов. Типы: `Locale`, `LocalizationKey`, `LocalizationTable`, `LocalizationManager`. | Нет |
| **Engine.Memory** | `Engine.Memory/Engine.Memory.csproj` | Арены, пулы, блоки, буферы, коллекции с повторным использованием памяти, нативная память и статистика. Каталоги: `Arenas`, `Blocks`, `Buffers`, `Collections`, `Diagnostics`, `Native`, `Pools`, `Scopes`. | Нет |
| **Engine.Navigation** | `Engine.Navigation/Engine.Navigation.csproj` | Навигационные сетки и карты, чанки, поиск пути и алгоритм A*. Ориентиры: `NavigationGrid`, `ChunkedNavigationMap`, `AStarPathfinder`; каталоги `Maps`, `Chunks`, `Pathfinding`. | `Engine.Core`, `Engine.Jobs` |
| **Engine.Networking** | `Engine.Networking/Engine.Networking.csproj` | Общая сетевая инфраструктура: архитектуры и топологии сети, authority, соединения, сообщения, пакеты, сессии, симуляция и транспорт. Каталоги: `Architecture`, `Authority`, `Connections`, `Messaging`, `Packets`, `Sessions`, `Simulation`, `Topology`, `Transport`. | `Engine.Core`, `Engine.Serialization` |
| **Engine.Networking.Replication** | `Engine.Networking.Replication/Engine.Networking.Replication.csproj` | Идентификация сетевых сущностей и репликация компонентов и состояния сущностей. Ориентиры: `NetworkEntityId`, `NetworkIdentity`, `ReplicatedComponentRegistry`, `ReplicationStateService`; также сериализация сообщений репликации. | `Engine.ECS`, `Engine.Networking`, `Engine.Serialization` |
| **Engine.Particles** | `Engine.Particles/Engine.Particles.csproj` | Модель частиц, эмиттеры, создание и инициализация частиц, модификаторы и определения эффектов. Ориентиры: `ParticleEmitter`, `ParticleEffectDefinition`, `ParticleEffectSimulation`; каталоги `Emission`, `Initialization`, `Modifiers`, `Particles`. | `Engine.Core` |
| **Engine.Physics** | `Engine.Physics/Engine.Physics.csproj` | Физика 2D: тела и коллайдеры, формы, материалы, соединения, broad phase, collision detection/resolution и запросы. Ориентиры: `PhysicsSystem2D`, `SpatialHashBroadPhase`, `CollisionDetector2D`; каталоги `BroadPhase`, `Collision`, `Components`, `Joints`, `Queries`, `Shapes`. | `Engine.Core`, `Engine.ECS` |
| **Engine.Runtime** | `Engine.Runtime/Engine.Runtime.csproj` | Координация подсистем движка и игрового выполнения. Типы: `EngineRuntime`, `EngineGame`, `EngineRuntimeServices`, `EngineRuntimeOptions`. Также содержит Runtime-механизмы частиц и тайловых карт и сервис пересборки движка. | `Engine.Audio`, `Engine.Core`, `Engine.ECS`, `Engine.Scripting`, `Engine.Simulations`, `Engine.Worlds`, `Engine.Physics`, `Engine.Graphics`, `Engine.Input`, `Engine.Tooling` |
| **Engine.Scripting** | `Engine.Scripting/Engine.Scripting.csproj` | Базовая модель игровых скриптов: `IEntityScript`, `ScriptContext`, `ScriptRegistry`, `ScriptRuntime`, назначения скриптов сущностям, подписки на события и ошибки скриптов. Наличие проекта не означает, что весь редакторский и игровой workflow скриптинга уже завершён. | `Engine.Core`, `Engine.ECS` |
| **Engine.Serialization** | `Engine.Serialization/Engine.Serialization.csproj` | Бинарная сериализация, чтение/запись, контексты, сериализация контента и сохранение/загрузка различных игровых типов. Каталоги: `Binary`, `Content`, `SaveLoad`, `Types`, `UI`. | `Engine.Audio`, `Engine.Content`, `Engine.Core`, `Engine.ECS`, `Engine.Graphics2D`, `Engine.Physics`, `Engine.Scripting`, `Engine.UI`, `Engine.Worlds` |
| **Engine.Simulations** | `Engine.Simulations/Engine.Simulations.csproj` | Механизмы симуляции, данные преобразования и скорости, lockstep и rollback. Типы: `Simulation`, `SimulationTransform`, `SimulationVelocity`, `LockstepCoordinator`, `RollbackBuffer`. | `Engine.Core`, `Engine.ECS`, `Engine.Networking`, `Engine.Worlds` |
| **Engine.Tests** | `Engine.Tests/Engine.Tests.csproj` | Автоматические unit- и интеграционные тесты подсистем движка. Каталоги организованы по темам: `ECS`, `Editor`, `Content`, `Graphics`, `Networking`, `Physics`, `Runtime`, `Scripting`, `Serialization`, `UI` и другие. | Большинство проверяемых проектов, включая `Game.Sandbox`; полный список смотреть в `Engine.Tests.csproj` |
| **Engine.Tooling** | `Engine.Tooling/Engine.Tooling.csproj` | Инструменты разработки и диагностики: консоль, отладочная визуализация, профилирование, инспекция и валидация. Каталоги: `Console`, `DebugVisualization`, `Debugging`, `Inspection`, `Profiling`, `Validation`. | `Engine.Content`, `Engine.Core`, `Engine.ECS`, `Engine.Graphics`, `Engine.Input`, `Engine.Physics`, `Engine.Worlds` |
| **Engine.Transform** | `Engine.Transform/Engine.Transform.csproj` | Базовые 2D-преобразования и операции над ними, включая обычные и fixed-point варианты. Каталог: `2D`; типы: `Transform2D`, `FixedTransform2D`. | `Engine.Core` |
| **Engine.UI** | `Engine.UI/Engine.UI.csproj` | Игровая UI-подсистема: UI-ассеты и элементы, действия, контролы, ввод, layout, экраны и стили. Каталоги: `Actions`, `Assets`, `Controls`, `Core`, `Input`, `Layout`, `Screens`, `Styling`. | `Engine.Core`, `Engine.Graphics`, `Engine.Input` |
| **Engine.Worlds** | `Engine.World/Engine.Worlds.csproj` | Пространственный мир, чанки, их жизненный цикл и состояния, пространственные запросы, сохранение и данные тайлов. Папка — `Engine.World`, файл проекта — `Engine.Worlds.csproj`. Ориентиры: `World`, `Chunk`; каталоги `Chunks`, `Persistence`, `Spatial`, `Tiles`. Не путать с ECS-миром из `Engine.ECS`. | `Engine.Core`, `Engine.ECS`, `Engine.Jobs`, `Engine.Memory` |
| **Game.Sandbox** | `Game.Sandbox/Game.Sandbox.csproj` | Интеграционная площадка для проверки совместной работы игровых подсистем: `SandboxGame`, игровые компоненты и системы, UI, сценарии и пробы сети/репликации/скриптов. **Не является обязательным шаблоном пользовательской игры** и не определяет, какие зависимости должен иметь каждый игровой проект. | Большая часть игровых подсистем, включая `Engine.Runtime`, `Engine.Hosting.Desktop`, `Engine.Graphics.OpenGL`, `Engine.Audio.OpenAL`, `Engine.Networking`, `Engine.Networking.Replication` и другие; полный список — в `Game.Sandbox.csproj` |

## Быстрый поиск по функциям

| Если нужно найти… | Начать с проекта |
|---|---|
| Анимации клипов и дорожек | `Engine.Animation` |
| Анимацию спрайтов и 2D-рендеринг | `Engine.Graphics2D` |
| Аудиоконтракты и управление звуком | `Engine.Audio` |
| Реализацию аудиоустройства OpenAL | `Engine.Audio.OpenAL` |
| Игровой цикл и общую инфраструктуру | `Engine.Core`, затем `Engine.Runtime` |
| ECS, сущности, компоненты и запросы | `Engine.ECS` |
| Документы, ассеты и команды редактора | `Engine.Editor` |
| Панели и оболочку редактора | `Engine.Editor.UI` |
| Графические контракты и ресурсы | `Engine.Graphics` |
| Конкретный OpenGL-бэкенд | `Engine.Graphics.OpenGL` |
| Создание и запуск настольного игрового хоста | `Engine.Hosting.Desktop` |
| Действия и привязки ввода | `Engine.Input` |
| Интеграцию ввода через Silk.NET | `Engine.Input.SilkNet` |
| Задачи и планировщик | `Engine.Jobs` |
| Локализацию | `Engine.Localization` |
| Пулы, арены и статистику памяти | `Engine.Memory` |
| Навигационную сетку и поиск пути | `Engine.Navigation` |
| Общую сетевую инфраструктуру | `Engine.Networking` |
| Репликацию сущностей и компонентов | `Engine.Networking.Replication` |
| Эффекты и эмиттеры частиц | `Engine.Particles` |
| Физические тела, коллайдеры и запросы | `Engine.Physics` |
| Runtime-интеграцию подсистем | `Engine.Runtime` |
| Скрипты, назначения и запуск скриптов | `Engine.Scripting` |
| Бинарную сериализацию и save/load | `Engine.Serialization` |
| Lockstep и rollback | `Engine.Simulations` |
| Автоматические тесты | `Engine.Tests` |
| Логи, инспекцию, профилирование и валидацию | `Engine.Tooling` |
| Преобразования и операции над ними | `Engine.Transform` |
| Игровые UI-ассеты, контролы и layout | `Engine.UI` |
| Пространственные чанки и тайловые данные | `Engine.Worlds` |
| Интеграционные демонстрации механизмов | `Game.Sandbox` |

## Границы справочника

1. Индекс описывает **место реализации**, а не обещает, что весь функционал завершён или прошёл все проверки.
2. Наличие тестов в `Engine.Tests` само по себе не означает, что протестированы все варианты использования. Для статуса готовности нужны конкретные результаты тестов и ручных проверок.
3. Прямые ссылки между `.csproj` могут меняться при рефакторинге. При изменении архитектуры обновляйте зависимости в таблице по соответствующим файлам проектов.
4. Поддерживаемые форматы, ограничения конкретных сценариев и статус готовности следует описывать отдельно — в документации возможностей и ограничений, а не выводить из названия проекта.
