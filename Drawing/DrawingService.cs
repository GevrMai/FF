using FF.Picking;
using FF.WarehouseData;

namespace FF.Drawing;

public class DrawingService
{
    private readonly Pen _whitePen;
    private readonly SolidBrush _redBrush;
    private readonly SolidBrush _greenBrush;
    private readonly SolidBrush _yellowBrush;
    private readonly SolidBrush _whiteBrush;
    private readonly Font _font;
    public Bitmap Bitmap;
    private Graphics _graphics;
    
    public event EventHandler BitmapChanged;
    
    public DrawingService()
    {
        _whitePen = new Pen(Color.FromKnownColor(KnownColor.White), 2);
        _redBrush = new SolidBrush(Color.Red);
        _greenBrush = new SolidBrush(Color.Green);
        _yellowBrush = new SolidBrush(Color.Yellow);
        _whiteBrush = new SolidBrush(Color.White);
        _font = new Font("Century Gothic", 14.25F, FontStyle.Bold);
        
        Bitmap = new Bitmap(Consts.PictureBoxWidth, Consts.PictureBoxHeight);
        
        _graphics = Graphics.FromImage(Bitmap);
    }
    
    public Bitmap DrawWarehouse()
    {
        var widthPerColumn = Consts.PictureBoxWidth / Consts.ColumnsCount;
        var heightPerRow = Consts.PictureBoxHeight / Consts.RowsCount;

        for (int row = 0; row < Consts.RowsCount; row++)
        {
            var currentYBorder = row * heightPerRow;

            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                var currentXBorder = column * widthPerColumn;

                //нечетные столбцы с 0 - шкафы с товарами
                // row 0, 11, 22 - проходы
                // тут - отрисовка шкафов
                if (WarehouseTopology.ColumnsWithRacks.Contains(column) && row != 0 && row != 11 && row != 22)
                {
                    _graphics.DrawRectangle(_whitePen, currentXBorder, currentYBorder, widthPerColumn, heightPerRow);
                    _graphics.FillEllipse(_redBrush, currentXBorder + widthPerColumn/2, currentYBorder + heightPerRow/2, 10, 10);
                    continue;
                }

                if (WarehouseTopology.DropPointsCoordinates.Contains((row, column)))
                {
                    // точка сброса, можно пройти сквозь
                    _graphics.FillEllipse(_whiteBrush, currentXBorder + widthPerColumn/2, currentYBorder + heightPerRow/2, 10, 10);
                    continue;
                }
                
                _graphics.FillEllipse(_greenBrush, currentXBorder + widthPerColumn/2, currentYBorder + heightPerRow/2, 10, 10);     // проход свободен
            }
        }

        return Bitmap;
    }

    public void DrawText(string text, int x, int y)
    {
        _graphics.DrawString(text, _font, _yellowBrush, x, y);
    }

    public async Task DrawNextStep(List<Picker> pickers)
    {
        Bitmap = new Bitmap(Consts.PictureBoxWidth, Consts.PictureBoxHeight);
        _graphics = Graphics.FromImage(Bitmap);

        DrawWarehouse();
        var widthPerColumn = Consts.PictureBoxWidth / Consts.ColumnsCount;
        var heightPerRow = Consts.PictureBoxHeight / Consts.RowsCount;

        for (int row = 0; row < Consts.RowsCount; row++)
        {
            var yCenter = row * heightPerRow + heightPerRow / 2;
        
            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                var xCenter = column * widthPerColumn + widthPerColumn / 2;
                if (WarehouseTopology.CellIsRack(row, column))
                {
                    var cellNumber = row * Consts.ColumnsCount + column;
                    DrawText(cellNumber.ToString(), xCenter + 10, yCenter - 5);
                }
            }
        }
        
        foreach (var picker in pickers)
        {
            var location = CoordinatesHelper.GetCellCenterCoordinates(picker.CurrentCellId);
            var xCenter = location.X + 10;
            var yCenter = location.Y - 5;
            DrawText($"{picker.Id} -> {picker.CurrentDestinationCellId}", xCenter, yCenter);
        }
            
        BitmapChanged.Invoke(this, EventArgs.Empty);
        
        GC.Collect();

        await Task.Delay(Consts.DrawingImageDelayMs);
    }
}

//TODO заюзать CoordinatesHelper где надо
//TODO id rack проставлять в DrawWarehouse
//TODO порефачить