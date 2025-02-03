using Domain.Enums;
using Domain.Interfaces;

namespace Domain.Models;

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
    
    public bool DoNextStep()
    {
        if (PathToNextTask is null)
        {
            return false;
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
            return false;
        }
        PassedCells++;
        PathToNextTask.RemoveAt(0);
        CurrentCellId = PathToNextTask.First();
        return true;
    }
}