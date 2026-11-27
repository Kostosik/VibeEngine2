
# Аудит Scene и World

**Дата:** 2026-11-27  
**Ветка:** master  
**Исходный commit:** ce412b38739cd42bc9b7132de09151e67bee966e  
**Тип проверки:** статический аудит исходников и тестов; без изменения production-кода и тестов.

## 1. Итог

В VibeEngine2 есть отдельные механизмы ECS, пространственного мира с чанками, authoring-документа Editor, бинарного сохранения мира, игровых сохранений, Prefab и runtime-жизненного цикла сцен. Они не образуют одну автоматически связанную систему загрузки и переключения сцен.

Основное архитектурное разделение:

1. **Engine.ECS.World** — хранилище сущностей и ECS-компонентов.
2. **Engine.Worlds.World** — уровень над ECS: чанки, пространственный индекс, координаты мира и состояние чанков.
3. **EditorDocument** — authoring-документ редактора, содержащий Engine.Worlds.World и обслуживающий выбор, команды, Inspector, Viewport и валидацию.
4. **IScene<TContext> / SceneManager<TContext>** — объектная runtime-модель жизненного цикла и переходов между пользовательскими сценами. Она не загружает файлы .world сама по себе.

Сериализация .world позволяет сохранить и восстановить ECS и чанки. Отдельный GameSaveLoadService добавляет к снимку мира зарегистрированное состояние игры и миграции. Prefab позволяет вынести и повторно создать иерархию сущностей с ремаппингом поддерживаемых ссылок.

**Главное незавершённое звено — startup scene workflow.** Editor сохраняет путь стартовой сцены в .vibeproject, но стандартная команда Play запускает игровой проект без передачи этого пути. Создаваемый шаблон игры также не загружает startup scene и не подключает SceneManager. Поэтому настройка стартовой сцены сейчас является сохранённой метаинформацией, а не работающим сквозным runtime-сценарием.

Это вывод аудита интеграции, а не утверждение о поломке сериализации или ECS.

## 2. Объём и метод

Проверены:

- Engine.ECS: World, EntityStore, EntityId, ComponentStorage, snapshots и persistence state;
- Engine.World: World, chunk lifecycle/registry/streaming, spatial manager/index и WorldPersistence;
- Engine.Runtime: EngineRuntime, EngineGame, IScene и SceneManager;
- Engine.Serialization: сериализаторы ECS, World, Prefab и GameSave;
- Engine.Editor / Engine.Editor.UI: EditorDocument, сохранение и открытие .world, PrefabDocument, project manifest и startup scene;
- тесты ECS, Transform, Prefab, Save/Load, Editor и integration.

Проверялись исходники и наличие/содержание тестов. Сборка, тесты, ручной запуск Editor и пользовательского проекта в рамках этого аудита не выполнялись. Наличие теста отмечается как свидетельство покрытого сценария, но не как повторно полученный результат запуска.

## 3. Модель World и ECS

### 3.1. ECS World

Engine.ECS.World управляет:
- созданием, проверкой существования и уничтожением сущностей;
- компонентными хранилищами и запросами;
- command buffer;
- snapshots и захватом/восстановлением ECS state;
- детерминированными хранилищами и безопасностью активных jobs.

EntityId — пара Index + Generation. При освобождении индекса и его повторном использовании поколение меняется. Это защищает от случайного использования устаревшего handle внутри жизненного цикла ECS World, но не является постоянным глобальным идентификатором объекта проекта.

При CaptureState сохраняются данные EntityStore — поколения, активные и свободные индексы, следующий индекс — и состояния компонентных хранилищ. Компонентные состояния упорядочиваются по имени типа. Перед захватом/восстановлением World проверяет отсутствие активных ECS jobs.

RestoreState валидирует состояние, восстанавливает EntityStore, пересоздаёт компонентные хранилища и очищает command buffer. Валидация отсекает некорректные индексы, повторяющиеся активные/свободные индексы, нулевые поколения и некорректные ссылки компонентов на состояние сущностей.

### 3.2. Пространственный World

Engine.Worlds.World композиционно содержит:
- ссылку на отдельный Engine.ECS.World;
- ChunkRegistry и lifecycle записей чанков;
- SpatialIndex;
- SpatialEntityManager;
- IChunkPersistence.

Он не заменяет ECS World и не владеет его освобождением: в стандартном EngineRuntime оба объекта создаются отдельно, а при завершении Runtime сначала освобождается World, затем ECS World.

SpatialEntityManager связывает WorldPositionComponent с пространственным индексом. Он реагирует на уничтожение ECS-сущности и снимает её индексную запись. Не каждая ECS-сущность обязана иметь мировую позицию и присутствовать в SpatialIndex.

Это полезное разделение: ECS хранит общую игровую модель, а World добавляет пространственную и chunk-модель поверх неё.

## 4. Transform и иерархия

### 4.1. 2D

Основные компоненты:
- LocalTransform2D — локальное фиксированное преобразование;
- TransformParent2D — ссылка на родительскую сущность;
- WorldTransform2D — вычисленное мировое преобразование.

TransformHierarchySystem обходит локальные трансформации, вычисляет мировые значения по родительской цепочке и явно проверяет согласованность необходимых компонентов. Цикл, отсутствующий родитель или неполный набор transform-компонентов приводит к InvalidOperationException, а не к молчаливому пропуску.

Стандартный EngineRuntime автоматически регистрирует эту систему в FixedUpdate перед PhysicsSystem2D. После Physics выполняется PhysicsTransformSyncSystem, затем SpatialSyncSystem, которая переносит позицию из WorldTransform2D в пространственную модель World.

EditorDocument.RefreshTransforms отдельно запускает TransformHierarchySystem и SpatialSyncSystem для authoring-документа.

### 4.2. 3D

В ECS присутствуют LocalTransform3D, WorldTransform3D, TransformParent3D и TransformHierarchy3DSystem. Однако при просмотре стандартной регистрации EngineRuntime обнаружена только регистрация 2D TransformHierarchySystem. Встроенный RegisterEngineComponents также регистрирует 2D transform-компоненты и не включает 3D transform-компоненты.

В сочетании с отсутствием 3D Scene View в проверенном Editor workflow (см. аудит Editor) это означает, что наличие отдельных 3D transform-механизмов пока не подтверждает законченный 3D scene workflow. В рамках этапа аудита 3D-направления следует оценивать отдельно, не смешивая наличие базовых ECS типов с поддержкой сцены целиком.

## 5. Чанки и потоковая загрузка

Chunk-модель содержит отдельные состояния:
- residency: загружен или выгружен;
- simulation: участвует в симуляции или приостановлен;
- presentation: считается релевантным для представления или нет.

ChunkStreamingPolicy строит квадратные области preload и presentation вокруг заданного центра. ChunkStreamingService применяет политику, планирует загрузку через JobScheduler, использует IChunkLoader для получения данных, восстанавливает ранее сохранённое состояние через IChunkPersistence и выгружает невостребованные чанки. Чанк с индексированными пространственными сущностями не выгружается.

По умолчанию World использует MemoryChunkPersistence. Это внутренняя память процесса, а не долговременное хранилище на диске. Другой backend можно передать через IChunkPersistence.

Важно: EngineRuntime создаёт World, SpatialSyncSystem и ECS/physics systems, но не создаёт и не запускает ChunkStreamingService автоматически. Для потоковой загрузки игра должна самостоятельно собрать эту часть: выбрать loader, persistence, policy и scheduler, а затем вызывать сервис с актуальным streaming interest.

## 6. Authoring-сцена в Editor

Основной документ сцены — EditorDocument (DocumentTypeId = vibeengine.world). Он содержит Engine.Worlds.World, selection, command history, Inspector, Viewport, validation и editor entity references.

Файлы с расширением .world открываются через EditorDocumentOpenService и IEditorDocumentFileService. BinaryEditorDocumentPersistence использует WorldSaveLoadService для загрузки/сохранения World. То есть .world — бинарный снимок ECS + chunk state, а не сериализованный экземпляр IScene<TContext>.

Prefab оформлен отдельным PrefabDocument, не смешан с World document. Основная модель иерархии в проверенном Editor workflow основана на 2D TransformParent2D; ограничения Inspector, selection и 3D viewport описаны в аудите Editor.

## 7. Сериализация и сохранения

Существуют три связанные, но разные формы данных.

### 7.1. ECS state

EcsWorldStateSerializer записывает entity state и компоненты через явный EcsComponentSerializerRegistry. Для компонента в файле используется зарегистрированный стабильный строковый ID сериализатора; тип CLR сам по себе не становится автоматически сохраняемым.

RegisterEngineComponents регистрирует набор встроенных 2D/игровых компонентов: transform, world position, physics, sprites, tilemap, scripting assignments, audio, particles, entity name и т. д. Пользовательские компоненты требуют собственной регистрации сериализатора в проекте игры.

### 7.2. World / scene file

WorldSaveStateSerializer сохраняет chunk size, ECS state, список чанков, их позиции, lifecycle states и массивы тайлов. WorldSaveLoadService умеет сохранять World, загружать данные в существующий World или создать новый World.

Это механизм сохранения/восстановления authoring-сцены и состояния World. Он не управляет тем, какая сцена активна, не выполняет переходы и не создаёт пользовательский IScene<TContext>.

### 7.3. Game save

GameSaveLoadService использует WorldPersistence и дополнительно PersistentStateRegistry. Игра может зарегистрировать данные, не являющиеся ECS-компонентами, через ID, capture/restore delegates и собственный binary serializer.

GameSaveStateSerializer имеет отдельную версию игрового сохранения. GameSaveMigrationRegistry применяет явно зарегистрированную цепочку миграций между версиями. Автоматической миграции без регистрации нужных переходов нет.

Сам Runtime не вызывает GameSaveLoadService автоматически. Игровой код создаёт/настраивает реестр и самостоятельно решает, когда снимать или восстанавливать прогресс.

### 7.4. Ссылки на ресурсы и компоненты

Сериализатор компонента сохраняет значение самого компонента. Например, строковый путь спрайта сохраняется как часть SpriteComponent; байты текстуры в .world не встраиваются. После восстановления ресурс разрешается через соответствующую runtime Content-инфраструктуру.

Интеграционный тест SceneReload_RestoresSpriteAndRendersItsTexture демонстрирует этот раздельный путь: сериализация восстанавливает компонент со строковым путём, а renderer и ContentManager отдельно получают и отображают текстуру.

## 8. Prefab и повторное использование сущностей

PrefabAsset описывает корневую сущность, набор prefab-local entity IDs и компоненты сущностей. Формат сериализации Prefab имеет версию 1 и использует тот же явный реестр сериализаторов ECS-компонентов.

PrefabAssetCapture.Create:
- собирает выбранную корневую сущность и её потомков по TransformParent2D;
- сортирует детей детерминированно;
- назначает локальные ID Prefab;
- копирует компоненты и ремапит ссылки, если тип компонента реализует IPrefabEntityReferenceRemappable;
- отклоняет поддерживаемые ссылки на сущности за пределами выбранной Prefab-иерархии.

PrefabInstantiator.Instantiate сначала создаёт все runtime entities, затем добавляет компоненты с remapping. Это позволяет поддерживать ссылки на сущности, объявленные позже в Prefab. При заданной позиции корня пространственные WorldPosition всех сущностей сдвигаются одинаковым offset; если у корня не было WorldPosition, позиция создаётся. При ошибке созданные сущности уничтожаются, что также удаляет их spatial-index entries через событие ECS.

Ограничения:
- захват иерархии выполняется по 2D TransformParent2D; это не полный снимок произвольной сцены;
- автоматический remapping ссылок возможен только для компонентов, реализующих IPrefabEntityReferenceRemappable;
- внешние ссылки на runtime entities в составе Prefab не являются поддерживаемым способом хранения;
- custom component serializers и регистрация prefab content loader требуют явной настройки.

Editor предоставляет отдельный PrefabDocument с изменением структуры/компонентов и command history. Полный цикл Capture → Save → Content load → Instantiate покрывает PrefabAssetLifecycleIntegrationTests.CaptureSaveLoadInstantiate_PreservesHierarchyAndSpatialData.

## 9. Runtime SceneManager и переходы

IScene<TContext> — пользовательский объект с Initialize, Update, Render и Dispose. SceneManager<TContext> управляет одним активным объектом сцены и одним ожидающим переходом.

Поведение менеджера:
- начальная сцена устанавливается через Start;
- переход запрашивается через RequestScene;
- смена выполняется после завершения Update текущей сцены;
- новая сцена инициализируется перед заменой старой;
- если Initialize нового экземпляра завершается ошибкой, менеджер очищает его и сохраняет старую активную сцену;
- Dispose освобождает активную и ожидающую сцены и собирает ошибки очистки.

Это lifecycle API для игровых классов, а не готовый scene asset loader. В проверенном EngineRuntime нет встроенного SceneManager, а EngineGame не предоставляет стандартного подключения к нему или к загрузчику .world. В списке Engine.Tests не найден отдельный тестовый файл для SceneManager; существующий SceneRenderingIntegrationTests проверяет сериализацию и rendering загруженного World, но не переходы между IScene.

## 10. Startup scene: Editor → project → Play

В проектном manifest предусмотрено поле startupScene. GameProject.SetStartupScene проверяет, что файл существует, имеет расширение .world, находится внутри каталога проекта и не проходит через symlink/reparse point; затем записывает относительный путь в .vibeproject. Editor вызывает это через Tools → Set as Startup Scene и проверяет, что документ сохранён и принадлежит открытому проекту.

При открытии manifest GameProject.Open проверяет относительность, расположение и расширение startupScene, а также reparse points. Проверки фактического существования файла при чтении manifest в этом пути не обнаружено; оно проверяется setter-ом при назначении сцены. Поэтому устаревший путь, записанный ранее в manifest, может остаться допустимым при открытии проекта.

Дальше стандартная цепочка не завершена:
- EditorApplication.StartGame вызывает GameProjectRunner.Start(project, mode) без application arguments;
- GameProjectRunner запускает dotnet run / dotnet watch, но не передаёт StartupSceneRelativePath;
- generated Program.cs принимает args, но не интерпретирует их для загрузки сцены;
- generated GameMain не загружает startup .world и не создаёт SceneManager;
- в ProjectCreationService есть helper CreateEmptyStartupScene, но в текущем потоке Create() вызова helper нет; новый шаблон не устанавливает startupScene в manifest.

Следовательно, настройка startup scene уже хранится и проверяется на этапе назначения, но стандартный Play flow не использует её для загрузки мира. Это приоритетное интеграционное направление для следующей реализации сцено-workflow.

## 11. Свидетельства в тестах

| Область | Тесты / проверяемый сценарий | Что именно подтверждается |
|---|---|---|
| ECS state validation | Engine.Tests/ECS/WorldStateValidationTests.cs | Некорректный state с повторным component storage отклоняется без подмены текущих компонентов в покрытом сценарии |
| 2D Transform hierarchy | Engine.Tests/Transform/TransformHierarchySystemTests.cs | Root/child/nested hierarchy, влияние rotation, циклы, отсутствующий parent и неконсистентные компоненты |
| Runtime transform pipeline | Engine.Tests/Runtime/TransformRuntimeIntegrationTests.cs | EngineRuntime вычисляет WorldTransform2D и синхронизирует spatial position; Physics movement проходит через несколько FixedUpdate |
| ECS save/load | Engine.Tests/Serialization/SaveLoad/EcsWorldSaveLoadTests.cs | Зарегистрированные engine-компоненты проходят binary round-trip |
| World persistence | Engine.Tests/Serialization/SaveLoad/WorldSaveLoadServiceTests.cs, WorldSaveStateSerializerTests.cs, Engine.Tests/WorldTests/WorldPersistenceTests.cs | Состояние ECS/chunks/tile data round-trip, проверки размеров и структуры, отдельные invalid restore cases |
| Game save | Engine.Tests/Serialization/SaveLoad/GameSaveLoadServiceTests.cs, GameSaveMigrationTests.cs | Дополнительные persistent states, восстановление script assignments/runtime instances, migration registry |
| Prefab capture/serialize/instantiate | Engine.Tests/ECS/Prefabs/PrefabAssetCaptureTests.cs, PrefabAssetSerializerTests.cs, Engine.Tests/WorldTests/Prefabs/PrefabInstantiatorTests.cs | Локальные IDs, ссылки, иерархия, данные и обработка ошибок создания |
| Prefab integrated lifecycle | Engine.Tests/Integration/Prefabs/PrefabAssetLifecycleIntegrationTests.cs | Capture → save → Content load → instantiate, remapping hierarchy и пространственные данные |
| World reload and render | Engine.Tests/Integration/SceneRenderingIntegrationTests.cs | Сохранённый World восстанавливает SpriteComponent + WorldTransform2D; renderer разрешает texture path и создаёт draw command |
| Startup scene metadata | Engine.Tests/Editor/GameProjectStartupSceneTests.cs | Относительный путь сохраняется в manifest; проверяются выход за корень и symlink/reparse-point ограничения. Тест не подтверждает загрузку сцены командой Play |

Отсутствие отдельного SceneManager test suite — это пробел свидетельств по lifecycle transitions, а не доказательство неправильного поведения самого класса.

## 12. Сводка статусов

| Область | Статус по этому аудиту | Комментарий |
|---|---|---|
| ECS data model и generational EntityId | Подтверждённая основа | State capture/restore и валидация реализованы; EntityId не является постоянным глобальным ID |
| Editor authoring для .world | Частично | Документ, Hierarchy/Inspector/Viewport и binary save/load существуют; 3D workflow отсутствует в проверенном Editor |
| 2D Transform hierarchy | Подтверждённая основа | Автоматически подключена в EngineRuntime и проверяется unit/integration тестами |
| Chunk/spatial World | Частично | Lifecycle, index, policy, jobs и persistence abstractions есть; streaming не подключён к стандартному Runtime, persistence default in-memory |
| Binary World persistence | Подтверждённый механизм с ограничениями | Сохраняет ECS и chunks через registry; неизвестные custom components требуют serializer registration |
| Game save/progress | Частично | PersistentStateRegistry и migrations существуют, но вызов/политика сохранений остаются ответственностью игры |
| Prefab | Частично | Capture/save/load/instantiate работает для поддерживаемой модели 2D hierarchy и remappable component references |
| Runtime SceneManager | Частично | Lifecycle/transition API существует, но не загружает .world и не встроен в EngineRuntime/EngineGame |
| Startup scene end-to-end | Не завершено | Editor записывает путь, стандартный Play и generated GameMain его не потребляют |
| 3D transform scene workflow | Требует отдельной проверки | Базовые типы/система есть, но нет стандартной runtime-регистрации и встроенной сериализации в RegisterEngineComponents |

## 13. Рекомендуемая последовательность дальнейшей работы

1. **Закрыть runtime-контракт startup scene.** Определить ожидаемый путь загрузки .world в пользовательском GameMain, место передачи startup path, владение загруженным World и связь с активным runtime scene.
2. **Определить связь asset scene и IScene.** Решить, должен ли SceneManager владеть игровыми объектами/World или оставаться общим механизмом lifecycle, который использует пользовательский адаптер.
3. **Сделать сквозную проверку Play.** Проект с назначенной startup scene должен стартовать с восстановленным ECS/world state; отдельно проверить отсутствие startupScene и несуществующий файл.
4. **Отдельно оценить durable chunk streaming.** Выбирать disk/region backend по реальным требованиям игры, не менять in-memory default без необходимости.
5. **Вернуться к 3D scene workflow на этапе аудита 3D.** Не расширять 2D serialization и runtime registration на 3D автоматически без проверки контрактов.

В следующем пункте нулевого аудита следует перейти к **0.5 Runtime**: lifecycle приложения/игры, игровой цикл, порядок систем, пользовательские регистрации, ошибки, ресурсы, Host и независимый запуск игрового проекта.

## 14. Границы заключения

- Это статический аудит исходников и тестов на указанном commit.
- Тесты и сборка не запускались.
- Выводы об интеграции Play/startup scene основаны на просмотре editor runner, project template и runtime entry points.
- Открытые пункты в отчёте — направления дальнейшей проверки/реализации, а не автоматическое объявление каждого из них дефектом.
