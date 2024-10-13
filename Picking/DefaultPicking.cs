using FF.Drawing;
using FF.TasksData;
using FF.WarehouseData;

namespace FF.Picking;

public class DefaultPicking : IPicking
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

    public async Task StartProcess(CancellationTokenSource cts)
    {
        if (!_topology.Pickers.Any())
        {
            throw new ArgumentException("There are no pickers");
        }
        
        while (true)
        {
            if (cts.IsCancellationRequested)
            {
                Console.WriteLine(_topology.Pickers.Sum(x => x.PassedCells));
                foreach (var picker in _topology.Pickers)
                {
                    picker.CurrentDestinationCellId = default;
                    picker.CurrentLoadKg = default;
                    picker.PassedCells = 0;
                    picker.TasksQueue?.Clear();
                }
                break;
            }

            foreach (var picker in _topology.Pickers)
            {
                if (picker.CurrentDestinationCellId is null)
                {
                    _taskService.TasksQueue.TryDequeue(out TaskNode task);

                    if (task is not null && picker.CanCarry(task.Weight))
                    {
                        picker.CurrentDestinationCellId = task.RackId;
                        picker.CurrentLoadKg += task.Weight;
                        picker.PathToNextTask = _pathFinder.FindShortestPath(picker);
                        picker.DestinationType = DestinationType.RackCell;
                        
                        Console.WriteLine($"PICKER > {picker.CurrentCellId}, TASK > {picker.CurrentDestinationCellId}");
                        Console.WriteLine("PATH: " + string.Join(", ", picker.PathToNextTask));
                    }
                    else if (task is not null && !picker.CanCarry(task.Weight)) // превышена нагрузка - надо идти на точку сброса
                    {
                        _taskService.TasksQueue.Enqueue(task);  // обратно в очередь
                        Console.WriteLine($"task with load {task.Weight} and id {task.RackId} is returned in queue");
                        
                        var firstDropPointId = CoordinatesHelper.GetCellId(WarehouseTopology.DropPointsCoordinates.First().row,
                                                                WarehouseTopology.DropPointsCoordinates.First().column);
                        var secondDropPointId = CoordinatesHelper.GetCellId(WarehouseTopology.DropPointsCoordinates.Last().row,
                                                                WarehouseTopology.DropPointsCoordinates.First().column);

                        var pathToFirstDropPoint = _pathFinder.FindShortestPath(picker.CurrentCellId, firstDropPointId);
                        
                        var pathToSecondDropPoint = _pathFinder.FindShortestPath(picker.CurrentCellId, secondDropPointId);

                        ChooseDropPoint(pathToFirstDropPoint, pathToSecondDropPoint, picker, firstDropPointId, secondDropPointId);
                        picker.DestinationType = DestinationType.DropPoint;
                        
                        Console.WriteLine($"PICKER > {picker.CurrentCellId}, DROP POINT > {picker.CurrentDestinationCellId}");
                        Console.WriteLine("PATH: " + string.Join(", ", picker.PathToNextTask));
                    }
                }

                picker.DoNextStep();
            }

            await _drawingService.DrawNextStep(_topology.Pickers);
        }
    }

    private static void ChooseDropPoint(List<int> pathToFirstDropPoint, List<int> pathToSecondDropPoint, Picker picker,
        int firstDropPointId, int secondDropPointId)
    {
        if (pathToFirstDropPoint.Count < pathToSecondDropPoint.Count)
        {
            picker.CurrentDestinationCellId = firstDropPointId;
            picker.PathToNextTask = pathToFirstDropPoint;
        }
        else
        {
            picker.CurrentDestinationCellId = secondDropPointId;
            picker.PathToNextTask = pathToSecondDropPoint;
        }
    }
}