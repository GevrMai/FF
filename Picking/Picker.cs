namespace FF.Picking;

public record Picker(int Id, int MaxWeight)
{
    public int CurrentCellId;
    public int? CurrentTaskCellId;
    public int CurrentLoadKg;
    public List<int>? Path;
    public (int X, int Y) Coordinates;

    public int PassedCells = 0;
    
    public bool CanCarry(int weightToCarry) => MaxWeight >= CurrentLoadKg + weightToCarry;

    public void DoNextStep()
    {
        if (Path is null)
        {
            return;
        }
        if (Path.Count == 2)    // последний элемент - id шкафа, путь завершен
        {
            CurrentTaskCellId = default;
            Path = default;
            return;
        }
        PassedCells++;
        Path.RemoveAt(0);
        CurrentCellId = Path.First();
    }
}