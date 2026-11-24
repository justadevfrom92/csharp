namespace Records.Models;

// The BASE of a small family: every way of arriving somewhere. "abstract" = you can't create a plain
// TransportationModel, only one of the kinds below. The kinds are "sealed": nobody can make a sub-kind of them.
// (C# may later get "closed": "these are ALL the kinds" - see the README. It doesn't compile with .NET 10 yet.)
public abstract record TransportationModel;
