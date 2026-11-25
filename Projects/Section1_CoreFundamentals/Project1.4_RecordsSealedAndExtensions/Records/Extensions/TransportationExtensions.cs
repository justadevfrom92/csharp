using Records.Models;

namespace Records.Extensions;

// .NET 10 EXTENSION BLOCK: "extension(TransportationModel transport) { ... }" adds members to a type you can't
// (or don't want to) change. Inside, "transport" is the object the member is called on: plane.ToLabel().
// Unlike the old "this" parameter style (Variations/ClassicExtensionsVariation), it can add PROPERTIES and STATIC members.
public static class TransportationExtensions
{
    extension(TransportationModel transport)
    {
        // An extension METHOD.
        public string ToLabel()
        {
            ArgumentNullException.ThrowIfNull(transport); // unlike instance methods, extensions CAN be called on null
            return transport switch
            {
                PlaneModel plane => $"flight {plane.FlightNumber}",
                ShipModel ship => $"ship {ship.Line}",
                BusModel bus => $"bus {bus.Line}",
                TrainModel train => $"train ({train.Operator})",
                CarModel => "car",
                // Needed although every kind above is handled: an "abstract" base could get new kinds in another
                // project, so the compiler warns (CS8509) without it. "closed" would make this line unnecessary.
                _ => throw new ArgumentException($"Unknown transportation {transport.GetType().Name}."),
            };
        }

        // An extension PROPERTY (new in .NET 10).
        public TimeOnly ArrivesAt
        {
            get
            {
                ArgumentNullException.ThrowIfNull(transport);
                return transport switch
                {
                    PlaneModel plane => plane.ArrivalTime,
                    ShipModel ship => ship.ArrivalTime,
                    BusModel bus => bus.ArrivalTime,
                    TrainModel train => train.ArrivalTime,
                    CarModel car => car.ExpectedArrivalTime,
                    _ => throw new ArgumentException($"Unknown transportation {transport.GetType().Name}."),
                };
            }
        }

        // "with" inside a switch: each kind is copied with its own time property moved. The original is unchanged.
        public TransportationModel DelayArrival(TimeSpan shift)
        {
            ArgumentNullException.ThrowIfNull(transport); // unlike instance methods, extensions CAN be called on null
            return transport switch
            {
                PlaneModel plane => plane with { ArrivalTime = plane.ArrivalTime.Add(shift) },
                ShipModel ship => ship with { ArrivalTime = ship.ArrivalTime.Add(shift) },
                BusModel bus => bus with { ArrivalTime = bus.ArrivalTime.Add(shift) },
                TrainModel train => train with { ArrivalTime = train.ArrivalTime.Add(shift) },
                CarModel car => car with { ExpectedArrivalTime = car.ExpectedArrivalTime.Add(shift) },
                _ => throw new ArgumentException($"Unknown transportation {transport.GetType().Name}."),
            };
        }

        // "when" adds a condition to a pattern: only THIS flight is delayed, everything else comes back as it was.
        public TransportationModel PostponeFlight(string flightNumber, TimeSpan delay)
        {
            ArgumentNullException.ThrowIfNull(transport); // unlike instance methods, extensions CAN be called on null
            return transport switch
            {
                PlaneModel plane when plane.FlightNumber == flightNumber => plane.DelayArrival(delay),
                _ => transport,
            };
        }
    }

    // No parameter name = STATIC extension members: called on the TYPE, TransportationModel.ByCar(...),
    // like a factory method the base type never had.
    extension(TransportationModel)
    {
        public static TransportationModel ByCar(TimeOnly expectedArrival)
        {
            return new CarModel(expectedArrival);
        }
    }
}
