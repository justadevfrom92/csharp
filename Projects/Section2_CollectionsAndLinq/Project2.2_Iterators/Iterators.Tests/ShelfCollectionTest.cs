using Iterators.Collections;
using Iterators.Enumerators;

namespace Iterators.Tests;

public class ShelfCollectionTest
{
    [Fact]
    public void Shelf_PositivePositive_ForeachLinqAndInitializerWork()
    {
        var shelf = new ShelfCollection { "Tea", "Coffee", "Cocoa" };
        var seen = new List<string>();
        foreach (string item in shelf)
        {
            seen.Add(item);
        }

        Assert.Equal(["Tea", "Coffee", "Cocoa"], seen);
        Assert.Equal(["Coffee", "Cocoa"], shelf.Where(item => item.StartsWith('C')));
    }

    [Fact]
    public void Shelf_PositiveNegative_AddDuringLoop_DoesNotThrow_LoopSeesTheOldCopy()
    {
        var shelf = new ShelfCollection { "Tea" };
        var seen = new List<string>();
        foreach (string item in shelf)
        {
            shelf.Add("Milk");
            seen.Add(item);
        }

        Assert.Equal(["Tea"], seen);
        Assert.Equal(["Tea", "Milk"], shelf);
    }

    [Fact]
    public void Shelf_NegativeNegative_BlankItem_AndCurrentOutsideTheLoop_Throw()
    {
        var shelf = new ShelfCollection();
        var cursor = new ShelfEnumerator(["Tea"]);

        Assert.Throws<ArgumentException>(() => shelf.Add(" "));
        Assert.Throws<InvalidOperationException>(() => cursor.Current); // before MoveNext
        Assert.True(cursor.MoveNext());
        Assert.False(cursor.MoveNext());
        Assert.False(cursor.MoveNext());                                // stays finished
        Assert.Throws<InvalidOperationException>(() => cursor.Current); // after the end
        cursor.Reset();
        Assert.True(cursor.MoveNext());
        Assert.Equal("Tea", cursor.Current);
    }
}
