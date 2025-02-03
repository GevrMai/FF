using Application.Helpers;
using Domain.Enums;
using Domain.Extensions;
using Domain.Interfaces;
using Domain.Models;
using Serilog;

namespace Application.Services;

public class DefaultPicking : IPicking
{
    private readonly IDrawingService _drawingService;
    private readonly ITaskService _taskService;
    private readonly WarehouseTopology _topology;
    //private readonly PathFinder _pathFinder;
    private readonly IMetricService _metricService;
    
    public DefaultPicking(
        IDrawingService drawingService,
        ITaskService taskService,
        WarehouseTopology topology,
        //PathFinder pathFinder,
        IMetricService metricService)
    {
        _drawingService = drawingService;
        _taskService = taskService;
        _topology = topology;
        //_pathFinder = pathFinder;
        _metricService = metricService;
    }

    public async Task StartProcess(CancellationTokenSource cts)
    {
        WarehouseTopology.CurrentPickingType = PickingType.Default;
        if (!_topology.Pickers.Any())
        {
            throw new ArgumentException("There are no pickers");
        }
        
        while (true)
        {
            if (cts.IsCancellationRequested)
            {
                WarehouseTopology.CurrentPickingType = PickingType.None;
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
                    _taskService.TryDequeue(out var task);

                    if (task is not null && picker.CanCarry(task.Weight))
                    {
                        picker.CurrentDestinationCellId = task.RackId;
                        picker.CurrentLoadKg += task.Weight;
                        picker.PathToNextTask = PathFinder.FindShortestPath(picker);
                        picker.DestinationType = DestinationType.RackCell;
                        
                        _metricService.IncStartedTasksCounter(WarehouseTopology.CurrentPickingType);
                        
                        Log.Information("PICKER - {PickerCellId} | TASK - {TaskCellId}", picker.CurrentCellId, picker.CurrentDestinationCellId);
                        Log.Information($"PATH: {string.Join(", ", picker.PathToNextTask)}");
                    }
                    else if (task is not null && !picker.CanCarry(task.Weight)
                             || task is null && picker.CurrentLoadKg != default) // превышена нагрузка - надо идти на точку сброса или свободен и загружен
                    {
                        if (task is not null)
                        {
                            _taskService.Enqueue(task);  // обратно в очередь
                            Log.Information("task with load {Weight} and id {TaskCellId} is returned in queue", task.Weight, task.RackId);
                        }
                        
                        var firstDropPointId = CoordinatesHelper.GetCellId(WarehouseTopology.DropPointsCoordinates.First().row,
                                                                WarehouseTopology.DropPointsCoordinates.First().column);
                        var secondDropPointId = CoordinatesHelper.GetCellId(WarehouseTopology.DropPointsCoordinates.Last().row,
                                                                WarehouseTopology.DropPointsCoordinates.Last().column);

                        var pathToFirstDropPoint = PathFinder.FindShortestPath(picker.CurrentCellId, firstDropPointId);
                        
                        var pathToSecondDropPoint = PathFinder.FindShortestPath(picker.CurrentCellId, secondDropPointId);

                        PathFinder.ChooseDropPoint(pathToFirstDropPoint, pathToSecondDropPoint, picker, firstDropPointId, secondDropPointId);
                        picker.DestinationType = DestinationType.DropPoint;
                        
                        Log.Information("PICKER - {PickerCellId}, DROP POINT - {DropPointCellId}", picker.CurrentCellId, picker.CurrentDestinationCellId);
                        Log.Information($"PATH: {string.Join(", ", picker.PathToNextTask)}");
                        
                        _metricService.IncDropPointVisitsCounter(WarehouseTopology.CurrentPickingType);
                    }
                }
                
                if (picker.DoNextStep())
                {
                    _metricService.IncPassedCellsCounter(WarehouseTopology.CurrentPickingType);
                }
            }

            await _drawingService.DrawNextStep(_topology.Pickers);
        }
    }
}