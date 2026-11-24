namespace Records.Models;

// "sealed": no class can inherit from CarModel ("record X : CarModel" doesn't compile), so a switch that
// checks "CarModel" knows exactly what it gets.
public sealed record CarModel(TimeOnly ExpectedArrivalTime) : TransportationModel;
