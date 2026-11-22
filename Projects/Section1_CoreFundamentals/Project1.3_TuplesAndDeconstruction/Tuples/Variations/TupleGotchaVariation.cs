using System.Text.Json;

namespace Tuples.Variations;

// Things about value tuples that surprise people. Each is checked by VariationsTest.
public static class TupleGotchaVariation
{
    // Tuple names exist only in the SOURCE CODE. JSON (System.Text.Json) skips fields by default, and a
    // value tuple's elements ARE fields - so the result is "{}". Records serialize properly.
    public static string ToJson((string Name, int Quantity) item)
    {
        return JsonSerializer.Serialize(item);
    }

    // With IncludeFields the values appear - but as Item1/Item2: the names are gone.
    public static string ToJsonIncludingFields((string Name, int Quantity) item)
    {
        return JsonSerializer.Serialize(item, new JsonSerializerOptions { IncludeFields = true });
    }

    // Value tuples are MUTABLE structs: elements can be changed, and a copy is a separate value.
    public static (int Original, int Copy) ChangeACopy()
    {
        (int X, int Y) point = (1, 2);
        (int X, int Y) copy = point;
        copy.X = 99;
        return (point.X, copy.X);
    }

    // == compares element by element, by POSITION. The names don't take part.
    public static bool SameByPosition()
    {
        (int Width, int Height) size = (3, 4);
        (int Count, int Total) other = (3, 4);
        return size == other;
    }
}
