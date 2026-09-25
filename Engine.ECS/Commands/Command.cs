using Engine.ECS.Entities;

namespace Engine.ECS.Commands;

internal interface ICommand
{
    void Apply(World world);
}