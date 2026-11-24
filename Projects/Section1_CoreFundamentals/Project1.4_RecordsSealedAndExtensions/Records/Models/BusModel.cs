namespace Records.Models;

// "sealed": no class can inherit from BusModel ("record X : BusModel" doesn't compile), so a switch that
// checks "BusModel" knows exactly what it gets.
public sealed record BusModel(string Line, TimeOnly ArrivalTime) : TransportationModel;
