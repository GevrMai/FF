using System.Collections.Concurrent;
using System.Windows.Forms;
using Domain.Interfaces;
using Domain.Models;

namespace Application.Services;

public class TaskService : ITaskService
{
    private readonly Random _rnd;
    private readonly WarehouseTopology _topology;
    private readonly ISnapshotSaver _snapshotSaver;
    private readonly IMetricService _metricService;

    private const bool UseGenerationSnapshot = false;

    public TaskService(WarehouseTopology topology, ISnapshotSaver snapshotSaver, IMetricService metricService)
    {
        _topology = topology;
        _snapshotSaver = snapshotSaver;
        _metricService = metricService;
        _rnd = new();
    }

    public readonly ConcurrentQueue<TaskNode> TasksQueue = new ();
    
    public async Task GetTasks(
        int tasksCountPerBatch,
        int batchesCount,
        int maxWeightKg,
        int delaySeconds,
        object generationsCountLabel,   //Label winForms
        CancellationToken token)
    {
        var label = (Label)generationsCountLabel;
        
        if (tasksCountPerBatch <= 0 || maxWeightKg <= 0 || delaySeconds < 0)
        {
            throw new ArgumentException("Invalid arguments. TasksCountPerBatch cannot be less or equal to 0. MaxWeight cannot be less or equal to 0. Delay cannot be less than 0.");
        }
        
        await Task.Delay(TimeSpan.FromSeconds(4), token);
        
        if (UseGenerationSnapshot)
        {
            await GetTasksFromSnapshot(delaySeconds, token);
            return;
        }

        var rackCells = _topology.GetRackCellIds().ToList();

        var batchesWithTasks = new List<List<TaskNode>>(batchesCount);
        for (int i = 0; i < batchesCount; i++)
        {
            if (token.IsCancellationRequested)
            {
                TasksQueue.Clear();
                break;
            }
            
            label.Text = $@"{i+1}/{batchesCount}";
            
            var tasksToSave = new List<TaskNode>();
            for (int j = 0; j < tasksCountPerBatch; j++)
            {
                var weight = _rnd.Next(1, maxWeightKg + 1);
                _metricService.IncTasksWeightCounter(weight, WarehouseTopology.CurrentPickingType);
                var task = new TaskNode(rackCells[_rnd.Next(0, rackCells.Count)], weight);
                TasksQueue.Enqueue(task);
                tasksToSave.Add(task);
            }
            
            batchesWithTasks.Add(tasksToSave);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
        }

        await _snapshotSaver.SaveGeneratedTasks(batchesWithTasks);
    }

    public int GetTasksInQueueCount() => TasksQueue.Count;
    public bool HasTasks() => TasksQueue.Any();
    public bool TryDequeue(out TaskNode? task)
    {
        return TasksQueue.TryDequeue(out task);
    }

    public void Enqueue(TaskNode task)
    {
        TasksQueue.Enqueue(task);
    }

    private async Task GetTasksFromSnapshot(int delaySeconds, CancellationToken token)
    {
        await foreach (var task in _snapshotSaver.GetTasksFromSnapshot(delaySeconds, token))
        {
            TasksQueue.Enqueue(task);
            _metricService.IncTasksWeightCounter(task.Weight, WarehouseTopology.CurrentPickingType);
        }
    }
}