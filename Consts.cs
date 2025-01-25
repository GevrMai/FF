using System.ComponentModel.DataAnnotations;

namespace FF;

public static class Consts
{
    public const int PictureBoxWidth = 2495;
    public const int PictureBoxHeight = 1250;
    
    public const int RowsCount = 23;
    public const int ColumnsCount = 10;

    public const int PickersCount = 4;
    public const int PickerMaxCarryWeight = 30;

    public const int DrawingImageDelayMs = 320;
    
    public const int TasksCountPerBatch = 90;
    public const int NumberOfBatches = 20;
    public const int MaxWeightOfTaskKg = 4;
    public const int DelayBetweenBatchesSeconds = 20;

    [Range(0, 100)]
    public const int PercentOfMaxCarriedWeight = 15;
}