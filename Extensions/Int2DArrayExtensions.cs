using System.Text;

namespace FF.Extensions;

public static class Int2DArrayExtensions
{
    public static string? Print(this int[,] array)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[");
        for (int i = 0; i < array.GetLength(0); i++)
        {
            sb.Append("  [");
            for (int j = 0; j < array.GetLength(1); j++)
            {
                sb.Append(array[i, j]);
                if (j < array.GetLength(1) - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append("]");
            if (i < array.GetLength(0) - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }
        sb.AppendLine("]");

        return sb.ToString();
    }
}