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
    
    public Task StartProcess(CancellationTokenSource cts)
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
                }
                break;
            }
            
            // await Task.Delay();

            if (!_taskService.TasksQueue.Any())
            {
                continue;
            }
            
            var pointsForGeneration = new List<int>(_taskService.TasksQueue.Count);
            
            Console.Write("Pickers: -> ");
            foreach (var picker in _topology.Pickers)
            {
               pointsForGeneration.Add(picker.CurrentCellId); 
               Console.Write(picker.CurrentCellId + " ");
            }
            
            Console.Write("\nTasks: -> ");
            while (_taskService.TasksQueue.TryDequeue(out var task))
            {
                pointsForGeneration.Add(task.RackId);
                Console.Write(task.RackId + " ");
            }
            
            pointsForGeneration.Add(int.MaxValue); // конец маршрута, расстояние до него будет всегда равно 0
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
                    
                    var path = _pathFinder.FindShortestPath(pointsForGeneration[i], pointsForGeneration[j]);
                    var distance = path.Count == 1 ? 0 : path.Count - 2; //начальная и конечная точки не считаются
                    distanceMatrixBetweenPoints[i, j] = distance;
                }
            }
            Console.WriteLine(distanceMatrixBetweenPoints.Print());
            
            // Instantiate the data problem.
            //var data = new DataModel();

            // Create Routing Index Manager
            var pickerStartPoints = Enumerable.Range(0, _topology.Pickers.Count).ToArray();
            var pickerEndPoints = new int[_topology.Pickers.Count].Select(x => pointsForGeneration.Count - 1).ToArray();
            var manager = new RoutingIndexManager(distanceMatrixBetweenPoints.GetLength(0), _topology.Pickers.Count, pickerStartPoints, pickerEndPoints);

            // Create Routing Model.
            var routing = new RoutingModel(manager);

            // Create and register a transit callback.
            var transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                                                                       {
                                                                           // Convert from routing variable Index to
                                                                           // distance matrix NodeIndex.
                                                                           var fromNode = manager.IndexToNode(fromIndex);
                                                                           var toNode = manager.IndexToNode(toIndex);
                                                                           return distanceMatrixBetweenPoints[fromNode, toNode];
                                                                       });

            // Define cost of each arc.
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // Add Distance constraint.
            routing.AddDimension(transitCallbackIndex, 0, 6000,
                                 true, // start cumul to zero
                                 "Distance");
            RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
            distanceDimension.SetGlobalSpanCostCoefficient(900);

            // Setting first solution heuristic.
            var searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

            // Solve the problem.
            var solution = routing.SolveWithParameters(searchParameters);

            // Print solution on console.
            PrintSolution(routing, manager, solution);

        }
        return Task.CompletedTask;
    }
    
    private void PrintSolution(in RoutingModel routing, in RoutingIndexManager manager,
        in Assignment solution)
    {
        Console.WriteLine($"Objective {solution.ObjectiveValue()}:");

        // Inspect solution.
        long maxRouteDistance = 0;
        for (int i = 0; i < _topology.Pickers.Count; ++i)
        {
            Console.WriteLine("Route for Vehicle {0}:", i);
            long routeDistance = 0;
            var index = routing.Start(i);
            while (routing.IsEnd(index) == false)
            {
                Console.Write("{0} -> ", manager.IndexToNode((int)index));
                var previousIndex = index;
                index = solution.Value(routing.NextVar(index));
                routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
            }
            Console.WriteLine("{0}", manager.IndexToNode((int)index));
            Console.WriteLine("Distance of the route: {0}m", routeDistance);
            maxRouteDistance = Math.Max(routeDistance, maxRouteDistance);
        }
        Console.WriteLine("Maximum distance of the routes: {0}m", maxRouteDistance);
    }
}