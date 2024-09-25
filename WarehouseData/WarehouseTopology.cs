using FF.Drawing;
using FF.Picking;

namespace FF.WarehouseData;

public class WarehouseTopology
{
    private readonly DrawingService _drawingService;
    public List<Picker> Pickers;
    private Random _rnd;
    
    public readonly int DistancesMatrixDim = Consts.RowsCount * Consts.ColumnsCount;

    public readonly WarehouseNode[,] Topology;
    public readonly int[,] DistancesMatrix;
    
    public WarehouseTopology(DrawingService drawingService)
    {
        _drawingService = drawingService;
        
        DistancesMatrix = new int[DistancesMatrixDim, DistancesMatrixDim];
        Topology = new WarehouseNode[Consts.RowsCount, Consts.ColumnsCount];
        _rnd = new Random();
        
        var setUpTopologyTask = Task.Run(SetUpTopology);
        var setUpDistancesMatrixTask = Task.Run(SetUpDistancesMatrix);

        Task.WaitAll(setUpTopologyTask, setUpDistancesMatrixTask);
        
        WithPickersCount(2);
    }

    private Task SetUpTopology()
    {
        var widthPerColumn = Consts.PictureBoxWidth / Consts.ColumnsCount;
        var heightPerRow = Consts.PictureBoxHeight / Consts.RowsCount;

        for (int row = 0; row < Consts.RowsCount; row++)
        {
            var yCenter = row * heightPerRow + heightPerRow / 2;

            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                var xCenter = column * widthPerColumn + widthPerColumn / 2;

                SetCellType(row, column, xCenter, yCenter);
            }
        }

        return Task.CompletedTask;
    }

    private void SetCellType(int row, int column, int xCenter, int yCenter)
    {
        if (CellIsRack(row, column))
        {
            Topology[row, column] = new WarehouseNode(NodeType.RackCell, new(xCenter, yCenter));

            var cellNumber = row * Consts.ColumnsCount + column;
            _drawingService.DrawText(cellNumber.ToString(), xCenter + 10, yCenter - 5);
            
            return; 
        }
                
        Topology[row, column] = new WarehouseNode(NodeType.EmptyCell, new(xCenter, yCenter));
    }
    
    // Дистанция из/в шкаф будет задана слишком большой, чтобы путь в него был,
    // но подборщику было выгоднее обойти шкаф, чем идти через него
    private Task SetUpDistancesMatrix()
    {
        for (int row = 0; row < Consts.RowsCount; row++)
        {
            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                if (column != Consts.ColumnsCount - 1)  // можем вправо
                {
                    if (!CellIsRack(row, column) && !CellIsRack(row, column + 1))   // свободная в свободную
                    {
                        DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column + 1] = 1;
                    }
                    else if (CellIsRack(row, column) || CellIsRack(row, column +1)) // из шкафа, в шкаф
                    {
                        DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column + 1] = 10_000;
                    }
                }
                
                if (column != 0)  // можем влево
                {
                    if (!CellIsRack(row, column) && !CellIsRack(row, column - 1))   // свободная в свободную
                    {
                        DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column - 1] = 1;
                    }
                    else if (CellIsRack(row, column) || CellIsRack(row, column - 1)) // из шкафа, в шкаф
                    {
                        DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column - 1] = 10_000;
                    }
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
    
    public static bool CellIsRack(int row, int column)
    {
        return column % 2 == 1 && row != 0 && row != 11 && row != 22;
    }
    
    private void WithPickersCount(int count)
    {
        Pickers = new List<Picker>(count);

        var emptyCellsIds = GetEmptyCells().ToList();
        
        for (int i = 1; i <= count; i++)
        {
            var cell = emptyCellsIds[_rnd.Next(0, emptyCellsIds.Count)];
            Pickers.Add(new(
                Id: i,
                MaxWeight: Consts.MaxWeight)
                {
                    CurrentCellId = cell.cellId,
                    Coordinates = new(cell.xCenter, cell.yCenter)
                } );
            _drawingService.DrawText(i.ToString(), cell.xCenter + 10, cell.yCenter - 5);
        }
    }

    public IEnumerable<int> GetRackCellIds()
    {
        for (int row = 0; row < Consts.RowsCount; row++)
        {
            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                if (Topology[row, column].Type == NodeType.RackCell)
                {
                    yield return row * Consts.ColumnsCount + column;
                }
            }
        }
    }
    
    public IEnumerable<(int cellId, int xCenter, int yCenter)> GetEmptyCells()
    {
        for (int row = 0; row < Consts.RowsCount; row++)
        {
            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                var node = Topology[row, column];
                if (node.Type == NodeType.EmptyCell)
                {
                    yield return new(row * Consts.ColumnsCount + column, node.Coordinates.CenterX, node.Coordinates.CenterY);
                }
            }
        }
    }
}