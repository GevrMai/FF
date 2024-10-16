using System.Text;
using FF.Drawing;
using FF.Extensions;
using FF.TasksData;
using FF.WarehouseData;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;

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
                Console.WriteLine(_topology.Pickers.Sum(x => x.PassedCells));
                Console.WriteLine(_topology.Pickers.Sum(x => x.TasksDone));
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
                    
                    Console.WriteLine($"PICKER {picker.Id} is loaded - {picker.CurrentLoadKg}");
                    Console.WriteLine($"PICKER > {picker.CurrentCellId}, DROP POINT > {picker.CurrentDestinationCellId}");
                    Console.WriteLine("PATH: " + string.Join(", ", picker.PathToNextTask));
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
                
        Console.Write("Pickers: -> ");
        foreach (var picker in _topology.Pickers)
        {
            pointsForGeneration.Add(new()
            {
                CellId = picker.CurrentCellId, WeightKg = null
            }); 
            weightDemands.Add(0); // для точки отправления требование веса = 0
            vehicleCapacities.Add(picker.MaxWeight - picker.CurrentLoadKg);
            Console.Write(picker.CurrentCellId + " ");
        }
                
        Console.Write("\nTasks: -> ");
        while (_taskService.TasksQueue.TryDequeue(out var task))
        {
            pointsForGeneration.Add(new()
            {
                CellId = task.RackId, WeightKg = task.Weight
            });
            weightDemands.Add(task.Weight);
            Console.Write(task.RackId + " ");
        }
         
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
        Console.WriteLine(distanceMatrixBetweenPoints.Print());
                
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
        //PrintSolution(routing, manager, solution, weightDemands);

        var (routes, droppedNodes) = GetRoutes(routing, manager, solution);

        foreach (var droppedNode in droppedNodes)
        {
            var taskNode = new TaskNode(pointsForGeneration[droppedNode].CellId, pointsForGeneration[droppedNode].WeightKg!.Value);
            _taskService.TasksQueue.Enqueue(taskNode);
            Console.WriteLine($"ENQUEUED TASK {taskNode.RackId} with weight {taskNode.Weight}");
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

    private void PrintSolution(in RoutingModel routing, in RoutingIndexManager manager,
        in Assignment solution, List<long> demands)
    {
        Console.WriteLine($"Objective {solution.ObjectiveValue()}:");

        // Inspect solution.
        // Display dropped nodes.
        string droppedNodes = "Dropped nodes:";
        for (int index = 0; index < routing.Size(); ++index)
        {
            if (routing.IsStart(index) || routing.IsEnd(index))
            {
                continue;
            }
            if (solution.Value(routing.NextVar(index)) == index)
            {
                droppedNodes += " " + manager.IndexToNode(index);
            }
        }
        Console.WriteLine("{0}", droppedNodes);
        // Inspect solution.
        long totalDistance = 0;
        long totalLoad = 0;
        for (int i = 0; i < _topology.Pickers.Count; ++i)
        {
            Console.WriteLine("Route for Vehicle {0}:", i);
            long routeDistance = 0;
            long routeLoad = 0;
            var index = routing.Start(i);
            while (routing.IsEnd(index) == false)
            {
                long nodeIndex = manager.IndexToNode(index);
                routeLoad += demands[(int)nodeIndex];
                Console.Write("{0} Load({1}) -> ", nodeIndex, routeLoad);
                var previousIndex = index;
                index = solution.Value(routing.NextVar(index));
                routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
            }
            Console.WriteLine("{0}", manager.IndexToNode((int)index));
            Console.WriteLine("Distance of the route: {0}m", routeDistance);
            totalDistance += routeDistance;
            totalLoad += routeLoad;
        }
        Console.WriteLine("Total Distance of all routes: {0}m", totalDistance);
        Console.WriteLine("Total Load of all routes: {0}m", totalLoad);
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
        
        Console.Write("DROPPED NODES ->");
        foreach (var droppedNode in droppedNodes)
        {
            Console.Write($" {droppedNode}");
        }
        Console.WriteLine();

        for (int i = 0; i < _topology.Pickers.Count; ++i)
        {
            Console.WriteLine("Route for Vehicle {0}:", i+1);
            var route = new List<int>();
            var routeDistance = 0l;
            var index = routing.Start(i);
            while (routing.IsEnd(index) == false)
            {
                Console.Write("{0} -> ", manager.IndexToNode((int)index));
                
                route.Add(manager.IndexToNode((int)index));
                
                var previousIndex = index;
                index = solution.Value(routing.NextVar(index));
                routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
            }
            //Console.WriteLine("{0}", manager.IndexToNode((int)index));    //конечная точка - нулевое депо, смысла не имеет
            Console.WriteLine("Distance of the route: {0}m", routeDistance);
            routes.Add(route);
        }

        return (routes, droppedNodes);
    }
}