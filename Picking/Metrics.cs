using System.Diagnostics.Metrics;
using FF.WarehouseData;

namespace FF.Picking;

static class Metrics
{
    private static readonly Meter PickingMetrics = new("Picking", "1.0.0");
    
    private static readonly Counter<int> PassedCellsCounter = PickingMetrics.CreateCounter<int>(
        name: "PassedCellsCounter",
        unit: "GridCell",
        description: "Счетчик количества пройденных ячеек при подборе");
    private static readonly Counter<int> TasksWeightCounter = PickingMetrics.CreateCounter<int>(
        name: "TasksWeightCounter",
        unit: "Kg",
        description: "Вес задач на подбор");
    private static readonly Counter<int> StartedTasksCounter = PickingMetrics.CreateCounter<int>(
        name: "StartedTasksCounter",
        unit: "Tasks",
        description: "Взятые задачи в работу");
    private static readonly Counter<int> DropPointVisitsCounter = PickingMetrics.CreateCounter<int>(
        name: "DropPointVisitsCounter",
        unit: "Quantity",
        description: "Походов на точку сброса");
    private static readonly Histogram<int> MakingDistanceMatrixForOptimizedPickingSecondsElapsedCounter = PickingMetrics.CreateHistogram<int>(
        name: "MakingDistanceMatrixForOptimizedPickingSecondsElapsedCounter",
        unit: "Seconds",
        description: "Секунд потрачено на построение матрицы расстояний");
    
    public static void IncPassedCellsCounter(PickingType pickingType)
    {
        PassedCellsCounter.Add(1, new KeyValuePair<string, object?>("PickingType", pickingType));
    }
    public static void IncTasksWeightCounter(int value, PickingType pickingType)
    {
        TasksWeightCounter.Add(value, new KeyValuePair<string, object?>("PickingType", pickingType));
    }
    public static void IncStartedTasksCounter(PickingType pickingType)
    {
        StartedTasksCounter.Add(1, new KeyValuePair<string, object?>("PickingType", pickingType));
    }
    public static void IncDropPointVisitsCounter(PickingType pickingType)
    {
        DropPointVisitsCounter.Add(1, new KeyValuePair<string, object?>("PickingType", pickingType));
    }
    public static void IncMakingDistanceMatrixForOptimizedPickingSecondsElapsedCounter(int seconds)
    {
        MakingDistanceMatrixForOptimizedPickingSecondsElapsedCounter.Record(seconds);
    }
}