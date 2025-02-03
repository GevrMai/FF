using Domain.Models;

namespace Domain.Interfaces;

public interface IDrawingService
{
    
    /// <returns>Bitmap</returns>
    object DrawWarehouse();
    
    void DrawText(string text, int x, int y);
    Task DrawNextStep(List<Picker> pickers);
    event EventHandler BitmapChanged;

    /// <returns>Bitmap</returns>
    object GetBitmap();
}