using Engine.Core.Events;
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Physics.Collision;
using Engine.Runtime;
using Game.Sandbox.Components;

namespace Game.Sandbox.Systems;

public sealed class ResourceCollectionSystem : IDisposable
{
    private readonly Engine.ECS.World _world;

    private readonly HashSet<EntityId> _nearbyResources = new();

    private readonly EventSubscription _physicsSubscription;

    private EntityId _player = EntityId.Invalid;

    public event Action<string?>? InteractionChanged;

    public event Action<int>? InventoryChanged;

    public ResourceCollectionSystem(
        EngineRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        _world = runtime.EcsWorld;

        _physicsSubscription =
            runtime.Events.Subscribe<PhysicsContactEvent>(
                OnPhysicsContact);
    }

    public int ResourceCount
    {
        get
        {
            if (!_player.IsValid ||
                !_world.Exists(_player) ||
                !_world.Has<ResourceInventoryComponent>(_player))
            {
                return 0;
            }

            return _world.Get<ResourceInventoryComponent>(
                _player).Amount;
        }
    }

    public void SetPlayer(EntityId player)
    {
        if (!_world.Exists(player))
        {
            throw new ArgumentException(
                "Player entity does not exist.",
                nameof(player));
        }

        _player = player;

        _nearbyResources.Clear();

        if (!_world.Has<ResourceInventoryComponent>(player))
        {
            _world.Add(
                player,
                new ResourceInventoryComponent());
        }
        else
        {
            ref var inventory =
                ref _world.Get<ResourceInventoryComponent>(
                    player);

            inventory.Amount = 0;
        }

        InventoryChanged?.Invoke(ResourceCount);
        InteractionChanged?.Invoke(null);
    }

    public void Update(bool collectRequested)
    {
        if (!collectRequested ||
            !_player.IsValid ||
            !_world.Exists(_player) ||
            !_world.Has<ResourceInventoryComponent>(_player))
        {
            return;
        }

        var resource = FindNearestResource();

        if (!resource.IsValid)
        {
            return;
        }

        ref var node =
            ref _world.Get<ResourceNodeComponent>(
                resource);

        var amount = node.Amount;

        // Не позволяем некорректному компоненту
        // изменить состояние инвентаря.
        if (amount <= 0)
        {
            _nearbyResources.Remove(resource);
            _world.DestroyEntity(resource);

            RefreshInteractionPrompt();

            return;
        }

        ref var inventory =
            ref _world.Get<ResourceInventoryComponent>(
                _player);

        inventory.Amount = checked(
            inventory.Amount + amount);

        var newResourceCount = inventory.Amount;

        _nearbyResources.Remove(resource);

        _world.DestroyEntity(resource);

        InventoryChanged?.Invoke(newResourceCount);

        RefreshInteractionPrompt();
    }

    private void OnPhysicsContact(
        PhysicsContactEvent @event)
    {
        if (@event.Type != PhysicsContactType.Trigger)
        {
            return;
        }

        var pair = @event.Manifold.Pair;

        EntityId other;

        if (pair.First == _player)
        {
            other = pair.Second;
        }
        else if (pair.Second == _player)
        {
            other = pair.First;
        }
        else
        {
            return;
        }

        if (!other.IsValid ||
            !_world.Exists(other) ||
            !_world.Has<ResourceNodeComponent>(other))
        {
            return;
        }

        switch (@event.Phase)
        {
            case PhysicsContactPhase.Enter:
                _nearbyResources.Add(other);
                RefreshInteractionPrompt();
                break;

            case PhysicsContactPhase.Exit:
                _nearbyResources.Remove(other);
                RefreshInteractionPrompt();
                break;
        }
    }

    private EntityId FindNearestResource()
    {
        if (!_world.Exists(_player) ||
            !_world.Has<WorldTransform2D>(_player))
        {
            return EntityId.Invalid;
        }

        var playerPosition =
            _world.Get<WorldTransform2D>(
                _player).Position;

        var nearest = EntityId.Invalid;

        var nearestDistance = Fixed32.Zero;

        var staleEntities = new List<EntityId>();

        foreach (var resource in _nearbyResources)
        {
            if (!_world.Exists(resource) ||
                !_world.Has<ResourceNodeComponent>(resource) ||
                !_world.Has<WorldTransform2D>(resource))
            {
                staleEntities.Add(resource);
                continue;
            }

            var resourcePosition =
                _world.Get<WorldTransform2D>(
                    resource).Position;

            var offset =
                resourcePosition - playerPosition;

            var distance =
                offset.LengthSquared();

            if (!nearest.IsValid ||
                distance < nearestDistance ||
                (distance == nearestDistance &&
                 resource.Index < nearest.Index))
            {
                nearest = resource;
                nearestDistance = distance;
            }
        }

        foreach (var resource in staleEntities)
        {
            _nearbyResources.Remove(resource);
        }

        return nearest;
    }

    private void RefreshInteractionPrompt()
    {
        var hasNearbyResource =
            _nearbyResources.Any(
                resource =>
                    _world.Exists(resource) &&
                    _world.Has<ResourceNodeComponent>(resource));

        InteractionChanged?.Invoke(
            hasNearbyResource
                ? "PRESS E TO COLLECT RESOURCE"
                : null);
    }

    public void Dispose()
    {
        _physicsSubscription.Dispose();

        _nearbyResources.Clear();

        _player = EntityId.Invalid;
    }
}