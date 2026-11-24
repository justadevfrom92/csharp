using Records.Models;

namespace Records.Tests;

// "var p2 = p1 with { Name = ... }" - copy a record with some properties changed.
public class WithExpressionTest
{
    [Fact]
    public void With_PositivePositive_CopyChangesOnlyWhatYouSay_OriginalUnchanged()
    {
        var p1 = new PersonModel("Anna Svensson", 34);

        var p2 = p1 with { Name = "Filip Ekberg" };
        var p3 = p1 with { Name = "Sam Lee", Age = 41 };

        Assert.Equal(("Anna Svensson", 34), (p1.Name, p1.Age));
        Assert.Equal(("Filip Ekberg", 34), (p2.Name, p2.Age));
        Assert.Equal(("Sam Lee", 41), (p3.Name, p3.Age));
    }

    [Fact]
    public void With_PositiveNegative_EmptyWithIsEqualButNotTheSameObject_ListsAreShared()
    {
        var buyer = new PersonModel("Ola", 30) { Orders = ["Tea"] };

        var same = buyer with { };
        var copy = buyer with { Name = "Kim" };
        copy.Orders.Add("Coffee");

        Assert.Equal(buyer, same);
        Assert.False(ReferenceEquals(buyer, same));
        Assert.Same(buyer.Orders, copy.Orders);            // SHALLOW copy: one list, two owners
        Assert.Equal(["Tea", "Coffee"], buyer.Orders);    // the original changed too
        var separate = buyer with { Orders = [.. buyer.Orders] }; // a real copy of the list
        separate.Orders.Add("Milk");
        Assert.DoesNotContain("Milk", buyer.Orders);
    }

    [Fact]
    public void With_NegativeNegative_CheckInInitRunsInWithToo()
    {
        var p1 = new PersonModel("Anna", 34);

        Assert.Throws<ArgumentException>(() => p1 with { Name = " " });
        Assert.Throws<ArgumentException>(() => new PersonModel("", 34));
    }

    [Fact]
    public void Equality_PositiveNegative_RecordsWithAListAreNotEqualByValue()
    {
        var first = new PersonModel("Anna", 34);
        var second = new PersonModel("Anna", 34);

        Assert.NotEqual(first, second);           // each has its OWN Orders list; lists compare by reference
        Assert.Equal(first, first with { });      // "with" shares the list, so the copy IS equal
        Assert.Equal(new PlaneModel("SK1", new TimeOnly(9, 0)), new PlaneModel("SK1", new TimeOnly(9, 0))); // only simple values: equal
    }

    [Fact]
    public void With_PositivePositive_ThroughTheBaseTypeKeepsTheRealKind()
    {
        TransportationModel leg = new PlaneModel("SK1415", new TimeOnly(9, 30));

        TransportationModel copy = leg with { };

        Assert.IsType<PlaneModel>(copy); // not a plain TransportationModel: records copy themselves
        Assert.Equal(leg, copy);
    }
}
