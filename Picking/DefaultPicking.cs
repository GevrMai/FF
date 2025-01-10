using FF.Drawing;
using FF.TasksData;
using FF.WarehouseData;
using Serilog;

namespace FF.Picking;

public class DefaultPicking : IPicking  //TODO если подборщик свободен и нету задачек - идти на сброс
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
                Log.Information("Stopping [DEFAULT]... Pickers` passed cells total: {PassedCells}",
                    _topology.Pickers.Sum(x => x.PassedCells));
                
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
                        
                        Log.Information("PICKER - {PickerCellId} | TASK - {TaskCellId}", picker.CurrentCellId, picker.CurrentDestinationCellId);
                        Log.Information($"PATH: {string.Join(", ", picker.PathToNextTask)}");
                    }
                    else if (task is not null && !picker.CanCarry(task.Weight)
                             || task is null && picker.CurrentLoadKg != default) // превышена нагрузка - надо идти на точку сброса или свободен и загружен
                    {
                        if (task is not null)
                        {
                            _taskService.TasksQueue.Enqueue(task);  // обратно в очередь
                            Log.Information("task with load {Weight} and id {TaskCellId} is returned in queue", task.Weight, task.RackId);
                        }
                        
                        var firstDropPointId = CoordinatesHelper.GetCellId(WarehouseTopology.DropPointsCoordinates.First().row,
                                                                WarehouseTopology.DropPointsCoordinates.First().column);
                        var secondDropPointId = CoordinatesHelper.GetCellId(WarehouseTopology.DropPointsCoordinates.Last().row,
                                                                WarehouseTopology.DropPointsCoordinates.Last().column);

                        var pathToFirstDropPoint = _pathFinder.FindShortestPath(picker.CurrentCellId, firstDropPointId);
                        
                        var pathToSecondDropPoint = _pathFinder.FindShortestPath(picker.CurrentCellId, secondDropPointId);

                        _pathFinder.ChooseDropPoint(pathToFirstDropPoint, pathToSecondDropPoint, picker, firstDropPointId, secondDropPointId);
                        picker.DestinationType = DestinationType.DropPoint;
                        
                        Log.Information("PICKER - {PickerCellId}, DROP POINT - {DropPointCellId}", picker.CurrentCellId, picker.CurrentDestinationCellId);
                        Log.Information($"PATH: {string.Join(", ", picker.PathToNextTask)}");
                    }
                }

                picker.DoNextStep();
            }

            await _drawingService.DrawNextStep(_topology.Pickers);
        }
    }
}