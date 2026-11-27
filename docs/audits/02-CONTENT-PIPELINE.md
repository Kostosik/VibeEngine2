# Аудит Content Pipeline VibeEngine2 (0.3)

**Дата аудита:** 2026-11-27  
**Репозиторий:** Kostosik/VibeEngine2  
**Ветка:** master  
**Исходный снимок:** 96a78adb1e77cf7eacd745f72d911b3ad89645c5  
**Статус:** статический аудит исходников и тестовых файлов завершён.

## 1. Цель и границы

Проверен жизненный цикл ресурсов VibeEngine2:

- Asset Browser и представление файлов проекта;
- AssetPath, IAssetSource и файловый источник;
- ContentCatalog и ContentAsset metadata;
- loader registry и механизмы загрузки;
- generic importer/processor pipeline;
- синхронная и асинхронная загрузка;
- кэширование, ссылки и зависимости;
- освобождение и повторная загрузка;
- Content Hot Reload;
- связь Content с TextureResourceManager и другими ресурсными подсистемами;
- копирование Assets в игровой output и публикацию;
- фактическая регистрация загрузчиков стандартным Desktop Host и шаблоном игрового проекта.

Проверялись исходники Engine.Content, Engine.Core/Assets, Engine.Graphics/Resources, Engine.Serialization/Content, Engine.Audio, Editor Asset Browser, Desktop Host, генератор игрового проекта, BuildTools и связанные тесты.

Сборка, запуск и выполнение тестов в рамках задачи 0.3 не проводились. Выводы об автоматическом покрытии означают наличие соответствующих тестовых сценариев в репозитории, но не повторный запуск тестов.

Код и тесты не изменялись.

## 2. Итоговая оценка

| Область | Оценка | Основание |
|---|---|---|
| Asset Browser | Готов с ограничениями | Навигация, поиск, фильтры, сортировка, создание и выделение файлов; preview реализован только для изображений |
| Asset identity и metadata | Частично | Используются относительные AssetPath и файловые metadata; отдельного стабильного GUID и import metadata database не обнаружено |
| FileAssetSource | Готов | Синхронное и асинхронное чтение, проверка выхода за корень и reparse points, отдельные тесты на traversal и symbolic links |
| FileContentCatalog | Готов с ограничениями | Рекурсивный filesystem snapshot с path/extension/size/last-write; обновление вызывается явно |
| ContentLoaderRegistry | Готов с ограничениями | Типизированный поиск загрузчика; неоднозначные совпадения обнаруживаются во время Resolve |
| ContentManager | Сильная основа | Кэш, внешние и dependency references, циклы, sync/async загрузка, очистка и Reload |
| Importer/Processor pipeline | Частично | Общие интерфейсы и ContentPipeline существуют; стандартные loaders преимущественно реализованы напрямую как IContentLoader |
| Стандартная регистрация форматов | Частично | Desktop Host регистрирует текстуры, Particle Effect, TileSet и Tilemap; дополнительные типы требуют регистрации приложения |
| Async и cancellation | Готов с оговорками | In-flight запросы объединяются; отмена отдельного ожидающего запроса не обязательно прерывает общую загрузку |
| Content Hot Reload | Частично | Есть polling по metadata и явный Reload<T>; нет автоматического применения изменений и полноценной интеграции с Host/графическими ресурсами |
| Ресурсные менеджеры подсистем | Разделены | TextureResourceManager использует ContentManager; AudioResourceManager читает через IAssetSource отдельно |
| Assets в игровом output | Готов | Шаблон проекта копирует Assets/**/* в output и publish; Desktop Host использует Assets рядом с исполняемым приложением |

Главный вывод: у VibeEngine2 есть зрелый runtime-механизм загрузки и владения типизированными ресурсами, но пока нет единого редакторского import workflow и единой системы жизненного цикла для всех типов ресурсов.

## 3. Asset Browser: файловый workflow редактора

### 3.1. Источник данных

Editor Asset Browser использует отдельные интерфейсы:
- IEditorAssetSource;
- FileSystemAssetSource;
- EditorAssetBrowser;
- EditorAssetBrowserViewState.

FileSystemAssetSource перечисляет папки и файлы текущего каталога. EditorAssetBrowser управляет:
- корнем Assets;
- текущим каталогом;
- историей переходов назад/вперёд;
- выделением;
- обновлением списка;
- навигацией вверх и ограничением пути корнем Asset Browser.

UI AssetBrowserPanelView добавляет:
- breadcrumbs;
- list/grid view;
- строку поиска;
- сортировку по имени;
- фильтр категорий;
- контекстное меню и создание ресурса;
- панель предпросмотра.

Это файловый браузер проекта, а не представление ContentCatalog. Он не запрашивает у ContentLoaderRegistry список загрузчиков и не показывает, может ли ContentManager загрузить выбранный файл в runtime-тип.

### 3.2. Категории файлов

EditorAssetTypeClassifier определяет категории по расширениям:

- Image;
- Audio;
- Video;
- Font;
- Script;
- Model;
- Data;
- Other.

Это классификация для интерфейса и фильтрации, а не матрица runtime-поддержки.

Например:
- Editor классифицирует GIF и WebP как Image, но ImageTextureContentLoader не объявляет эти расширения среди поддерживаемых.
- Editor классифицирует OGG, MP3, FLAC, AAC и M4A как Audio, но текущий AudioLoader работает с WAV и имеет более узкие ограничения декодирования.
- Model, Video и Font представлены как категории Asset Browser, но стандартных Content loaders для этих категорий не найдено.

Разработчик не должен трактовать видимую категорию или наличие файла в Assets как гарантию его загрузки в runtime.

### 3.3. Preview и metadata

EditorAssetInspector формирует простые файловые сведения:
- полный путь;
- имя;
- расширение;
- размер;
- время изменения;
- тип предпросмотра.

EditorAssetPreviewPanelView отображает image preview, если файл классифицирован как изображение. Для него используется отдельный ImageTextureLoader, прямое File.ReadAllBytes и временная текстура через ITextureManager. Предпросмотр освобождает собственную текстуру при смене ресурса или закрытии.

Preview не проходит через ContentManager и не использует его кэш. Для прочих категорий показываются metadata и сообщение об отсутствии preview.

### 3.4. Создание файлов

EditorAssetCreationRegistry предоставляет шаблоны для Folder, TileSet, Prefab, Tilemap, Particle Effect, Animation Clip, Sprite Animation, Script Definition, World и UI Document.

EditorAssetCreationService:
- проверяет имя и расширение;
- создаёт текстовый файл через CreateNew, не перезаписывая существующий;
- поддерживает generated files;
- проверяет, что целевой путь находится внутри логического корня Asset Browser;
- обновляет и выбирает созданный элемент.

EditorAssetCreationWorkflow связывает шаблон с конкретной записью документа, если соответствующий тип поддержан.

Это полезный authoring workflow, но создание файла не является импортом. Для обычных ресурсов не создаются import settings, отдельная запись asset database или сгенерированный импортированный артефакт.

## 4. AssetPath и файловый источник

### 4.1. AssetPath

AssetPath является строковым относительным идентификатором пути. Конструктор:
- нормализует обратные слеши в прямые;
- убирает разделители по краям;
- не выполняет самостоятельную полную проверку path traversal.

Безопасность пути зависит от конкретного IAssetSource или специализированного resolver.

### 4.2. FileAssetSource

FileAssetSource реализует IAssetSource и IAsyncAssetSource.

Поддерживаются:
- Load — чтение файла в память;
- LoadAsync — асинхронное чтение файла с CancellationToken;
- вычисление полного пути относительно корня;
- отказ для пути, выходящего за корень;
- проверка корня и существующих сегментов пути на symbolic links / reparse points;
- FileNotFoundException для отсутствующего файла.

В репозитории есть отдельные тесты:
- FileAssetSourceTests.LoadAndLoadAsync_RejectSymbolicLinkAssetRoot;
- FileAssetSourceTests.LoadAndLoadAsync_RejectSymbolicLinkOutsideAssetRoot;
- FileAssetSourceTests.LoadRejectsPathOutsideAssetRoot;
- FileAssetSourceTests.LoadAsyncRejectsPathOutsideAssetRoot;
- FileAssetSourceTests.LoadRejectsPathOnDifferentDrive;
- успешное чтение и обработка отсутствующего файла.

Это подтверждённая защитная граница runtime-файлового источника по содержимому тестов. Выполнение этих тестов в текущем аудите не повторялось.

### 4.3. Отличие Editor path validation

EditorProjectAssetPathResolver и EditorAssetCreationService проверяют логический выход пути за корень по Path.GetFullPath / Path.GetRelativePath.

При этом Editor FileSystemAssetSource и EditorAssetBrowser не выполняют эквивалентную проверку reparse points на каждом сегменте пути, которую выполняет FileAssetSource.

Это означает, что Editor filesystem traversal и runtime file loading используют разные политики symbolic link handling. Это не доказывает конкретную уязвимость, но является подтверждённым различием границ проверки и требует отдельного решения, если одинаковая модель безопасности редактора и runtime является требованием.

## 5. ContentCatalog и metadata

### 5.1. FileContentCatalog

FileContentCatalog сканирует корневой каталог и подкаталоги, создавая ContentAsset для каждого обнаруженного файла.

ContentAsset содержит:
- AssetPath;
- Extension;
- SizeBytes;
- LastModifiedUtc.

Refresh строит новый Dictionary и публикует его только после успешного сканирования. Если корневой каталог исчезает и сканирование завершается ошибкой, прежний snapshot сохраняется.

В тестах это отражено сценарием ContentPipelineTests.Refresh_WhenRootDisappears_PreservesPreviousSnapshot.

### 5.2. Границы текущих metadata

В ContentAsset не обнаружены:
- стабильный GUID ресурса;
- import settings;
- версия импортёра;
- hash исходного содержимого;
- путь к импортированному derived artifact;
- editor-specific metadata;
- прямой список статических asset dependencies.

Каталог описывает filesystem inventory, а не полноценную asset database.

Идентичность ресурса в ContentManager строится по паре Type + AssetPath. Переименование или перемещение меняет AssetPath и, следовательно, ключ кэша.

### 5.3. Явное обновление snapshot

FileContentCatalog не является файловым watcher. Изменения каталога видны после вызова Refresh.

ContentManager при загрузке запрашивает IContentCatalog.TryGet и не выполняет автоматическое сканирование каталога. Следовательно, новый файл не станет доступен для Content.Load<T>, пока каталог не обновлён.

## 6. Загрузчики и импорт

### 6.1. Loader contracts

IContentLoader<T> определяет:
- AssetType;
- CanLoad(ContentAsset);
- Load(path, context);
- LoadAsync(path, context, cancellationToken).

IContentLoadContext предоставляет:
- чтение исходных байтов;
- синхронную и асинхронную загрузку других типов ресурсов как dependencies.

ContentLoaderRegistry хранит loaders в списке. При разрешении типа и файла он:
1. отбирает loaders с точным AssetType;
2. вызывает CanLoad;
3. возвращает единственное совпадение;
4. бросает InvalidOperationException, если совпадений больше одного.

Register не проверяет неоднозначность заранее. Конфликт становится виден в момент фактического запроса ресурса.

### 6.2. Generic importer/processor model

В Engine.Content существуют:
- IContentImporter<T>;
- IContentProcessor<TSource, TResult>;
- ContentPipeline<TSource, TResult>.

ContentPipeline объединяет чтение исходного файла, Import и Process, затем предоставляет результат как IContentLoader<TResult>.

В Content.Serialization также существует BinaryContentImporter<T> и BinaryContentLoader<T>, который использует IBinarySerializer<T> и SerializationContext.

При этом generic ContentPipeline не является универсальной редакторской импорт-системой:
- импортирование и обработка не создают отдельные persistent import artifacts;
- в модели ContentAsset отсутствуют import settings;
- нет версии импортёра и механизма повторной генерации derived resources;
- async ContentPipeline асинхронно читает байты, но Import и Process остаются синхронными.

Стандартные Host loaders преимущественно реализуют IContentLoader<T> напрямую.

### 6.3. Фактические loaders стандартного Desktop Host

DesktopGameHost создаёт ContentLoaderRegistry и регистрирует:

| Loader | Runtime type | Формат |
|---|---|---|
| ImageTextureContentLoader | TextureData | PNG, JPG, JPEG, BMP, TGA |
| JsonParticleEffectContentLoader | ParticleEffectDefinition | .particle.json |
| JsonTileSetContentLoader | TileSetDefinition | .tileset.json |
| JsonTilemapContentLoader | TilemapDefinition | .tilemap.json |

После регистрации этих четырёх loaders вызывается DesktopGameHostOptions.ConfigureContentLoaders. Пользовательское приложение может добавить собственные loaders до создания ContentManager.

Generated Program.cs игрового проекта дополнительно регистрирует BinaryContentLoader<UiAsset> для .ui.

### 6.4. Loaders, не подключённые стандартным Desktop Host

В репозитории также существуют:
- PrefabAssetContentLoader для .prefab;
- JsonScriptDefinitionContentLoader для .script.json;
- общий BinaryContentLoader<T> для пользовательских сериализуемых типов.

Эти loaders не регистрируются в стандартном DesktopGameHost по умолчанию. Generated game template также не регистрирует PrefabAssetContentLoader или JsonScriptDefinitionContentLoader.

Наличие loader-класса или возможности открыть документ в Editor не означает, что тот же файл автоматически доступен через Content.Load<T> в игре.

Sprite Animation, Animation Clip и другие документы Editor также не должны автоматически считаться стандартными runtime content types только на основании их наличия в Editor document opening service.

## 7. ContentManager: загрузка, кэш и ownership

### 7.1. Ключ кэша

ContentManager кэширует объекты по паре:
- CLR Type;
- AssetPath.

Один путь может загружаться в разные типы, если зарегистрированы подходящие loaders. IsLoaded<T> проверяет только конкретную пару Type + AssetPath.

### 7.2. Внешние и dependency references

Для ContentEntry учитываются:
- ExternalReferences — запросы ресурса со стороны игрового кода/внешнего потребителя;
- DependencyReferences — ссылки со стороны других загружаемых ресурсов;
- Dependencies — набор ресурсов, загруженных текущим loader через IContentLoadContext.

Повторный внешний Load<T> возвращает тот же объект и увеличивает ExternalReferences.

Загрузка dependency через активный loader регистрирует связь между ресурсами. При освобождении владельца dependency reference уменьшается. Ресурс удаляется из кэша и Dispose вызывается, когда внешних и dependency references больше не остаётся.

Повторное создание циклических зависимостей блокируется; есть отдельный тест Load_ThrowsForCyclicDependency.

### 7.3. Unload, ClearCache и Dispose

Unload<T> освобождает одну внешнюю ссылку. Если остаются ссылки других потребителей или зависимости, ресурс остаётся в кэше.

ClearCache очищает весь текущий кэш и освобождает все объекты, реализующие IDisposable, независимо от обычного внешнего ownership. Это принудительная операция, а не замена парным Unload.

Dispose помечает менеджер завершённым, очищает кэш и loader registry.

### 7.4. Reload

Reload<T>:
- при отсутствии ресурса выполняет обычный Load<T>;
- для существующего ресурса требует ровно одну внешнюю ссылку;
- требует отсутствия dependency references;
- запрещает замену при активной синхронной или асинхронной загрузке;
- загружает replacement отдельно от действующего кэша;
- заменяет cache entry только после успешной загрузки;
- при неудаче освобождает replacement и оставляет прежнюю запись.

Тесты покрывают замену ресурса, отказ при нескольких внешних ссылках, отказ при наличии dependencies и сохранение старого объекта при ошибке замены.

Следствие: Reload — контролируемая операция для ресурса с однозначным owner, а не безусловный broadcast reload для любого живого графа ресурсов.

### 7.5. Асинхронная загрузка

LoadAsync объединяет конкурентные запросы одного Type + AssetPath через общий in-flight task.

CancellationToken отдельного потребителя отменяет ожидание этого потребителя. Общая загрузка создаётся независимо от конкретного caller token, чтобы отмена одного caller не сломала других ожидающих. Если все ожидания отменены, успешный результат без оставшихся ссылок удаляется при cleanup.

Это подтверждается тестами:
- LoadAsync_ConcurrentRequestsShareInFlightLoad;
- LoadAsync_CancelledRequest_DoesNotLeakExternalReference;
- LoadAsync_AllRequestsCancelled_DisposesUnreferencedResult;
- Dispose_DuringAsyncLoad_DisposesCompletedAsset.

Важно: отмена LoadAsync не равнозначна гарантированной немедленной отмене общей физической операции loader. Она прежде всего освобождает конкретного ожидающего потребителя.

### 7.6. Открытый concurrency-вопрос

Асинхронный сценарий Dispose во время незавершённой загрузки покрыт тестом. Отдельный эквивалентный тест синхронного Load, одновременно выполняющегося с ClearCache или Dispose, в просмотренных ContentManagerTests не найден.

В статической реализации LoadFresh публикует результат в кэш после вызова loader, но в этой синхронной ветке не видно такой же явной проверки disposed state, которая есть в асинхронной ветке перед публикацией. Это не объявляется подтверждённым дефектом в рамках аудита, но является обоснованным сценарием для отдельной targeted verification на дальнейших этапах.

## 8. Интеграция с графическими и аудио ресурсами

### 8.1. TextureResourceManager

TextureResourceManager использует IContentManager для получения TextureData и ITextureManager для создания GPU texture handle.

Он:
- кэширует GPU texture handles по AssetPath и TextureColorSpace;
- переиспользует существующий handle;
- освобождает GPU handle и Content reference через Unload;
- кэширует TextureAtlas отдельно;
- освобождает свои ресурсы при UnloadAll/Dispose.

ContentManager владеет TextureData, а TextureResourceManager владеет созданными GPU handles. Это два разных уровня ресурса с разными правилами lifetime.

ContentManager.Reload<TextureData> сам по себе не является операцией обновления уже созданного GPU TextureHandle: в интерфейсе ITextureResourceManager нет Reload/refresh operation. Поэтому автоматическое обновление текстуры в графическом backend требует отдельной координации Content data cache и GPU resource cache.

### 8.2. AudioResourceManager

Аудио не использует стандартный ContentManager pipeline.

AudioResourceManager получает IAssetSource и IAudioDevice, а затем:
- сам вызывает AudioLoader;
- создаёт IAudioBuffer;
- кеширует буферы в собственном Dictionary;
- освобождает их через Unload/UnloadAll/Dispose.

DesktopGameHost создаёт AudioManager отдельно от ContentManager, передавая ему тот же FileAssetSource. Это отдельный ресурсный путь с собственным cache/lifetime.

Текущая документация SUPPORT_MATRIX.md указывает, что AudioLoader ограничен WAV PCM, 1 или 2 каналами и 8/16 битами. Категории аудиофайлов, отображаемые Asset Browser, шире этой runtime-поддержки.

### 8.3. Другие ресурсы

Некоторые ресурсы могут загружаться непосредственно через специализированные сервисы или исходный IAssetSource, минуя IContentManager. Например, Editor image preview читает файл самостоятельно; шрифты могут создаваться из bytes через graphics font manager.

Следовательно, ContentManager сегодня следует рассматривать как общий typed content cache для подключённых типов и загрузчиков, но не как обязательную точку прохождения абсолютно всех файловых ресурсов движка.

## 9. Content Hot Reload

ContentHotReloadService предоставляет polling workflow:

1. Update вызывает IContentCatalog.Refresh.
2. Сравнивает новый ContentAsset snapshot с предыдущим.
3. Сообщает новые и изменённые пути.
4. Удалённые пути убирает из собственного snapshot.
5. Reload<T>(path) явно вызывает ContentManager.Reload<T>, если ресурс существует в каталоге и уже загружен.

Критерии изменения:
- SizeBytes;
- LastModifiedUtc;
- Extension.

Ограничения текущей реализации:
- это polling service, а не встроенный watcher;
- Update не выполняется автоматически DesktopGameHost;
- стандартный DesktopGameHostContext не предоставляет IContentCatalog или ContentHotReloadService;
- удалённые ресурсы не возвращаются в changed list как отдельное событие удаления;
- сервис не перебирает все типы, в которых загружен путь, и не выполняет автоматическую reload всех зависимых ресурсов;
- Reload<T> наследует ограничения ownership и dependency references из ContentManager;
- обновление TextureData не обновляет автоматически GPU texture, уже созданную TextureResourceManager.

ContentHotReloadService пока является низкоуровневым механизмом для приложения, а не завершённой пользовательской функцией live asset reload в Editor.

Тест ContentPipelineTests.HotReload_Update_ReportsAssetThatReappearsAfterRemoval проверяет сценарий удаления файла, последующего повторного появления и ручного Reload.

## 10. Путь Assets от проекта до запущенной игры

### 10.1. Создание проекта

GameProject хранит AssetsDirectoryPath = Project.RootDirectory/Assets.

ProjectCreationService:
- создаёт каталог Assets;
- формирует игровой .csproj;
- добавляет None Update="Assets/**/*";
- задаёт CopyToOutputDirectory=PreserveNewest;
- задаёт CopyToPublishDirectory=PreserveNewest.

Тем самым ресурсы проекта попадают в output и publish игры.

### 10.2. Запуск и runtime root

DesktopGameHost использует DesktopGameHostOptions.AssetsDirectory, если он задан. Иначе default root — AppContext.BaseDirectory/Assets.

Стандартный generated game project запускает SilkNetDesktopHost.Run. Следовательно, копирование Assets в output соответствует стандартному runtime root.

BuildTools Runtime Builder публикует необходимые engine runtime assemblies, а пользовательский game project публикуется отдельным процессом через dotnet tooling. Этот механизм поставки не является импортёром отдельных asset files; он обеспечивает наличие engine assemblies и проектных файлов/ресурсов в опубликованном приложении.

### 10.3. Loader registration в generated game template

Шаблон игрового проекта добавляет BinaryContentLoader<UiAsset> для .ui через ConfigureContentLoaders.

Остальные custom loaders разработчик подключает через такой же ConfigureContentLoaders callback. Стандартный DesktopGameHost создаёт ContentLoaderRegistry до ContentManager и затем вызывает callback. Позже через context.Content доступен только IContentManager interface, без публичного Register.

С точки зрения разработчика игры это означает: собственные loaders требуется зарегистрировать при настройке Host до запуска приложения.

## 11. Тестовые свидетельства в репозитории

Найденные тесты Content и связанных ресурсов:

### Engine.Tests/Content

- ContentManagerTests.cs — sync/async cache, конкурентные запросы, dependencies, cycles, reference accounting, Reload, Dispose и cancellation;
- ContentPipelineTests.cs — FileContentCatalog, загрузчики, binary serialized asset, dependencies и Content Hot Reload;
- ParticleEffectContentLoaderTests.cs;
- ParticleEffectDefinitionJsonSerializerTests.cs;
- PrefabAssetContentLoaderTests.cs;
- ScriptDefinitionContentLoaderTests.cs;
- TileSetContentLoaderTests.cs;
- TilemapContentLoaderTests.cs;
- TilemapDefinitionJsonSerializerTests.cs.

### Другие связанные области

- Engine.Tests/Assets/FileAssetSourceTests.cs;
- Engine.Tests/Assets/AssetPathTests.cs;
- Engine.Tests/Graphics/ImageTextureLoaderTests.cs;
- Engine.Tests/Graphics/TextureResourceManagerTests.cs;
- Engine.Tests/Editor/EditorAssetBrowserTests.cs;
- Engine.Tests/Editor/EditorAssetCreationServiceTests.cs;
- Engine.Tests/Editor/EditorAssetCreationRegistryTests.cs;
- Engine.Tests/Editor/EditorContextAssetBrowserTests.cs;
- Engine.Tests/Editor/ViewportProjectTextureCacheTests.cs;
- Engine.Tests/UI/Assets/UiAssetLoaderTests.cs.

Тесты достаточно широко покрывают низкоуровневый cache/lifetime и часть конкретных loaders. Они не образуют полного end-to-end доказательства для всех категорий Asset Browser, всех форматов, всех backend и всего import/reimport workflow.

## 12. Подтверждённая основа и открытые вопросы

### Подтверждено как существующая основа

- единый AssetPath value type;
- filesystem source с sync/async чтением и reparse-point checks;
- filesystem catalog с атомарным snapshot update;
- loader registry и typed IContentLoader contract;
- generic importer/processor interfaces;
- ContentManager с кэшированием по type/path;
- external и dependency reference accounting;
- обнаружение cyclic content dependencies;
- конкурентная sync/async загрузка и cancellation handling;
- безопасный replacement path в Reload<T>;
- TextureResourceManager поверх ContentManager;
- копирование Assets в output и publish игрового проекта.

### Подтверждённые ограничения

1. Editor Asset Browser и ContentCatalog являются разными файловыми представлениями.
2. Asset Browser classifier отображает категории файлов шире, чем стандартная runtime loader support.
3. Нет полноценной import database с stable GUID, import settings, importer version и derived artifacts.
4. Стандартный Desktop Host подключает ограниченный набор loaders; Prefab и Script Definition loaders требуют явной регистрации.
5. Audio использует самостоятельный AudioResourceManager и AudioLoader, а не ContentManager.
6. Content Hot Reload не интегрирован в стандартный Host и не обновляет автоматически зависимые GPU resources.
7. AssetPath сам по себе не обеспечивает безопасность; проверки зависят от источника/резолвера.
8. Editor path validation и FileAssetSource используют неодинаковую политику проверки symbolic links/reparse points.
9. ContentHandle<T> существует как отдельный тип, но не участвует в публичных операциях IContentManager, которые возвращают asset object и требуют явный Unload<T>(path).
10. Сценарий sync Load одновременно с ClearCache/Dispose требует отдельной проверки.

### Рекомендуемые вопросы для будущего развития

- Нужна ли полноценная importer/asset database с GUID, настройки импорта и версионируемые derived artifacts, или текущая модель прямых файловых loaders достаточно эффективна для ближайших 2D workflow?
- Следует ли согласовать классификацию Asset Browser с реально доступными loaders и preview providers?
- Нужна ли единая модель ownership/handles для Content, Audio и GPU resources, или достаточно разных контрактов с ясной документацией?
- Как должен выглядеть полный Hot Reload workflow: обнаружение изменений, зависимостей, повторный импорт, замена typed assets и обновление GPU/Audio resources?
- Нужна ли регистрация IContentCatalog/HotReload service через DesktopGameHostContext?
- Должны ли Prefab и Script Definition быть стандартными loaders или оставаться явно подключаемыми функциями проекта?
- Нужна ли runtime/Editor одинаковая политика проверки reparse points в Assets paths?

Это открытые архитектурные вопросы, не согласованные задачи реализации. Их не следует автоматически превращать в рефакторинг.

## 13. Итоги задачи 0.3

**Задача 0.3 — аудит Content Pipeline — завершена.**

ContentManager имеет достаточно сильную основу для typed loading, caching, dependency tracking и resource lifetime. Сквозная доставка Assets из игрового проекта в output/publish также согласована со стандартным Desktop Host.

Главный разрыв находится между filesystem-oriented Editor Asset Browser и runtime typed Content Pipeline: Editor пока управляет файлами и небольшим набором document/resource templates, но не предоставляет полного import workflow и не отражает фактическую доступность каждого файла через runtime loaders.

Кроме того, разные подсистемы (Content, Audio, Graphics preview) управляют частью ресурсов через отдельные API и caches. Это следует учитывать при разработке Hot Reload и общего resource workflow.

## 14. Следующий шаг

Следующая задача нулевого этапа — **0.4. Аудит Scene и World**.

Проверить:
- World/ECS и модель пространственных данных;
- локальные и мировые Transform;
- иерархию и её синхронизацию;
- создание и редактирование World documents;
- сериализацию сцен и component registries;
- Prefab model, переиспользование и instantiation;
- ссылки на ресурсы в scene data;
- разделение authoring state, runtime state и сохранения игрового прогресса;
- startup scene и загрузку сцены в Runtime.

Результаты 0.2 Editor и 0.3 Content Pipeline использовать как входные данные. Повторно проводить эти аудиты не требуется без конкретной причины.
