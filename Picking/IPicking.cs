namespace FF.Picking;

public interface IPicking
{
    Task StartProcess(CancellationTokenSource cts);
}