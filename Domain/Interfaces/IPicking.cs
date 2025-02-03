namespace Domain.Interfaces;

public interface IPicking
{
    Task StartProcess(CancellationTokenSource cts);
}