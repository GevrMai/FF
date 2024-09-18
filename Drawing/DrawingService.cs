namespace FF.Drawing;

public class DrawingService
{
    private readonly Pen _whitePen;
    private readonly SolidBrush _redBrush;
    private readonly SolidBrush _greenBrush;
    private readonly Bitmap _bitmap;
    private readonly Graphics _graphics;

    private readonly int _width;
    private readonly int _height;
    
    //private const int RowsNumber = 23;
    //private const int ColumnsNumber = 7;
    
    public DrawingService(int width, int height)
    {
        _width = width;
        _height = height;
        
        _whitePen = new Pen(Color.FromKnownColor(KnownColor.White), 2);
        _redBrush = new SolidBrush(Color.Red);
        _greenBrush = new SolidBrush(Color.Green);
        _bitmap = new Bitmap(_width, _height);
        
        _graphics = Graphics.FromImage(_bitmap);
    }
    
    public Bitmap DrawWarehouse()
    {
        var widthPerColumn = _width / Consts.ColumnsCount;
        var heightPerRow = _height / Consts.RowsCount;

        for (int row = 0; row < Consts.RowsCount; row++)
        {
            var currentYBorder = row * heightPerRow;

            for (int column = 0; column < Consts.ColumnsCount; column++)
            {
                var currentXBorder = column * widthPerColumn;

                //нечетные столбцы с 0 - шкафы с товарами
                // row 0, 11, 22 - проходы
                // тут - отрисовка шкафов
                if (column % 2 == 1 && row != 0 && row != 11 && row != 22)
                {
                    _graphics.DrawRectangle(_whitePen, currentXBorder, currentYBorder, widthPerColumn, heightPerRow);
                    _graphics.FillEllipse(_redBrush, currentXBorder + widthPerColumn/2, currentYBorder + heightPerRow/2, 10, 10);
                    continue;
                }
                
                _graphics.FillEllipse(_greenBrush, currentXBorder + widthPerColumn/2, currentYBorder + heightPerRow/2, 10, 10);     // проход свободен
            }
        }

        return _bitmap;
    }
}