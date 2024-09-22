using System.Collections.Concurrent;
using FF.WarehouseData;

namespace FF.TasksData;

public class TaskService
{
    private readonly Random rnd;
    private readonly WarehouseTopology _topology;

    public TaskService(WarehouseTopology topology)
    {
        _topology = topology;

        rnd = new();
    }

    public ConcurrentQueue<TaskNode> TasksQueue = new ();
    
    public async Task GenerateTasks(int tasksCountPerBatch, int numberOfTimes, int maxWeightG, int delayMs, CancellationTokenSource cts)
    {
        if (tasksCountPerBatch <= 0 || maxWeightG <= 0 || delayMs < 0)
        {
            throw new ArgumentException("Invalid arguments. TasksCountPerBatch cannot be less or equal to 0. MaxWeight cannot be less or equal to 0. Delay cannot be less than 0.");
        }
        
        var rackCells = _topology.GetRackCellIds().ToList();
        
        for (int i = 0; i < numberOfTimes; i++)
        {
            if (cts.IsCancellationRequested)
            {
                TasksQueue.Clear();
                break;
            }
            
            for (int j = 0; j < tasksCountPerBatch; j++)
            {
                TasksQueue.Enqueue( new(rackCells[rnd.Next(0, rackCells.Count)], rnd.Next(1, maxWeightG) ));
            }
            
            await Task.Delay(delayMs);
        }
    }
}