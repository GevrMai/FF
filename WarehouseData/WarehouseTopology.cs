namespace FF.WarehouseData;

public static class WarehouseTopology
{
    static WarehouseTopology()
    {
        SetUpTopology();
    }
    public static WarehouseNode[,] Topology = new WarehouseNode[Consts.RowsCount, Consts.ColumnsCount];

    private static void SetUpTopology()
    {
        var widthPerColumn = Consts.PictureBoxWidth / Consts.ColumnsCount;
        var heightPerRow = Consts.PictureBoxHeight / Consts.RowsCount;

        for (int row = 0; row < Consts.RowsCount; row++)
        {
            var yCenter = row * heightPerRow + heightPerRow / 2;

            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                var xCenter = column * widthPerColumn + widthPerColumn / 2 ;
                
                if (column % 2 == 1 && row != 0 && row != 11 && row != 22)
                {
                    Topology[row, column] = new WarehouseNode(NodeType.RackCell, new(xCenter, yCenter));
                    continue;
                }
                
                Topology[row, column] = new WarehouseNode(NodeType.EmptyCell, new(xCenter, yCenter));
            }
        }
    }
}