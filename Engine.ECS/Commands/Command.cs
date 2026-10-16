namespace Engine.ECS.Commands;

internal interface ICommand
{
    void Apply(World world);
}