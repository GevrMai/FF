using FF.Drawing;
using FF.TasksData;
using FF.WarehouseData;

namespace FF.Picking;

public class DefaultPicking
{
    private readonly DrawingService _drawingService;
    private readonly TaskService _taskService;
    private readonly WarehouseTopology _topology;
    private readonly PathFinder _pathFinder;

    public DefaultPicking(
        DrawingService drawingService,
        TaskService taskService,
        WarehouseTopology topology,
        PathFinder pathFinder)
    {
        _drawingService = drawingService;
        _taskService = taskService;
        _topology = topology;
        _pathFinder = pathFinder;
    }

    public async Task StartProccess(CancellationTokenSource cts)
    {
        if (!_topology.Pickers.Any())
        {
            throw new ArgumentException("There are no pickers");
        }
        
        var retries = 0;
        while (true)
        {
            if (cts.IsCancellationRequested)
            {
                foreach (var picker in _topology.Pickers)
                {
                    picker.CurrentTaskCellId = default;
                    picker.CurrentLoadKg = default;
                }
                break;
            }

            foreach (var picker in _topology.Pickers)
            {
                if (picker.CurrentTaskCellId is null)
                {
                    _taskService.TasksQueue.TryDequeue(out TaskNode task);

                    if (task is not null && picker.CanCarry(task.Weight))
                    {
                        picker.CurrentTaskCellId = task.RackId;
                        picker.CurrentLoadKg += task.Weight;

                        picker.Path = _pathFinder.FindShortestPath(picker);
                        Console.WriteLine($"picker at {picker.CurrentCellId}, task at {picker.CurrentTaskCellId!}");
                        Console.WriteLine("path: " + string.Join(", ", picker.Path));
                    }
                }

                picker.DoNextStep();
                Console.WriteLine($"picker {picker.Id} at pos {picker.CurrentCellId}");
            }

            await _drawingService.DrawNextStep(_topology.Pickers);
        }
    }
}