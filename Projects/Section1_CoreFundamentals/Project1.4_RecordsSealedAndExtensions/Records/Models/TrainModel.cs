namespace Records.Models;

// "sealed": no class can inherit from TrainModel ("record X : TrainModel" doesn't compile), so a switch that
// checks "TrainModel" knows exactly what it gets.
public sealed record TrainModel(string Operator, TimeOnly ArrivalTime) : TransportationModel;
