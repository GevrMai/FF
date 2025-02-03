using System.Diagnostics;
using System.Text;
using Application.Helpers;
using Domain;
using Domain.Enums;
using Domain.Extensions;
using Domain.Interfaces;
using Domain.Models;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Application.Services;

public class OptimizedPicking : IPicking
{
    private readonly IDrawingService _drawingService;
    private readonly ITaskService _taskService;
    private readonly WarehouseTopology _topology;
    //private readonly PathFinder _pathFinder;
    private readonly IMetricService _metricService;
    
    public OptimizedPicking(
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
        WarehouseTopology.CurrentPickingType = PickingType.Optimized;
        if (!_topology.Pickers.Any())
        {
            throw new ArgumentException("There are no pickers");
        }

        while (true)
        {
            if (cts.IsCancellationRequested)
            {
                WarehouseTopology.CurrentPickingType = PickingType.None;
                
                Log.Information("Stopping [OPTIMIZED]... Pickers` passed cells total: {PassedCells}",
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
            
            var allPickersAreFree = true;
            foreach (var picker in _topology.Pickers)
            {
                if (picker.TasksQueue is not null && picker.TasksQueue.Any() || picker.CurrentDestinationCellId is not null)
                {
                    allPickersAreFree = false;
                    break;
                }
            }
            
            foreach (var picker in _topology.Pickers)
            {
                var maxLoadToParticipateInGenerationKg =
                    ((double)Consts.PercentOfMaxCarriedWeight / 100.0) * (double)Consts.PickerMaxCarryWeight;
                if(picker.CurrentDestinationCellId is null  // никуда не идет
                   && picker.CurrentLoadKg > maxLoadToParticipateInGenerationKg      // загружен
                   && picker.TasksQueue is not null         // в очереди нет задач на подбор
                   && !picker.TasksQueue.Any())
                {
                    Log.Information($"CurrentLoad is {picker.CurrentLoadKg} which is bigger than {maxLoadToParticipateInGenerationKg}");
                    
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

            if (allPickersAreFree)
            {
                if (!_taskService.HasTasks())
                {
                    continue;
                }
                GenerateOptimizedSolution();
            }
            
            foreach (var picker in _topology.Pickers)
            {
                if (picker.CurrentDestinationCellId is null && picker.TasksQueue!.TryDequeue(out var task))
                {
                    if (picker.CanCarry(task.Weight))
                    {
                        _metricService.IncStartedTasksCounter(WarehouseTopology.CurrentPickingType);
                        picker.CurrentDestinationCellId = task.RackId;
                        picker.CurrentLoadKg += task.Weight;
                        picker.PathToNextTask = PathFinder.FindShortestPath(picker);
                        picker.DestinationType = DestinationType.RackCell;
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

    private void GenerateOptimizedSolution()
    {
        var tasksInQueueCount = _taskService.GetTasksInQueueCount();
        var pointsForGeneration = new List<OptimizedPickingGenerationPoint>(tasksInQueueCount);
        var weightDemands = new List<long>(tasksInQueueCount + _topology.Pickers.Count + 1); // 1 - конечное депо
        var vehicleCapacities = new List<long>(tasksInQueueCount + _topology.Pickers.Count);
        
        foreach (var picker in _topology.Pickers)
        {
            pointsForGeneration.Add(new()
            {
                CellId = picker.CurrentCellId, WeightKg = null
            }); 
            weightDemands.Add(0); // для точки отправления требование веса = 0
            vehicleCapacities.Add(picker.MaxWeight - picker.CurrentLoadKg);
        }
        
        Log.Information("Pickers are at: {PickerCellIds}", string.Join(" ", _topology.Pickers.Select(x => x.CurrentCellId)));

        var tasksLog = new StringBuilder("Tasks are at: ");
        while (_taskService.TryDequeue(out var task))
        {
            pointsForGeneration.Add(new()
            {
                CellId = task!.RackId, WeightKg = task.Weight
            });
            weightDemands.Add(task.Weight);
            tasksLog.Append($"{task.RackId} ");
        }
        Log.Information("{TaskCellIds}", tasksLog.ToString());
         
        // конец маршрута, расстояние до него будет всегда равно 0
        // https://developers.google.com/optimization/routing/routing_tasks?hl=ru#allowing_arbitrary_start_and_end_locations
        weightDemands.Add(0);
        pointsForGeneration.Add(new() { CellId = int.MaxValue });

        var distanceMatrixBetweenPoints = new int[pointsForGeneration.Count, pointsForGeneration.Count];
        var sw = new Stopwatch();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 6
        };
        sw.Start();

        Parallel.For(0, pointsForGeneration.Count, parallelOptions, i =>
        {
            for (int j = 0; j < pointsForGeneration.Count; j++)
            {
                if (i == pointsForGeneration.Count - 1 || j == pointsForGeneration.Count - 1) // последний элемент - конец маршрута
                {
                    distanceMatrixBetweenPoints[i, j] = 0;
                    continue;
                }
                        
                var path = PathFinder.FindShortestPath(pointsForGeneration[i].CellId, pointsForGeneration[j].CellId);

                var (fromPoint, toPoint) =
                    (CoordinatesHelper.GetCellRowAndColumn(pointsForGeneration[i].CellId), CoordinatesHelper.GetCellRowAndColumn(pointsForGeneration[j].CellId));
                        
                var pathCoef = WarehouseTopology.CellIsRack(fromPoint.Row, fromPoint.Column) &&
                               WarehouseTopology.CellIsRack(toPoint.Row, toPoint.Column)
                    ? 3
                    : 2;
                var distance = path.Count == 1 ? 0 : path.Count - pathCoef; //начальная и конечная точки не считаются
                distanceMatrixBetweenPoints[i, j] = distance;
            }
        });
        sw.Stop();
        Log.Information($"Elapsed seconds for distance matrix calc {sw.Elapsed.TotalSeconds} | TasksCount {pointsForGeneration.Count}");
        _metricService.IncMakingDistanceMatrixSecondsElapsedHistogram(sw.Elapsed.TotalSeconds);
                
        var pickerStartPoints = Enumerable.Range(0, _topology.Pickers.Count).ToArray();
        var pickerEndPoints = new int[_topology.Pickers.Count].Select(x => pointsForGeneration.Count - 1).ToArray();
        var manager = new RoutingIndexManager(distanceMatrixBetweenPoints.GetLength(0), _topology.Pickers.Count, pickerStartPoints, pickerEndPoints);

        var routing = new RoutingModel(manager);

        var transitCallbackIndex = routing.RegisterTransitCallback((fromIndex, toIndex) =>
        {
            var fromNode = manager.IndexToNode(fromIndex);
            var toNode = manager.IndexToNode(toIndex);
            return distanceMatrixBetweenPoints[fromNode, toNode];
        });
                
        routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);
        
        var demandCallbackIndex = routing.RegisterUnaryTransitCallback(fromIndex =>
        {
            var fromNode = manager.IndexToNode(fromIndex);
            return weightDemands[fromNode];
        });
        routing.AddDimensionWithVehicleCapacity(demandCallbackIndex, 0,
            vehicleCapacities.ToArray(),
            true,
            "Capacity");
        
        // Allow to drop nodes.
        var penalty = 1000l;
        for (int i = _topology.Pickers.Count; i < distanceMatrixBetweenPoints.GetLength(0) - 1; i++)
        {
            routing.AddDisjunction(new long[] { manager.NodeToIndex(i) }, penalty);
        }
        
        var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        searchParameters.TimeLimit = new Duration { Seconds = 1 };

        var solution = routing.SolveWithParameters(searchParameters);

        var (routes, droppedNodes) = GetRoutes(routing, manager, solution);

        var logBuilder = new StringBuilder("Enqueued points for next generation: [");
        foreach (var droppedNode in droppedNodes)
        {
            var taskNode = new TaskNode(pointsForGeneration[droppedNode].CellId, pointsForGeneration[droppedNode].WeightKg!.Value);
            _taskService.Enqueue(taskNode);
            logBuilder.Append($"[task at: {taskNode.RackId}, weight: {taskNode.Weight}]");
        }
        Log.Information(logBuilder.ToString());

        for (int i = 0; i < routes.Count; i++)  // маршрут i-ого подборщика
        {
            _topology.Pickers[i].TasksQueue = new Queue<TaskNode>(routes[i].Count);
            routes[i].RemoveAt(0);  // текущая точка
            for (int j = 0; j < routes[i].Count; j++)
            {
                _topology.Pickers[i].TasksQueue!.Enqueue( new(
                    RackId: pointsForGeneration[routes[i][j]].CellId, Weight: pointsForGeneration[routes[i][j]].WeightKg!.Value));
            }
        }
    }
    
    private (List<List<int>> routes, List<int> droppedNodes) GetRoutes(in RoutingModel routing, in RoutingIndexManager manager,
        in Assignment solution)
    {
        var routes = new List<List<int>>();
        var droppedNodes = new List<int>();
        
        for (int i = 0; i < routing.Size(); i++)
        {
            if (routing.IsStart(i) || routing.IsEnd(i))
            {
                continue;
            }
            if (solution.Value(routing.NextVar(i)) == i)
            {
                droppedNodes.Add(manager.IndexToNode(i));
            }
        }
        
        Log.Information("Dropped nodes: {DroppedNodes}", string.Join(" ", droppedNodes));

        for (int i = 0; i < _topology.Pickers.Count; ++i)
        {
            Log.Information("Route for picker {PickerId}:", i + 1);
            var route = new List<int>();
            var routeDistance = 0l;
            var index = routing.Start(i);

            var logBuilder = new StringBuilder();
            while (routing.IsEnd(index) == false)
            {
                logBuilder.Append($"{manager.IndexToNode((int)index)} -> ");
                
                route.Add(manager.IndexToNode((int)index));
                
                var previousIndex = index;
                index = solution.Value(routing.NextVar(index));
                routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
            }
            logBuilder.Append($"Distance of the route: {routeDistance} cells");
            Log.Information("{PickerRoute}", logBuilder.ToString());
            
            routes.Add(route);
        }

        return (routes, droppedNodes);
    }
}