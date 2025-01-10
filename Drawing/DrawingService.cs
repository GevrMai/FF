using FF.Picking;
using FF.WarehouseData;

namespace FF.Drawing;

public class DrawingService
{
    private readonly Pen _whitePen;
    private readonly Pen _brownPen;
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
        _whitePen = new Pen(Color.White, 2);
        _brownPen = new Pen(Color.Brown, 2);
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
                // row 0, 11, 12, 13, 24 - проходы
                // тут - отрисовка шкафов
                if (WarehouseTopology.ColumnsWithRacks.Contains(column) && row != 0 && row != 11 && row != 12 && row != 13 && row != 24)
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
                
                if (WarehouseTopology.LiftCoordinates.Contains((row, column)))
                {
                    // лифт, доступны слева и справа
                    _graphics.DrawRectangle(_brownPen, currentXBorder, currentYBorder, widthPerColumn, heightPerRow);

                    switch (column)
                    {
                        case 4:
                            DrawText("<--- access", currentXBorder + 7, currentYBorder + 13, FontSize.Small);
                            break;
                        case 5:
                            DrawText("access --->", currentXBorder + 15, currentYBorder + 13, FontSize.Small);
                            break;
                    }
                    
                    continue;
                }
                
                _graphics.FillEllipse(_greenBrush, currentXBorder + widthPerColumn/2, currentYBorder + heightPerRow/2, 10, 10);     // проход свободен
            }
        }

        return Bitmap;
    }

    public void DrawText(string text, int x, int y, FontSize size = FontSize.Medium)
    {
        _graphics.DrawString(text, GetFont(size), _yellowBrush, x, y);
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

        await Task.Delay(450);
    }

    public enum FontSize
    {
        Small,
        Medium,
        Big
    }

    private Font GetFont(FontSize size)
    {
        return size switch
        {
            FontSize.Small => new Font("Century Gothic", 10.25F, FontStyle.Bold),
            FontSize.Medium => new Font("Century Gothic", 14.25F, FontStyle.Bold),
            FontSize.Big => new Font("Century Gothic", 18.25F, FontStyle.Bold),
            _ => throw new ArgumentOutOfRangeException("Unimplemented enum value")
        };
    }
}

//TODO заюзать CoordinatesHelper где надо
//TODO id rack проставлять в DrawWarehouse
//TODO пофиксить баг с проходом сквозь ячейки
//TODO порефачить