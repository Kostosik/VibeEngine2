
# Аудит Runtime

**Дата:** 2026-11-27  
**Ветка:** master  
**Исходный commit:** 21264a38b9d31165c98709f2b0f3eab680a6e5d9  
**Тип проверки:** статический аудит исходников и тестов; без изменения production-кода и тестов.

## 1. Итог

Runtime уже предоставляет рабочую основу для пользовательской игры: жизненный цикл EngineGame, фиксированный игровой цикл, ECS scheduler с зависимостями систем, обработку команд и событий, встроенные игровые системы, диагностику и управление временем жизни собственных ресурсов.

Главные выводы:

1. Пользовательская игра расширяет Runtime через EngineGame.ConfigureRuntime(). Этот метод вызывается до EngineRuntime.Initialize(), поэтому пользователь успевает зарегистрировать компоненты, системы, скрипты, обработчики и детерминированное состояние до сборки scheduler.
2. GameLoop отделяет Update от FixedUpdate, ограничивает время кадра и число fixed steps за кадр. При превышении лимита накопленное отставание сбрасывается, а не приводит к бесконечному догоняющему циклу.
3. SystemScheduler строит отдельные планы Update и FixedUpdate, проверяет зависимости, циклы и фазы систем. Стандартный EngineRuntime использует последовательную стратегию; альтернативная JobSystemExecutionStrategy существует, но не подключается через стандартные EngineRuntimeOptions.
4. Desktop Host создаёт графику, ввод, аудио, Content и Runtime, передаёт их пользовательскому приложению и отвечает за их внешнее время жизни. EngineRuntime освобождает собственные системы и кеши, но не владеет переданными ему Host-сервисами.
5. Отдельная интеграционная проверка собирает и запускает опубликованный пользовательский проект вне репозитория движка. Она подтверждает регистрацию пользовательской системы/компонента, загрузку ресурсов и ручную загрузку .world из игрового приложения.

**Главная граница текущего Runtime — расширяемость существует, но часть конфигурации остаётся фиксированной внутри EngineRuntime.** В частности, scheduler создаётся с последовательной стратегией без точки её подмены в Runtime options; также пользовательский код отвечает за регистрацию собственных сериализаторов, обработку общих ошибок систем и освобождение своих подписок/ресурсов.

Этот этап — аудит текущих контрактов. Он не предполагает обязательного расширения Runtime или добавления новых абстракций без конкретного пользовательского сценария.

## 2. Объём и метод

Проверены:

- Engine.Core/Application: IApplication, Application, GameLoop и интерфейсы управления игровым циклом;
- Engine.Core/Systems: SystemScheduler, SystemRegistration, SystemExecutionPlan и стратегии выполнения;
- Engine.Simulations: Simulation, команды, события и детерминированное состояние;
- Engine.Runtime: EngineGame, EngineRuntime, EngineRuntimeOptions и EngineRuntimeServices;
- Engine.Scripting: ScriptRuntime и обработка ошибок пользовательских скриптов;
- Engine.Hosting, Engine.Hosting.Desktop и Engine.Hosting.Desktop.SilkNet: стандартный Host, window lifecycle, host context и конфигурация backend;
- Engine.Jobs/Integration: JobSystemExecutionStrategy;
- тесты Application, Systems, Simulations и Runtime, включая запуск опубликованного пользовательского проекта.

Проверялись исходники и содержание тестов. Сборка, тесты, интерактивный запуск Desktop Host и ручная проверка пользовательской игры в рамках этого аудита не выполнялись. Поэтому тестовые файлы приведены как свидетельства существующего покрытия, без утверждения о повторном запуске.

## 3. Модель приложения и пользовательской игры

### 3.1. IApplication

Базовый контракт приложения включает пять вызовов:

- Initialize();
- Update(TimeSnapshot);
- FixedUpdate(SimulationTime);
- Render(interpolationAlpha);
- Shutdown().

Engine.Core.Application.Application предоставляет пустую базовую реализацию. Desktop Host может работать с произвольным IApplication; EngineGame — стандартная базовая реализация для пользовательских игр, связанная с EngineRuntime.

### 3.2. EngineGame lifecycle

EngineGame предоставляет точки расширения:

- ConfigureRuntime() — регистрация игровых систем и настройка Runtime до его инициализации;
- OnInitialize() — инициализация игровой логики после EngineRuntime.Initialize();
- OnUpdate() — код на каждый кадр;
- OnFixedUpdate() — код на каждый simulation tick;
- OnRender() — пользовательская отрисовка;
- OnShutdown() — очистка игровых ресурсов.

Порядок Initialize:

1. ConfigureRuntime();
2. Runtime.Initialize();
3. OnInitialize().

Повторная инициализация того же EngineGame не допускается. Если ConfigureRuntime или OnInitialize выбрасывает исключение, EngineGame вызывает OnShutdown, безопасно завершает уже запущенный Runtime, помечает экземпляр завершённым и сохраняет исходное исключение.

Shutdown вызывает OnShutdown и в finally завершает Runtime. Повторный Shutdown безопасен. После остановки вызовы Update, FixedUpdate и Render отклоняются.

### 3.3. Порядок вызовов в кадре

Для стандартного EngineGame:

| Фаза | Порядок |
|---|---|
| Update | Input.Update → OnUpdate → EngineRuntime.Update |
| FixedUpdate | OnFixedUpdate → EngineRuntime.FixedUpdate |
| Render | OnRender → RenderTilemaps → RenderSprites → RenderParticles |
| Shutdown | OnShutdown → EngineRuntime.Shutdown |

Таким образом, пользовательский OnUpdate выполняется до систем EngineRuntime.Update. Пользовательский OnFixedUpdate выполняется перед fixed системами. OnRender выполняется до встроенной последовательности отрисовки Tilemap, Sprite и Particle.

Input обновляется один раз на обычный кадр перед пользовательским OnUpdate, а не отдельно перед каждым fixed tick.

Пользовательская отрисовка подключается через OnRender либо через собственное IApplication. В стандартном SystemScheduler нет отдельной Render-фазы или интерфейса IRenderSystem.

## 4. GameLoop и управление временем

GameLoop отделяет частоту отрисовки/Update от фиксированной частоты симуляции.

В стандартном конструкторе используются:
- максимальный delta одного кадра — 250 мс;
- максимум 8 fixed steps за один кадр;
- частота симуляции берётся из EngineRuntime.SimulationRate; значение по умолчанию в EngineRuntimeOptions — 20 ticks в секунду.

Поведение цикла:

1. Delta кадра ограничивается MaxFrameDelta.
2. Ограниченный delta добавляется к elapsed time и, если цикл не на паузе, к fixed accumulator.
3. Выполняется один Update с TimeSnapshot.
4. Пока в accumulator хватает времени и не превышен лимит, выполняется FixedUpdate с фиксированным delta и последовательным Tick.
5. Если после достижения максимума fixed steps накопленное время всё ещё больше одного tick, остаток accumulator сбрасывается в ноль. Это осознанное ограничение догоняющей симуляции.
6. Render получает InterpolationAlpha, рассчитанный по остатку accumulator и ограниченный диапазоном 0..1.

Pause прекращает накопление fixed time и сбрасывает accumulator; обычный Update при этом продолжает вызываться. RequestStep позволяет запросить один fixed tick на паузе. Resume возобновляет обычное накопление времени без попытки догнать период паузы.

В стандартном DesktopGameHost создаётся GameLoop с параметрами по умолчанию. EngineRuntimeOptions позволяет задать SimulationRate, но не предоставляет параметров MaxFrameDelta и MaxFixedStepsPerFrame. Их можно задавать при самостоятельном создании GameLoop через расширенный конструктор, но не через стандартную конфигурацию DesktopGameHost.

## 5. SystemScheduler

SystemScheduler принимает системы двух фаз:
- Update — IUpdateSystem;
- FixedUpdate — IFixedUpdateSystem.

Регистрация имеет следующие правила:
- одна регистрация на один конкретный SystemType в пределах scheduler;
- система должна реализовывать интерфейс выбранной фазы;
- зависимости Before<T>() и After<T>() задают порядок выполнения;
- зависимость на незарегистрированную систему или систему другой фазы отклоняется;
- циклическая зависимость отклоняется при Build();
- независимые системы попадают в один execution batch и упорядочиваются по порядку регистрации.

Build() строит отдельные execution plans для Update и FixedUpdate и блокирует последующие изменения регистрации и зависимостей. Simulation.Initialize() вызывает Build(), поэтому все пользовательские регистрации должны завершиться до Runtime.Initialize().

Выполнение систем обрамляется событиями SystemExecuting и SystemExecuted. SystemExecuted вызывается в finally для начатого выполнения системы, что используется SchedulerProfiler.

### 5.1. Стратегия выполнения

SystemScheduler сам поддерживает ISystemExecutionStrategy. По умолчанию используется SequentialSystemExecutionStrategy: системы выполняются последовательно, внутри batch — в порядке регистрации, batch — в порядке зависимостей.

В Engine.Jobs.Integration существует JobSystemExecutionStrategy, который планирует системы одного batch через JobScheduler параллельно, а следующий batch запускает после завершения предыдущего. Это не означает, что стандартный Runtime уже использует параллельное исполнение.

EngineRuntime создаёт Scheduler через new SystemScheduler() и не принимает ISystemExecutionStrategy в EngineRuntimeOptions или EngineRuntimeServices. Публичный Scheduler позволяет добавлять системы, но заменить его strategy после создания нельзя. Для стандартного EngineRuntime подтверждённая конфигурация — последовательное исполнение систем.

## 6. Состав и порядок встроенных Runtime-систем

В конструкторе EngineRuntime создаются ECS World, пространственный World, Scheduler, Commands, Events, ScriptRegistry, ScriptRuntime и PhysicsSystem2D.

В scheduler регистрируются:
- ScriptRuntime.UpdateSystem в фазе Update;
- ScriptRuntime.FixedUpdateSystem в фазе FixedUpdate;
- PhysicsSystem2D в FixedUpdate;
- TransformHierarchySystem в FixedUpdate до PhysicsSystem2D;
- PhysicsTransformSyncSystem после PhysicsSystem2D;
- SpatialSyncSystem после PhysicsTransformSyncSystem.

В итоге fixed pipeline учитывает иерархические трансформации до Physics, затем синхронизирует transform после Physics и обновляет пространственный индекс.

Некоторые системы не регистрируются в общем Scheduler:
- AudioEcsSystem создаётся при наличии AudioSourceComponent или AudioListenerComponent и обновляется после Simulation.Update;
- ParticleRuntime создаётся при первом обращении либо при обнаружении ParticleEffectComponent и обновляется после Simulation.Update;
- TilemapRenderSystem и SpriteRenderSystem создаются лениво при наличии соответствующих компонентов и вызываются в render sequence EngineGame.

Это смешанная модель: логика Update/FixedUpdate использует Scheduler, а отдельные аудио-, particle- и render-проходы управляются EngineRuntime напрямую.

## 7. Регистрация игровых расширений

Пользовательская игра может использовать общий ECS API и не обязана регистрировать свои struct-компоненты в отдельном центральном runtime-реестре для обычной работы ECS.

Обычный путь расширения:
1. В ConfigureRuntime создать/зарегистрировать пользовательские компоненты и системы.
2. Добавить системы через Runtime.Scheduler.Add(system, phase).
3. При необходимости зарегистрировать игровые скрипты в Runtime.ScriptRegistry.
4. Если пользовательское состояние должно участвовать в deterministic state hash, зарегистрировать IDeterministicState через Runtime.Simulation.RegisterDeterministicState до Simulation.Initialize.

Тест EngineGameRegistrationTests проверяет использование пользовательского компонента и выполнение пользовательской fixed системы. ExternalGameAssemblyRegistrationTests проверяет регистрацию пользовательской системы из отдельной сборки игры.

Сериализация — отдельный контракт: пользовательский ECS-компонент может работать без сериализатора в памяти, но для сохранения через EcsWorldStateSerializer необходимо явно зарегистрировать его serializer в EcsComponentSerializerRegistry. Это соответствует разделению Runtime и Serialization, а не ограничению ECS.

## 8. Commands, Events и deterministic state

Simulation задаёт порядок обработки:

- Update: SystemScheduler.Update → EventBus.DispatchPending;
- FixedUpdate: CommandDispatcher.DispatchPending → SystemScheduler.FixedUpdate → EventBus.DispatchPending.

Commands, помещённые в CommandQueue, обрабатываются перед fixed системами. CommandDispatcher требует зарегистрированный handler точного типа команды. Если handler отсутствует, выполнение выбрасывает InvalidOperationException. Текущая команда извлекается из очереди до вызова handler; автоматического повторного помещения команды после исключения нет.

EventBus хранит события до DispatchPending и вызывает подписчиков точного типа события. События, опубликованные в Update, доставляются после Update-систем; события, опубликованные в FixedUpdate, — после fixed систем. Подписка возвращает disposable EventSubscription. Runtime не управляет временем жизни пользовательских подписок автоматически — игровой код должен освобождать их в OnShutdown или собственном Dispose.

EngineRuntime регистрирует для deterministic hash ECS World, пространственный World и PhysicsSettings. Simulation позволяет дополнительно зарегистрировать IDeterministicState до инициализации. GetStateHash доступен после инициализации.

## 9. Ошибки и диагностика

EngineRuntime создаёт DiagnosticCollector и DiagnosticReporter. При включённом Tooling диагностика дополнительно выводится в Console через ConsoleDiagnosticSink. FatalError.Throw отправляет Critical diagnostic и выбрасывает FatalEngineException; ошибка диагностического sink не должна подменять исходную fatal exception.

Для ScriptRuntime реализована локальная изоляция ошибок: исключения пользовательских скриптов в Update, FixedUpdate и callbacks событий отключают конкретный экземпляр скрипта, освобождают его event subscriptions и отправляют ScriptFault. EngineRuntime преобразует ScriptFault в Error diagnostic. Тест Runtime подтверждает, что ошибка одного скрипта не останавливает другой здоровый скрипт.

Для произвольных IUpdateSystem, IFixedUpdateSystem и command handlers общего механизма изоляции исключений нет. Исключения проходят через Simulation/Scheduler наружу. В стандартном Host это завершает текущий вызов игрового цикла, после чего finally-блоки запускают Shutdown и освобождение ресурсов. Автоматического продолжения с пропуском ошибочной системы не обнаружено.

Диагностика и FatalError предоставляют явные API, но не являются глобальным перехватчиком всех runtime-исключений.

## 10. Runtime services и владение ресурсами

EngineRuntimeServices принимает внешние сервисы:
- IGraphicsDevice;
- IInput;
- Camera2D;
- IAudioManager;
- IContentManager;
- необязательный InputActionMap.

Свойства сервисов выбрасывают InvalidOperationException, если соответствующий сервис не был настроен. Поэтому headless Runtime может работать без Host-сервисов только в конфигурации ToolingEnabled = false и при условии, что пользовательский код не обращается к графике, аудио, Content или вводу.

По умолчанию EngineRuntimeOptions.ToolingEnabled = true. Включённый Tooling требует создания Runtime с настроенными EngineRuntimeServices; конструктор EngineRuntime(options) без services специально отклоняет такую конфигурацию.

EngineRuntime лениво создаёт TextureResourceManager и render/audio/particle systems по мере необходимости. При Shutdown/Dispose освобождает принадлежащие Runtime кеши и системы. Переданные сервисы не принадлежат EngineRuntime: например, тест Dispose_DoesNotDisposeInjectedAudioService прямо проверяет, что Runtime не уничтожает внешний Audio service.

В стандартной DesktopGameHost именно Host владеет жизненным циклом Window/Graphics, AudioManager и ContentManager. Порядок using scopes обеспечивает освобождение Runtime перед Audio и Content, чтобы внутренние runtime-кеши могли освободиться до уничтожения внешних сервисов.

## 11. Desktop Host и граница платформы

DesktopGameHost — composition root стандартного desktop-приложения. Он:
1. Проверяет DesktopGameHostOptions и создаёт backend registries.
2. Создаёт и инициализирует окно.
3. Создаёт FileAssetSource, FileContentCatalog и ContentLoaderRegistry.
4. Регистрирует стандартные loaders и вызывает ConfigureContentLoaders приложения.
5. Создаёт ContentManager, AudioManager, InputActionMap и ActionInput.
6. Собирает EngineRuntimeServices, EngineRuntime и DesktopGameHostContext.
7. Вызывает application factory, оборачивает IApplication в HostedApplication и запускает GameLoop.
8. В finally останавливает GameLoop и освобождает приложение и Host-owned ресурсы.

DesktopGameHost.Run принимает произвольный IApplication. Отдельная generic overload предоставляет короткий путь для EngineGame. DesktopGameHostContext выдаёт доступ к Runtime, AssetSource, Content, Graphics, Input, Audio, Camera, InputActions, оконным настройкам, text input, cursor и событию resize. PointerInput имеет nullable-контракт.

SilkNetDesktopHost задаёт стандартные реализации OpenGL graphics backend и Silk.NET platform backend, но DesktopGameHostOptions позволяет зарегистрировать дополнительные graphics/platform providers. Стандартный Host напрямую создаёт OpenALAudioDevice; аналогичной конфигурации audio backend в DesktopGameHostOptions не обнаружено.

## 12. Независимый запуск пользовательского проекта

ExternalGameAssemblyRegistrationTests создаёт отдельный .NET проект, копирует runtime assemblies, добавляет пользовательскую зависимость, создаёт Assets и scene-файл, выполняет dotnet publish и запускает опубликованный DLL из publish directory.

Проверяемая игра:
- создаёт EngineRuntime без tooling;
- загружает .world через WorldSaveLoadService в ConfigureRuntime;
- регистрирует собственный ECS component и IFixedUpdateSystem;
- выполняет fixed update;
- проверяет восстановленную сущность и transform;
- читает скопированный игровой asset и вызывает метод пользовательской зависимости.

Этот тест подтверждает, что Runtime и пользовательские расширения могут работать в отдельно собранной и опубликованной игре без ссылки на Editor и без запуска из репозитория движка.

Граница свидетельства: тест напрямую создаёт EngineRuntime и EngineGame, а не запускает графический DesktopGameHost. Он также вручную загружает .world в пользовательском коде; автоматический startup-scene workflow из Editor в стандартном Play отдельно рассмотрен в [аудите Scene и World](03-SCENE-AND-WORLD.md).

## 13. Свидетельства в тестах

| Область | Тесты | Что проверяется |
|---|---|---|
| GameLoop | Engine.Tests/Application/GameLoopTests.cs | fixed timestep, последовательные ticks, clamp delta, лимит fixed steps, сброс отставания, interpolation, pause/resume, single-step, lifecycle и ошибки инициализации/остановки |
| SystemScheduler | Engine.Tests/Systems/SystemSchedulerTests.cs | фазы Update/FixedUpdate, порядок Before/After, регистрация, duplicate type, отсутствующие/cross-phase зависимости, циклы и lock после Build |
| EngineGame lifecycle | Engine.Tests/Runtime/EngineGameLifecycleTests.cs | порядок и ошибки ConfigureRuntime/OnInitialize/OnShutdown, идемпотентный shutdown, пользовательские subscriptions и изоляция экземпляров Runtime |
| Game extensions | Engine.Tests/Runtime/EngineGameRegistrationTests.cs | пользовательский компонент и система, зарегистрированные до Runtime.Initialize |
| External game | Engine.Tests/Runtime/ExternalGameAssemblyRegistrationTests.cs | dotnet publish и запуск отдельного проекта, custom component/system, зависимость, Assets и ручная загрузка .world |
| Runtime | Engine.Tests/Runtime/EngineRuntimeTests.cs | options, headless runtime, диагностика, script faults, profiling, deterministic hash, освобождение ресурсов и сервисов, command/event cleanup |
| Transform pipeline | Engine.Tests/Runtime/TransformRuntimeIntegrationTests.cs | 2D transform hierarchy и синхронизация spatial position в EngineRuntime |
| Deterministic simulation | Engine.Tests/Simulations/DeterministicSimulationTests.cs | повторяемость состояния при одинаковом мире и командах |
| Simulation command/event lifecycle | Engine.Tests/Simulations/SimulationTests.cs и связанные тесты | dispatch команд/событий и интеграция систем в Simulation |

В проверенном наборе Engine.Tests не обнаружен отдельный suite, который запускает полноценный DesktopGameHost с реальным окном и backend. Host lifecycle был проверен статически по исходникам; отдельный published-project test подтверждает работу EngineRuntime без графического Host.

## 14. Сводка статусов

| Область | Статус по этому аудиту | Комментарий |
|---|---|---|
| EngineGame lifecycle | Подтверждённая основа | Есть управляемые lifecycle hooks и тесты ошибок/повторного shutdown |
| GameLoop | Подтверждённая основа | Fixed timestep, clamp, catch-up limit, pause/step и interpolation покрыты тестами |
| SystemScheduler | Подтверждённая основа с ограничениями | Фазы, порядок и validation реализованы; scheduler строится один раз и после Build блокируется |
| Пользовательские компоненты и системы | Подтверждённый сценарий | Регистрация через ConfigureRuntime подтверждена unit- и external-game тестами |
| Параллельное выполнение систем | Частично | JobSystemExecutionStrategy существует, но стандартный EngineRuntime не предоставляет её конфигурацию |
| Commands и Events | Подтверждённая основа | Определённый порядок dispatch и disposable subscriptions; пользователь отвечает за cleanup и обработку ошибок handlers |
| Diagnostics | Подтверждённая основа | Collector, Reporter, Console sink и FatalError существуют; общие ошибки систем не изолируются автоматически |
| Runtime resource ownership | Подтверждённая основа | Runtime освобождает свои системы/кеши и не освобождает injected services |
| Desktop Host | Частично подтверждён | Composition root и backend extension points существуют; реальный Host workflow не покрыт отдельным интеграционным test suite в просмотренном наборе |
| Независимый опубликованный проект | Подтверждённый сценарий | Отдельный проект публикуется и запускается вне репозитория; графический Desktop Host в этой проверке не используется |

## 15. Рекомендуемая последовательность дальнейшей работы

1. **Не менять lifecycle без пользовательского сценария.** Текущая последовательность EngineGame/GameLoop/Host уже имеет явные границы и тесты ошибок очистки.
2. **Определить требование к параллельному исполнению систем.** Если оно действительно нужно Runtime, отдельно спроектировать конфигурацию ISystemExecutionStrategy и владение JobScheduler; не включать параллельность только потому, что strategy-класс существует.
3. **Уточнить общую политику ошибок пользовательских систем.** Сейчас ScriptRuntime изолирует ошибки скриптов, а исключения систем и handlers завершают проход/кадр. Решение о per-system recovery должно учитывать целостность состояния игры.
4. **При необходимости добавить Host integration test.** Проверять его только в подходящей CI/графической среде; не подменять реальную проверку окна тестом с моками.
5. **Сохранить текущую границу ответственности ресурсов.** Host владеет injected services, Runtime — созданными им runtime systems/caches, Game — пользовательскими подписками и объектами.

Следующий пункт нулевого аудита — **0.6. 2D Engine**: сквозная интеграция Sprite/Materials, TileSet/Tilemap, Camera/Viewport, Animation, Lighting/Normal Mapping, Physics 2D, Particles, UI, Audio и Navigation. Результаты ранее проведённых аудитов RenderGraph и 2D Lighting следует использовать без повторения уже проверенных работ.

## 16. Границы заключения

- Это статический аудит исходников и тестов на указанном commit.
- Сборка, тесты и графический запуск Host в рамках данного этапа не выполнялись.
- Наличие интеграционного теста отдельного опубликованного проекта не подтверждает работу всех backend и платформ.
- Замечания о конфигурации strategy, обработке ошибок и Host integration — подтверждённые границы текущих API/тестовых свидетельств, а не автоматически утверждённые дефекты.
