using Domain;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Models;

namespace Application.Services;

public class WarehouseTopology
{
    public static readonly int[,] DistancesMatrix;
    public static readonly HashSet<int> ColumnsWithRacks = [1, 2, 4, 5, 7, 8, 10, 11];  //indexes
    public static readonly HashSet<int> RowsWithGoThrew = [0, (Consts.RowsCount-1) / 2, Consts.RowsCount - 1];  //indexes
    public static readonly List<(int row, int column)> DropPointsCoordinates = [
        (0, 0),
        (Consts.RowsCount - 1, Consts.ColumnsCount - 1)
    ];
    public List<Picker> Pickers;
    public static PickingType CurrentPickingType = PickingType.None;
    
    private readonly IDrawingService _drawingService;
    private readonly ISnapshotSaver _snapshotSaver;
    private readonly WarehouseNode[,] _topology;
    private readonly Random _rnd;
    
    private const int DistancesMatrixDim = Consts.RowsCount * Consts.ColumnsCount;
    private const bool UserPickersFromSnapshot = false;

    static WarehouseTopology()
    {
        DistancesMatrix = new int[DistancesMatrixDim, DistancesMatrixDim];
    }
    
    public WarehouseTopology(IDrawingService drawingService, ISnapshotSaver snapshotSaver)
    {
        _drawingService = drawingService;
        _snapshotSaver = snapshotSaver;
        
        _topology = new WarehouseNode[Consts.RowsCount, Consts.ColumnsCount];
        _rnd = new Random();
        
        var setUpTopologyTask = Task.Run(SetUpTopology);
        var setUpDistancesMatrixTask = Task.Run(SetUpDistancesMatrix);

        Task.WaitAll(setUpTopologyTask, setUpDistancesMatrixTask);
        
        WithPickersCount(Consts.PickersCount);
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
            _topology[row, column] = new WarehouseNode(NodeType.RackCell, new(xCenter, yCenter));

            var cellNumber = row * Consts.ColumnsCount + column;
            _drawingService.DrawText(cellNumber.ToString(), xCenter + 10, yCenter - 5);
            
            return; 
        }
                
        _topology[row, column] = new WarehouseNode(NodeType.EmptyCell, new(xCenter, yCenter));
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
                    if (CellIsRack(row, column) && CellIsRack(row, column + 1)) // из шкафа в шкаф
                    {
                        DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column + 1] = 50_000;
                    }
                    else if(!CellIsRack(row, column) && !CellIsRack(row, column + 1))   // свободная в свободную
                    {
                        DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column + 1] = 1;
                    }
                    else    // из свободной в шкаф или наоборот
                    {
                        DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column + 1] = 10_000;
                    }
                }
                
                if (column != 0)  // можем влево
                {
                    if (CellIsRack(row, column) && CellIsRack(row, column - 1)) // из шкафа в шкаф
                    {
                        DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column - 1] = 50_000;
                    }
                    else if(!CellIsRack(row, column) && !CellIsRack(row, column - 1))   // свободная в свободную
                    {
                        DistancesMatrix[row * Consts.ColumnsCount + column, row * Consts.ColumnsCount + column - 1] = 1;
                    }
                    else    // из свободной в шкаф или наоборот
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
        return ColumnsWithRacks.Contains(column) && !RowsWithGoThrew.Contains(row);
    }
    
    private void WithPickersCount(int count)
    {
        if (UserPickersFromSnapshot)
        {
            GetPickersFromSnapshot().GetAwaiter().GetResult();
            return;
        }
        
        Pickers = new List<Picker>(count);
        var emptyCellsIds = GetEmptyCells().ToList();
        
        for (int i = 1; i <= count; i++)
        {
            var cell = emptyCellsIds[_rnd.Next(0, emptyCellsIds.Count)];
            Pickers.Add(new(
                Id: i,
                MaxWeight: Consts.PickerMaxCarryWeight)
                {
                    CurrentCellId = cell.cellId,
                    Coordinates = new(cell.xCenter, cell.yCenter)
                } );
            _drawingService.DrawText(i.ToString(), cell.xCenter + 10, cell.yCenter - 5);
        }

        _snapshotSaver.SavePickersPositions(Pickers
            .Select(x => new PickerToSaveSnapshot(
                Id: x.Id,
                MaxWeight: x.MaxWeight,
                CurrentCellId: x.CurrentCellId,
                X: x.Coordinates.X,
                Y: x.Coordinates.Y))
            .ToList());
    }

    private async Task GetPickersFromSnapshot()
    {
        var dataFromSnapshot = _snapshotSaver.GetPickersFromSnapshot();
        Pickers = new List<Picker>();
        int i = 1;
        await foreach (var picker in dataFromSnapshot)
        {
            Pickers.Add(new Picker(Id: picker.Id, MaxWeight: picker.MaxWeight)
            {
                CurrentCellId = picker.CurrentCellId,
                Coordinates = (picker.X, picker.Y)
            });
            _drawingService.DrawText(i.ToString(), picker.X + 10, picker.Y - 5);
            i++;
        }
    }

    public IEnumerable<int> GetRackCellIds()
    {
        for (int row = 0; row < Consts.RowsCount; row++)
        {
            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                if (_topology[row, column].Type == NodeType.RackCell)
                {
                    yield return row * Consts.ColumnsCount + column;
                }
            }
        }
    }

    private IEnumerable<(int cellId, int xCenter, int yCenter)> GetEmptyCells()
    {
        for (int row = 0; row < Consts.RowsCount; row++)
        {
            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                var node = _topology[row, column];
                if (node.Type == NodeType.EmptyCell)
                {
                    yield return new(row * Consts.ColumnsCount + column, node.Coordinates.CenterX, node.Coordinates.CenterY);
                }
            }
        }
    }
}