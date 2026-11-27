# Аудит общей архитектуры VibeEngine2 (0.1)

**Дата аудита:** 2026-11-27  
**Репозиторий:** Kostosik/VibeEngine2  
**Ветка:** master  
**Исходный снимок:** dba8ba7bcb9526abbb5b50d0135fc078a0afa6d2  
**Статус:** завершён в рамках статического обзора структуры проектов, прямых зависимостей и ключевых точек композиции.

## 1. Цель и границы

Проверена общая архитектура репозитория в рамках задачи 0.1 плана развития VibeEngine2.

Проверялись:
- состав C#-проектов;
- Target Framework проектов;
- прямые ProjectReference основных слоёв;
- границы Core, ECS, Runtime, Hosting, Editor, Backend и приложений;
- ключевые композиционные точки;
- соответствие отдельных архитектурных утверждений фактическому составу исходников.

Это аудит исходников и проектных файлов. Сборка решения, запуск тестов и ручной запуск Editor/Game.Sandbox в рамках этого аудита не выполнялись. Поэтому документ не объявляет новые сценарии runtime-поведения проверенными.

Код не изменялся.

## 2. Общий вывод

Архитектура уже имеет выраженное разделение на фундаментальные механизмы, игровые подсистемы, runtime-композицию, платформенные реализации, инструменты редактора и запускаемые приложения.

Главный принцип «движок предоставляет механизмы, игра определяет смысл» отражён в структуре проектов: пользовательская игровая логика может находиться в Game, тогда как Engine предоставляет ECS, simulation, graphics, physics, content, audio, input и другие сервисы.

Сейчас основная архитектурная задача — не создавать новую структуру с нуля, а довести существующие границы до ясного и документированного состояния. Обнаруженные вопросы преимущественно относятся к согласованности точек композиции и направлений зависимостей; по одному факту наличия зависимости нельзя заключать, что она является дефектом.

## 3. Состав репозитория

В дереве master на исходном снимке найдено **44 файла проекта C# (.csproj)**.

### Фундамент и модель мира

- Engine.Core
- Engine.Memory
- Engine.Jobs
- Engine.Transform
- Engine.ECS
- Engine.World
- Engine.Simulations

### Игровые механизмы

- Engine.AI
- Engine.Animation
- Engine.Camera
- Engine.Navigation
- Engine.Particles
- Engine.Physics
- Engine.Scripting
- Engine.Networking
- Engine.Networking.Replication
- Engine.Localization
- Engine.UI

### Контент и сериализация

- Engine.Content
- Engine.Serialization

### Графика

- Engine.Graphics
- Engine.Graphics.2D
- Engine.Graphics3D
- Engine.Graphics.OpenGL

### Ввод, аудио, платформы и хостинг

- Engine.Input
- Engine.Input.SilkNet
- Engine.Audio
- Engine.Audio.OpenAL
- Engine.Platform
- Engine.Platform.SilkNet
- Engine.Hosting
- Engine.Hosting.Desktop
- Engine.Hosting.Desktop.SilkNet

### Runtime, инструменты и Editor

- Engine.Runtime
- Engine.Tooling
- Engine.Editor
- Engine.Editor.UI

### Сборка, тестирование и приложения

- BuildTools.Core
- BuildTools.CLI
- Engine.Tests
- Engine.Benchmarks
- Editor.Sandbox
- Game.Sandbox
- FullscreenProbe

Все просмотренные проекты используют .NET 8; Editor.Sandbox выделен в net8.0-windows. Это отражает текущую конфигурацию проекта, а не подтверждение поддержки всех ОС или платформ.

## 4. Основные границы и зависимости

Ниже приведены существенные прямые зависимости из csproj. Это не полный транзитивный граф; таблица предназначена для фиксации архитектурных границ.

| Проект | Существенные прямые зависимости | Роль в архитектуре |
|---|---|---|
| Engine.Core | Нет ProjectReference | Фундаментальные контракты и механизмы |
| Engine.Memory | Нет ProjectReference | Низкоуровневые механизмы памяти |
| Engine.Jobs | Engine.Core | Планирование задач |
| Engine.Transform | Engine.Core | Пространственные данные и трансформации |
| Engine.ECS | Core, Jobs, Memory, Transform | ECS-модель и работа с компонентами |
| Engine.World | Core, ECS, Jobs, Memory | Мир, spatial data и связанные механизмы |
| Engine.Simulations | Core, ECS, Networking, World | Симуляция и lockstep/rollback интеграции |
| Engine.Content | Engine.Core | Управление и загрузка ресурсов |
| Engine.Serialization | Audio, Content, Core, ECS, Graphics.2D, Physics, Scripting, UI, World | Сериализация конкретных типов и игровых данных |
| Engine.Tooling | Content, Core, ECS, Graphics, Input, Physics, World | Инспекция, диагностика, визуализация и профилирование |
| Engine.Runtime | Audio, Core, ECS, Scripting, Simulations, World, Physics, Graphics, Input, Tooling | Композиция и жизненный цикл runtime-подсистем |
| Engine.Hosting | Core, Camera, Graphics, Input, Audio | Общий контракт игрового хоста |
| Engine.Hosting.Desktop | Audio, Audio.OpenAL, Content, Core, Graphics, Input, Platform, Runtime | Стандартная desktop-композиция |
| Engine.Hosting.Desktop.SilkNet | Hosting.Desktop, Platform.SilkNet, Graphics.OpenGL, Runtime | Конкретная связка Silk.NET и OpenGL |
| Engine.Editor | Core, ECS, Serialization, Tooling, UI, World, BuildTools.Core | Предметная логика редактора и проектные операции |
| Engine.Editor.UI | Editor, Physics, Tooling, UI | UI-слой редактора |
| Engine.Graphics | Camera, Content, Core | Общие графические контракты и ресурсы |
| Engine.Graphics.2D | Animation, Camera, ECS, Graphics, Particles | 2D-команды и механизмы |
| Engine.Graphics3D | Core, ECS, Graphics | 3D-команды, данные и материалы |
| Engine.Graphics.OpenGL | Core, Graphics.2D, Graphics3D, Graphics | OpenGL-реализация графических команд |
| Engine.Platform | Input | Платформенные контракты |
| Engine.Platform.SilkNet | Platform, Input.SilkNet | Конкретная платформенная реализация |
| Engine.Audio.OpenAL | Engine.Audio | Конкретная реализация аудиоустройства |
| Engine.Input.SilkNet | Engine.Input | Конкретная реализация ввода |

Остальные специализированные проекты имеют относительно узкие зависимости. Полные списки прямых ссылок следует сверять с соответствующими csproj; актуальные ориентиры по проектам находятся в PROJECT_INDEX.md.

## 5. Подтверждённые архитектурные решения

### 5.1. Core — нижний слой

Engine.Core не имеет ProjectReference на другие проекты решения. Это подтверждает его положение в качестве фундаментального слоя и соответствует архитектурной цели сохранять Core независимым от ECS, Graphics, Physics, Editor и игровых модулей.

Engine.Memory также не имеет проектных зависимостей. Engine.Jobs и Engine.Transform зависят от Core.

### 5.2. ECS и World — самостоятельные механизмы

Engine.ECS зависит от Core, Jobs, Memory и Transform, но не от Graphics, Editor или конкретного backend.

Engine.World использует ECS и инфраструктуру Jobs/Memory. Такая граница позволяет использовать мир и его пространственные данные отдельно от редакторского UI и графической реализации.

### 5.3. Runtime — композиционный слой

Engine.Runtime зависит от основных игровых подсистем и содержит EngineRuntime, EngineGame и EngineRuntimeServices.

EngineRuntimeServices принимает интерфейсы графики, ввода, камеры, аудио и контента. EngineGame предоставляет точки расширения игрового проекта, в том числе ConfigureRuntime, OnInitialize, OnUpdate, OnFixedUpdate, OnRender и OnShutdown.

Это подходящая точка композиции для базового runtime. При дальнейшем развитии важно не превращать Engine.Runtime в место для произвольных игровых правил: здесь уместна интеграция общих подсистем, но не семантика конкретной игры.

### 5.4. Editor разделён на предметную логику и UI

Engine.Editor содержит контекст редактора, документы, действия, проекты, иерархию, инспекцию и состояние рабочего пространства.

Engine.Editor.UI содержит приложение редактора, панели, диалоги, authoring-представления и UI-команды.

Оба проекта не имеют прямой зависимости от OpenGL или Silk.NET. Конкретное приложение редактора собирается на уровне Editor.Sandbox, где выполняется настройка окна, графического устройства, UI и сервисов редактора.

### 5.5. Backend-реализации вынесены в отдельные проекты

Общие контракты и реализации разделены для:
- Audio и Audio.OpenAL;
- Input и Input.SilkNet;
- Platform и Platform.SilkNet;
- Graphics и Graphics.OpenGL;
- Hosting.Desktop и Hosting.Desktop.SilkNet.

Это снижает распространение конкретных библиотек по базовым контрактам. Конкретная готовность альтернативных backend не оценивается одним лишь наличием интерфейсов и реестров.

### 5.6. BuildTools не зависит от Engine

BuildTools.Core не имеет ProjectReference на проекты движка. Engine.Editor использует BuildTools.Core для операций подготовки игрового проекта и runtime-поставки. Это однонаправленная зависимость от инструментария сборки к редакторской операции; она не создаёт обратной зависимости BuildTools от Engine.

### 5.7. Sandbox остаётся на краю архитектуры

Editor.Sandbox и Game.Sandbox имеют широкие наборы прямых ссылок, поскольку выполняют композицию и интеграционную проверку множества подсистем.

Такие зависимости ожидаемы для запускаемых приложений и интеграционного полигона. Не следует переносить их полный набор в базовый игровой шаблон или в Engine.Editor.

## 6. Архитектурные вопросы для дальнейшего рассмотрения

Ниже перечислены наблюдения, а не подтверждённые дефекты. Код не менялся; решения по этим вопросам должны приниматься только после проверки конкретных потребительских сценариев.

### 6.1. Два подхода к игровому Host

В Engine.Hosting существует интерфейс IGameHost. При этом Engine.Hosting.Desktop не ссылается на Engine.Hosting, а предоставляет статический DesktopGameHost и собственные контракты IDesktopWindow, IDesktopPlatformBackend и IDesktopGraphicsBackend.

Нужно уточнить назначение IGameHost:
- используется ли он как самостоятельный контракт другими приложениями;
- является ли он альтернативным или историческим способом host integration;
- требуется ли согласование его жизненного цикла с DesktopGameHost.

Это вопрос консолидации архитектурной модели, а не основание удалять контракт без дополнительных проверок.

### 6.2. Неодинаковая конфигурация backend в Desktop Host

DesktopGameHost создаёт DesktopGraphicsBackendRegistry и DesktopPlatformBackendRegistry, после чего вызывает callbacks ConfigureGraphicsBackends и ConfigurePlatformBackends из DesktopGameHostOptions.

SilkNetDesktopHost регистрирует OpenGLDesktopGraphicsBackend и SilkNetDesktopPlatformBackend как стандартные providers, затем передаёт управление DesktopGameHost.

Аудио в DesktopGameHost создаётся непосредственно через AudioManager и OpenALAudioDevice. В DesktopGameHostOptions нет аналогичного аудио-реестра или фабрики.

Таким образом, графическая и платформенная границы имеют точки регистрации providers, а стандартная аудиокомпозиция жёстко использует OpenAL. Это следует учитывать, если появится реальная задача выбора нескольких аудиобэкендов. Сам факт наличия registry не подтверждает работу произвольной альтернативной реализации.

### 6.3. Runtime и Tooling

Engine.Runtime имеет прямую зависимость от Engine.Tooling. В EngineRuntimeOptions есть ToolingEnabled: этот параметр влияет на создание диагностических, инспекционных, профилировочных и debug-сервисов во время runtime.

При этом отключение Tooling не устраняет проектную зависимость Engine.Runtime от Engine.Tooling. Если в будущем потребуется минимальная runtime-поставка, уменьшение размера дистрибутива или независимое обновление development tools, эту границу следует оценить отдельно.

На текущем этапе нет оснований выносить Tooling в другой проект без таких требований.

### 6.4. Simulation и Networking

Engine.Simulations напрямую ссылается на Engine.Networking. В проекте находятся NetworkLockstep и связанные lockstep-типы, использующие сетевые session/channel API.

Базовый класс Simulation выполняет системы, команды, события и детерминированное состояние; сетевой механизм находится в специализированных типах lockstep.

Это осознанная интеграция, но она означает, что сборка Engine.Simulations транзитивно связана с Networking. В будущем можно оценить, нужен ли отдельный проект интеграции для случаев, когда игра использует локальную simulation без сетевого lockstep. Сейчас это не дефект и не требует изменения.

### 6.5. Serialization и Tooling как интеграционные проекты

Engine.Serialization зависит от многих типов движка, включая Audio, ECS, Graphics.2D, Physics, Scripting, UI и World. Engine.Tooling также зависит от нескольких runtime-подсистем.

Это указывает на роль данных проектов как интеграционных слоёв, а не минимальных универсальных библиотек. Важно сохранить это понимание в документации и не размещать в них фундаментальные контракты, которые должны принадлежать Core или специализированной подсистеме.

### 6.6. Editor и BuildTools

Engine.Editor напрямую ссылается на BuildTools.Core. В частности, ProjectCreationService использует runtime-часть BuildTools для подготовки создаваемого проекта.

Это допустимо как редакторская операция, но следует сохранять направление зависимости: BuildTools не должен начинать зависеть от Engine.Editor или UI редактора.

### 6.7. OpenGL backend знает о 2D и 3D командах

Engine.Graphics.OpenGL напрямую ссылается на Engine.Graphics.2D и Engine.Graphics3D. Это позволяет backend реализовывать конкретные типы команд этих подсистем.

Следствие: добавление нового graphics backend потребует реализации не только общих контрактов Engine.Graphics, но и согласованной поддержки нужных 2D/3D command families. Это не нарушает разделение API и backend, но уточняет реальный объём backend-интеграции.

## 7. Выявленное расхождение документации и исходников

При сверке обнаружены устаревшие записи, исправленные вместе с этим аудитом:

1. ENGINE.md описывал 3D как будущее направление и утверждал, что Physics содержит только 2D-реализацию. В исходниках уже существуют Engine.Graphics3D и отдельная область Engine.Physics/3D. Наличие этих исходников не означает полной готовности 3D workflow; это будет оцениваться отдельной задачей 0.7.
2. PROJECT_INDEX.md содержал устаревшие прямые зависимости для Editor.Sandbox, Engine.Editor, Engine.Graphics.OpenGL, Engine.Hosting.Desktop и Engine.Benchmarks; записи приведены к текущим csproj. Добавлен отдельный проект Engine.Hosting.Desktop.SilkNet.
3. SUPPORT_MATRIX.md описывал DesktopGameHost как напрямую создающий OpenGLWindow и SilkNetInputBackend. Текущая реализация использует реестры графического и платформенного providers, а стандартная связка регистрируется в SilkNetDesktopHost. При этом прямое создание OpenALAudioDevice в DesktopGameHost подтверждено и сохранено в описании ограничения.
4. README.md и план развития ссылались на отдельный docs/ROADMAP.md, который описывал частный workflow создания и запуска проекта. Этот файл удалён по решению владельца проекта; общая стратегия находится в ENGINE_DEVELOPMENT_PLAN.md, а подтверждённые сценарии и ограничения — в SUPPORT_MATRIX.md и READINESS_REPORT.md.

## 8. Итоги задачи 0.1

**Статус: завершено.**

Подтверждено:
- состав из 44 C#-проектов;
- независимое положение Engine.Core;
- базовые зависимости ECS/World;
- роль Engine.Runtime как слоя композиции;
- разделение Editor core и Editor UI;
- вынесение основных backend-реализаций в отдельные проекты;
- расположение Sandbox и BuildTools на уровне приложений/инструментов.

Зафиксированы архитектурные вопросы:
- соотношение IGameHost и DesktopGameHost;
- отсутствие симметричной регистрации аудио backend;
- зависимость Runtime от Tooling;
- интеграция Networking в Engine.Simulations;
- роль Serialization и Tooling как интеграционных проектов.

Ни один из этих пунктов не трактуется как автоматически требующий рефакторинга. Они передаются в соответствующие последующие аудиты и решения по развитию.

## 9. Следующий шаг

Следующая задача нулевого этапа — **0.2. Аудит Editor**.

В её рамках необходимо отдельно проверить Documents, Hierarchy, Selection, Inspector, Viewport, Actions/Undo/Redo, сохранение, состояние рабочего пространства и реальные сценарии редактирования. Архитектурные наблюдения из этого документа служат исходными данными, повторно проводить общий аудит зависимостей не требуется.
