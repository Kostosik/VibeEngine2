# Аудит Developer Tools VibeEngine2

**Дата:** 27 ноября 2026 года  
**Ветка:** `master`  
**Исходная ревизия:** `dfc50a30ee69de9981a5452a5810755caa4de688`  
**Тип проверки:** статический анализ исходников, связей Runtime/Host/Editor, benchmark entry points и тестовых файлов. Сборка, тесты, benchmark-сценарии и ручная проверка Editor/игры в рамках этого этапа не запускались.

## 1. Итог

В VibeEngine2 существует базовый набор Developer Tools: диагностический pipeline, консоль с командами, runtime pause/resume/step, инспекция ECS, валидация World/Physics, CPU-профилирование scheduler и фаз кадра, а также standalone benchmark-проект.

Часть инструментов уже включена в стандартную desktop-композицию:

- Desktop Host подключает runtime-команды к игровому циклу;
- Runtime предоставляет консоль, диагностические sink, profiler и базовые validators, если Tooling включён;
- Editor Console поддерживает просмотр сообщений, уровни, текстовый фильтр и выполнение команд;
- профиль кадра доступен через команду `profile`.

При этом набор пока нельзя считать полноценной средой диагностики движка. Существенные границы:

- отдельный Frame Debugger и GPU timestamp profiling в исходниках не обнаружены;
- текущая Physics Debug Visualization ограничена 2D и не вызывается автоматически базовым `EngineGame.Render()`;
- `ContentValidator` существует как отдельный validator, но не регистрируется стандартным Runtime;
- команда `validate` возвращает только общий результат и количество проблем, а не сами записи с кодами и сообщениями;
- benchmark-проект запускается отдельно и не предоставляет автоматическую систему baseline/regression thresholds;
- часть инструментов имеет unit tests, но debug console, overlay и визуализация не имеют сопоставимого набора прямых тестов.

**Общая оценка:** Developer Tools реализованы частично. Диагностика, консоль, CPU frame/scheduler profiling, базовая ECS inspection и benchmark CLI уже полезны; инструменты анализа графического кадра, GPU времени и комплексной диагностики систем требуют дальнейшего развития.

## 2. Состав и связи

| Уровень | Реализация |
|---|---|
| Общая диагностика | `Engine.Core.Diagnostics`: Diagnostic, levels, reporter, sink, collector, FatalError |
| Метрики | `Engine.Core.Diagnostics.Metrics`: MetricCollector и PerformanceTimer |
| Инструменты | `Engine.Tooling`: DebugConsole, debug commands, profiling, validation, ECS inspection, debug visualization |
| Runtime | Создаёт tooling services, регистрирует базовые команды и validators, подключает profiling к scheduler/game loop |
| Desktop Host | Передаёт Runtime стандартный GameLoop и вызывает `AttachGameLoopController` |
| Editor | `EditorConsolePanel` отображает сообщения, фильтрует уровни и текст, предоставляет command input |
| Benchmark | `Engine.Benchmarks` — отдельное executable-приложение с набором сценариев |

`EngineRuntimeOptions.ToolingEnabled` управляет созданием полного набора runtime tooling. Headless-конфигурация без сервисов требует отключённого tooling; при таком режиме Console, Profiler, Validation, Inspection и Debug Visualization недоступны. Диагностический collector и reporter существуют и в базовом runtime, но без полного console pipeline.

## 3. Diagnostics и Console

### 3.1. Диагностический pipeline

`DiagnosticReporter` формирует структурированное сообщение с уровнем, source/code, текстом и необязательным Exception. Поддерживаются Trace, Debug, Info, Warning, Error и Critical. `CompositeDiagnosticSink` позволяет передать одно сообщение нескольким получателям.

Стандартный Runtime с включённым tooling направляет диагностику одновременно:

- в `DiagnosticCollector` для программного чтения;
- в `ConsoleDiagnosticSink` для отображения в runtime console.

`SystemConsoleSink` отдельно выводит сообщения в системную консоль. Исключения сохраняются в `ConsoleMessage`; Editor Console formatter умеет показывать сообщение исключения и stack trace.

`DiagnosticCollector` хранит записи в обычном списке до вызова Clear; лимита ёмкости в самом collector нет. Runtime DebugConsole использует ограниченный кольцевой буфер на 5000 сообщений по умолчанию и вытесняет самые старые сообщения при заполнении. Это разные хранилища и разные политики срока жизни.

### 3.2. Debug Console

`DebugConsole` предоставляет:

- registry команд без учёта регистра;
- tokenizer с поддержкой аргументов в кавычках;
- историю команд;
- сообщение о неизвестной команде и ошибке выполнения;
- несколько output sinks;
- событие `MessageWritten`;
- ограниченный буфер сообщений.

При включённом tooling Runtime регистрирует команды:

- `help`;
- `validate`;
- `entities`;
- `inspect`;
- `metrics`;
- `profile`;
- `physics.debug on|off`;
- команду пересборки движка.

После подключения к GameLoop добавляются `pause`, `resume` и `step`. Стандартный `DesktopGameHost.Run` сам вызывает `runtime.AttachGameLoopController(gameLoop)`, если DebugRegistry создан. Поэтому команды управления циклом не требуют отдельной регистрации в пользовательской игре при использовании стандартного Host.

### 3.3. UI консоли

Есть два отдельных способа отображения runtime console:

- `EditorConsolePanel` — редакторская панель со severity toggles, поиском по тексту/code, прокруткой, вводом команд и навигацией по истории;
- `DebugConsoleOverlay` — игровой overlay с текстовым вводом и командами, который приложение может встроить в собственный render/input workflow.

`Game.Sandbox` создаёт `DebugConsoleOverlay` вручную и передаёт его в sandbox state. Сам стандартный Desktop Host не создаёт внутриигровой overlay автоматически. Это корректная точка расширения, но для отдельной игры нужна собственная интеграция overlay или иной UI.

## 4. ECS Inspection и Validation

### 4.1. Inspection

`Engine.ECS` предоставляет `IWorldInspector` и `WorldInspector` для перечисления активных сущностей, типов компонентов и значений компонентов. `WorldInspectionService` оборачивает эти операции для tooling.

Runtime команды:

- `entities` — перечисляет сущности и краткую информацию о компонентах;
- `inspect <entityIndex>` — выводит компоненты одной сущности.

Editor имеет собственный Inspector для выбранной сущности и зарегистрированные property editors. Это authoring UI, а не замена runtime inspection commands.

Текущая консольная инспекция предназначена для диагностики и выводит текстовое представление компонентов. Это не интерактивный runtime property editor и не live watch window с графиками значений.

### 4.2. Validation

`ValidationService` поддерживает регистрацию независимых `IValidator` и объединяет их результаты. В стандартном Runtime регистрируются:

- `WorldValidator` — проверка согласованности списка entities/components, доступности компонентов и согласованности типов;
- `PhysicsValidator` — проверка условий использования 2D Physics: наличие WorldTransform2D, Collider2D с PhysicsBody2D и несовместимость динамического/кинематического тела с TransformParent2D.

`ContentValidator` реализован отдельно и проверяет наличие подходящего loader для asset, а также неоднозначные loaders одного типа. Но в стандартной регистрации Runtime он не добавлен: в конструкторе Runtime регистрируются только WorldValidator и PhysicsValidator. Для его использования приложение должно само получить catalog/loader registry и зарегистрировать validator.

В просмотренном наборе 3D validators нет; PhysicsValidator проверяет только 2D-компоненты.

### 4.3. Ограничение команды validate

Команда `validate` запускает ValidationService, но её результат содержит только:

- «Validation passed.»;
- количество предупреждений;
- либо количество ошибок.

Сами `ValidationIssue` с code/message/severity команда не перечисляет в Console. Результат доступен через `EngineRuntime.Validation.Validate(...)` в коде, однако обычный console workflow не позволяет по одному сообщению определить, какие entity/component требуют исправления. Это ограничение именно output команды, а не самой модели ValidationResult.

## 5. Profiling и Frame Timing

### 5.1. Scheduler Profiler

`Profiler` из `Engine.Tooling.Profiling` умеет измерять именованные scopes и агрегирует метрики через MetricCollector.

`SchedulerProfiler` подписывается на события `SystemScheduler.SystemExecuting` и `SystemExecuted`, создаёт scope для системы и фиксирует время выполнения с именем, включающим phase и SystemType. Это CPU-время выполнения зарегистрированных Update/FixedUpdate систем. Оно не включает автоматическое профилирование произвольных участков пользовательского кода вне scheduler, если приложение само не открывает scopes.

Команда `metrics` выводит собранные profiler metrics. Профилирование по умолчанию выключено и включается явно.

### 5.2. FrameTimingProfiler

`FrameTimingProfiler` сохраняет до 1024 последних samples по умолчанию в кольцевом буфере и считает статистику:

- Frame Interval;
- Update CPU;
- FixedUpdate CPU;
- Render CPU;
- EndFrame CPU.

Для каждой фазы доступны Average, Minimum, Maximum, P95 и P99. По среднему Frame Interval команда рассчитывает оценочный average FPS.

Подключение работает сквозным образом:

1. Runtime создаёт FrameTimingProfiler при `StartProfiling()`;
2. SchedulerProfiler подключается к системным событиям;
3. GameLoop измеряет Update, FixedUpdate и Render;
4. DesktopWindow обрамляет graphics frame, измеряет вызов GraphicsDevice.EndFrame и закрывает sample;
5. `profile report` выводит итоговую статистику.

Команда `profile` поддерживает `start`, `stop`, `status`, `report`, `reset`. При остановке собранные данные сохраняются; повторный старт начинает новую capture-сессию.

### 5.3. Границы profiling

Все доступные сейчас значения — CPU-side timings. `EndFrame CPU` измеряет длительность вызова `IGraphicsDevice.EndFrame()` с помощью Stopwatch. В исходниках не обнаружены GPU timestamp queries, чтение GPU timing results или отдельный GPU profiler. Поэтому EndFrame CPU нельзя трактовать как GPU frame time.

Также нет отдельного профайлера отдельных render passes/draw calls в UI. Сcheduler profiling показывает время зарегистрированных систем, а не внутреннюю стоимость GPU-команд или отдельных узлов RenderGraph.

## 6. Debug Visualization

Tooling содержит generic `DebugDrawList`, providers, service и renderer для линий, прямоугольников и окружностей. Renderer преобразует эти primitives в общие debug render commands.

Текущий встроенный `PhysicsDebugVisualizer`:

- работает с `PhysicsBody2D` и `Collider2D`;
- рисует AABB, Circle и Polygon;
- может показывать вектор velocity;
- поддерживает включение/выключение и настройку цветов.

Runtime создаёт provider и регистрирует его в DebugVisualizationService. Команда `physics.debug on|off` меняет Enabled.

Однако `EngineRuntime.RenderDebugVisualization()` явно вызывает Build + Render. Базовый `EngineGame.Render()` вызывает пользовательский OnRender, затем автоматически рисует Tilemaps, Sprites и Particles, но не вызывает RenderDebugVisualization. Значит, пользовательский Game должен сам встроить этот вызов в свой render path. Само переключение `physics.debug on` не добавляет визуализацию в кадр автоматически.

Debug visualization сейчас 2D-only: данные draw primitives — FixedVector2, а PhysicsDebugVisualizer использует только PhysicsBody2D/Collider2D. Встроенного 3D collider/body visualization, contact normals, broad-phase bounds или solver contacts не обнаружено.

## 7. Benchmarks

`Engine.Benchmarks` — отдельный executable-проект, запускаемый через аргумент `--benchmark`. В CLI перечислены сценарии:

- ECS и runtime;
- Physics broad-phase, narrow-phase, solver, PhysicsSystem и queries;
- Graphics Window;
- Tilemap Rendering;
- Transform Hierarchy.

`GameBenchmarkRunner` поддерживает warmup и измеряемые кадры; отчёт содержит median, P95, max для timing samples, количество аллокаций и аллокации на кадр, Gen0/Gen1/Gen2 GC collections, managed memory delta, setup/initialization и shutdown time.

`EngineRuntimeGameBenchmark` моделирует ECS workload с 10 000 сущностями, 120 warmup frames и 600 measured frames. Physics/graphics/tilemap/transform benchmarks реализованы отдельными сценариями.

Ограничения:

- benchmark выполняется отдельной командой, а не из Editor Profiler;
- в просмотренном тестовом дереве нет benchmark correctness/performance tests;
- не обнаружена встроенная система сохранения baseline и проверки regression threshold;
- отдельного 3D mesh/rendering benchmark в списке CLI не обнаружено;
- результаты зависят от конфигурации, машины, build configuration и условий запуска; их нельзя сравнивать без фиксации этих условий.

## 8. Frame Debugger и другие отсутствующие инструменты

В полном дереве исходников текущей ревизии не обнаружен отдельный Frame Debugger, который сохраняет и показывает последовательность render passes/commands кадра с привязкой ресурсов, shader state, render targets, draw calls и изменением GPU state.

Не обнаружены также:

- GPU timestamp query profiler;
- GPU memory/resource inspector;
- визуальный runtime ECS watch window;
- 3D Physics debug visualization;
- универсальная визуализация контактов/solver diagnostics;
- единая UI-панель Benchmark Results с историей запусков и сравнением baseline.

Это вывод о текущем дереве файлов и просмотренных интеграциях, а не утверждение о невозможности добавить такие средства поверх существующих render/diagnostic contracts.

## 9. Тестовое покрытие, найденное в исходниках

Тестовые файлы покрывают:

- `DiagnosticCollector`, `DiagnosticReporter`, `CompositeDiagnosticSink`;
- `MetricCollector` и `PerformanceTimer`;
- `Profiler` и `FrameTimingProfiler`, включая запуск/остановку, очистку, ring buffer и статистику;
- `ValidationService`, `WorldValidator`, `PhysicsValidator` и `ValidateDebugCommand`;
- runtime profiling lifecycle, attach GameLoop и debug commands;
- отображение exception/stack trace в Editor Console formatter;
- editor diagnostic forwarding.

В просмотренном дереве не обнаружены отдельные тестовые файлы для `DebugConsole`, `DebugConsoleOverlay`, `DebugVisualizationService`, `PhysicsDebugVisualizer`, `ContentValidator`, полного UI Console interaction и benchmark regression. Это перечень найденных тестов; тесты в рамках аудита не запускались.

## 10. Матрица готовности

| Область | Оценка | Что подтверждено / что ограничено |
|---|---|---|
| Diagnostics | Частично | Structured diagnostics, levels, exception data, collector и console sinks; нет полной централизованной диагностики всех подсистем |
| Debug Console | Частично | Runtime commands, message history/sinks, Editor Console и Sandbox overlay; overlay требует ручной интеграции |
| Pause / Resume / Step | Готово для standard Desktop Host | Host подключает GameLoop controller и регистрирует команды |
| ECS Inspection | Частично | Runtime entities/inspect commands и Editor Inspector; нет live watch/interactive runtime inspector |
| Validation | Частично | World + Physics 2D validators подключены; ContentValidator отдельно, не зарегистрирован; нет 3D validator |
| CPU profiling | Частично | Scheduler scopes и frame phase timings, P95/P99, report command; нет профилирования GPU |
| GPU profiling | Не обнаружено | Нет timestamp-query workflow или GPU timings |
| Debug Visualization | Частично | Generic 2D draw list и PhysicsDebugVisualizer для 2D; вызов рендеринга не входит в базовый EngineGame path |
| Frame Debugger | Не обнаружен | Нет пользовательского per-frame capture/inspection workflow |
| Benchmarks | Частично | Standalone CLI со сценариями ECS/Runtime/Physics/Graphics/Tilemap/Transform; нет baseline/regression runner и интеграции в Editor |

## 11. Рекомендуемая последовательность развития

1. **Закончить существующий diagnostic workflow:** выводить из `validate` конкретные issue codes/messages/entities, подключить `ContentValidator` там, где Runtime имеет доступ к catalog/loaders, и покрыть эти пути тестами.
2. **Завершить debug visualization lifecycle:** определить единый способ включения и вызова визуализации в игре; сохранить возможность отключения; расширять 3D-визуализацию вместе с завершением 3D Physics.
3. **Сделать Frame Debugger:** начать с backend-neutral снимка кадра (pass sequence, commands, targets, shaders/resources, state transitions), затем построить UI для просмотра конкретного кадра.
4. **Добавить GPU timings после Frame Debugger:** отдельный backend contract для timestamp queries и асинхронного чтения результатов; не смешивать GPU time с CPU EndFrame duration.
5. **Улучшить benchmark workflow:** формализовать Release-конфигурацию, набор повторяемых workloads, сохранение результатов и сравнение baseline; добавить нужные 3D workloads после появления полноценного 3D runtime/render path.
6. **Расширять inspection адресно:** watch values/снимки компонентов и визуальные диагностики добавлять под реальные сценарии отладки, не дублируя текущий Editor Inspector без необходимости.

Пункты являются выводом аудита, а не изменением кода или автоматическим утверждением roadmap реализации.

## 12. Основные исходники и тесты

Исходники:

- `Engine.Core/Diagnostics/**`, `Engine.Core/Diagnostics/Metrics/**`;
- `Engine.Tooling/Debugging/**`;
- `Engine.Tooling/Profiling/**`;
- `Engine.Tooling/Inspection/**`;
- `Engine.Tooling/Validation/**`;
- `Engine.Tooling/DebugVisualization/**`;
- `Engine.Runtime/EngineRuntime.cs`, `Engine.Runtime/EngineGame.cs`;
- `Engine.Runtime/Debugging/ProfileDebugCommand.cs`;
- `Engine.Core/Application/GameLoop.cs`;
- `Engine.Hosting.Desktop/DesktopGameHost.cs`, `DesktopWindow.cs`;
- `Engine.Editor.UI/Panels/EditorConsolePanel.cs`;
- `Engine.Benchmarks/**`.

Тесты:

- `Engine.Tests/Core/Diagnostics/**`;
- `Engine.Tests/Core/Metrics/**`;
- `Engine.Tests/Tooling/Profiling/**`;
- `Engine.Tests/Validation/**`;
- `Engine.Tests/Runtime/EngineRuntimeTests.cs`;
- `Engine.Tests/Editor/EditorDiagnosticsTests.cs`;
- `Engine.Tests/Editor/EditorConsoleMessageFormattingTests.cs`.

Связанные документы: [Аудит Runtime](04-RUNTIME.md), [Аудит 2D Engine](05-2D-ENGINE.md), [Аудит 3D Engine](06-3D-ENGINE.md), [Матрица поддержки](../SUPPORT_MATRIX.md).
