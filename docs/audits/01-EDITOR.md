# Аудит Editor VibeEngine2 (0.2)

**Дата аудита:** 2026-11-27  
**Репозиторий:** Kostosik/VibeEngine2  
**Ветка:** master  
**Исходный снимок:** 13d39392294c96d7034618547033b14f5b1927f4  
**Статус:** статический аудит исходников и тестовых файлов завершён.

## 1. Цель и границы

Проверена архитектура Editor и основные пользовательские сценарии:

- управление открытыми документами;
- создание, открытие, сохранение и закрытие;
- состояние изменений и предупреждения при закрытии;
- Hierarchy и Selection;
- Inspector и добавление компонентов;
- Scene View / Viewport;
- Undo / Redo;
- панели, docking и сохранение workspace;
- точки расширения редактора;
- интеграция с активным игровым проектом.

Проверялись исходники Engine.Editor, Engine.Editor.UI, Editor.Sandbox и список тестов Engine.Tests/Editor.

Это статический аудит исходников, проектных файлов и наличия соответствующих тестов. Сборка, запуск Editor и выполнение тестов в рамках задачи 0.2 не выполнялись. Поэтому наличие теста не выдаётся за повторно полученный результат его выполнения.

Код и тесты не изменялись.

## 2. Итоговая оценка

| Область | Оценка | Основание |
|---|---|---|
| Разделение Editor core и UI | Готово | Engine.Editor содержит предметную модель и сервисы; Engine.Editor.UI — приложение и визуальные представления |
| Управление документами | Готово с ограничениями | Общий IEditorDocument, EditorSession.AllDocuments, открытие/сохранение нескольких форматов и dirty-close workflow |
| Hierarchy | Частично | Рабочая ECS-иерархия для World/EditorDocument, EntityName, TransformParent2D, базовые create/rename/delete и действия |
| Inspector | Частично | Компоненты регистрируются явно; отражение публичных свойств и набор UI property editors; редактирование выбранной одной сущности |
| Scene View / Viewport | Частично | 2D viewport, pan/zoom, hit test, move/rotate/scale gizmos; 3D Scene View в проверенном UI workflow не обнаружен |
| Undo / Redo | Частично | MainShell поддерживает World, UI Document, Tilemap и Prefab; другие открываемые типы документов в меню Undo/Redo не включены |
| Workspace persistence | Частично | API и сериализация состояния существуют, но не подключены к штатному запуску Editor.Sandbox |
| Editor session persistence | Частично | Сохранение сессии ограничено World-документами; в штатном запуске Editor.Sandbox не подключено |
| Расширяемость | Есть базовые точки регистрации | Панели, действия, hierarchy actions, типы компонентов, property providers и UI property editors регистрируются явно |

Эта оценка относится к текущим проверенным путям Editor. Она не отменяет историческую общую оценку первой версии в READINESS_REPORT.md, но уточняет границы конкретных редакторских возможностей.

## 3. Архитектура Editor

### 3.1. Разделение предметной логики и UI

Подтверждено разделение двух сборок:

- Engine.Editor — EditorContext, EditorSession, документы, команды, действия, Asset Browser, Hierarchy source, Inspector model, Viewport state/transform, Workspace и проектные сервисы.
- Engine.Editor.UI — EditorApplication, EditorMainShell, панели, UI представления, диалоги, обработчики клавиатуры и property editors.
- Editor.Sandbox — запускаемое приложение, где настраиваются окно, графика, ввод, UI, сериализация и сервисы редактора.

Engine.Editor и Engine.Editor.UI не имеют прямой зависимости от OpenGL или Silk.NET. Конкретный backend и окно подключаются на уровне запускаемого Editor приложения.

Это соответствует архитектурной границе: визуальное представление не должно становиться обязательной частью предметной модели документов и команд.

### 3.2. Центральная композиция

EditorContext объединяет:
- EditorSession;
- EditorWorkspace;
- EditorActionRegistry;
- EditorComponentTypeRegistry;
- EditorComponentDependencyRegistry;
- активный игровой проект;
- Asset Browser;
- фабрики и операции открытия/сохранения документов.

EditorApplication связывает Editor с UI, диалогами, активным проектом, запуском игрового процесса, консолью и событиями панелей.

Фактический orchestration-код сосредоточен в крупных классах: EditorApplication, EditorMainShell, HierarchyPanelView, InspectorPanelView, ViewportPanelView и EditorWorkspaceView. Это наблюдение о концентрации ответственности, а не утверждение о дефекте. Рефакторинг этих классов не предлагается без конкретной задачи, которая покажет реальное затруднение сопровождения.

## 4. Документы и их жизненный цикл

### 4.1. Общий контракт

IEditorDocument задаёт:
- DocumentTypeId;
- DisplayName;
- FilePath;
- IsDirty;
- MarkSaved().

EditorSession хранит два представления:
- AllDocuments — все документы, реализующие общий контракт;
- Documents — совместимый список только EditorDocument (World documents).

Также поддерживаются активный документ, переключение, закрытие, discard, события изменения активного документа и освобождение IDisposable документов при закрытии.

Повторное открытие того же экземпляра запрещено; второй EditorDocument для уже открытого того же World не допускается.

### 4.2. Типы документов, открываемые стандартным Editor

EditorDocumentOpenService распознаёт следующие форматы:

| Формат | Тип документа |
|---|---|
| .world | EditorDocument |
| .prefab | PrefabDocument |
| .ui | EditorUiDocument |
| .tilemap.json | TilemapDocument |
| .tileset.json | TileSetDocument |
| .sprite-animation.json | SpriteAnimationDocument |
| .animation.json | AnimationClipDocument |
| .particle.json | ParticleEffectDocument |
| .script.json | ScriptDefinitionDocument |

EditorDocumentSaveService содержит отдельную ветку сохранения для каждого из перечисленных типов.

World сохраняется через IEditorDocumentFileService и настроенный IEditorDocumentPersistence; в стандартной конфигурации используется BinaryEditorDocumentPersistence и WorldSaveLoadService. Prefab сохраняется через PrefabAssetSaveLoadService. UI документ сохраняется через EditorUiAssetFile. Документы остальных перечисленных типов используют соответствующие методы Save() своих document classes.

### 4.3. Dirty state и закрытие

EditorApplication ищет несохранённые документы через Editor.Session.AllDocuments, а не только через список World documents. SaveAllDocuments также проходит по всем открытым документам.

При закрытии приложения dirty-документы последовательно предлагаются к сохранению или discard. Отмена закрытия документа отменяет и ожидающее закрытие приложения.

EditorSession.Close не позволяет закрыть dirty-документ без предварительного сохранения или discard. Discard является отдельной явной операцией.

Для World documents EditorContext.CloseDocument маршрутизирует решения Save/Discard/Cancel. Для прочих типов общий UI close workflow использует EditorDocumentSaveService и EditorSession.

Подтверждающие тесты:
- EditorSessionTests.Close_RejectsDirtyUiDocument;
- EditorContextDocumentLifecycleTests.CloseDocument_SaveFailure_LeavesDirtyDocumentOpen;
- EditorContextDocumentLifecycleTests.CloseDocument_Cancel_LeavesDocumentOpen;
- EditorContextDocumentLifecycleTests.CloseDocument_Discard_ClosesDirtyDocument;
- EditorContextDocumentLifecycleTests.CloseDocument_Save_ClosesDocumentAfterSave;
- EditorDocumentFileServiceTests.SaveAs_WhenPersistenceFails_PreservesDocumentStateAndAllowsRetry.

## 5. Hierarchy и Selection

### 5.1. Источник данных

EcsHierarchySource строит узлы из ECS World:
- имя берётся из EntityName, если оно задано;
- иначе используется Entity {Index};
- связь родитель/потомок получается из TransformParent2D.

Hierarchy source возвращает существующие сущности и сохраняет их в плоском списке nodes с parent reference. UI строит дерево из этих данных.

### 5.2. Действия

Стандартный HierarchyPanelView регистрирует действия:
- Create Entity;
- Rename;
- Delete.

Дополнительно есть реестр EditorHierarchyActionRegistry с фильтрацией по surface, проверкой CanExecute, порядком и регистрацией дополнительных действий. В Editor также существуют команды установки родителя, изменения Transform, а также отдельные Prefab-команды.

Для transform hierarchy доступен отдельный блок управления родителем, использующий SetTransformParent2DCommand.

### 5.3. Ограничения Selection

SelectionSet<T> как модель данных поддерживает набор выбранных элементов. Однако проверенный UI путь Hierarchy выбирает сущность через EntitySelection.Set(entity), то есть одиночным выбором.

EditorInspector.TryGetSelectedEntity возвращает сущность только при Count == 1. Следовательно, multi-entity Inspector или редактирование группы сущностей текущим проверенным workflow не подтверждены.

Тесты:
- EcsHierarchySourceTests.GetNodes_UsesEntityNameWhenPresent;
- EcsHierarchySourceTests.GetNodes_WhenParentDoesNotExist_TreatsEntityAsRoot;
- EcsHierarchySourceTests.GetNodes_WhenHierarchyContainsCycle_ReturnsAllEntities;
- HierarchyActionRegistryTests;
- TransformParentEditorCommandTests.

## 6. Inspector и компоненты

### 6.1. Регистрация компонентов

EditorComponentTypeRegistry принимает только non-primitive value types (ECS-компоненты-структуры). Компонент может быть зарегистрирован с фабрикой значения по умолчанию или с Activator.CreateInstance.

RegisterEngineComponents() регистрирует конкретный набор компонентов: TilemapComponent, LocalTransform2D, WorldTransform2D, SpriteComponent, AudioSourceComponent, AudioListenerComponent, ParticleEffectComponent.

В Editor.Sandbox/Program.cs дополнительно зарегистрированы NetworkIdentity, PhysicsBody2D и Collider2D, а зависимости PhysicsBody/Collider от transform/body зарегистрированы отдельно.

Add Component picker строит список из Editor.ComponentTypes.Types, исключает уже добавленные и блокирует типы с незаполненными зависимостями. Поэтому наличие ECS-компонента в Engine само по себе не делает его доступным в Add Component: тип должен быть зарегистрирован в контексте Editor.

Реестр позволяет host-коду зарегистрировать пользовательский компонент и factory. Автоматическое обнаружение компонентов из сборки активного игрового проекта в проверенном стандартном Editor startup path не найдено.

### 6.2. Отображение и изменение свойств

EditorInspector получает provider из EditorPropertyProviderRegistry.

Стандартные providers:
- EcsComponentPropertyProvider с приоритетом 100 для ECS value types;
- ReflectionPropertyProvider с более низким приоритетом для обычных объектов.

ECS provider отражает публичные instance properties без indexer. Для public setter создаётся setter; read-only properties остаются только для чтения. Публичные поля сами по себе этим provider не перечисляются.

Изменение значения компонента проходит через SetEditorPropertyCommand и EditorDocument.Execute, поэтому для World document оно попадает в историю команд.

UI InspectorPropertyEditorRegistry по умолчанию поддерживает bool, scalar/string/numeric, enum, PhysicsMaterial2D, PhysicsShape2D, Fixed32 и FixedVector2. Последний read-only editor служит fallback для типов без специализированного редактора. Host может регистрировать дополнительные property editors и prefab property editors.

Inspector работает с одной выбранной сущностью. Для Prefab существует отдельное представление и отдельный набор property editors.

Тесты:
- EcsComponentPropertyProviderTests.InspectorPropertyEdit_TracksDirtyStateAndUndoRedo;
- EcsComponentPropertyProviderTests.PhysicsBodyMassAndInertia_AreEditableAndUndoable;
- EcsComponentPropertyProviderTests.GetProperties_PrivateSetterIsReadOnly;
- InspectorComponentPickerTests;
- PhysicsMaterialInspectorPropertyEditorTests;
- PhysicsShapeInspectorPropertyEditorTests;
- PrefabDocumentCommandHistoryTests.

## 7. Scene View / Viewport

В текущем ViewportPanelView подтверждён 2D workflow:

- отображение World entities, sprites, tilemaps и отладочных элементов;
- hit testing и выбор сущности кликом;
- pan средней кнопкой мыши;
- zoom колёсиком с сохранением world position под курсором;
- инструменты Move, Rotate и Scale;
- translation gizmo с осями X/Y;
- взаимодействие с LocalTransform2D и WorldTransform2D в предусмотренных ветках.

Для пространственной сущности без LocalTransform2D и WorldTransform2D gizmo-перемещение недоступно. Rotate/Scale в просмотренной реализации работают с LocalTransform2D.

EditorViewport хранит собственные Size, Center и Zoom для Editor document; эти параметры не являются runtime Camera и не означают наличие 3D Scene View.

В проверенном Editor UI нет 3D viewport/scene authoring workflow. Наличие Engine.Graphics3D и 3D типов в Engine не означает наличия 3D Scene View в редакторе.

Тест ViewportPanelViewTests.EntityHitTest_ScalesWithWorldMarkerAtHighZoom подтверждает конкретный hit-test сценарий. Полного набора UI-интеграционных тестов всех операций gizmo в каталоге Engine.Tests/Editor по результатам просмотра не найдено; это не доказывает отсутствия ручных проверок.

## 8. Undo / Redo

В Editor разделены две сущности:

- EditorActionRegistry регистрирует доступные действия редактора (например, Validate, Undo, Redo).
- EditorCommandHistory хранит команды изменения состояния документа и позиции Undo/Redo.

EditorCommandHistory поддерживает:
- Execute;
- Undo;
- Redo;
- отметку сохранённой позиции;
- Dirty state;
- сохранение корректной redo-ветки при неудачном Execute;
- пометку состояния как dirty после создания новой ветки поверх undone-команд.

В EditorMainShell.CanUndo, CanRedo, Undo и Redo явно поддерживаются четыре типа активного документа:
- EditorDocument (World);
- EditorUiDocument;
- TilemapDocument;
- PrefabDocument.

Для SpriteAnimationDocument, AnimationClipDocument, TileSetDocument, ParticleEffectDocument и ScriptDefinitionDocument отдельная команда Undo/Redo в MainShell не подключена. Эти документы могут отслеживать Dirty state и сохраняться, но полноценная история изменений через стандартные кнопки Undo/Redo для них не подтверждена.

Тесты:
- EditorCommandHistoryTests;
- EditorActionTests.UndoAction_UsesActiveDocument;
- EditorActionTests.RedoAction_UsesActiveDocument;
- PrefabDocumentCommandHistoryTests;
- PrefabDocumentEntityEditingTests;
- PrefabDocumentStructureEditingTests;
- EditorUiDocumentTests.

## 9. Workspace и восстановление состояния

EditorContext регистрирует стандартные панели:
- Hierarchy;
- DocumentArea;
- Viewport;
- Inspector;
- AssetBrowser;
- Console.

EditorWorkspace управляет открытием, docking area, порядком, размером и активной панелью. EditorWorkspaceState проверяет уникальность ID, корректность области, порядок и размер. RestoreState допускает отсутствующие панели, сохраняет инвариант одной active panel на область и пытается откатить состояние при ошибке восстановления.

JsonEditorWorkspacePersistence сохраняет состояние в JSON через AtomicTextFile. Для этого существуют тесты round-trip и восстановления после ошибки записи.

Однако проверенный стандартный путь Editor.Sandbox/Program.cs не создаёт JsonEditorWorkspacePersistence и не вызывает EditorContext.SaveWorkspace / LoadWorkspace. Следовательно, persistence API существует, но сохранение и восстановление пользовательской раскладки в стандартном запуске Editor не подключено.

## 10. Editor session и активный игровой проект

EditorContext отдельно хранит:
- ActiveGameProject;
- активный документ в EditorSession.

Это правильное разделение: проект, содержащий Assets и StartupScene, не подменяется текущим открытым документом.

EditorProjectStateStore, используемый Editor.Sandbox, сохраняет последний путь к .vibeproject и восстанавливает его при запуске редактора.

Отдельно существуют EditorSessionState, JsonEditorSessionPersistence, EditorContext.SaveSession и LoadSession. Но в текущем API есть важная граница:
- CaptureSessionState перебирает Session.Documents, а это только World documents;
- ActiveDocumentPath также берётся от World ActiveDocument;
- восстановление использует IEditorDocumentFileService, который открывает World document;
- другие типы документов из Session.AllDocuments в сериализованное состояние не входят.

Кроме того, Editor.Sandbox/Program.cs не вызывает SaveSession / LoadSession. Таким образом, штатное приложение сохраняет активный игровой проект, но не восстанавливает полный набор открытых документов.

Тесты EditorSessionPersistenceTests проверяют восстановление World documents, дубликаты путей, active document и поведение при ошибках, но не восстановление полного набора типов документов.

## 11. Создание ресурсов и расширяемость

EditorAssetCreationRegistry содержит базовые определения для Folder, TileSet, Prefab, Tilemap, Particle Effect, Animation Clip, Sprite Animation, Script Definition, World и UI Document.

EditorAssetCreationService ограничивает создаваемые пути корнем Asset Browser, использует CreateNew и обновляет/выбирает созданный элемент. EditorAssetCreationWorkflow связывает создание ресурсов с документами.

Автоматическое открытие после создания сейчас ограничено отдельным набором типов (World, UI, Animation Clip, Sprite Animation, TileSet, Prefab); другие созданные типы могут оставаться выбранными в Asset Browser без автоматического открытия документа.

Основные точки расширения Editor:
- EditorWorkspace.RegisterPanel;
- EditorActionRegistry;
- EditorHierarchyActionRegistry;
- EditorComponentTypeRegistry;
- EditorPropertyProviderRegistry;
- InspectorPropertyEditorRegistry;
- EditorAssetCreationRegistry.

Это точки регистрации в коде приложения. Наличие этих реестров не доказывает наличие универсального plugin loader или автоматической загрузки инструментов из сторонних сборок.

## 12. Тестовое покрытие, найденное в репозитории

В Engine.Tests/Editor найдено 58 тестовых файлов на момент исходного снимка. Среди них:

- EditorSessionTests;
- EditorContextDocumentLifecycleTests;
- EditorDocumentFileServiceTests;
- EditorDocumentPersistenceTests;
- EditorWorkspaceTests;
- EditorWorkspacePersistenceTests;
- EditorCommandHistoryTests;
- EditorActionTests;
- EcsHierarchySourceTests;
- EditorComponentTypeRegistryTests;
- EcsComponentPropertyProviderTests;
- InspectorComponentPickerTests;
- ViewportPanelViewTests;
- отдельные тесты Prefab, UI authoring, Tilemap, TileSet, Animation, Particle и Script Definition.

Это широкое покрытие отдельных editor-механизмов. В рамках аудита не запускался тестовый набор; актуальный статус их выполнения не подтверждается этим документом.

## 13. Выводы и приоритеты для будущих этапов

### Подтверждено как существующая основа

- устойчивый контракт IEditorDocument и общая сессия;
- поддержка нескольких типов документов;
- отдельные сервисы открытия и сохранения;
- dirty-state и close confirmation;
- базовые команды и Undo/Redo для нескольких типов документов;
- ECS Hierarchy, Inspector, компонентные зависимости;
- полноценный набор базовых операций 2D Viewport;
- dock workspace и сериализуемая модель layout;
- реестры расширения редактора.

### Подтверждённые ограничения workflow

1. Undo/Redo доступен не всем типам документов.
2. Стандартная session persistence охватывает только World documents и не подключена в Editor.Sandbox.
3. Workspace persistence API существует, но не подключён в Editor.Sandbox.
4. 3D Scene View в текущем Editor UI не обнаружен.
5. Inspector поддерживает одну выбранную сущность; UI иерархии использует одиночный выбор.
6. Добавляемые ECS-компоненты зависят от явной регистрации и фабрик в контексте Editor.
7. Изменяемые ECS-свойства определяются через публичные свойства, а не произвольные поля.
8. Для части документов нет стандартного Undo/Redo workflow.

Это не означает, что все ограничения нужно устранять немедленно. Приоритет зависит от целевого пользовательского сценария и от результатов следующих аудитов Content Pipeline и Scene/World.

### Рекомендуемые дальнейшие решения

- При аудите Scene/World (0.4) проверить, какие редакторские изменения должны быть атомарными и должны ли все document types иметь единый command-history contract.
- При дальнейшем развитии Editor решить, требуется ли восстановление полного набора документов и раскладки рабочего пространства в стандартном приложении.
- Для полноценного 3D-направления выделить отдельный Scene View workflow, не смешивая его с текущим 2D viewport.
- При развитии пользовательских компонентов определить, достаточно ли явной регистрации в Editor host или нужен механизм discovery для игровых сборок. Не вводить его до подтверждения требования.

## 14. Следующий шаг

Задача **0.2 — аудит Editor — завершена**.

Следующая задача нулевого этапа — **0.3. Аудит Content Pipeline**: Asset Browser, asset source, content catalog, loaders, import/reload, зависимости ресурсов, кэширование, разрешение путей и доступность ресурсов в runtime.
