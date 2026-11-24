using Records.Extensions;
using Records.Models;
using Records.Variations;

namespace Records.Tests;

public class VariationsTest
{
    [Fact]
    public void ClassicExtension_PositivePositive_SameLabelsAsTheExtensionBlock()
    {
        TransportationModel[] legs = [new PlaneModel("SK1", new TimeOnly(9, 0)), new CarModel(new TimeOnly(9, 0)), new BusModel("4", new TimeOnly(9, 0))];

        Assert.All(legs, leg => Assert.Equal(leg.ToLabel(), leg.ToLabelClassic()));
        Assert.All(legs, leg => Assert.Equal(leg.ToLabel(), ClassicExtensionsVariation.Label(leg))); // also callable as a normal static method
    }

    [Fact]
    public void ClassicExtension_PositiveNegative_BothStylesAreStaticMethodsUnderneath()
    {
        Assert.NotNull(typeof(ClassicExtensionsVariation).GetMethod("ToLabelClassic", [typeof(TransportationModel)]));
        Assert.NotNull(typeof(TransportationExtensions).GetMethod("ToLabel", [typeof(TransportationModel)])); // the block compiles to the same shape
    }

    [Fact]
    public void ClassicExtension_NegativeNegative_NullThrowsTheSame()
    {
        Assert.Throws<ArgumentNullException>(() => ((TransportationModel)null!).ToLabelClassic());
    }

    [Fact]
    public void Unsealed_PositiveNegative_ASubKindAlsoMatchesTheBasePattern()
    {
        UnsealedVehicleVariation vehicle = new ElectricBusVariation("4", 80);

        string kind = vehicle switch
        {
            ElectricBusVariation => "electric",
            UnsealedVehicleVariation => "plain",
        };

        Assert.Equal("electric", kind);
        Assert.True(vehicle is UnsealedVehicleVariation); // "is the base" is true for every sub-kind
        Assert.False(typeof(UnsealedVehicleVariation).IsSealed);
    }

    [Fact]
    public void Unsealed_NegativeNegative_EqualityChecksTheExactType()
    {
        UnsealedVehicleVariation plain = new("4");
        UnsealedVehicleVariation electric = new ElectricBusVariation("4", 80);

        Assert.NotEqual(plain, electric); // same Line, different kinds: records compare the type too
    }

    [Fact]
    public void WithTargets_PositivePositive_StructAndAnonymousType()
    {
        var point = new PointStructVariation { X = 1, Y = 2 };

        PointStructVariation moved = WithTargetsVariation.MoveStruct(point);

        Assert.Equal((11, 2), (moved.X, moved.Y));
        Assert.Equal(1, point.X);
        Assert.Equal((1, 5), WithTargetsVariation.ChangeAnonymousType());
    }

    [Fact]
    public void WithSkipsCheck_PositiveNegative_ConstructorOnlyCheck_IsSkippedByWith()
    {
        var valid = new UncheckedPersonVariation("Anna");

        UncheckedPersonVariation blank = valid with { Name = "" }; // no exception!

        Assert.Equal("", blank.Name);
        Assert.Throws<ArgumentException>(() => new UncheckedPersonVariation("")); // the constructor still checks
    }

    [Fact]
    public void InitializerSkipsInit_NegativeNegative_ConstructorLetsABlankNameThrough_WithCatchesIt()
    {
        var blank = new InitializerSkipsInitVariation(" "); // no exception: "= Name" wrote the field directly

        Assert.Equal(" ", blank.Name);
        Assert.Throws<ArgumentException>(() => new InitializerSkipsInitVariation("Anna") with { Name = " " });
    }
}
