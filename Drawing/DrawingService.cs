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
    
    private const int RowsNumber = 23;
    private const int ColumnsNumber = 7;
    
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
        var widthPerColumn = _width / ColumnsNumber;     //TODO не хардкод а конст - колво промежутков или типа того
        var heightPerRow = _height / RowsNumber;     //TODO не хардкод а конст - колво промежутков или типа того

        for (int row = 0; row < RowsNumber; row++)  // Идем с 1, потому что первый ряд свободен для прохода 
        {
            var currentYBorder = row * heightPerRow;

            for (int column = 0; column < ColumnsNumber; column++)
            {
                var currentXBorder = column * widthPerColumn;

                if (column % 2 == 1 && row != 0 && row != 11 && row != 22)
                {
                    _graphics.DrawRectangle(_whitePen, currentXBorder, currentYBorder, widthPerColumn, heightPerRow);
                    _graphics.FillEllipse(_redBrush, currentXBorder + widthPerColumn/2, currentYBorder + heightPerRow/2, 10, 10);
                    continue;
                }
                
                _graphics.FillEllipse(_greenBrush, currentXBorder + widthPerColumn/2, currentYBorder + heightPerRow/2, 10, 10);
            }
        }

        return _bitmap;
    }
}