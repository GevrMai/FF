using System.Diagnostics.Metrics;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services;

internal class MetricService : IMetricService
{
    private static readonly Meter PickingMetrics = new("Picking", "1.0.0");
    
    private readonly Counter<int> _passedCellsCounter = PickingMetrics.CreateCounter<int>(
        name: "PassedCellsCounter",
        unit: "GridCell",
        description: "Счетчик количества пройденных ячеек при подборе");
    private readonly Counter<int> _tasksWeightCounter = PickingMetrics.CreateCounter<int>(
        name: "TasksWeightCounter",
        unit: "Kg",
        description: "Вес задач на подбор");
    private readonly Counter<int> _startedTasksCounter = PickingMetrics.CreateCounter<int>(
        name: "StartedTasksCounter",
        unit: "Tasks",
        description: "Взятые задачи в работу");
    private readonly Counter<int> _dropPointVisitsCounter = PickingMetrics.CreateCounter<int>(
        name: "DropPointVisitsCounter",
        unit: "Quantity",
        description: "Походов на точку сброса");
    private readonly Histogram<double> _makingDistanceMatrixForOptimizedPickingSecondsElapsedHistogram = PickingMetrics.CreateHistogram<double>(
        name: "MakingDistanceMatrixForOptimizedPickingSecondsElapsedHistogram",
        unit: "Seconds",
        description: "Секунд потрачено на построение матрицы расстояний");
    
    public void IncPassedCellsCounter(PickingType pickingType)
    {
        _passedCellsCounter.Add(1, new KeyValuePair<string, object?>("PickingType", pickingType));
    }
    public void IncTasksWeightCounter(int value, PickingType pickingType)
    {
        _tasksWeightCounter.Add(value, new KeyValuePair<string, object?>("PickingType", pickingType));
    }
    public void IncStartedTasksCounter(PickingType pickingType)
    {
        _startedTasksCounter.Add(1, new KeyValuePair<string, object?>("PickingType", pickingType));
    }
    public void IncDropPointVisitsCounter(PickingType pickingType)
    {
        _dropPointVisitsCounter.Add(1, new KeyValuePair<string, object?>("PickingType", pickingType));
    }
    public void IncMakingDistanceMatrixSecondsElapsedHistogram(double seconds)
    {
        _makingDistanceMatrixForOptimizedPickingSecondsElapsedHistogram.Record(seconds);
    }
}