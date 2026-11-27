# Аудит 3D Engine VibeEngine2

**Дата:** 27 ноября 2026 года  
**Ветка:** `master`  
**Исходная ревизия:** `0eef5cdeee4bb7a8ad42294d06a655ff5926ace1`  
**Тип проверки:** статический анализ исходников, проектных зависимостей, дерева файлов и тестовых сценариев. Сборка, автоматические тесты и ручной 3D preview в рамках этого этапа не запускались.

## 1. Итог

В репозитории есть работающий по архитектуре базовый путь передачи индексированной 3D-геометрии в OpenGL: ECS-компонент `MeshRenderer3D` обрабатывается отдельным `MeshRenderSystem3D`, формируется `DrawMeshCommand`, команда проходит через OpenGL dispatcher и рисуется через `OpenGLMeshRenderer`. `Game.Sandbox` содержит самостоятельный 3D preview, который собирает этот путь вручную.

При этом 3D-направление пока нельзя считать завершённой системой создания игр. Основные ограничения находятся не только в количестве графических функций, а в связях между подсистемами:

- 3D rendering не включён в стандартный `EngineRuntime` и требует ручной композиции в пользовательском коде.
- `EngineRuntimeServices` предоставляет `Camera2D`, но не `Camera3D`.
- Стандартная сериализация ECS не содержит 3D transform, mesh renderer и 3D physics serializers.
- В Content Pipeline не обнаружен импорт моделей или загрузчик mesh/model assets.
- Editor сохраняет 2D workflow; 3D Scene View, authoring meshes/materials и 3D gizmos не обнаружены.
- Physics 3D содержит данные тел, коллайдеров, формы, proxy и pairwise collision detector, но не полный физический цикл.
- Generic Animation API позволяет применять типизированные значения через callback, но не образует skeletal animation workflow.

**Общая оценка:** 3D графический фундамент — частично реализован; интеграция в стандартный жизненный цикл, импорт, сохранение, редактор и полноценная 3D-физика — не завершены.

## 2. Графические проекты и ответственность

| Проект | Фактическая роль |
|---|---|
| `Engine.Graphics` | Общие графические контракты, render commands/passes, shader и buffer abstractions |
| `Engine.Graphics3D` | Mesh data, mesh handles, ECS renderer component, 3D render command, базовые material/light data и отдельная render system |
| `Engine.Graphics.OpenGL` | Реализация mesh manager и renderer; обработка `DrawMeshCommand` в dispatcher |
| `Engine.Camera` | Независимые от backend `Camera2D`, `Camera3D` и контроллер 3D-камеры |
| `Engine.ECS` / `Engine.Transform` | Обычные и fixed-point 3D transforms, связи parent-child и расчёт world transforms |
| `Game.Sandbox` | Отдельная демонстрационная сборка 3D preview; не является стандартной runtime-композицией |

Проект `Engine.Graphics3D` напрямую зависит от `Engine.Core`, `Engine.ECS` и `Engine.Graphics`. OpenGL backend ссылается на `Engine.Graphics3D`. При этом `Engine.Runtime.csproj` не ссылается на `Engine.Graphics3D`, а `Engine.Serialization.csproj`, `Engine.Editor.csproj` и `Engine.Editor.UI.csproj` не имеют прямой ссылки на 3D-графический проект.

## 3. Mesh и путь от ECS до GPU

### 3.1. Данные и ресурсы

Реализованы:

- `Vertex3D` и `VertexLayout` для вершинных атрибутов;
- immutable CPU-side `MeshData` с indexed triangle geometry и проверкой индексов;
- `MeshPrimitives` для создания базовых примитивов: triangle, cube, tetrahedron и UV sphere;
- `MeshHandle`, `MeshDescription`, `IMeshManager` и `IMeshManagerProvider`;
- `MeshRenderer3D` с mesh/shader handles, material, layer и Enabled.

`MeshData` — геометрия в памяти. В рассмотренном Content Pipeline нет model/mesh asset loader, формата model resource, импорта или автоматического перехода от файла модели к `MeshData`. Таким образом, Sandbox создаёт геометрию фабриками примитивов; произвольные модели должны быть подготовлены и переданы через код/API вручную.

### 3.2. Runtime rendering path

Подтверждённый кодом путь выглядит так:

```text
ECS World
  └─ MeshRenderer3D + WorldTransform3D
       ↓
MeshRenderSystem3D.Render()
       ↓
DrawMeshCommand (mesh, shader, model, view, projection, material)
       ↓
IGraphicsDevice.Submit()
       ↓
OpenGLRenderCommandDispatcher
       ↓
OpenGLMeshRenderer.Draw()
       ↓
OpenGLMeshManager / VAO / vertex & index buffers
       ↓
OpenGL DrawElements(Triangles)
```

`MeshRenderSystem3D` выбирает сущности по `MeshRenderer3D` + `WorldTransform3D`, пропускает выключенные/невалидные компоненты и отсутствующие mesh/shader handles, собирает model matrix и отправляет команду. OpenGL dispatcher завершает текущий batch 2D renderer, включает depth test на время mesh draw и восстанавливает состояние render pass после команды.

OpenGL mesh manager создаёт vertex/index buffers и VAO, настраивает позиции, нормали и texture coordinates и управляет освобождением ресурсов. Это базовая реализация indexed 3D draw, а не полный renderer с импортом моделей, инстансингом, LOD, skinning или полноценной системой материалов.

### 3.3. Material и lighting

`Material3D` содержит BaseColor, AmbientStrength и DiffuseStrength. `SceneLighting3D` содержит один directional light и до четырёх point lights. OpenGL renderer передаёт эти значения в shader uniforms.

В проверенном material API нет PBR-набора параметров, material texture maps, normal maps или emissive workflow. В 3D lighting API не обнаружена реализация shadow maps/теневых проходов. Поэтому текущий 3D материал — базовая цветовая модель с ambient/diffuse lighting, а не универсальный production material pipeline.

`RenderPass3D.CreateWorld` формирует world pass с depth testing и back-face culling. Контекст освещения передаётся через `RenderPass.ContextData`; OpenGL executor извлекает `SceneLighting3D` и устанавливает lighting context в dispatcher.

## 4. Camera и Transform 3D

### Camera3D

`Camera3D` — backend-independent API с позицией, quaternion rotation, FOV, aspect ratio, near/far planes, ViewMatrix, ProjectionMatrix, LookAt и SetViewportSize. `Camera3DController` добавляет перемещение относительно ориентации камеры, yaw/pitch, mouse look и zoom.

Изоляция камеры от backend реализована правильно. Но стандартный `EngineRuntimeServices` содержит только `Camera2D`; host/runtime не предоставляет аналогичную готовую точку внедрения 3D-камеры. В Sandbox камера создаётся и обслуживается вручную.

### Transform и иерархия

Доступны float и fixed-point наборы:

- `LocalTransform3D`, `WorldTransform3D`, `TransformParent3D`;
- `FixedLocalTransform3D`, `FixedWorldTransform3D`, `FixedTransformParent3D`;
- `TransformHierarchy3DSystem` для Update;
- `FixedTransformHierarchy3DSystem` для FixedUpdate.

Тестовые исходники покрывают root/child/nested hierarchy, влияние rotation и scale, а также отсутствующие компоненты, родителя и циклы.

Однако стандартный `EngineRuntime` автоматически регистрирует 2D Transform hierarchy и 2D physics ordering; 3D hierarchy systems в стандартном scheduler не добавлены. В 3D preview обычная hierarchy system создаётся и вызывается вручную. Поэтому наличие 3D Transform API не означает готовность 3D world lifecycle.

## 5. Physics 3D

В `Engine.Physics/3D` есть следующие строительные блоки:

- `PhysicsBody3D`: Static/Dynamic/Kinematic, fixed-point масса, linear/angular velocity, force/torque, диагональная inertia, damping и sleeping;
- `Collider3D`: shape, offset, rotation, material, trigger, enabled, collision layer/mask;
- `PhysicsShape3D`: Sphere, Box, Capsule и Plane;
- `PhysicsBounds3` и `PhysicsColliderProxy3D`;
- `PhysicsSettings3D`: gravity, substeps, position/velocity iteration counts и sleep/correction settings;
- `CollisionDetector3D`: pairwise detection с выдачей contact manifold.

### Ограничение narrow phase

По switch в `CollisionDetector3D.TryDetect` явно обрабатываются следующие пары (с обратным порядком для симметричных случаев):

- Sphere – Sphere;
- Sphere – Box;
- Sphere – Plane;
- Box – Plane;
- Capsule – Plane.

Остальные сочетания в текущем detector возвращают false. В частности, в switch не обнаружены Box–Box, Sphere–Capsule, Capsule–Capsule и Capsule–Box.

### Отсутствующий полный physics cycle

В дереве 3D physics source не обнаружены отдельные реализации:

- physics stepping/system, подключённая к ECS FixedUpdate;
- реальный broad-phase manager/algorithm (есть proxy и bounds, но не broad-phase implementation);
- contact solver и collision response/resolver;
- 3D physics queries/raycast;
- joints/constraints;
- интеграция 3D physics с Runtime, Transform synchronization и debug visualization.

Наличие параметров итераций в `PhysicsSettings3D` само по себе не подтверждает наличие solver, использующего их. В standard Runtime работает `PhysicsSystem2D`; 3D physics types в этот цикл не включены.

## 6. Animation 3D

`Engine.Animation` содержит generic `AnimationClip`, типизированные tracks/keyframes, интерполяторы (в том числе Vector3), `AnimationPlayer` и `AnimationBinding<T>`, где приложение задаёт callback для применения семплированного значения.

Это полезный общий механизм анимации значений. Но в исходниках не обнаружены 3D skeleton/bone hierarchy, skin weights, inverse bind matrices, skinned mesh renderer, skeletal animation system или model animation importer. Автоматическая привязка animation tracks к 3D ECS transforms также не предоставлена как готовый workflow. Поэтому generic Animation API нельзя приравнивать к поддержке skeletal Animation 3D.

## 7. Import, Content и serialization

### Import и Content

В полном дереве текущей ревизии нет файлов поддерживаемых model formats (`.obj`, `.gltf`, `.glb`, `.fbx`, `.dae`, `.3ds`) и нет импортера/loader, преобразующего модель в mesh/material ресурсы. `MeshData` пока создаётся напрямую в коде.

### ECS serialization

В `Engine.Serialization/Types` присутствуют 2D transform и 2D physics serializers, а также другие 2D/game serializers. 3D serializers для `LocalTransform3D`, `WorldTransform3D`, `TransformParent3D`, `MeshRenderer3D`, `PhysicsBody3D` и `Collider3D` в дереве не обнаружены. Сам проект сериализации напрямую зависит от Graphics2D, но не Graphics3D.

Следствие: стандартный world save/load не предоставляет готового встроенного round-trip workflow для перечисленных 3D компонентов. Приложение может добавить собственные serializers, но это отдельная работа и ответственность пользовательской игры.

## 8. Editor workflow

В `Engine.Editor` и `Engine.Editor.UI` нет прямой зависимости от `Engine.Graphics3D`. В каталогах этих проектов не обнаружены 3D Scene View, mesh/model asset editor, material editor для `Material3D`, 3D transform commands или 3D gizmos.

Существующий Editor viewport и hierarchy workflow остаются 2D-ориентированными. Следовательно, 3D-объекты нельзя полноценно создавать, компоновать, редактировать и сохранять через стандартный Editor workflow. Sandbox preview является программной демонстрацией, а не заменой Scene View.

## 9. Интеграция в Game.Sandbox и Runtime

`Sandbox3DPreviewGame` вручную:

1. получает `IMeshManagerProvider` от графического device;
2. создаёт `Camera3D` и `Camera3DController`;
3. очищает pipeline и добавляет `RenderPass3D`;
4. создаёт GLSL/HLSL shader source и GPU mesh ресурсы из примитивов;
5. создаёт `TransformHierarchy3DSystem` и `MeshRenderSystem3D`;
6. добавляет ECS сущности с `LocalTransform3D`, `WorldTransform3D`, `MeshRenderer3D`;
7. вручную обновляет 3D hierarchy в `OnUpdate` и вызывает renderer в `OnRender`;
8. вручную освобождает mesh и shader handles.

Это полезный интеграционный пример и подтверждение того, что базовые части можно скомпоновать в одном приложении. Однако он не проверяет автоматическую регистрацию в Runtime, загрузку 3D assets, сохранение сцены или editor authoring. Также в `EngineRuntimeServices` по-прежнему есть только `Camera2D`.

## 10. Тестовое покрытие, найденное в исходниках

В репозитории имеются unit-тесты для:

- `Camera3D` и `Camera3DController`;
- `DirectionalLight3D`, `DrawMeshCommand`, `Material3D`;
- `MeshData`, `MeshPrimitives`, `MeshResource`, `RenderPass3D`;
- `PhysicsBody3D` и `PhysicsShape3D`;
- float/fixed 3D Transform и обеих hierarchy systems.

В просмотренном наборе не обнаружены тесты для `MeshRenderSystem3D`, end-to-end прохождения mesh draw через OpenGL backend, 3D model import/content loading, 3D ECS serialization, 3D editor workflow, полного Physics 3D step/solver и pairwise `CollisionDetector3D`.

Это перечень тестовых файлов и заявленных ими сценариев. Тесты в рамках этого аудита не запускались; прохождение текущего набора не утверждается.

## 11. Матрица готовности

| Возможность | Оценка | Что подтверждено / что отсутствует |
|---|---|---|
| Mesh data и базовые примитивы | Частично | CPU geometry, handles и создание GPU mesh; нет model import и asset lifecycle |
| Базовый 3D draw в OpenGL | Частично | ECS component → command → dispatcher → OpenGL draw; вручную собранный Sandbox path, нет подтверждённого общего workflow |
| Camera3D | Частично | Camera API и controller существуют; нет стандартной runtime service/active camera integration |
| Float/fixed 3D Transform | Частично | Данные и hierarchy systems с unit tests; нет стандартной регистрации и serializer round-trip |
| Material3D и lighting | Частично | Base color, ambient/diffuse, directional + до 4 point lights; нет PBR, texture maps и shadow maps |
| Physics 3D | Не готово как complete subsystem | Данные, формы, proxies и ограниченный pair detector; нет world stepping, broad phase, solver, queries, joints и Runtime integration |
| Animation 3D / skeletal animation | Не готово | Есть generic value tracks/bindings; нет skeleton, skinning и готового binding workflow |
| Model import / Content Pipeline | Не готово | Не найден importer/model loader или формат model asset |
| 3D scene serialization | Не готово в стандартном наборе | Нет built-in 3D component serializers |
| 3D Editor workflow | Не готово | Нет 3D Scene View и authoring tools |

## 12. Рекомендуемая последовательность развития

Это вывод аудита, а не изменение кода или утверждение нового плана реализации:

1. **Runtime foundation:** определить жизненный цикл активной Camera3D, TransformHierarchy3DSystem и MeshRenderSystem3D; интегрировать 3D systems в Runtime/Render Pipeline без ручного вызова из каждого Game.
2. **Scene persistence:** добавить и проверить сериализуемые 3D-компоненты и ссылки на mesh/material assets; подтвердить полный scene save/load round trip.
3. **Content assets:** определить mesh/model/material asset model и импорт (отдельно оценить glTF), владение GPU ресурсами и повторную загрузку.
4. **3D Editor:** построить Scene View, 3D transforms/gizmos, Inspector для camera/light/mesh/material и authoring workflow, согласованный с persistence.
5. **Physics 3D:** реализовать законченный fixed-step pipeline с broad phase, contact generation, solver/response, queries и Runtime synchronization; покрыть supported shape pairs тестами.
6. **Advanced rendering/animation:** расширять PBR, textures, shadows, skeletal animation и skinning после закрепления базового scene/content/runtime workflow.

Порядок Physics 3D и model workflow может уточняться отдельным техническим планом, но выдавать текущие 3D data types за готовые системы нельзя.

## 13. Источники в репозитории

Основные исходники:

- `Engine.Graphics3D/**`;
- `Engine.Graphics.OpenGL/Rendering/OpenGLRenderCommandDispatcher.cs`;
- `Engine.Graphics.OpenGL/Rendering/OpenGLMeshRenderer.cs`;
- `Engine.Graphics.OpenGL/Resources/OpenGLMeshManager.cs`;
- `Engine.Camera/Camera3D.cs`, `Camera3DController.cs`;
- `Engine.ECS/Components/*Transform3D.cs`, `TransformParent3D.cs`;
- `Engine.ECS/Systems/TransformHierarchy3DSystem.cs`, `FixedTransformHierarchy3DSystem.cs`;
- `Engine.Physics/3D/**`;
- `Engine.Animation/**`;
- `Game.Sandbox/Sandbox3DPreviewGame.cs`;
- project references in `Engine.Runtime.csproj`, `EngineRuntimeServices.cs`, `Engine.Serialization.csproj`, `Engine.Editor.csproj`, `Engine.Editor.UI.csproj`.

Связанные документы: [Аудит Scene и World](03-SCENE-AND-WORLD.md), [Аудит Runtime](04-RUNTIME.md), [Аудит 2D Engine](05-2D-ENGINE.md), [Графическая система](../GRAPHICS.md).
