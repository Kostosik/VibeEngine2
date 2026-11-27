# Матрица поддержки и известных ограничений VibeEngine2

Документ фиксирует подтверждённые рабочие сценарии, ограничения, прямо указанные в API-документации, и области, для которых пока нельзя сделать общий вывод о готовности.

**Правило интерпретации:** наличие кода или проекта не означает, что реализованы все ожидаемые сценарии. Статус «проверено» относится только к указанному сценарию и не распространяется автоматически на всю подсистему. Неизвестное или не проверенное не следует называть ни поддерживаемым, ни неподдерживаемым без отдельной проверки.

## Значения статусов

| Статус | Что означает |
|---|---|
| **Подтверждённый сценарий** | Сценарий подтверждается названным автоматическим тестом или зафиксированным результатом практической проверки в `docs/audits/` либо `docs/READINESS_REPORT.md`. Это не гарантирует полноту проверки всей подсистемы. |
| **Поддерживается с ограничениями** | Документация прямо описывает условия, допустимые форматы или поведение, на которые должен рассчитывать разработчик. |
| **Требует отдельной проверки** | Реализация или API существуют, но доступных свидетельств недостаточно, чтобы объявить всю возможность готовой для произвольного производственного сценария. |
| **Не поддерживается** | Применяется только к конкретному сценарию, если он явно исключён текущей реализацией. Не выводить такой статус только из отсутствия тестов или документации. |

## Подтверждённые рабочие сценарии

Аудит матрицы сверяет документацию и тестовые исходники в текущем `master`. Проверка наличия теста и его утверждений не равнозначна повторному запуску тестового набора в рамках этого аудита; исторические результаты практических проверок следует читать с датой и областью проверки.

| Область | Что подтверждено | Свидетельство и границы вывода |
|---|---|---|
| Сборка и повторный запуск игрового проекта | Тестовый игровой проект запускается, ошибка компиляции отображается как ошибка, после исправления исходника проект повторно запускается. | [`GameProjectRunnerBuildTests.cs`](../Engine.Tests/Editor/GameProjectRunnerBuildTests.cs) — `Restart_BuildsUpdatedSource_AndRecoversAfterBuildFailure`. Подтверждён этот путь восстановления, а не любая возможная ошибка сборки. |
| Ошибка при выполнении игры | При необработанном исключении тестовый процесс выводит сообщение исключения и путь к `Program.cs`; ошибка помечается как ошибка, после исправления исходника возможен повторный запуск. | [`GameProjectRunnerBuildTests.cs`](../Engine.Tests/Editor/GameProjectRunnerBuildTests.cs) — `Start_RuntimeException_ReportsExceptionAndStackTrace`. Дополнительно сценарий новой сессии покрывает `SessionManager_RuntimeFailure_ReportsErrorAndAllowsNewSession`. |
| Восстановление сессии после runtime-сбоя | После ошибки запуска сессии можно исправить исходники и создать новую рабочую сессию. | `SessionManager_RuntimeFailure_ReportsErrorAndAllowsNewSession`. |
| Вывод сообщения с исключением | Форматтер сохраняет текст обычного сообщения и добавляет представление исключения, включая stack trace, если исключение передано. | [`EditorConsoleMessageFormattingTests.cs`](../Engine.Tests/Editor/EditorConsoleMessageFormattingTests.cs) — `FormatText_WithException_IncludesMessageAndStackTrace` и `FormatText_WithoutException_ReturnsOriginalMessage`. Это проверяет форматирование сообщения, а не все способы доставки диагностики в UI. |
| Камера | Историческая запись фиксирует проверку игрового workflow камеры. Отдельные автоматические тесты проверяют координатные преобразования, viewport, границы видимости, zoom и обработку неверных параметров. | [`CameraTests.cs`](../Engine.Tests/Graphics/CameraTests.cs) — `WorldCenterMapsToScreenCenter`, `ChangingViewportUpdatesWorldBoundsAndScreenCenter`, `WorldBoundsAndVisibilityFollowCameraPositionAndZoom`, `ScreenToWorldIsInverseOfWorldToScreen`. Историческая запись фиксирует workflow, но перечисленные unit-тесты сами по себе не доказывают всю интеграцию камеры с каждой сценой и backend. |
| Частицы | Историческая запись отмечает проверенным рабочий путь ресурса `.particle.json`, назначения эффекта сущности, обновления/отрисовки в runtime и сохранения настроек. Автоматические тесты отдельно проверяют симуляцию частиц, формы эмиссии, модификаторы, сериализацию ресурса и некоторые editor-действия. | [`ParticleEffectSimulationTests.cs`](../Engine.Tests/Particles/ParticleEffectSimulationTests.cs), [`ParticleEffectDefinitionJsonSerializerTests.cs`](../Engine.Tests/Content/ParticleEffectDefinitionJsonSerializerTests.cs), [`ParticleEffectDocumentTests.cs`](../Engine.Tests/Editor/ParticleEffectDocumentTests.cs), [`ParticleEffectInspectorTests.cs`](../Engine.Tests/Editor/ParticleEffectInspectorTests.cs). Сквозной workflow отмечен в исторической записи; по результатам этого аудита отдельный единый end-to-end тест всего пути не найден. |
| Hot Reload C# через `dotnet watch` | Автоматический тест подтверждает применение изменения тела метода без смены PID, продолжение наблюдения после ошибки компиляции и применение исправления; отдельное изменение, требующее перезапуска, увеличивает итерацию запуска. Также проверено, что наблюдатель остаётся активен после завершения самого приложения и повторно запускает его после изменения файла. | [`DotnetWatchHotReloadTests.cs`](../Engine.Tests/Editor/DotnetWatchHotReloadTests.cs) — `DotnetWatch_AppliesMethodBodyChange_WithoutRestartingApp` и `DotnetWatch_RemainsWatchingAfterApplicationExits`. Это тестирование `dotnet watch` на временном .NET-проекте, а не гарантия Hot Reload для всех изменений C# или всех конфигураций Editor. |
| Интеграционный путь игрового проекта | Историческая запись отмечает проверенным путь создания/подключения игрового проекта, сборки, запуска, остановки и повторного запуска, включая проверку работы вне репозитория исходников движка. Автоматические тесты покрывают отдельные части этого пути: запуск и восстановление проекта, работу процесса/сессий, проверку состава runtime и восстановление сцены со спрайтом. | Историческая запись в `docs/READINESS_REPORT.md`; [`GameProjectRunnerBuildTests.cs`](../Engine.Tests/Editor/GameProjectRunnerBuildTests.cs), [`EngineRuntimeBuilderTests.cs`](../Engine.Tests/Editor/EngineRuntimeBuilderTests.cs), [`SceneRenderingIntegrationTests.cs`](../Engine.Tests/Integration/SceneRenderingIntegrationTests.cs). Историческая запись является свидетельством выполненной сквозной проверки; перечисленные автоматические тесты покрывают отдельные аспекты и не доказывают совместимость со всеми вариантами публикации. |
| Производительность проверенных подсистем | В историческом отчёте зафиксирован Release baseline для ECS, JobScheduler, Content Pipeline и Engine.Runtime; проверены время выполнения, аллокации, GC и память при создании/освобождении ECS World. В репозитории есть отдельные бенчмарки Runtime, физики, графики, тайлового рендеринга и Transform Hierarchy. | Историческая запись в `docs/READINESS_REPORT.md`; проекты [`Engine.Benchmarks`](../Engine.Benchmarks/) и соответствующие исходники. В рамках этой сверки сохранённый результат benchmark заново не воспроизводился; вывод относится к ранее измеренным сценариям и конфигурации, а не ко всем нагрузкам и платформам. |

### Как трактовать покрытие тестами

В историческом отчёте о готовности записан baseline покрытия **47,96% строк и 39,17% ветвей** на момент измерения. Эти проценты не являются показателем готовности каждой функции: они не говорят, какие именно пользовательские сценарии проверены, а какие нет. При повторной оценке следует обновлять показатели с датой и конфигурацией запуска.

## Результаты аудита Editor (2026-11-27)

Статический аудит Editor зафиксирован в [docs/audits/01-EDITOR.md](audits/01-EDITOR.md). В рамках аудита были просмотрены исходники Engine.Editor, Engine.Editor.UI, Editor.Sandbox и тестовые файлы Engine.Tests/Editor. Сборка и тесты не запускались.

| Область | Подтверждённое состояние | Ограничение / граница |
|---|---|---|
| Документы | Общий IEditorDocument; открытие и сохранение World, Prefab, UI, Tilemap, TileSet, Sprite Animation, Animation Clip, Particle Effect и Script Definition; dirty-close workflow обрабатывает AllDocuments | Сессия Editor, сохраняемая через EditorContext.CaptureSessionState, включает только World documents |
| Undo / Redo | История команд подключена к World, UI Document, Tilemap и Prefab | В стандартном MainShell Undo/Redo не подключён к Sprite Animation, Animation Clip, TileSet, Particle Effect и Script Definition |
| Hierarchy | ECS-иерархия использует EntityName и TransformParent2D; доступны Create/Rename/Delete и расширяемые hierarchy actions | Проверенный UI selection path выбирает одну сущность; Inspector также требует ровно одну выбранную сущность |
| Inspector | Компоненты и property editors регистрируются; изменение поддерживаемого свойства World-компонента проходит через undoable command | Добавление компонента требует явной регистрации и factory; ECS reflection provider перечисляет public instance properties, а не произвольные поля |
| Viewport | Подтверждён 2D workflow: pan/zoom, hit test, Move/Rotate/Scale gizmos | 3D Scene View в проверенном Editor UI не обнаружен |
| Workspace/session | Модели состояния, JSON persistence и тесты существуют | SaveWorkspace/LoadWorkspace и SaveSession/LoadSession не подключены к стандартному запуску Editor.Sandbox; стандартный Editor сохраняет путь к последнему игровому проекту |

## Результаты аудита Content Pipeline (2026-11-27)

Статический аудит зафиксирован в [docs/audits/02-CONTENT-PIPELINE.md](audits/02-CONTENT-PIPELINE.md). Проверены Editor Asset Browser, AssetPath, файловый источник, ContentCatalog, загрузчики, кэш и ownership, асинхронная загрузка, Hot Reload, TextureResourceManager, AudioResourceManager и доставка Assets в output/publish. Сборка и тесты не запускались.

| Область | Подтверждённое состояние | Ограничение / граница |
|---|---|---|
| Asset Browser | Навигация, поиск, фильтры, сортировка, создание ресурсов и image preview | Это filesystem view, не ContentCatalog; категории шире runtime loader support; preview использует отдельное чтение и GPU texture |
| FileAssetSource | Sync/async чтение, защита от выхода за корень и symbolic links/reparse points | AssetPath сам по себе не валидирует traversal; безопасность зависит от IAssetSource |
| FileContentCatalog | Рекурсивный snapshot с extension, size и last-write metadata; неудачный Refresh сохраняет прошлый snapshot | Нет stable GUID, import settings, importer version, content hash или derived artifact database; Refresh вызывается явно |
| ContentManager | Кэш по Type + AssetPath, external/dependency references, cycle detection, sync/async load, ownership-aware Unload и ограниченный Reload | Reload требует одной внешней ссылки и отсутствия dependencies; ClearCache — принудительная очистка всего кэша |
| Default loaders | Desktop Host регистрирует TextureData, ParticleEffectDefinition, TileSetDefinition и TilemapDefinition; game template дополнительно регистрирует UiAsset | Prefab и Script Definition loaders существуют, но не регистрируются по умолчанию; custom loaders подключаются через ConfigureContentLoaders |
| Resource lifetime | TextureResourceManager использует ContentManager для TextureData и отдельно владеет GPU handles | Content Reload не обновляет уже созданную GPU texture автоматически; AudioResourceManager работает через отдельный AudioLoader/cache |
| Hot Reload | ContentHotReloadService обнаруживает новые/изменённые файлы по metadata и позволяет явно вызвать Reload<T> | Не подключён к стандартному Host, не возвращает отдельные deletion events и не обновляет автоматически зависимости/GPU resources |
| Project output | Assets/**/* копируется в output и publish; стандартный Host читает Assets рядом с исполняемым приложением | Это доставка исходных файлов, а не import/asset compilation pipeline |

## Результаты аудита Scene и World (2026-11-27)

Статический аудит зафиксирован в [docs/audits/03-SCENE-AND-WORLD.md](audits/03-SCENE-AND-WORLD.md). Проверены ECS World, пространственный World и чанки, 2D/3D Transform, world/game save, Prefab, runtime SceneManager и связка startup scene с Play. Просмотрены исходники и тесты; сборка и тесты в рамках этого аудита не запускались.

| Область | Подтверждённое состояние | Ограничение / граница |
|---|---|---|
| ECS World | Generational EntityId, component storage, state capture/restore и валидация состояния | EntityId не является постоянным глобальным ID; custom component data требует явного serializer registration |
| Пространственный World | Составляет ECS World, ChunkRegistry, SpatialIndex, WorldPosition и chunk lifecycle | World не владеет временем жизни ECS World; default chunk persistence — MemoryChunkPersistence |
| 2D Transform | EngineRuntime автоматически запускает TransformHierarchySystem перед Physics, затем синхронизирует physics transform и spatial position | Для самостоятельного SystemScheduler порядок/регистрация остаются обязанностью приложения; отсутствующие родители и циклы отклоняются исключением |
| 3D Transform | Базовые 3D-компоненты и TransformHierarchy3DSystem присутствуют | Стандартный EngineRuntime не регистрирует 3D hierarchy system; RegisterEngineComponents не включает 3D transform serializers; полного 3D scene workflow это не подтверждает |
| .world persistence | WorldSaveLoadService сериализует ECS state и chunk/tile state; Editor использует его для World documents | Это формат состояния World, а не runtime IScene и не механизм scene transitions; custom components требуют serializer registration |
| Game save | GameSaveLoadService комбинирует World state с PersistentStateRegistry; есть GameSaveMigrationRegistry | Runtime не вызывает сервис автоматически; игра сама регистрирует persistent state и определяет момент сохранения/восстановления |
| Prefab | Есть capture, бинарная сериализация, Content loader и instantiate с ремаппингом prefab-local entity IDs | Capture строит иерархию по TransformParent2D; произвольные entity links ремапятся только компонентами IPrefabEntityReferenceRemappable |
| Chunk streaming | ChunkStreamingPolicy/Service, IChunkLoader, JobScheduler и IChunkPersistence реализованы | ChunkStreamingService не подключён стандартным EngineRuntime; стандартный persistence backend — память процесса |
| Runtime SceneManager | Lifecycle API управляет активной и ожидающей IScene, инициализацией, переходом и Dispose | SceneManager не загружает .world и не интегрирован в EngineRuntime/EngineGame; отдельный SceneManager test suite не обнаружен |
| Startup scene | Editor сохраняет относительный startupScene в .vibeproject и ограничивает путь каталогом проекта | Стандартный Editor Play не передаёт этот путь; generated GameMain не загружает сцену. Открытие manifest не проверяет существование ранее записанного startupScene |

## Результаты аудита Runtime (2026-11-27)

Статический аудит зафиксирован в [docs/audits/04-RUNTIME.md](audits/04-RUNTIME.md). Проверены EngineGame, EngineRuntime, GameLoop, SystemScheduler, Simulation, commands/events, diagnostics, ресурсное владение и Desktop Host. Также изучен тест отдельного опубликованного пользовательского проекта. Сборка, тесты и графический запуск Host в рамках аудита не выполнялись.

| Область | Подтверждённое состояние | Ограничение / граница |
|---|---|---|
| EngineGame lifecycle | ConfigureRuntime → Runtime.Initialize → OnInitialize; Update, FixedUpdate, Render и Shutdown имеют определённые точки расширения | Повторная инициализация запрещена; пользовательские подписки/ресурсы должны освобождаться игровым кодом |
| GameLoop | Fixed timestep, clamp delta, лимит fixed steps, interpolation, pause/resume и step | Стандартно 250 мс max frame delta и 8 fixed steps; остаток накопленного времени после лимита сбрасывается. Desktop Host использует эти значения по умолчанию |
| SystemScheduler | Отдельные Update/FixedUpdate планы; Before/After, проверка типов фаз, циклов и отсутствующих зависимостей | После Build регистрации блокируются; один экземпляр каждого конкретного типа системы; стандартная strategy последовательная |
| Параллельное исполнение | JobSystemExecutionStrategy умеет исполнять независимые batch через JobScheduler | EngineRuntime создаёт SystemScheduler с default sequential strategy и не предоставляет настройки стратегии через RuntimeOptions/Services |
| User components/systems | Подтверждены ConfigureRuntime registration и исполнение системы из внешней игровой сборки | Для сохранения custom ECS-компонентов требуется отдельная регистрация serializer; это не требуется для обычного ECS в памяти |
| Commands/Events | Команды обрабатываются перед fixed systems; события — после Update/FixedUpdate систем | Нет общей изоляции исключений handlers; пользователь владеет сроком жизни EventSubscription |
| Ошибки | ScriptRuntime отключает ошибочный экземпляр скрипта и репортит fault; FatalError отправляет Critical diagnostic и выбрасывает exception | Исключения обычных систем и command handlers проходят наружу и прерывают текущий проход |
| Runtime resources | Runtime освобождает собственные systems/caches и не уничтожает injected Audio/Content/Graphics services | Внешними сервисами владеет Host; игровым кодом — его собственные ресурсы и подписки |
| Desktop Host | Создаёт window, backend, input, Audio, Content, Runtime и управляет GameLoop lifecycle | В просмотренном Engine.Tests нет отдельного полного графического Host integration suite; audio backend стандартного Host напрямую связан с OpenAL |
| Независимый проект | Отдельный проект публикуется и запускается вне репозитория, использует custom dependency, Assets, custom component/system и вручную загружает .world | Тест не использует графический Desktop Host и не подтверждает автоматическую загрузку startup scene из manifest |

## Результаты аудита 2D Engine (2026-11-27)

Статический аудит исходников и тестовых файлов; сборка и тесты в этом проходе не запускались. Полный отчёт: [05-2D-ENGINE.md](audits/05-2D-ENGINE.md).

| Область | Фактическое состояние | Ограничения / границы |
|---|---|---|
| Sprite и Sprite Materials | EngineGame автоматически вызывает SpriteRenderSystem; renderer использует WorldTransform2D, Camera2D, texture resource manager и поддерживает Albedo/Normal/Emission каналы. | Для настройки всех параметров SpriteMaterialComponent отдельный специализированный Editor workflow в исходниках не найден; проверенные сценарии назначения albedo texture не доказывают полноту authoring материалов. |
| TileSet / Tilemap | Есть версии JSON форматов, стандартные Content loaders, Editor documents/panels/preview, runtime rendering и ECS serializers. | Tilemap asset не интегрирован автоматически с chunk tile storage/streaming из Engine.World. Размер тайла стандартного runtime renderer сейчас фиксирован на 1.0 world unit. |
| Camera2D | Стандартный Host передаёт Camera2D в Runtime; спрайты, Tilemap и Particles используют её. Реализованы zoom, bounds/culling, coordinate transforms, follow/dead zone, limits и shake. | Базовая runtime конфигурация использует одну общую host camera; это не ECS-система выбора активной камеры. |
| Animation | AnimationClip, AnimationPlayer, SpriteAnimation, serializers/file service и Editor documents/panels существуют. | В стандартном EngineRuntime не обнаружена ECS animation system; игровые объекты и bindings должны обновляться пользовательским кодом. Стандартные content loaders для .animation.json и .sprite-animation.json не зарегистрированы. |
| Lighting / Normal Mapping | Sprite material передаёт Normal/Emission и NormalTransform в графические команды; 2D lighting и normal-buffer код присутствует. | Внутренности Lighting/RenderGraph не проверялись повторно в 0.6; использовать результаты отдельного графического аудита и не выводить готовность только по наличию типов. |
| Physics 2D | PhysicsSystem2D включён в FixedUpdate; существуют shapes, colliders, materials, joints, contact events, queries, serializers, validation и debug visualization. | Статический аудит не подтверждает каждый возможный стрессовый сценарий и все игровые масштабы. |
| Particles | ParticleEffectComponent автоматически синхронизируется с ParticleRuntime; стандартный EngineGame обновляет и рендерит эффекты. Есть ручной emitter API, loader, serializer и Editor workflow. | Ограничения формата и доступных модификаторов остаются в таблице Particle effects ниже. |
| UI | Есть retained UI, widgets/layout/input/focus/screens, бинарный UiAsset и мост Editor → UiAsset → runtime widgets/actions. | EngineGame/EngineRuntime/стандартный Desktop Host не создают и не обновляют UiSystem автоматически; интеграция выполняется игровым кодом. |
| Audio | AudioEcsSystem автоматически обслуживает AudioSourceComponent/AudioListenerComponent в Runtime.Update; стандартный Desktop Host использует OpenAL. | Один активный listener; форматные ограничения WAV PCM перечислены ниже. |
| Navigation / AI | Существуют grid/chunk maps, path queries и jobs, AISystem, scheduler, perception, memory, Behavior Trees и Utility AI. | Отсутствует стандартная связь с Tilemap/World и автоматическая регистрация AI в EngineRuntime. Текущий AStarPathfinder задаёт приоритет очереди как накопленную стоимость пути (Dijkstra-style), без heuristic term. |
| Зависимости Engine.Runtime | Runtime исходники используют типы Engine.Graphics.2D и Engine.Particles. | В текущем Engine.Runtime.csproj явные ProjectReference на эти проекты не обнаружены. Это статическое несоответствие требует отдельной проверки сборкой; в рамках аудита build не запускался и build failure не объявляется подтверждённым. |

Наличие тестовых файлов по перечисленным областям не означает, что весь набор был запущен в рамках этого аудита. Детальные пути к исходникам и тестам, а также оставшиеся сквозные вопросы приведены в отчёте 05-2D-ENGINE.md.


## Результаты аудита 3D Engine (2026-11-27)

Статический аудит исходников, project references, полного дерева файлов и тестовых файлов; сборка, тесты и ручной запуск 3D preview в этом проходе не выполнялись. Полный отчёт: [06-3D-ENGINE.md](audits/06-3D-ENGINE.md).

| Область | Фактическое состояние | Ограничения / границы |
|---|---|---|
| Mesh и 3D rendering | `MeshData`, базовые примитивы, `MeshRenderer3D`, `MeshRenderSystem3D`, `DrawMeshCommand`, OpenGL mesh manager/renderer и dispatch path присутствуют. `Game.Sandbox` вручную собирает 3D preview. | Нет model asset importer/loader; 3D renderer не подключён к стандартному EngineRuntime и вручную запускается в Sandbox. |
| Material и lighting | `Material3D` передаёт BaseColor и ambient/diffuse strength; `SceneLighting3D` поддерживает directional light и до четырёх point lights. | Не обнаружены PBR параметры, material texture maps и shadow map workflow. |
| Camera3D | Backend-independent `Camera3D` и `Camera3DController` реализованы и имеют unit tests. | `EngineRuntimeServices` предоставляет только `Camera2D`; стандартная runtime camera integration отсутствует. |
| Transform 3D | Float/fixed components, parent links и обе hierarchy systems существуют; отдельные тесты покрывают иерархию и преобразования. | Standard Runtime запускает 2D hierarchy; 3D системы вызываются вручную. Built-in 3D transform serializers не обнаружены. |
| Physics 3D | Есть bodies, colliders, Sphere/Box/Capsule/Plane shapes, bounds/proxy, settings и pairwise `CollisionDetector3D`. | Нет PhysicsSystem3D/solver/response, broad-phase implementation, queries, joints и Runtime FixedUpdate integration. Detector обрабатывает лишь часть shape pairs. |
| Animation 3D | Generic AnimationClip/track/player/binding поддерживает применение типизированных значений через callback. | Нет skeleton/bones, skinning, inverse bind matrices и готового ECS binding workflow для skeletal animation. |
| Model import и Content | В mesh API есть CPU geometry и GPU handles. | В текущем tree не обнаружены model formats/importer или model content loader; произвольная модель не проходит готовый Content workflow. |
| Serialization | Стандартная сериализация содержит 2D-компоненты и 2D physics serializers. | Built-in serializers для 3D transforms, mesh renderer и PhysicsBody3D/Collider3D не обнаружены; scene round-trip 3D не обеспечен стандартным набором. |
| Editor | Стандартный Editor имеет 2D viewport/authoring workflow. | Нет 3D Scene View, mesh/model/material authoring, 3D gizmos и прямой зависимости Editor проектов от Graphics3D. |

Тестовые исходники найдены для Camera3D/controller, mesh data/primitives/resources, material, render pass, 3D Transform hierarchy, PhysicsBody3D и PhysicsShape3D. В просмотренном наборе не обнаружены интеграционные тесты полного OpenGL draw path, MeshRenderSystem3D, model import, 3D serialization, 3D Editor, Physics3D solver или CollisionDetector3D. Наличие файлов не означает запуск тестов в рамках этого аудита.

## Документированные ограничения

| Область | Поддерживаемое поведение / ограничение | Практическое следствие |
|---|---|---|
| Платформа и дистрибутив | GitHub Actions workflow сборки и тестов выполняется на `windows-latest` с .NET 8; workflow дистрибутива публикует Editor, Game.Sandbox и runtime под `win-x64` как framework-dependent приложение. README дистрибутива требует Windows x64 и .NET 8 Desktop Runtime x64. | Подтверждённая CI/дистрибутивная конфигурация — Windows x64 + .NET 8. Это не доказывает невозможность запуска на других платформах, но для заявления об их поддержке нужны отдельные CI и практические проверки. Основание: `.github/workflows/dotnet.yml`, `.github/workflows/build-distribution.yml`. |
| Стандартный desktop host и графический backend | `DesktopGameHost` выбирает graphics и platform providers через `DesktopGraphicsBackendRegistry` и `DesktopPlatformBackendRegistry`. `SilkNetDesktopHost` регистрирует OpenGL и Silk.NET как стандартные providers. Аудио создаётся напрямую через `AudioManager` и `OpenALAudioDevice`; отдельного audio backend registry в options нет. | Графическая и платформенная точки расширения существуют, но произвольная альтернативная конфигурация требует регистрации совместимых providers. Аудио в стандартном host жёстко связано с OpenAL. Основание: `Engine.Hosting.Desktop/DesktopGameHost.cs`, `DesktopGameHostOptions.cs`, `Engine.Graphics.OpenGL/OpenGLWindow.cs`. |
| Указатель и UI | `DesktopGameHostContext.PointerInput` имеет nullable-контракт `IPointerInput?`, поэтому пользовательский код должен учитывать возможность `null`. При этом текущий стандартный host создаёт `SilkNetInputBackend`, который реализует `IPointerInput`. | Для стандартного desktop host указатель предоставляется текущей реализацией; `null` остаётся возможным для альтернативной реализации host, но не является заявленным ограничением текущей конфигурации. Основание: `DesktopGameHostContext.cs`, `OpenGLWindow.cs`, `SilkNetInputBackend.cs`. |
| Привязки ввода | `InputActionMap.GetValue` выбирает среди привязок одно значение с наибольшим абсолютным значением; значения нескольких привязок не складываются. | Если игре нужно суммирование или другая стратегия объединения ввода, её необходимо реализовать отдельно. Основание: `Engine.Input/InputActionMap.cs`. |
| Форматы аудио | `AudioLoader` принимает расширение `.wav`; декодер поддерживает только PCM, 1 или 2 канала, 8 или 16 бит на сэмпл. Он также проверяет структуру RIFF, выравнивание блока и byte rate. Другие расширения, WAV-кодеки и комбинации параметров не поддерживаются текущим декодером. | Использовать PCM WAV в поддерживаемой комбинации или расширять загрузчик и покрывать новый формат тестами. Основание: `Engine.Audio/Loading/AudioLoader.cs`, `WavAudioDecoder.cs`, `Engine.Tests/Audio/WavAudioDecoderTests.cs`. |
| Текстуры изображений | `ImageTextureContentLoader.CanLoad` выбирает ассеты с расширениями `.png`, `.jpg`, `.jpeg`, `.bmp`, `.tga`; декодер преобразует изображение в RGBA8. | Автоматический тест [`ImageTextureLoaderTests.cs`](../Engine.Tests/Graphics/ImageTextureLoaderTests.cs) явно проверяет PNG → RGBA8. Для остальных перечисленных расширений этот тестовый файл не подтверждает отдельное декодирование каждого формата; не следует трактовать список расширений как исчерпывающую матрицу проверенных форматов. Основание: `Engine.Graphics/Resources/ImageTextureContentLoader.cs`, `ImageTextureLoader.cs`. |
| Particle effects | JSON-ресурсы с суффиксом `.particle.json`; текущая версия формата — 2. Загрузчик распознаёт формы эмиссии `point`, `circle`, `rectangle` и модификаторы `acceleration`, `drag`, `sizeOverLifetime`. | Тесты проверяют разбор корректных эффектов, все три формы, сериализацию полей, неизвестную форму/модификатор, неверную версию и некорректные данные. Основание: `Engine.Particles/ParticleEffectDefinition.cs`, `Engine.Serialization/Content/JsonParticleEffectContentLoader.cs`, `Engine.Tests/Content/ParticleEffectContentLoaderTests.cs`, `ParticleEffectDefinitionJsonSerializerTests.cs`. |
| TileSet | JSON-ресурсы с суффиксом `.tileset.json`; текущая версия формата — 1. Ресурс описывает атласы и tile ID, связанные с атласом и индексом. ID атласов и тайлов должны быть уникальными; ссылки на неизвестный атлас, абсолютные пути и пути с `.`/`..` не допускаются. | Тесты покрывают загрузку через Content Manager, сериализацию/десериализацию, путь текстуры вне Assets и неизвестный atlas ID. Основание: `Engine.Graphics.2D/Tilemaps/TileSetDefinition.cs`, `JsonTileSetDefinitionSerializer.cs`, `Engine.Tests/Content/TileSetContentLoaderTests.cs`. |
| Tilemap | JSON-ресурсы с суффиксом `.tilemap.json`; текущая версия формата — 1. Требуются положительные размеры, путь к `.tileset.json` внутри Assets и количество tile ID, равное `width × height`; ID ячеек хранятся как `uint`, `0` используется как пустая ячейка в модели tileset. | Тесты покрывают загрузку карты через Content Manager, сериализацию/десериализацию, неверное число ячеек, путь tileset вне Assets и отрицательное значение тайла. Основание: `Engine.Graphics.2D/Tilemaps/TilemapDefinition.cs`, `JsonTilemapDefinitionSerializer.cs`, `Engine.Tests/Content/TilemapContentLoaderTests.cs`, `TilemapDefinitionJsonSerializerTests.cs`. |
| JSON Script Definition | Существует loader для суффикса `.script.json`; сериализатор проверяет версию структуры определения скрипта. Сам loader не регистрируется стандартным `DesktopGameHost` по умолчанию. | Для использования необходимо зарегистрировать `JsonScriptDefinitionContentLoader` через `DesktopGameHostOptions.ConfigureContentLoaders` или собственную конфигурацию Content. Тесты: `Engine.Tests/Content/ScriptDefinitionContentLoaderTests.cs`. Наличие JSON definition loader само по себе не является доказательством поддержки произвольных форматов исполняемого кода. |

| Hot reload Content | `ContentManager.Reload<T>` отказывает, если ресурс имеет не ровно одну внешнюю ссылку, используется другими ресурсами как зависимость или уже участвует в синхронной/асинхронной загрузке. При ошибке загрузки замены старый ресурс сохраняется. | Сначала освободить дополнительные ссылки и зависимости; не выполнять reload во время активной загрузки. Это ограничение операции перезагрузки, а не обычного `Load`. Основание: `Engine.Content/ContentManager.cs`, тесты `Reload_ReplacesSoleExternalReference`, `Reload_RejectsMultipleExternalReferences`, `Reload_RejectsAssetUsedAsDependency`, `Reload_WhenReplacementFails_PreservesExistingAsset` в `Engine.Tests/Content/ContentManagerTests.cs`. |
| Бинарная сериализация | Контейнер `BinaryContainer` сверяет версию файла с `SerializationContext.FormatVersion`; стандартная версия — 1. Значение payload по умолчанию ограничено 16 MiB. | Файл с другой версией контейнера автоматически не преобразуется. Для game save предусмотрен отдельный `GameSaveMigrationRegistry`, но переход к более новой версии требует зарегистрированных миграций для каждого нужного шага. Основание: `Engine.Serialization/Binary/BinaryContainer.cs`, `SerializationContext.cs`, `SaveLoad/Game/GameSaveMigrationRegistry.cs`; тесты `BinaryContainerTests.cs` и `GameSaveMigrationTests.cs`. |
| Стандартные content loaders | `DesktopGameHost` по умолчанию регистрирует загрузчики текстур, particle effects, tilesets и tilemaps. Другие loaders, включая JSON script definitions и бинарные игровые типы, должны быть зарегистрированы приложением через `ConfigureContentLoaders` либо собственную конфигурацию Content. | Наличие loader-класса в репозитории не означает, что стандартный host автоматически зарегистрировал его. Основание: `Engine.Hosting.Desktop/DesktopGameHost.cs`, `DesktopGameHostOptions.cs`, `Engine.Serialization/Content/JsonScriptDefinitionContentLoader.cs`. |
| Время жизни аудио-буфера | `AudioResourceManager` кеширует буферы по `AssetPath`; `Load` повторно возвращает тот же буфер, а `Unload(path)` освобождает его и удаляет из кеша. `UnloadAll` и `Dispose` освобождают оставшиеся буферы. У менеджера нет счётчика внешних ссылок, поэтому он не отслеживает, продолжает ли игровой код использовать буфер после `Unload`. | Вызывать `Unload` только когда буфер больше не нужен источникам и игровому коду; не считать получение буфера отдельной учитываемой ссылкой. Основание: `Engine.Audio/Resources/AudioResourceManager.cs`, `Engine.Audio/AudioManager.cs`. |
| Иерархия Transform | `EngineRuntime` сам регистрирует `TransformHierarchySystem` в `FixedUpdate` и задаёт порядок перед `PhysicsSystem2D`. При использовании Runtime повторная регистрация не требуется. Система ожидает согласованный набор компонентов: `LocalTransform2D` и `WorldTransform2D`; некорректная ссылка на родителя или цикл приводит к `InvalidOperationException`. | При самостоятельном использовании `SystemScheduler` без `EngineRuntime` приложение должно добавить систему и настроить порядок. Тесты покрывают вложенную иерархию, влияние поворота, отсутствующего родителя и циклы: `Engine.Tests/Transform/TransformHierarchySystemTests.cs`. |
| Game.Sandbox | Sandbox — интеграционная площадка и имеет прямые ссылки на многие конкретные подсистемы и backend. | Не считать его зависимости обязательным набором ссылок для пользовательской игры и не использовать один только Sandbox как шаблон архитектуры игрового проекта. |

## Области, где требуется уточнение перед заявлением о полной поддержке

Это **не список утверждённых дефектов**. Это перечень тем, по которым в рамках текущего документа нет достаточной информации, чтобы заявить полную поддержку всех сценариев.

| Область | Что необходимо уточнить |
|---|---|
| Альтернативные графические, аудио- и input-backend | Какие реализации, помимо текущей конфигурации desktop host, реально можно подключить и какие проверки интеграции для них существуют. Сейчас документированное ограничение относится именно к стандартному host. |
| Контент и сериализация в целом | В рамках этого прохода проверены загрузчики текстур, частиц, tileset и tilemap, перечисленные в таблице ограничений ниже. Другие типы ассетов, сериализаторы игровых состояний и правила миграции нужно оценивать отдельно — общее наличие Content Pipeline не означает универсальной поддержки форматов. |
| Скриптинг | Необходимо отдельно перечислить проверенные пользовательские сценарии — регистрация в игровой сборке, назначение сущности, вызовы жизненного цикла, обработка ошибок и сохранение назначений — и не расширять вывод за пределы реально проверенных тестов. |
| Networking и Replication | Наличие сетевых API и проекта репликации не доказывает готовность любой сетевой архитектуры, транспорта, восстановления соединения, авторитетности и масштабирования. Статус следует фиксировать по конкретным требованиям и интеграционным сценариям игры. |
| Кроссплатформенность и распространение | Нужны результаты сборки и запуска для каждой платформы и конфигурации поставки, которую планируется объявить поддерживаемой. Основная среда Windows сама по себе не является заявлением об исключительной поддержке Windows или о готовности других платформ. |
| Границы готовности редактора | Готовность редактора следует подтверждать отдельно для конкретных типов документов и сценариев редактирования/сохранения. Готовность одного редактора ресурса не означает, что каждый тип ресурса имеет одинаково завершённый workflow. |

## Правило фиксации нового ограничения

Когда обнаруживается ограничение, добавляйте запись по шаблону:

- **Область и сценарий:** где именно проявляется ограничение.
- **Фактическое поведение:** что происходит сейчас, без предположений о причинах.
- **Ожидаемое поведение:** только если оно определено требованиями.
- **Свидетельство:** тест, путь к исходнику, минимальный воспроизводимый пример или результат ручной проверки.
- **Статус:** подтверждённое ограничение, проверенное поведение или открытый вопрос.
- **Обходной путь:** если он существует и проверен.

Не переносите статус с одного сценария на всю подсистему. Не объявляйте функцию «готовой» только по наличию классов, наличию тестов или успешной сборке решения.

## Связанные документы

- [Индекс проектов — где искать реализацию](PROJECT_INDEX.md)
- [Архитектура движка](ENGINE.md)
- [Руководство по API](API.md)
- [Диагностика и решение типичных проблем](TROUBLESHOOTING.md)
- [План развития движка](ENGINE_DEVELOPMENT_PLAN.md) — долгосрочная стратегия и аудит.
- [Аудит 2D Engine](audits/05-2D-ENGINE.md) — сквозные связи 2D подсистем, Editor, сериализации и Runtime.
