using Domain.Models;

namespace Domain.Interfaces;

public interface ISnapshotSaver
{
    Task SaveGeneratedTasks(List<List<TaskNode>> batchesWithTasks);
    IAsyncEnumerable<TaskNode> GetTasksFromSnapshot(int delaySeconds, CancellationToken token);
    void SavePickersPositions(List<PickerToSaveSnapshot> pickers);
    IAsyncEnumerable<PickerToSaveSnapshot> GetPickersFromSnapshot();
}