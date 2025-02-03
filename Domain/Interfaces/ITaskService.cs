using Domain.Models;

namespace Domain.Interfaces;

public interface ITaskService
{
    Task GetTasks(
        int tasksCountPerBatch,
        int batchesCount,
        int maxWeightKg,
        int delaySeconds,
        object generationsCountLabel,   //Label winForms
        CancellationToken token);
    
    int GetTasksInQueueCount();
    bool HasTasks();
    bool TryDequeue(out TaskNode? task);
    void Enqueue(TaskNode task);
}