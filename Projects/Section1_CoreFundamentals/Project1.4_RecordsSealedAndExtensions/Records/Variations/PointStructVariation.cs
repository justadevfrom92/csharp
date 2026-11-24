namespace Records.Variations;

// A plain STRUCT (not a record): "with" works on it too (C# 10+), because a struct is copied by value anyway.
public struct PointStructVariation
{
    public int X { get; set; }

    public int Y { get; set; }
}
