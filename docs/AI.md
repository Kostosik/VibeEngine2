# Engine.AI

`Engine.AI` — независимая подсистема искусственного интеллекта для ECS-сущностей. Она предоставляет механизмы принятия решений и выполнения поведения, а конкретные игровые правила остаются в проекте игры.

Основные возможности:

- AI-агенты и управление их жизненным циклом;
- планирование частоты обновлений агентов;
- Behavior Trees;
- Utility AI;
- восприятие окружающего мира через сенсоры;
- типизированная память и последние известные позиции целей;
- получение отладочной информации о решениях Utility AI.

## 1. Зависимости

Прямые зависимости проекта:

- `Engine.Core`;
- `Engine.ECS`.

`Engine.AI` не зависит напрямую от физики, графики, ввода или игрового Runtime. Координаты, фильтрация целей и игровые действия передаются через компоненты и делегаты, определяемые проектом игры.

## 2. Подключение к EngineRuntime

`AISystem` реализует `IUpdateSystem`. Зарегистрируй его в системном планировщике до инициализации Runtime — например, в `ConfigureRuntime()` своего `EngineGame`.

```csharp
using Engine.AI.Systems;
using Engine.Core.Systems;
using Engine.Runtime;

public sealed class MyGame : EngineGame
{
    private AISystem? _ai;

    public MyGame(EngineRuntime runtime)
        : base(runtime)
    {
    }

    protected override void ConfigureRuntime()
    {
        var ai = new AISystem(Runtime.EcsWorld);

        Runtime.Scheduler.Add(
            ai,
            SystemPhase.Update);

        _ai = ai;
    }

    protected override void OnShutdown()
    {
        _ai?.Dispose();
        _ai = null;
    }
}
```

`AISystem` использует тот же ECS-мир, что и Runtime: `Runtime.EcsWorld`. Создавать отдельный `World` для AI не нужно.

Порядок AI относительно других update-систем можно задавать через `Before<TSystem>()` и `After<TSystem>()` у результата `Runtime.Scheduler.Add(...)`.

## 3. Создание AI-агента

Чтобы ECS-сущность стала AI-агентом, добавь к ней `AIComponent` и назначь поведение через `AISystem.SetBehavior()`.

```csharp
using Engine.AI.Components;
using Engine.ECS.Entities;

EntityId agent = Runtime.EcsWorld.CreateEntity();

Runtime.EcsWorld.Add(
    agent,
    new AIComponent());

_ai!.SetBehavior(agent, behavior);
```

Агент включён по умолчанию. У одного агента одновременно может быть одно активное `IAIBehavior`.

Отключение `AIComponent.Enabled` приостанавливает обновление агента, сохраняя его runtime-состояние. Повторное включение продолжает работу с сохранённого состояния.

## 4. Behavior Trees

Behavior Tree организует поведение в виде дерева узлов.

Основные узлы:

- `SequenceNode` — последовательно выполняет дочерние узлы; завершает ветвь неудачей при первом `Failure`;
- `SelectorNode` — проверяет ветви по порядку и выбирает первую успешную или выполняющуюся;
- `ConditionNode` — возвращает `Success` или `Failure` по результату условия;
- `ActionNode` — выполняет пользовательский обработчик;
- `WaitNode` — ожидает заданную длительность.

Декораторы: `InverterNode`, `RepeaterNode`, `CooldownNode`.

Пример:

```csharp
using Engine.AI.BehaviorTrees;
using Engine.AI.BehaviorTrees.Core;
using Engine.AI.BehaviorTrees.Nodes;

var tree = new BehaviorTree(
    "Guard",
    new SelectorNode(
        new SequenceNode(
            new ConditionNode(
                "ThreatDetected",
                (in BehaviorTreeContext context) =>
                    IsThreatDetected(context.Entity)),
            new ActionNode(
                "Attack",
                (in BehaviorTreeContext context) =>
                {
                    StartAttack(context.Entity);
                    return BehaviorStatus.Running;
                },
                abort: (in BehaviorTreeContext context) =>
                {
                    StopAttack(context.Entity);
                })),
        new ActionNode(
            "Patrol",
            (in BehaviorTreeContext context) =>
            {
                UpdatePatrol(context.Entity);
                return BehaviorStatus.Running;
            })));

_ai!.SetBehavior(agent, tree);
```

`IsThreatDetected`, `StartAttack`, `StopAttack` и `UpdatePatrol` — функции игрового проекта, а не API движка.

Статусы узлов:

- `Success` — узел завершился успешно;
- `Failure` — узел не смог выполнить задачу;
- `Running` — узел продолжает выполнение и будет обновлён позднее.

Для длительных игровых действий возвращай `Running` и указывай `abort`, чтобы остановить действие при сбросе или прерывании ветви. Состояние дерева хранится отдельно для каждого агента, поэтому один экземпляр дерева можно назначить нескольким сущностям.

По умолчанию `BehaviorTree` перезапускается после завершения. Это поведение можно изменить через `RestartOnCompletion`.

## 5. Utility AI

Utility AI оценивает доступные действия и выбирает наиболее подходящее. Действие создаётся через `UtilityAction`, а его оценка формируется набором `UtilityConsideration`.

```csharp
using Engine.AI.Core;
using Engine.AI.DecisionMaking.Utility;

var behavior = new UtilityAIBehavior
{
    MinimumSwitchScoreDelta = 0.05f
};

var attack = new UtilityAction(
    "Attack",
    (in AIContext context) =>
    {
        StartAttack(context.Entity);
    });

attack.AddConsideration(
    "TargetAvailable",
    new DelegateUtilityConsideration(
        "TargetAvailable",
        (in AIContext context) =>
            HasAvailableTarget(context.Entity)));

var patrol = new UtilityAction(
    "Patrol",
    (in AIContext context) =>
    {
        UpdatePatrol(context.Entity);
    });

patrol.AddConsideration(
    "NoThreat",
    new DelegateUtilityConsideration(
        "NoThreat",
        (in AIContext context) =>
            IsSafe(context.Entity)));

behavior.AddAction(attack);
behavior.AddAction(patrol);

_ai!.SetBehavior(agent, behavior);
```

Оценки consideration нормализуются в диапазон `0..1`; итоговая полезность действия рассчитывается с учётом весов и кривых. Действия без consideration не выбираются.

`MinimumSwitchScoreDelta` задаёт минимальный выигрыш в оценке, необходимый для переключения с текущего действия на другое.

### Жизненный цикл Utility Action

`UtilityAction` поддерживает:

- `OnEnter` — действие стало текущим;
- `OnExit` — действие перестало быть текущим;
- `Cooldown` — временный запрет повторного выполнения обработчика.

Состояние Utility AI, включая cooldown, хранится отдельно для каждого агента.

### Использование Behavior Tree как Utility Action

Для запуска полного дерева в рамках выбранного Utility Action используется адаптер:

```csharp
var combatAction = UtilityAction.FromBehaviorTree(
    "Combat",
    combatTree);

behavior.AddAction(combatAction);
```

При переключении с такого действия вызывается отмена дерева. Для длительного дерева оставляй `Cooldown = Duration.Zero` (значение по умолчанию): ненулевой cooldown приостанавливает вызов обработчика дерева на время cooldown.

## 6. Perception

Восприятие работает через сенсоры, которые регистрируются на агенте через `AISystem.Perception`.

Доступны:

- `RadiusSensor<TComponent>` — обнаруживает сущности в заданном радиусе;
- `FieldOfViewSensor<TComponent>` — учитывает радиус и направление обзора.

Сенсоры работают с компонентами и делегатами, предоставленными игрой. Например, можно использовать собственный компонент позиции:

```csharp
using Engine.Core.Math;

public struct PositionComponent
{
    public Vector2 Position { get; set; }
}
```

Зарегистрируй сенсор:

```csharp
using Engine.AI.Perception.Core;
using Engine.AI.Perception.Sensors;

_ai!.Perception.RegisterSensor(
    agent,
    new RadiusSensor<PositionComponent>(
        "Vision",
        50f,
        static (in PerceptionContext context) =>
            context.World
                .GetReadOnly<PositionComponent>(
                    context.Observer)
                .Position,
        static (_, component) =>
            component.Position));
```

У наблюдателя и обнаруживаемых сущностей должен быть `PositionComponent`. В примере предполагается, что компонент добавлен к ним заранее.

Конструкторы сенсоров также поддерживают фильтр целей и делегат проверки видимости. Частоту обновления и приоритет сенсора можно задать через `PerceptionSensorSettings`.

Во время `AISystem.Update` сначала обновляется Perception, затем обновляется память последних известных целей, после чего планируются обновления поведений агентов.

## 7. Память агента

В `AIContext` доступны:

- `Memory` — типизированная память агента;
- `LastKnownTargets` — память последних известных позиций обнаруженных целей.

Пример чтения последней известной позиции:

```csharp
public void Update(in AIContext context)
{
    if (context.LastKnownTargets.TryGet(
            target,
            context.Now,
            out var remembered))
    {
        NavigateTo(remembered.Position);
    }
}
```

`AILastKnownTargetMemory` хранит идентификатор цели, последнюю известную позицию, confidence, имя сенсора и время наблюдения. По умолчанию запись хранится 30 секунд; срок задаётся свойством `Retention`.

Память последней известной позиции пополняется наблюдениями, у которых есть `TargetPosition`. Встроенные радиусный сенсор и сенсор поля зрения передают позицию обнаруженной цели.

Когда ECS-сущность уничтожается, сведения о ней удаляются из памяти остальных агентов.

Для произвольных данных можно создать собственный `AIMemoryKey<T>` и использовать `context.Memory.Set(...)` / `TryGet(...)`. Память агента сохраняется при замене поведения, но очищается при удалении поведения.

## 8. Планирование обновлений

Планирование выполняется на двух уровнях:

- `SystemScheduler` определяет, когда вызывается сам `AISystem` в игровом цикле;
- `AIScheduler` определяет, какие агенты должны обновиться при очередном вызове `AISystem`.

Частота обновления отдельного агента задаётся компонентом `AIUpdateSettings`:

```csharp
using Engine.AI.Components;
using Engine.AI.Scheduling;
using Engine.Core.Time;

Runtime.EcsWorld.Add(
    agent,
    new AIUpdateSettings(
        group: AIUpdateGroup.Background,
        priority: 0,
        updateInterval: Duration.FromSeconds(1)));
```

Доступные группы: `EveryUpdate`, `Critical`, `High`, `Normal`, `Low`, `Background`.

Нулевой пользовательский интервал означает, что используется интервал группы. Если компонент `AIUpdateSettings` отсутствует, применяется значение по умолчанию — `EveryUpdate`.

Параметры `AISchedulerOptions` позволяют настраивать лимит обновляемых агентов за один вызов и интервалы групп.

## 9. Состояние и жизненный цикл поведения

Экземпляры `BehaviorTree` и `UtilityAIBehavior` могут использоваться несколькими агентами: runtime-состояние хранится отдельно для каждого агента.

При создании собственного `IAIBehavior` не храни состояние конкретной сущности в полях экземпляра поведения, если этот экземпляр назначается нескольким агентам. Используй `AIContext.AgentState`:

```csharp
var state = context.AgentState.GetOrCreate(
    this,
    static () => new MyAgentState());
```

Поведение может реализовать `IAIBehaviorLifecycle`. Тогда при замене или удалении поведения, а также при уничтожении агента вызывается `OnDetached`. Используй этот метод для освобождения принадлежащих поведению ресурсов и отмены активных операций.

Отключение `AIComponent.Enabled` только приостанавливает обновления: оно не вызывает `OnDetached` и не сбрасывает состояние. Удаление поведения и уничтожение сущности выполняют очистку состояния.

## 10. Отладочная информация Utility AI

Для получения подробностей оценки действий включи:

```csharp
behavior.EnableDebugInformation = true;
```

После обновления агента можно получить `UtilityDecisionDebugInfo` через `UtilityAIBehavior.GetDebugInfo(...)`. Снимок содержит выбранное действие, итоговую оценку, оценки отдельных действий и их consideration.

Подробная информация собирается только при включённом `EnableDebugInformation`.

## 11. Владение ресурсами

Код, создающий `AISystem`, отвечает за его освобождение через `Dispose()`. Система владеет внутренней `PerceptionSystem`, отписывается от событий ECS World и очищает своё состояние.

При интеграции с `EngineGame` освобождай `AISystem` в `OnShutdown()`, как показано в разделе подключения к Runtime.
