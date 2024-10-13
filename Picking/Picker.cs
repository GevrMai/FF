using FF.TasksData;

namespace FF.Picking;

public record Picker(int Id, int MaxWeight)
{
    public int CurrentCellId;
    public int? CurrentTaskCellId;
    public int CurrentLoadKg;
    public List<int>? PathToNextTask;
    public Queue<TaskNode>? TasksQueue;
    public (int X, int Y) Coordinates;

    public int PassedCells = 0;
    
    public bool CanCarry(int weightToCarry) => MaxWeight >= CurrentLoadKg + weightToCarry;

    public void DoNextStep()
    {
        if (PathToNextTask is null)
        {
            return;
        }
        if (PathToNextTask.Count == 2)    // последний элемент - id шкафа, путь завершен
        {
            CurrentTaskCellId = default;
            PathToNextTask = default;
            return;
        }
        PassedCells++;
        PathToNextTask.RemoveAt(0);
        CurrentCellId = PathToNextTask.First();
    }
}