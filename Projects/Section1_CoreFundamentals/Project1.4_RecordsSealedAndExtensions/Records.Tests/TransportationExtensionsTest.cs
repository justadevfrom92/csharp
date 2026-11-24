using Records.Extensions;
using Records.Models;

namespace Records.Tests;

public class TransportationExtensionsTest
{
    private static readonly PlaneModel Plane = new("SK1415", new TimeOnly(9, 30));

    [Fact]
    public void Extensions_PositivePositive_MethodPropertyAndStaticMember()
    {
        Assert.Equal("flight SK1415", Plane.ToLabel());
        Assert.Equal("ship Viking", new ShipModel("Viking", new TimeOnly(7, 0)).ToLabel());
        Assert.Equal("bus 42", new BusModel("42", new TimeOnly(8, 0)).ToLabel());
        Assert.Equal("train (SJ)", new TrainModel("SJ", new TimeOnly(13, 5)).ToLabel());
        Assert.Equal(new TimeOnly(9, 30), Plane.ArrivesAt);                                 // extension PROPERTY
        Assert.Equal(new TimeOnly(20, 0), TransportationModel.ByCar(new TimeOnly(20, 0)).ArrivesAt); // STATIC extension
    }

    [Fact]
    public void Extensions_PositiveNegative_DelayCopiesEachKind_PostponeOnlyTheMatchingFlight()
    {
        TransportationModel car = new CarModel(new TimeOnly(23, 30));

        Assert.Equal(new TimeOnly(10, 15), Plane.DelayArrival(TimeSpan.FromMinutes(45)).ArrivesAt);
        Assert.Equal(new TimeOnly(9, 30), Plane.ArrivesAt);                                     // original unchanged
        Assert.Equal(new TimeOnly(0, 30), car.DelayArrival(TimeSpan.FromHours(1)).ArrivesAt);  // TimeOnly wraps past midnight
        Assert.Same(Plane, Plane.PostponeFlight("XX999", TimeSpan.FromHours(1)));              // other flight: the SAME object back
        Assert.Same(car, car.PostponeFlight("SK1415", TimeSpan.FromHours(1)));                 // not a plane at all
    }

    [Fact]
    public void Extensions_NegativeNegative_NullTransport_Throws()
    {
        TransportationModel nothing = null!;

        Assert.Throws<ArgumentNullException>(() => nothing.ToLabel()); // an extension CAN be called on null, so it checks itself
        Assert.Throws<ArgumentNullException>(() => nothing.ArrivesAt);
    }

    [Fact]
    public void Sealed_PositivePositive_EveryKindIsSealed_TheBaseIsAbstract()
    {
        Type[] kinds = [typeof(PlaneModel), typeof(ShipModel), typeof(BusModel), typeof(TrainModel), typeof(CarModel)];

        Assert.All(kinds, kind => Assert.True(kind.IsSealed));
        Assert.True(typeof(TransportationModel).IsAbstract);
        Assert.All(kinds, kind => Assert.Equal(typeof(TransportationModel), kind.BaseType));
    }
}
