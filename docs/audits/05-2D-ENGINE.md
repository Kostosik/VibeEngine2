# Аудит 2D Engine VibeEngine2 (0.6)

**Дата:** 2026-11-27  
**Ветка:** master  
**Исходный commit:** e4b17ea42518e55209c192cadc4b60bf9fd9e56e  
**Тип проверки:** статический аудит исходников, project references и тестовых файлов. Сборка и тесты в рамках этого этапа не запускались.

## 1. Цель и границы

Проверен путь от 2D-ресурсов и ECS-компонентов до редакторского authoring, сериализации, runtime и отрисовки:

- Sprite и Sprite Materials;
- TileSet и Tilemap;
- Camera2D и Viewport;
- обычная Animation и Sprite Animation;
- Lighting и Normal Mapping;
- Physics 2D;
- Particles;
- UI;
- Audio;
- Navigation и связанная AI-подсистема.

Ранее выполненные проверки RenderGraph и 2D Lighting повторно не проводились. Здесь проверялась только их граница с 2D-компонентами и общим игровым workflow.

Изменения production-кода и тестов не выполнялись. Присутствие теста в репозитории не считается повторным запуском теста.

## 2. Итог

В движке уже есть значительная часть базового 2D runtime workflow. Стандартный `EngineGame` автоматически обновляет физику, аудио и эффекты частиц, а при рендеринге вызывает отрисовку Tilemap, Sprite и Particles. У этих подсистем есть ECS-компоненты, необходимые сериализаторы и тестовое покрытие соответствующих частей сценария.

При этом все 2D-возможности пока не образуют одинаково завершённый пользовательский цикл:

1. **Sprite / Tilemap / Physics / Particles / Audio** имеют прямую интеграцию со стандартным `EngineRuntime` и `EngineGame`.
2. **Animation** предоставляет clips, player, sprite animation и редакторские документы, но стандартной ECS-системы анимации в `EngineRuntime` не обнаружено. Игровой код сам создаёт и обновляет проигрыватель.
3. **UI** — самостоятельная retained-mode подсистема. Есть редакторский формат и мост Editor → UiAsset → runtime widgets, но `EngineGame` и `EngineRuntime` не создают и не обновляют `UiSystem` автоматически.
4. **Navigation и AI** доступны как отдельные библиотеки. Их связь с Tilemap, World и стандартным Runtime должна быть организована игровым кодом.
5. Tilemap asset и chunk-based tile storage из `Engine.Worlds.World` — разные модели данных. Автоматического преобразования одной в другую не найдено.

### 2.1. Сводная оценка

| Область | Оценка | Что подтверждается исходниками |
|---|---|---|
| Sprite | Готово с ограничениями | ECS-компонент автоматически отрисовывается стандартным EngineGame; есть camera culling, transform, масштаб, поворот и ресурсный кэш |
| Sprite Materials | Готово с ограничениями | Renderer загружает Albedo, Normal и Emission каналы и передаёт параметры материала; полнота отдельного authoring workflow материала требует уточнения |
| TileSet / Tilemap | Готово с ограничениями | Форматы, loaders, Editor documents, viewport preview, runtime renderer и сериализация компонентов связаны; tilemap не интегрирован автоматически с chunk storage |
| Camera2D | Готово с ограничениями | Камера предоставляет координатные преобразования, zoom, bounds, follow, dead zone, shake и используется стандартными 2D renderer-ами |
| Animation | Частично | Clip/player и sprite animation существуют, поддерживаются сериализация и редакторские документы; стандартной ECS/runtime-системы проигрывания не обнаружено |
| Lighting / Normal Mapping | Требует отдельной сверки с ранее выполненным аудитом | Sprite renderer передаёт normal/emission channels и normal transform; внутренняя архитектура Lighting/RenderGraph здесь повторно не оценивалась |
| Physics 2D | Готово с ограничениями | PhysicsSystem2D включён в FixedUpdate; есть collider shapes, materials, joints, contact events, queries и debug/validation |
| Particles | Готово с ограничениями | ParticleEffectComponent автоматически синхронизируется с ParticleRuntime; update/render включены в EngineGame; есть ручное создание emitter |
| UI | Частично | Есть retained UI, widgets, layout, input/focus, screens, UI asset loader и Editor converter; интеграция в стандартный EngineGame остаётся ручной |
| Audio | Готово с ограничениями | AudioEcsSystem автоматически обслуживает AudioSource/AudioListener; стандартный Desktop Host использует OpenAL; форматы ограничены WAV PCM |
| Navigation | Частично | Grid/chunk maps, pathfinding и job scheduling существуют; связи с Tilemap, World и Editor не обнаружено |
| AI | Частично | AISystem реализует IUpdateSystem, scheduling, perception, memory, Behavior Trees и Utility AI; стандартный Runtime его автоматически не регистрирует |

Эти статусы относятся к перечисленным сценариям и не означают, что каждая подсистема проверена во всех игровых проектах, backend или масштабах нагрузки.

## 3. Sprite и Sprite Materials

### Runtime

`Engine.Graphics.2D/Sprites/SpriteComponent.cs` хранит относительный путь к текстуре, размеры и render layer. `SpriteRenderSystem` запрашивает пары `SpriteComponent + WorldTransform2D`, загружает текстуру через `ITextureResourceManager` и формирует команду отрисовки.

В текущем пути:
- пустая текстура и недопустимые размеры пропускаются;
- позиция WorldTransform трактуется как центр спрайта;
- масштаб Transform учитывается при вычислении размера;
- отрицательный масштаб отражает UV;
- Rotation передаётся в renderer;
- SpriteRenderer отсекает объекты вне Camera2D bounds;
- TextureResourceManager может быть общим для Sprite, Tilemap и Particles либо принадлежать конкретному renderer.

`EngineGame.Render()` вызывает `Runtime.RenderSprites()`, а EngineRuntime лениво создаёт SpriteRenderSystem только при наличии соответствующих сущностей.

### Материалы

`SpriteMaterialComponent` содержит пути Normal и Emission текстур и параметры `SpriteMaterialParameters`. `SpriteRenderSystem` загружает Normal в linear color space, Emission — как обычную цветовую текстуру, затем формирует `SpriteMaterial`.

Сериализаторы `SpriteComponentSerializer` и `SpriteMaterialComponentSerializer` зарегистрированы в `EcsComponentSerializerRegistryExtensions.RegisterEngineComponents()`.

В Editor подтверждён инспекторский сценарий назначения/очистки albedo texture с Undo/Redo (`Engine.Tests/Editor/SpriteTextureInspectorTests.cs`). Отдельный специализированный editor property view для настройки всех параметров SpriteMaterialComponent при просмотре структуры исходников не найден; наличие generic Inspector не трактуется как полноценный авторинг материалов.

Внутреннее выполнение Lighting и Normal Buffer относится к ранее проведённому графическому аудиту и в этом документе не пересматривается.

**Основные исходники:** `Engine.Graphics.2D/Sprites/SpriteComponent.cs`, `SpriteMaterialComponent.cs`, `SpriteRenderSystem.cs`, `SpriteRenderer.cs`, `Engine.Graphics/Materials/SpriteMaterial.cs`, `SpriteMaterialParameters.cs`.

## 4. TileSet и Tilemap

### Форматы и Content

TileSet хранит atlas definitions и tile definitions. Формат версии 1 проверяет уникальность atlas ID и tile ID, наличие atlas для каждого tile, положительные размеры тайла и относительные пути текстур внутри Assets. ID тайла 0 зарезервирован для пустой ячейки.

Tilemap версии 1 хранит один путь к TileSet, ширину, высоту и плоский массив uint ID ячеек. Количество элементов должно равняться `width × height`; путь к TileSet должен указывать на относительный `.tileset.json`. Модель не задаёт собственные слои тайлов, collision metadata или navigation metadata.

Стандартный DesktopGameHost регистрирует `JsonTileSetContentLoader` и `JsonTilemapContentLoader` наряду с загрузчиком текстур и particle effects.

### Editor

Есть отдельные `TileSetDocument` и `TilemapDocument`, панели редактирования TileSet/Tilemap, выбор TileSet, палитра и viewport preview. TilemapDocument поддерживает поклеточное изменение, групповой stroke, Undo и Redo. Соответствующие тестовые файлы присутствуют в Engine.Tests.

### Runtime

`TilemapRenderSystem` запрашивает `TilemapComponent + WorldTransform2D`, загружает TilemapDefinition и её TileSet, создаёт TilemapRenderer и передаёт ему position, scale, rotation и layer. Renderer выполняет camera culling и отправляет команды только для видимых непустых ячеек. В стандартной runtime-конфигурации размер одной ячейки в мировых единицах задаётся как `1.0f`.

TilemapComponent сериализуется и зарегистрирован стандартным набором ECS сериализаторов. `EngineGame.Render()` вызывает `Runtime.RenderTilemaps()`.

### Граница с World

`Engine.Worlds.World` имеет отдельный chunk/tile storage. Данные `.tilemap.json` через TilemapComponent не преобразуются автоматически в это chunk storage и не участвуют автоматически в ChunkStreamingService. Это два самостоятельных механизма; объединение должно быть спроектировано отдельно, если игре нужен большой потоковый tile world.

**Основные исходники:** `Engine.Graphics.2D/Tilemaps/*`, `Engine.Runtime/Tilemaps/TilemapRenderSystem.cs`, `Engine.Serialization/Content/JsonTileSetContentLoader.cs`, `JsonTilemapContentLoader.cs`, `Engine.Editor/Documents/TileSetDocument.cs`, `TilemapDocument.cs`, `Engine.Editor.UI/Workspace/TileSetEditorPanelView.cs`, `TilemapEditorPanelView.cs`, `ViewportTilemapRenderer.cs`.

## 5. Camera2D и Viewport

`Camera2D` — объект runtime-сервиса, а не ECS-компонент. Он реализует:
- World ↔ Screen преобразования;
- viewport size и zoom;
- world bounds и visibility;
- world limit и clamping;
- follow, smoothing и dead zone;
- детерминированный camera shake.

Стандартный DesktopGameHost передаёт Camera2D в EngineRuntimeServices и предоставляет её через DesktopGameHostContext. SpriteRenderSystem, TilemapRenderSystem и ParticleRenderer используют эту камеру. В базовом стандартном пути это одна общая камера host, а не система выбора активной камеры по ECS-сущности.

Editor Viewport использует собственные preview renderer-ы для сущностей, спрайтов и тайлмапов. Поэтому его preview не следует автоматически считать полным воспроизведением всех runtime post-process, lighting или UI проходов.

Тестовый исходник `Engine.Tests/Graphics/CameraTests.cs` покрывает координатные преобразования, bounds/culling, resize viewport, zoom, follow, dead zone, world limits и shake. `CameraRenderTargetTransform2DTests.cs` проверяет независимый масштаб X/Y и преобразование координат для Render Target.

**Основные исходники:** `Engine.Camera/Camera2D.cs`, `Engine.Graphics.2D/Rendering/CameraRenderTargetTransform2D.cs`, `Engine.Hosting.Desktop/DesktopGameHostContext.cs`.

## 6. Animation

Есть две связанные, но самостоятельные модели:

1. `Engine.Animation` предоставляет AnimationClip, tracks, keyframes, interpolators, AnimationPlayer, binding API и JSON serialization/file service.
2. `Engine.Graphics.2D/Sprites/SpriteAnimation` строит playback спрайтовых кадров поверх AnimationClip/AnimationPlayer. Он поддерживает Play/Pause/Stop/Reset/Seek, скорость, loop/once, FrameChanged и Completed.

Для Sprite Animation есть `.sprite-animation.json` версии 1, `SpriteAnimationDocument`, editor panel, frame list и preview. Для общих AnimationClip есть `.animation.json`, AnimationClipDocument и timeline/editor panel.

Ограничение интеграции: в EngineRuntime не обнаружена встроенная animation ECS component/system, которая автоматически находит анимации, обновляет их по игровому времени и применяет bindings к компонентам. SpriteAnimation и AnimationPlayer должны создаваться, обновляться и связываться игровым кодом. В стандартном DesktopGameHost также не обнаружен content loader для `.sprite-animation.json` или `.animation.json`; имеющиеся сервисы/сериализаторы позволяют работать с файлами напрямую.

**Практический вывод:** анимационные primitives и редакторские документы существуют, но полноценный стандартный сценарий «создать animation asset → назначить ECS-сущности → автоматически проигрывать в runtime» пока не подтверждён.

**Основные исходники:** `Engine.Animation/*`, `Engine.Graphics.2D/Sprites/SpriteAnimation.cs`, `SpriteAnimationDefinition.cs`, `SpriteAnimationDefinitionJsonSerializer.cs`, `Engine.Editor/Documents/AnimationClipDocument.cs`, `SpriteAnimationDocument.cs`, соответствующие editor panels.

## 7. Lighting и Normal Mapping

Повторный внутренний аудит Lighting/RenderGraph в рамках 0.6 не выполнялся. Точка интеграции 2D-материала видна в `SpriteRenderSystem`: он передаёт Albedo, Normal, Emission, параметры SpriteMaterial и NormalTransform в render command. В репозитории также присутствуют 2D Lighting providers/extensions и OpenGL Normal Buffer renderer.

Для окончательного статуса следует использовать ранее выполненную проверку Lighting/Normal Buffer и RenderGraph, а не делать вывод только по наличию компонентов или шейдеров. Этот этап не объявляет внутреннюю реализацию света повторно проверенной.

## 8. Physics 2D

`PhysicsSystem2D` реализует `IFixedUpdateSystem` и автоматически регистрируется EngineRuntime в FixedUpdate. Порядок runtime систем обеспечивает выполнение TransformHierarchySystem перед Physics, PhysicsTransformSyncSystem после Physics и SpatialSyncSystem после синхронизации физики.

В просмотренных исходниках присутствуют:
- Dynamic / Static / Kinematic PhysicsBody2D;
- AABB, Circle и Polygon shapes;
- Collider2D с trigger, collision layer/mask, offset и PhysicsMaterial2D;
- SpatialHashBroadPhase и альтернативный BruteForceBroadPhase;
- collision detection/resolution;
- DistanceJoint2D;
- substeps, damping, sleeping и contact state;
- contact/trigger enter-stay-exit events;
- 2D physics queries и raycast/overlap API.

В Engine есть сериализаторы PhysicsBody2D, Collider2D и DistanceJoint2D, validators, debug visualizer и редакторские property editors для physics shape/material. Стандартный ECS serializer registry включает эти типы.

Тестовый набор `Engine.Tests/Physics/PhysicsSystem2DTests.cs` содержит проверки движения тела, столкновений форм, масштабирования коллайдеров, триггеров, контактных событий, сна, демпфирования и ряда устойчивых contact сценариев. Отдельные тесты есть для broad phase, joints, shapes, queries и validation.

Физика 2D — одна из наиболее интегрированных игровых подсистем в текущем стандартном Runtime. Утверждение о полном покрытии всех типов столкновений, масштабов и стрессовых условий из этого статического просмотра не следует.

**Основные исходники:** `Engine.Physics/2D/*`, `Engine.Runtime/EngineRuntime.cs`, `Engine.Serialization/Types/PhysicsBody2DSerializer.cs`, `Collider2DSerializer.cs`, `DistanceJoint2DSerializer.cs`.

## 9. Particles

`ParticleRuntime` поддерживает как явное создание emitter из игрового кода, так и автоматическую синхронизацию `ParticleEffectComponent` с сущностями ECS. Он обновляет симуляцию в EngineRuntime.Update и отрисовывает активные emitter-ы через `EngineGame.Render()`.

Удаление сущности удаляет связанный emitter; изменение пути эффекта или initial burst count приводит к обновлению экземпляра. Для частиц используются общие texture resources, когда ParticleRuntime создаётся стандартным EngineRuntime.

Есть `.particle.json` loader в стандартной конфигурации DesktopGameHost, ECS serializer для ParticleEffectComponent, ParticleEffectDocument, Inspector и editor preview. Тестовые файлы покрывают симуляцию, emitter, modifiers/initializers, сериализацию и editor-действия. Ранее зафиксированная ручная проверка полного particle workflow остаётся отдельным свидетельством; в рамках этого этапа её повторно не выполняли.

Ограничения формата и поддерживаемые emitter shapes/modifiers уже указаны в общей матрице поддержки и здесь не дублируются.

**Основные исходники:** `Engine.Runtime/Particles/ParticleRuntime.cs`, `ParticleEmitterInstance.cs`, `Engine.Particles/*`, `Engine.Graphics.2D/Particles/ParticleRenderer.cs`.

## 10. UI

`Engine.UI` — самостоятельная retained-mode UI подсистема с widget tree, controls, layout, scroll views, focus, pointer/key routing, drag-and-drop, screens и overlays.

Есть полноценная авторская цепочка ресурса:
- EditorUiDocument и editor UI workspace;
- EditorUiAssetConverter;
- UiAsset / UiAssetSerializer / UiAssetFileSerializer;
- UiAssetLoader, создающий runtime widgets;
- UiActionRuntime, связывающий именованные действия UI с Engine EventBus через пользовательские IEvent.

Однако `Engine.Runtime.csproj` не ссылается на Engine.UI. `EngineGame.Render()` не вызывает UiSystem, а стандартный DesktopGameHost не создаёт его для игры. Игровой код должен самостоятельно создать `UiSystem` с Graphics, viewport size и input services, загрузить UiAsset, зарегистрировать actions и вызвать Update/Render в своём lifecycle. Это допустимая архитектурная модель отдельной подсистемы, но она означает отсутствие автоматической стандартной интеграции с EngineGame.

Тесты присутствуют для UiAssetLoader/UiActionRuntime, controls, focus, input routing, drag/drop, scroll, layout и screen manager.

**Основные исходники:** `Engine.UI/Core/UiSystem.cs`, `Engine.UI/Assets/UiAssetLoader.cs`, `Engine.UI/Actions/UiActionRuntime.cs`, `Engine.Editor/UI/Authoring/EditorUiAssetConverter.cs`, `Engine.Serialization/UI/UiAssetSerializer.cs`.

## 11. Audio

Стандартный DesktopGameHost создаёт AudioManager на базе OpenALAudioDevice и передаёт его в EngineRuntimeServices / DesktopGameHostContext.

`AudioEcsSystem` лениво создаётся EngineRuntime при наличии AudioSourceComponent или AudioListenerComponent. Он:
- создаёт и обновляет audio sources для сущностей с WorldTransform2D;
- применяет gain, pitch, looping и PlayOnStart;
- отслеживает изменение AudioPath;
- рассчитывает velocity по изменению позиции;
- поддерживает только один включённый AudioListenerComponent одновременно;
- сбрасывает listener при его отсутствии.

EngineGame.Update вызывает Runtime.Update, поэтому audio ECS path выполняется в стандартном игровом цикле. Сериализаторы AudioSourceComponent и AudioListenerComponent зарегистрированы в RegisterEngineComponents.

Форматный предел уже документирован: AudioLoader поддерживает WAV PCM в указанных матрицей комбинациях каналов и разрядности; универсальной audio content pipeline или большого набора кодеков текущая реализация не предоставляет.

**Основные исходники:** `Engine.Runtime/AudioEcsSystem.cs`, `Engine.Audio/AudioManager.cs`, `Engine.Audio/Loading/AudioLoader.cs`, `WavAudioDecoder.cs`.

## 12. Navigation и AI

### Navigation

`Engine.Navigation` предоставляет:
- NavigationGrid;
- ChunkedNavigationMap;
- traversability и traversal cost;
- NavigationQueryService;
- path query/result;
- AStarPathfinder;
- возможность отправить path query в JobScheduler.

NavigationGrid и ChunkedNavigationMap — самостоятельные структуры. Прямой адаптер из TilemapDefinition, Engine.World chunk data или физических collider-ов не найден. Также не обнаружены стандартные navigation asset/document/editor workflows и регистрация NavigationQueryService в EngineRuntime.

Дополнительная деталь реализации: текущий AStarPathfinder использует стоимость накопленного пути как приоритет очереди (`fScore = newCost`) без эвристической добавки. По фактическому порядку поиска это Dijkstra-style поиск, хотя класс называется AStarPathfinder. Это не само по себе доказательство неверного результата, но означает отсутствие ожидаемого heuristic prioritization и требует учитывать стоимость поиска на больших картах.

### AI

`Engine.AI` предоставляет AISystem, AIScheduler, perception sensors, memory, Behavior Trees и Utility AI. AISystem реализует `IUpdateSystem` и может быть зарегистрирован в пользовательском Scheduler через ConfigureRuntime. Но EngineRuntime не создаёт AISystem по умолчанию и не связывает его автоматически с NavigationQueryService.

**Практический вывод:** API для AI и pathfinding есть, но игровой проект должен самостоятельно создавать карты, синхронизировать их с миром, регистрировать AISystem, назначать behavior и решать, как агент получает и выполняет маршрут.

**Основные исходники:** `Engine.Navigation/Maps/*`, `Engine.Navigation/Pathfinding/*`, `Engine.AI/Systems/AISystem.cs`, `Engine.AI/Perception/*`, `Engine.AI/BehaviorTrees/*`, `Engine.AI/DecisionMaking/*`.

## 13. Обнаруженный вопрос границ проектов

В текущем `master` обнаружено несоответствие между исходниками `Engine.Runtime` и явными ссылками его project file:

- `Engine.Runtime/EngineRuntime.cs` использует типы из Engine.Graphics.2D, включая SpriteRenderSystem и 2D component types;
- `Engine.Runtime/Tilemaps/TilemapRenderSystem.cs` использует типы TileSet/Tilemap из Engine.Graphics.2D;
- `Engine.Runtime/Particles/ParticleRuntime.cs` использует типы Engine.Particles;
- при этом `Engine.Runtime/Engine.Runtime.csproj` явно ссылается на Engine.Graphics, Physics и другие проекты, но не содержит ProjectReference на Engine.Graphics.2D или Engine.Particles;
- в просмотренных прямых зависимостях Engine.Graphics и Engine.Tooling эти две зависимости также не обнаружены.

Это статическое несоответствие project references и потребляемых типов. В рамках 0.6 сборка не запускалась, поэтому я не объявляю build failure подтверждённым. Перед дальнейшей реализацией необходимо проверить фактическую сборку Engine.Runtime и всей solution, а затем явно определить, где должны проходить зависимости Graphics2D/Particles. Не следует скрывать этот вопрос дополнительными случайными ссылками из игровых или тестовых проектов.

## 14. Тестовое покрытие и непроверенные сценарии

В репозитории присутствуют соответствующие тестовые исходники:

| Область | Примеры тестовых файлов |
|---|---|
| Sprite | `Engine.Tests/Graphics/SpriteRenderSystemTests.cs`, `Engine.Tests/Graphics2D/SpriteRendererTests.cs` |
| Tilemap | `Engine.Tests/Graphics/TilemapRenderSystemTests.cs`, `TilemapRendererTests.cs`, `Engine.Tests/Content/TilemapContentLoaderTests.cs` |
| Camera | `Engine.Tests/Graphics/CameraTests.cs`, `Engine.Tests/Graphics2D/CameraRenderTargetTransform2DTests.cs` |
| Animation | `Engine.Tests/Animation/AnimationPlayerTests.cs`, `Engine.Tests/Graphics2D/SpriteAnimationPlaybackTests.cs`, document/serializer tests |
| Physics | `Engine.Tests/Physics/PhysicsSystem2DTests.cs`, `PhysicsQuery2DTests.cs`, shape/joint/broadphase tests |
| Particles | `Engine.Tests/Particles/ParticleEffectSimulationTests.cs`, emitter, loader, serializer и Editor tests |
| UI | `Engine.Tests/UI/UiInputRouterTests.cs`, `UiActionRuntimeTests.cs`, `UiAssetLoaderTests.cs`, controls/layout/screen tests |
| Audio | `Engine.Tests/Audio/AudioEcsSystemTests.cs`, `AudioManagerTests.cs`, `WavAudioDecoderTests.cs` |
| Navigation / AI | `Engine.Tests/Navigation/*`, `Engine.Tests/AI/*` |

Наличие этих файлов подтверждает существование автоматизированных сценариев, но не их текущий результат. На этом этапе не запускались dotnet build, dotnet test, Engine.Sandbox или отдельная пользовательская игра.

Отдельно остаются неподтверждёнными полные сквозные сценарии:
- анимационный asset назначается сущности и автоматически воспроизводится без пользовательского кода;
- UI asset автоматически создаётся, обновляется, рендерится и управляет вводом стандартным EngineGame;
- Tilemap связана с chunk streaming, collision и navigation без отдельной синхронизации;
- Navigation и AI совместно работают на одной карте в реальном игровом проекте;
- полный 2D workflow работает одинаково в Editor preview, Game.Sandbox и внешнем опубликованном игровом проекте.

## 15. Приоритеты дальнейшей работы

По результатам аудита рекомендуется следующий порядок, без предварительного переписывания существующих подсистем:

1. **Проверить сборочные зависимости Engine.Runtime.** Подтвердить или опровергнуть найденное отсутствие project references к Engine.Graphics.2D и Engine.Particles. Это первичный вопрос до изменений Runtime API.
2. **Определить требуемый стандартный уровень интеграции Animation.** Если ожидается ECS-driven workflow, согласовать минимальный component/binding/update path и только затем планировать реализацию.
3. **Решить, нужен ли встроенный UI lifecycle в EngineGame.** Сейчас UI может оставаться opt-in подсистемой; важно определить ожидаемый workflow, а не добавлять автоматическую интеграцию без требования.
4. **Описать источник Navigation данных.** Для tile-based игры решить, должен ли navigation grid строиться из Tilemap, chunk storage, physics или игрового кода; универсальная связь не должна предполагаться автоматически.
5. **Проверить авторинг Sprite Materials.** Уточнить, достаточно ли существующего Inspector workflow для Normal/Emission и параметров материала.
6. **После этого выполнить практический 2D integration pass** в отдельной игре: сцена, камера, sprite/material, tilemap, animation, physics, particles, UI, audio и pathfinding, с отдельной проверкой Editor preview и опубликованного runtime.

## 16. Заключение

У VibeEngine2 уже есть интегрированное 2D ядро для рендеринга спрайтов и тайлмапов, физики, частиц и аудио. Самые заметные пробелы находятся не в отсутствии низкоуровневых библиотек, а в связях между подсистемами и стандартном игровом workflow: Animation, UI, Navigation/AI и единая модель tile world пока требуют явной композиции игровым кодом.

Следующий этап общего аудита — **0.7. 3D Engine**. Обнаруженное несоответствие project references следует проверить отдельным build/review шагом до расширения Runtime.
