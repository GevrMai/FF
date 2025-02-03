using Domain.Enums;

namespace Domain.Interfaces;

public interface IMetricService
{
    void IncPassedCellsCounter(PickingType pickingType);
    void IncTasksWeightCounter(int value, PickingType pickingType);
    void IncStartedTasksCounter(PickingType pickingType);
    void IncDropPointVisitsCounter(PickingType pickingType);
    void IncMakingDistanceMatrixSecondsElapsedHistogram(double seconds);
}