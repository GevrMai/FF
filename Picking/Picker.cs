using FF.TasksData;
using FF.WarehouseData;

namespace FF.Picking;

public record Picker(int Id, int MaxWeight)
{
    public int CurrentCellId;
    public int? CurrentDestinationCellId;
    public int CurrentLoadKg;
    public List<int>? PathToNextTask;
    public Queue<TaskNode>? TasksQueue;
    public (int X, int Y) Coordinates;
    public DestinationType DestinationType;

    public int PassedCells = 0;
    public int TasksDone = 0;
    
    public bool CanCarry(int weightToCarry) => MaxWeight >= CurrentLoadKg + weightToCarry;
    
    public void DoNextStep()
    {
        if (PathToNextTask is null)
        {
            return;
        }
        if (PathToNextTask.Count == 2) // конечная точка достигнута
        {
            if (DestinationType == DestinationType.DropPoint)
            {
                CurrentLoadKg = 0;
            }
            else//TODO выпилить
            {
                TasksDone++;
            }
            CurrentDestinationCellId = default;
            PathToNextTask = default;
            return;
        }
        PassedCells++;
        Metrics.IncPassedCellsCounter(WarehouseTopology.CurrentPickingType);
        PathToNextTask.RemoveAt(0);
        CurrentCellId = PathToNextTask.First();
    }
}