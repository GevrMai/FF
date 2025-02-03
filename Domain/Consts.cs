using System.ComponentModel.DataAnnotations;

namespace Domain;

public static class Consts
{
    public const int PictureBoxWidth = 2495;
    public const int PictureBoxHeight = 1250;
    
    public const int RowsCount = 27;
    public const int ColumnsCount = 13;

    public const int PickersCount = 3;
    public const int PickerMaxCarryWeight = 30;

    public const int DrawingImageDelayMs = 320;

    public const int TasksCountPerBatch = 45;
    public const int NumberOfBatches = 1;
    public const int MaxWeightOfTaskKg = 1;
    public const int DelayBetweenBatchesSeconds = 30;

    [Range(0, 100)]
    public const int PercentOfMaxCarriedWeight = 20;
}