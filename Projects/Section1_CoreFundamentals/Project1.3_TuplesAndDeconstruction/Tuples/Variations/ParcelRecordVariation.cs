namespace Tuples.Variations;

// ParcelModel (original) is a class with a hand-written Deconstruct. A POSITIONAL RECORD writes it for you.
public record ParcelRecordVariation(decimal WeightKg, string Destination);
