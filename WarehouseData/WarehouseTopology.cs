namespace FF.WarehouseData;

public static class WarehouseTopology
{
    static WarehouseTopology()
    {
        var setUpTopologyTask = Task.Run(SetUpTopology);
        var setUpDistancesMatrixTask = Task.Run(SetUpDistancesMatrix);

        Task.WhenAll(setUpTopologyTask, setUpDistancesMatrixTask);
    }

    public static readonly int DistancesMatrixDim = Consts.RowsCount * Consts.ColumnsCount;

    public static readonly WarehouseNode[,] Topology = new WarehouseNode[Consts.RowsCount, Consts.ColumnsCount];
    public static readonly int[,] DistancesMatrix = new int[DistancesMatrixDim, DistancesMatrixDim];

    private static Task SetUpTopology()
    {
        var widthPerColumn = Consts.PictureBoxWidth / Consts.ColumnsCount;
        var heightPerRow = Consts.PictureBoxHeight / Consts.RowsCount;

        for (int row = 0; row < Consts.RowsCount; row++)
        {
            var yCenter = row * heightPerRow + heightPerRow / 2;

            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                var xCenter = column * widthPerColumn + widthPerColumn / 2;

                SetTopology(row, column, xCenter, yCenter);
            }
        }

        return Task.CompletedTask;
    }

    private static void SetTopology(int row, int column, int xCenter, int yCenter)
    {
        if (CellIsRack(row, column))
        {
            Topology[row, column] = new WarehouseNode(NodeType.RackCell, new(xCenter, yCenter));
            return; 
        }
                
        Topology[row, column] = new WarehouseNode(NodeType.EmptyCell, new(xCenter, yCenter));
    }
    
    private static Task SetUpDistancesMatrix()
    {
        for (int row = 0; row < Consts.RowsCount; row++)
        {
            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                if (column != Consts.ColumnsCount - 1)   // можем вправо 
                {
                    DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column + 1] = 1;
                }

                if (column != 0)    // можем влево
                {
                    DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column - 1] = 1;
                }
                
                if (row != Consts.RowsCount - 1 && !CellIsRack(row, column) && !CellIsRack( row+1, column))    // можем вниз, текущая ячейка не шкаф, вниз не шкаф
                {
                    DistancesMatrix[row * Consts.ColumnsCount + column, (row + 1) * Consts.ColumnsCount + column] = 1;
                }
                
                if (row != 0 && !CellIsRack(row, column) && !CellIsRack(row - 1, column))    // можем вверх, текущая ячейка не шкаф, вверх не шкаф
                {
                    DistancesMatrix[row * Consts.ColumnsCount + column, (row - 1) * Consts.ColumnsCount + column] = 1;
                }
            }
        }

        return Task.CompletedTask;
    }
    
    private static bool CellIsRack(int row, int column)
    {
        return column % 2 == 1 && row != 0 && row != 11 && row != 22;
    }
}