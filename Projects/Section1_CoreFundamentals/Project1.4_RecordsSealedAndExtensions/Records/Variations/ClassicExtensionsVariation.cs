using Records.Models;

namespace Records.Variations;

// The SAME ToLabel and DelayArrival, written the pre-C# 14 way: a static method with "this" on the first parameter.
// Called exactly the same way (plane.ToLabelClassic()). It can't add properties or static members.
// (Named "...Classic" because two extension methods with the same name and target would be ambiguous.)
public static class ClassicExtensionsVariation
{
    public static string ToLabelClassic(this TransportationModel transport)
    {
        ArgumentNullException.ThrowIfNull(transport); // extensions CAN be called on null: check it
        return transport switch
        {
            PlaneModel plane => $"flight {plane.FlightNumber}",
            ShipModel ship => $"ship {ship.Line}",
            BusModel bus => $"bus {bus.Line}",
            TrainModel train => $"train ({train.Operator})",
            CarModel => "car",
            _ => throw new ArgumentException($"Unknown transportation {transport.GetType().Name}."),
        };
    }

    // Without extensions at all: a plain static helper, called as ClassicExtensionsVariation.Label(plane).
    public static string Label(TransportationModel transport)
    {
        return transport.ToLabelClassic();
    }
}
