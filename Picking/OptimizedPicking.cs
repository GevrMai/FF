using FF.Drawing;
using FF.Extensions;
using FF.TasksData;
using FF.WarehouseData;
using Google.OrTools.ConstraintSolver;

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
                foreach (var picker in _topology.Pickers)
                {
                    picker.CurrentTaskCellId = default;
                    picker.CurrentLoadKg = default;
                    picker.PassedCells = 0;
                    picker.TasksQueue?.Clear();
                }
                break;
            }
            
            var allPickersAreFree = true;
            foreach (var picker in _topology.Pickers)
            {
                if (picker.TasksQueue is not null && picker.TasksQueue.Any() || picker.CurrentTaskCellId is not null)
                {
                    allPickersAreFree = false;
                    break;
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
                if (picker.CurrentTaskCellId is null && picker.TasksQueue!.TryDequeue(out var task))
                {
                    if (picker.CanCarry(task.Weight))
                    {
                        picker.CurrentTaskCellId = task.RackId;
                        picker.CurrentLoadKg += task.Weight;

                        picker.PathToNextTask = _pathFinder.FindShortestPath(picker);
                        //Console.WriteLine($"picker at {picker.CurrentCellId}, task at {picker.CurrentTaskCellId!}");
                        //Console.WriteLine("path: " + string.Join(", ", picker.PathToNextTask));
                    }
                }

                picker.DoNextStep();
            }

            await _drawingService.DrawNextStep(_topology.Pickers);
        }
    }

    private void GenerateOptimizedSolution()
    {
        //var pointsForGeneration = new List<int>(_taskService.TasksQueue.Count);
        var pointsForGeneration = new List<OptimizedPickingGenerationPoint>(_taskService.TasksQueue.Count);
                
        Console.Write("Pickers: -> ");
        foreach (var picker in _topology.Pickers)
        {
            pointsForGeneration.Add(new()
            {
                CellId = picker.CurrentCellId, WeightKg = null
            }); 
            Console.Write(picker.CurrentCellId + " ");
        }
                
        Console.Write("\nTasks: -> ");
        while (_taskService.TasksQueue.TryDequeue(out var task))
        {
            pointsForGeneration.Add(new()
            {
                CellId = task.RackId, WeightKg = task.Weight
            }); 
            Console.Write(task.RackId + " ");
        }
                
        //pointsForGeneration.Add(int.MaxValue); // конец маршрута, расстояние до него будет всегда равно 0
                
        pointsForGeneration.Add(new() { CellId = int.MaxValue }); // конец маршрута, расстояние до него будет всегда равно 0
        // https://developers.google.com/optimization/routing/routing_tasks?hl=ru#allowing_arbitrary_start_and_end_locations

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

        var transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
        {
            // Convert from routing variable Index to
            // distance matrix NodeIndex.
            var fromNode = manager.IndexToNode(fromIndex);
            var toNode = manager.IndexToNode(toIndex);
            return distanceMatrixBetweenPoints[fromNode, toNode];
        });
                
        routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

        routing.AddDimension(transitCallbackIndex, 0, 6000,
            true, // start cumul to zero
            "Distance");
        RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
        distanceDimension.SetGlobalSpanCostCoefficient(900);

        var searchParameters =
            operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

        var solution = routing.SolveWithParameters(searchParameters);

        var routes = GetRoutes(routing, manager, solution);

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

    private List<List<int>> GetRoutes(in RoutingModel routing, in RoutingIndexManager manager,
        in Assignment solution)
    {
        var routes = new List<List<int>>();

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

        return routes;
    }
}