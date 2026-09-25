namespace Engine.Jobs.Jobs;

public interface IJobParallelFor
{
    void Execute(
        int index);
}