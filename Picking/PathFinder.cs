using FF.WarehouseData;

namespace FF.Picking;

public class PathFinder
{
    private readonly WarehouseTopology _topology;

    public PathFinder(WarehouseTopology topology)
    {
        _topology = topology;
    }
    
    public List<int> FindShortestPath(Picker picker)
    {
        // Проверка на корректность входных данных
        if (picker.CurrentCellId == picker.CurrentTaskCellId)
            return new List<int> { picker.CurrentCellId };

        // Инициализация
        var distances = new int[_topology.DistancesMatrix.GetLength(0)]; // Массив расстояний от начальной точки
        var previous = new int?[_topology.DistancesMatrix.GetLength(0)]; // Массив предшественников для реконструкции пути
        var visited = new bool[_topology.DistancesMatrix.GetLength(0)]; // Массив посещенных вершин
        var queue = new PriorityQueue<int, int>(); // Очередь с приоритетами (по расстоянию)

        // Начальные значения для начальной вершины
        distances[picker.CurrentCellId] = 0;
        queue.Enqueue(picker.CurrentCellId, 0);

        // Алгоритм Дейкстры
        while (queue.Count > 0)
        {
            var currentCellId = queue.Dequeue();

            // Если достигли целевой вершины
            if (currentCellId == picker.CurrentTaskCellId)
                break;

            visited[currentCellId] = true;

            // Проход по соседям
            for (int i = 0; i < _topology.DistancesMatrix.GetLength(0); i++)
            {
                // Если сосед не посещен и доступен для прохода
                if (!visited[i] && _topology.DistancesMatrix[currentCellId, i] != 0)
                {
                    // Расстояние до соседа
                    var distanceToNeighbor = distances[currentCellId] + _topology.DistancesMatrix[currentCellId, i];

                    // Если текущее расстояние до соседа меньше, чем предыдущее
                    if (distanceToNeighbor < distances[i] || distances[i] == 0)
                    {
                        distances[i] = distanceToNeighbor;
                        previous[i] = currentCellId;
                        queue.Enqueue(i, distanceToNeighbor);
                    }
                }
            }
        }

        // Реконструкция пути
        return ReconstructPath(previous, picker.CurrentTaskCellId);
    }

    private List<int> ReconstructPath(int?[] previous, int? targetCellId)
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