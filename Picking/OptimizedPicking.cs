using System.Text;
using FF.Drawing;
using FF.TasksData;
using FF.WarehouseData;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace FF.Picking;

public class OptimizedPicking : IPicking
{
    private readonly DrawingService _drawingService;
    private readonly TaskService _taskService;
    private readonly WarehouseTopology _topology;
    private readonly PathFinder _pathFinder;
    
    public OptimizedPicking(
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
                if(picker.CurrentDestinationCellId is null  // никуда не идет
                   && picker.CurrentLoadKg != default       // загружен
                   && picker.TasksQueue is not null         // в очереди нет задач на подбор
                   && !picker.TasksQueue.Any())             // TODO - % т максимального веса
                {
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

            if (allPickersAreFree)
            {
                if (!_taskService.TasksQueue.Any())
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
                        picker.CurrentDestinationCellId = task.RackId;
                        picker.CurrentLoadKg += task.Weight;
                        picker.PathToNextTask = _pathFinder.FindShortestPath(picker);
                        picker.DestinationType = DestinationType.RackCell;
                    }
                }

                picker.DoNextStep();
            }

            await _drawingService.DrawNextStep(_topology.Pickers);
        }
    }

    private void GenerateOptimizedSolution()
    {
        var pointsForGeneration = new List<OptimizedPickingGenerationPoint>(_taskService.TasksQueue.Count);
        var weightDemands = new List<long>(_taskService.TasksQueue.Count + _topology.Pickers.Count + 1); // 1 - конечное депо
        var vehicleCapacities = new List<long>(_taskService.TasksQueue.Count + _topology.Pickers.Count);
        
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
        while (_taskService.TasksQueue.TryDequeue(out var task))
        {
            pointsForGeneration.Add(new()
            {
                CellId = task.RackId, WeightKg = task.Weight
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
        for (int i = 0; i < pointsForGeneration.Count; i++)
        {
            for (int j = 0; j < pointsForGeneration.Count; j++)
            {
                if (i == pointsForGeneration.Count - 1 || j == pointsForGeneration.Count - 1) // последний элемент - конец маршрута
                {
                    distanceMatrixBetweenPoints[i, j] = 0;
                    continue;
                }
                        
                var path = _pathFinder.FindShortestPath(pointsForGeneration[i].CellId, pointsForGeneration[j].CellId);

                var (fromPoint, toPoint) =
                    (CoordinatesHelper.GetCellRowAndColumn(pointsForGeneration[i].CellId), CoordinatesHelper.GetCellRowAndColumn(pointsForGeneration[j].CellId));
                        
                var pathCoef = WarehouseTopology.CellIsRack(fromPoint.Row, fromPoint.Column) &&
                               WarehouseTopology.CellIsRack(toPoint.Row, toPoint.Column)
                    ? 3
                    : 2;
                var distance = path.Count == 1 ? 0 : path.Count - pathCoef; //начальная и конечная точки не считаются
                distanceMatrixBetweenPoints[i, j] = distance;
            }
        }
        Log.Information("{@DistanceMatrix}", distanceMatrixBetweenPoints);
                
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
        searchParameters.TimeLimit = new Duration { Seconds = 5 };

        var solution = routing.SolveWithParameters(searchParameters);

        var (routes, droppedNodes) = GetRoutes(routing, manager, solution);

        foreach (var droppedNode in droppedNodes)
        {
            var taskNode = new TaskNode(pointsForGeneration[droppedNode].CellId, pointsForGeneration[droppedNode].WeightKg!.Value);
            _taskService.TasksQueue.Enqueue(taskNode);
            Log.Information("Enqueued task at {TaskCellId} and weight {Weight}", taskNode.RackId, taskNode.Weight);
        }

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