namespace Tuples.Models;

// A normal CLASS made deconstructable by writing a Deconstruct method:
//   var (weight, destination) = parcel;
// Records get this for free (see Variations/ParcelRecordVariation).
public class ParcelModel
{
    public ParcelModel(decimal weightKg, string destination)
    {
        WeightKg = weightKg;
        Destination = destination;
    }

    public decimal WeightKg { get; }

    public string Destination { get; }

    public void Deconstruct(out decimal weightKg, out string destination)
    {
        weightKg = WeightKg;
        destination = Destination;
    }
}
