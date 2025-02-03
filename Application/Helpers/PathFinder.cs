using Application.Services;
using Domain.Models;

namespace Application.Helpers;

public static class PathFinder
{
    public static List<int> FindShortestPath(Picker picker)
    {
        var distances = new int[WarehouseTopology.DistancesMatrix.GetLength(0)];
        var previous = new int?[WarehouseTopology.DistancesMatrix.GetLength(0)];
        var visited = new bool[WarehouseTopology.DistancesMatrix.GetLength(0)];
        var queue = new PriorityQueue<int, int>();

        distances[picker.CurrentCellId] = 0;
        queue.Enqueue(picker.CurrentCellId, 0);

        while (queue.Count > 0)
        {
            var currentCellId = queue.Dequeue();

            if (currentCellId == picker.CurrentDestinationCellId)
                break;

            visited[currentCellId] = true;

            for (int i = 0; i < WarehouseTopology.DistancesMatrix.GetLength(0); i++)
            {
                if (!visited[i] && WarehouseTopology.DistancesMatrix[currentCellId, i] != 0)
                {
                    var distanceToNeighbor = distances[currentCellId] + WarehouseTopology.DistancesMatrix[currentCellId, i];

                    if (distanceToNeighbor < distances[i] || distances[i] == 0)
                    {
                        distances[i] = distanceToNeighbor;
                        previous[i] = currentCellId;
                        queue.Enqueue(i, distanceToNeighbor);
                    }
                }
            }
        }

        return ReconstructPath(previous, picker.CurrentDestinationCellId);
    }
    
    public static List<int> FindShortestPath(int from, int to)
    {
        var distances = new int[WarehouseTopology.DistancesMatrix.GetLength(0)];
        var previous = new int?[WarehouseTopology.DistancesMatrix.GetLength(0)];
        var visited = new bool[WarehouseTopology.DistancesMatrix.GetLength(0)];
        var queue = new PriorityQueue<int, int>();

        distances[from] = 0;
        queue.Enqueue(from, 0);

        while (queue.Count > 0)
        {
            var currentCellId = queue.Dequeue();

            if (currentCellId == to)
                break;

            visited[currentCellId] = true;

            for (int i = 0; i < WarehouseTopology.DistancesMatrix.GetLength(0); i++)
            {
                if (!visited[i] && WarehouseTopology.DistancesMatrix[currentCellId, i] != 0)
                {
                    var distanceToNeighbor = distances[currentCellId] + WarehouseTopology.DistancesMatrix[currentCellId, i];

                    if (distanceToNeighbor < distances[i] || distances[i] == 0)
                    {
                        distances[i] = distanceToNeighbor;
                        previous[i] = currentCellId;
                        queue.Enqueue(i, distanceToNeighbor);
                    }
                }
            }
        }

        return ReconstructPath(previous, to);
    }
    
    public static void ChooseDropPoint(List<int> pathToFirstDropPoint, List<int> pathToSecondDropPoint, Picker picker,
        int firstDropPointId, int secondDropPointId)
    {
        if (pathToFirstDropPoint.Count < pathToSecondDropPoint.Count)
        {
            picker.CurrentDestinationCellId = firstDropPointId;
            picker.PathToNextTask = pathToFirstDropPoint;
            return;
        }
        
        picker.CurrentDestinationCellId = secondDropPointId;
        picker.PathToNextTask = pathToSecondDropPoint;
    }

    private static List<int> ReconstructPath(int?[] previous, int? targetCellId)
    {
        var path = new List<int>();
        var current = targetCellId;

        while (current != null)
        {
            path.Add(current.Value);
            current = previous[current.Value];
        }

        path.Reverse();
        return path;
    }
}