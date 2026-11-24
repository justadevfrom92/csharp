namespace Records.Variations;

// "with" works on records, record structs, plain structs and anonymous types - NOT on normal classes
// ("someClass with { ... }" is compile error CS8858).
public static class WithTargetsVariation
{
    public static PointStructVariation MoveStruct(PointStructVariation point)
    {
        return point with { X = point.X + 10 };
    }

    public static (int OriginalCount, int CopyCount) ChangeAnonymousType()
    {
        var original = new { Name = "Tea", Count = 1 };
        var copy = original with { Count = 5 };
        return (original.Count, copy.Count);
    }
}
