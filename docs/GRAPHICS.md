# Graphics System — VibeEngine2

**Состояние документа:** 27 ноября 2026 года  
**Проверенная версия исходников перед обновлением документа:** `master`, commit `c56263d`  
**Назначение:** зафиксировать текущие возможности графической системы, её архитектуру и области, требующие дополнительной проверки.

---

## 1. Общая архитектура

Графическая система VibeEngine2 разделена на несколько проектов:

| Проект | Ответственность |
|---|---|
| `Engine.Graphics` | Общие графические контракты, команды, ресурсы и Render Pipeline |
| `Engine.Graphics.2D` | 2D-отрисовка, спрайты, тайловые карты и частицы |
| `Engine.Graphics.OpenGL` | Реализация графического backend через OpenGL |
| `Engine.Graphics3D` | 3D-графические команды, ресурсы и освещение |
| `Engine.Camera` | Камера и координатные преобразования |

Основной принцип: общие графические контракты не должны зависеть от конкретной реализации графического API.

Текущая стандартная реализация desktop-графики — OpenGL через Silk.NET.

## 2. Graphics API

`Engine.Graphics` предоставляет:

- `IGraphics`;
- `IGraphicsDevice`;
- `IRenderCommand`;
- `RenderQueue`;
- `RenderPipeline`;
- `RenderPass`;
- `RenderState`;
- `RenderTargetHandle`;
- `TextureHandle`;
- `ShaderHandle`;
- `GraphicsBufferHandle`;
- менеджеры графических ресурсов.

`IGraphicsDevice` предоставляет управление кадром:

1. `BeginFrame()`;
2. `Submit(...)`;
3. `EndFrame()`.

Графические команды отправляются устройству, а не выполняют OpenGL-операции непосредственно.

## 3. Render Queue

`RenderQueue` отвечает за накопление команд отрисовки.

Поддерживаются:

- отправка графических команд;
- отправка данных отрисовки мировых текстур;
- сортировка по слоям;
- сохранение порядка отправки внутри одного слоя;
- очистка очереди между кадрами.

В текущей реализации сортировка выполняется по `Layer`, затем по порядку отправки команды.

## 4. Render Pipeline

Render Pipeline определяет последовательность графических проходов.

Поддерживаются:

- несколько Render Passes;
- отдельные Render Targets;
- фильтрация команд по диапазону слоёв;
- индивидуальные Render States;
- очистка цветового и глубинного буфера как параметры прохода;
- передача дополнительных Context Data;
- Post Processing.

Для стандартной 2D-конфигурации предусмотрены:

- World;
- UI;
- Debug;
- Present.

World Pass является источником изображения для Post Processing.

## 5. Render Graph

В текущем OpenGL backend выполнение кадров организовано через Render Graph.

`RenderPipelineGraphBuilder` преобразует конфигурацию Render Pipeline в граф выполнения.

`RenderGraphCompiler` выполняет:

- проверку структуры графа;
- проверку зависимостей ресурсов;
- построение порядка выполнения;
- топологическую сортировку;
- определение проходов, влияющих на результат;
- формирование оптимизированного плана выполнения.

`RenderGraphExecutor` отвечает за исполнение скомпилированного графа.

Поддерживаются:

- transient resources;
- импорт существующих Render Targets;
- импорт Backbuffer;
- чтение и запись графических ресурсов;
- зависимости между проходами;
- управление временем жизни временных ресурсов;
- повторное использование временных Render Targets;
- удержание transient outputs через `RenderGraphExecutionResult`;
- возврат output target в пул при `RenderGraphExecutionResult.Dispose()`;
- уничтожение всех targets пула, включая удерживаемые результатами, при `RenderGraphExecutor.Dispose()`.

Текущая реализация поддерживает один цветовой выход на Render Pass.

Также существует механизм расширения графа через `IRenderGraphExtensionRegistry`.

## 6. Post Processing

Для постобработки предусмотрена `PostProcessChain`.

Поддерживаются:

- подключение графических эффектов;
- включение и отключение эффектов;
- выполнение fullscreen passes;
- использование пользовательских шейдеров;
- последовательная обработка результата;
- Ping-Pong Render Targets.

Результат World Pass передаётся на обработку, после чего выполняется презентация изображения.

## 7. Графические ресурсы

### Textures

Поддерживаются:

- создание и освобождение текстур;
- Texture Handles;
- Texture Description;
- цветовые пространства;
- настройки фильтрации и адресации;
- загрузка изображений через Content Pipeline.

Текущие поддерживаемые расширения загрузчика изображений:

- PNG;
- JPG / JPEG;
- BMP;
- TGA.

Данные изображения преобразуются в RGBA8.

### Render Targets

Поддерживаются:

- создание;
- изменение размера;
- получение Color Texture;
- использование Depth Attachment;
- освобождение ресурсов.

Render Target Manager управляет связанными Framebuffer, текстурами и Depth Buffer.

### Shaders

Реализованы:

- создание Shader Programs;
- Shader Handles;
- компиляция и связывание программ;
- управление жизненным циклом;
- получение программ по Handle.

Текущий OpenGL Shader Manager поддерживает GLSL Vertex и Fragment Shader с точкой входа `main`.

### Buffers

Реализованы:

- Graphics Buffer Manager;
- создание и обновление буферов;
- Vertex Buffers;
- Index Buffers;
- освобождение GPU-ресурсов.

### Meshes

В `Engine.Graphics3D` и OpenGL backend предусмотрены:

- Mesh Data;
- Mesh Handles;
- создание Mesh;
- Vertex Layout;
- Vertex Array Objects;
- индексированная отрисовка.

## 8. 2D Rendering

Основные компоненты:

- `SpriteComponent`;
- `SpriteMaterialComponent`;
- `SpriteRenderSystem`;
- `SpriteRenderer`;
- `TilemapRenderer`;
- рендереры частиц.

Для спрайтов реализованы:

- загрузка текстур;
- изменение размеров;
- позиционирование;
- вращение;
- отрицательный масштаб и отражение UV;
- использование слоёв;
- Camera Culling;
- поддержка дополнительных каналов материала.

Отрисовка спрайтов использует ECS и World Transform.

Позиция Transform используется как центр спрайта.

## 9. Normal Mapping и Lighting

Для 2D предусмотрена работа с дополнительными каналами материалов:

- Albedo;
- Normal;
- Emission.

`SpriteMaterialComponent` содержит пути дополнительных текстур и параметры материала.

`SpriteRenderSystem` загружает необходимые ресурсы и формирует графические команды с данными материала.

В OpenGL backend присутствует `OpenGLNormalBufferRenderer2D`.

Поддерживаются:

- отрисовка Normal Maps;
- передача Normal Transform;
- изменение силы Normal Mapping;
- преобразование координат с учётом камеры;
- обработка вращения и отражений;
- работа с отдельным Normal Buffer Pass.

## 10. 3D Graphics

В `Engine.Graphics3D` реализованы графические механизмы для 3D:

- Mesh;
- Mesh Rendering;
- Model, View и Projection matrices;
- Material3D;
- Point Lights;
- Directional Lights;
- 3D Render Pass.

В OpenGL backend существует `OpenGLMeshRenderer`.

В `Game.Sandbox` имеется отдельный 3D Preview workflow, используемый для проверки взаимодействия графических компонентов.

Наличие этих механизмов не означает подтверждённую готовность универсального 3D-рендера для всех игровых сценариев.

## 11. Render State

`RenderState` предоставляет управление:

- Blending;
- Depth Test;
- Depth Write;
- Depth Function;
- Culling;
- Polygon Mode;
- Polygon Offset;
- Scissor Test.

`OpenGLRenderState` хранит текущее состояние и применяет только необходимые изменения, когда его кэш актуален.

Также предусмотрена инвалидация кэша состояния.

## 12. OpenGL Rendering

В OpenGL backend реализованы специализированные компоненты:

- `OpenGLTextureRenderer`;
- `OpenGLDebugRenderer`;
- `OpenGLFontRenderer`;
- `OpenGLNormalBufferRenderer2D`;
- `OpenGLMeshRenderer`;
- `OpenGLRenderCommandDispatcher`;
- `OpenGLRenderPassExecutor`;
- `OpenGLFullscreenRenderer`.

`OpenGLRenderCommandDispatcher` выбирает соответствующий renderer и управляет его переключением.

`OpenGLRenderPassExecutor` отвечает за Framebuffer, Viewport, Render State и исполнение проходов.

## 13. Производительность

Текущие механизмы оптимизации:

- сортировка Render Queue;
- Texture Batching;
- повторное использование временных Render Targets;
- кэширование OpenGL Render State;
- Camera Culling;
- предварительно выделенные массивы вершин в Texture Renderer.

В `OpenGLTextureRenderer` предусмотрены следующие размеры пакета:

| Параметр | Значение |
|---|---:|
| Максимальное количество спрайтов | 1024 |
| Максимальное количество текстур | 16 |

Texture Renderer объединяет несколько команд в пакет и отправляет их на GPU при необходимости Flush.

Существующие CPU-бенчмарки позволяют оценивать формирование команд и некоторые расходы managed memory.

Они не заменяют измерения реальной производительности GPU.

## 14. Тестирование

Существуют автоматические тесты следующих областей:

| Тесты | Проверяемая область |
|---|---|
| `CameraTests` | Camera, Viewport, Zoom и координатные преобразования |
| `SpriteRenderSystemTests` | ECS-спрайты, материалы, вращение и масштабирование |
| `RenderQueueTests` | Порядок выполнения команд |
| `RenderPipelineTests` | Проходы, слои, порядок и Post Processing |
| `RenderGraphCompilerTests` | Проверка структуры графа, зависимостей и допустимости операций с ресурсами |
| `RenderGraphExecutorTests` | Выполнение проходов, разрешение ресурсов и жизненный цикл transient targets |
| `RenderPipelineGraphBuilderTests` | Преобразование Render Pipeline в Render Graph |
| `SceneRenderingIntegrationTests` | Восстановление сцены и отрисовка спрайтов |

Также существуют тесты Content Pipeline для загрузки TileSet и Tilemap.

`Engine.Benchmarks` содержит отдельный бенчмарк Tilemap Rendering.

Текущий `TilemapRenderingBenchmark` измеряет формирование команд и managed allocations без использования GPU.

Наличие теста подтверждает только покрытые им сценарии.

## 15. Области дополнительной проверки

Следующие области требуют отдельной системной проверки.

### 15.1. Координатные пространства и размеры

- Logical Window Size;
- Framebuffer Size;
- Viewport;
- разные размеры Render Targets;
- DPI Scaling;
- изменение размеров окна;
- изменение размеров Render Target;
- Camera Zoom и Camera Movement.

### 15.2. Взаимодействие Render Passes

- последовательность World, UI и Debug;
- Post Processing;
- Render Graph Extensions;
- переключение Render Targets;
- сохранение графического состояния;
- взаимодействие 2D и 3D проходов.

### 15.3. 2D Lighting

- Normal Buffer;
- Sprite Materials;
- Normal Transform;
- взаимодействие Camera и Normal Buffer;
- разные размеры Render Targets;
- сочетание Normal Mapping и других графических проходов.

### 15.4. Управление ресурсами

- жизненный цикл текстур;
- жизненный цикл Render Targets;
- Resize;
- освобождение временных ресурсов;
- повторное использование ресурсов;
- управление Mesh и Buffer;
- освобождение ресурсов при ошибках выполнения.

### 15.5. OpenGL State

- Blending;
- Depth Test;
- Depth Write;
- Scissor;
- Culling;
- переключение состояний;
- восстановление состояния после fullscreen passes;
- инвалидация кэша.

### 15.6. Производительность

- фактическое количество Draw Calls;
- количество Flush;
- переключение текстур;
- переключение Shader Programs;
- количество переключений Render State;
- CPU-время выполнения Render Graph;
- GPU-время выполнения кадра;
- managed allocations;
- поведение при большом количестве спрайтов и Render Passes.

---

### 15.7. Результаты пройденных этапов аудита Render Graph

В рамках аудита Render Graph отдельно проверены следующие сценарии.

| Область | Проверка | Статус |
|---|---|---|
| Transient resource и Load | Запрещено загружать transient resource через `Load` до появления содержимого. Проверка уже присутствует в `RenderGraphBuilder.ValidateDeclarations()`; локально выровнено ожидание теста с фактическим сообщением об ошибке | Проверено, тест зелёный |
| Независимая очистка Color и Depth | `ClearDepth` отделён от `ClearColor` и передаётся через Render Pipeline и Render Graph независимо. Значение по умолчанию сохраняет прежнее поведение: `ClearDepth = ClearColor` | Локально реализовано и проверено |
| Наличие Depth Attachment | Компилятор отклоняет проход, который объявляет Depth Attachment для ресурса без depth buffer в descriptor | Локально реализовано и проверено |
| Содержимое Depth после DontCare | Для импортированного ресурса запрещена последующая загрузка depth-содержимого после `StoreAction.DontCare` | Локально реализовано и проверено |
| Pass без Color Output | Side-effect pass может выполнить callback без открытия Render Pass; для прохода с Color Output вызов `End()` гарантирован после успешного `Begin()`, в том числе при исключении во время выполнения | Проверено тестами |
| Владение transient outputs | Результат удерживает output target как lease в пуле исполнителя. `RenderGraphExecutionResult.Dispose()` возвращает lease в пул; `RenderGraphExecutor.Dispose()` уничтожает все targets пула, включая ещё удерживаемые результатами | Подтверждено существующими тестами |

Все соответствующие проверки, выполненные в локальной рабочей версии, прошли успешно.

**Важно о состоянии Git:** на момент этой сверки опубликованный `master` находился на `c56263d`. Проверки независимой очистки Color/Depth, наличия Depth Attachment и перехода depth-содержимого через `DontCare` были выполнены в локальной рабочей версии и ещё не представлены в этом коммите. Эта запись фиксирует результаты аудита и локального тестирования, а не утверждает, что все перечисленные изменения уже опубликованы в `master`.

---

## 16. Критерии завершения аудита

Аудит графической системы считается завершённым после:

1. Проверки архитектурных границ между Graphics, Graphics2D и OpenGL.
2. Проверки взаимодействия Render Pipeline и Render Graph.
3. Проверки управления ресурсами.
4. Проверки основных графических проходов.
5. Проверки взаимодействия 2D Rendering и Lighting.
6. Проверки графических состояний.
7. Оценки производительности на основании измерений.
8. Фиксации оставшихся ограничений и непроверенных сценариев.

## 17. Принцип развития

Развитие системы должно основываться на реальных требованиях и выявленных проблемах.

Не следует добавлять архитектурные слои или усложнять существующие механизмы исключительно ради абстракций.

Существующие графические возможности необходимо сохранять и проверять при изменениях.

**Этот документ описывает текущее состояние графической системы VibeEngine2 и границы проведённого аудита. Он не является заявлением о полной поддержке всех возможностей графического API.**
