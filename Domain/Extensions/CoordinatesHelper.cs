namespace Domain.Extensions;

public static class CoordinatesHelper
{
    public static (int X, int Y) GetCellCenterCoordinates(int cellId)
    {
        var row = cellId / Consts.ColumnsCount;
        var column = cellId % Consts.ColumnsCount;

        var cellWidth = Consts.PictureBoxWidth / Consts.ColumnsCount;
        var cellHeight = Consts.PictureBoxHeight / Consts.RowsCount;
        
        return new(
            (int)((column + 0.5) * cellWidth),
            (int)((row + 0.5) * cellHeight));
    }
    
    public static (int Row, int Column) GetCellRowAndColumn(int cellId)
    {
        var row = cellId / Consts.ColumnsCount;
        var column = cellId % Consts.ColumnsCount;
        
        return new(row, column);
    }

    public static int GetCellId(int row, int column)
    {
        return row * Consts.ColumnsCount + column;
    }
}