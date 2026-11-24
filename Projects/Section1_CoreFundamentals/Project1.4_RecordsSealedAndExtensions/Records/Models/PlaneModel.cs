namespace Records.Models;

// "sealed": no class can inherit from PlaneModel ("record X : PlaneModel" doesn't compile), so a switch that
// checks "PlaneModel" knows exactly what it gets.
public sealed record PlaneModel(string FlightNumber, TimeOnly ArrivalTime) : TransportationModel;
