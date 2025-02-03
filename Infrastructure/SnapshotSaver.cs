using System.Text.Json;
using Domain.Interfaces;
using Domain.Models;
using Serilog;

namespace Infrastructure;

public class SnapshotSaver : ISnapshotSaver
{
    private const string TasksSnapshotFileName = "GeneratedTasks.json";
    private const string PickersSnapshotFileName = "PickersPositions.json";

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
    {
        WriteIndented = true
    };
    
    public async Task SaveGeneratedTasks(List<List<TaskNode>> batchesWithTasks)
    {
        var batchesJson = JsonSerializer.Serialize(batchesWithTasks, _jsonOptions);
        await using var sw = new StreamWriter(TasksSnapshotFileName);
        await sw.WriteAsync(batchesJson);
        Log.Information("Successfully wrote snapshot data in {FileName}", TasksSnapshotFileName);
    }

    public async IAsyncEnumerable<TaskNode> GetTasksFromSnapshot(int delaySeconds, CancellationToken token)
    {
        await using var fileStream = File.OpenRead(TasksSnapshotFileName);
        var batchesWithTasks = JsonSerializer.DeserializeAsyncEnumerable<List<TaskNode>>(fileStream, cancellationToken: token)!;

        await foreach (var batchWithTasks in batchesWithTasks.WithCancellation(token))
        {
            foreach (var task in batchWithTasks!)
            {
                yield return task;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
        }
    }

    public void SavePickersPositions(List<PickerToSaveSnapshot> pickers)
    {
        var pickersJson = JsonSerializer.Serialize(pickers, _jsonOptions);
        using var sw = new StreamWriter(PickersSnapshotFileName);
        sw.Write(pickersJson);
        Log.Information("Successfully wrote snapshot data in {FileName}", PickersSnapshotFileName);
    }

    public async IAsyncEnumerable<PickerToSaveSnapshot> GetPickersFromSnapshot()
    {
        await using var fileStream = File.OpenRead(PickersSnapshotFileName);
        var pickers = JsonSerializer.DeserializeAsyncEnumerable<PickerToSaveSnapshot>(fileStream);

        await foreach (var picker in pickers)
        {
            yield return picker!;
        }
    }
}