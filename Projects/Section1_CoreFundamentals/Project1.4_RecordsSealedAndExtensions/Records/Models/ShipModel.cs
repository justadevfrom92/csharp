namespace Records.Models;

// "sealed": no class can inherit from ShipModel ("record X : ShipModel" doesn't compile), so a switch that
// checks "ShipModel" knows exactly what it gets.
public sealed record ShipModel(string Line, TimeOnly ArrivalTime) : TransportationModel;
