namespace Engine.Core.Systems;

public interface IFixedUpdateSystem
{
    void FixedUpdate(FixedSystemContext context);
}