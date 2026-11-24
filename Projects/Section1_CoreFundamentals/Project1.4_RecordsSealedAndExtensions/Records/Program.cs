using Records.Extensions;
using Records.Models;

// 1. "with": copy a record with some properties changed. The original stays the same.
var p1 = new PersonModel("Anna Svensson", 34);
var p2 = p1 with { Name = "Filip Ekberg" };
var p3 = p1 with { Name = "Sam Lee", Age = 41 };   // several at once
Console.WriteLine($"1. p1: {p1.Name}, {p1.Age} | p2: {p2.Name}, {p2.Age} | p3: {p3.Name}, {p3.Age}");

// 2. An empty "with" is an equal copy - but a different object.
var p4 = p1 with { };
Console.WriteLine($"2. p4 == p1: {p4 == p1}, same object: {ReferenceEquals(p4, p1)}");

// 3. Shallow copy: the Orders LIST is shared by both copies.
var buyer = new PersonModel("Ola", 30) { Orders = ["Tea"] };
var copy = buyer with { Name = "Kim" };
copy.Orders.Add("Coffee");
Console.WriteLine($"3. buyer's orders after adding to the COPY: {string.Join(", ", buyer.Orders)}");

// 4. The sealed family + C# 14 extension members.
List<TransportationModel> trip =
[
    new PlaneModel("SK1415", new TimeOnly(9, 30)),
    new TrainModel("SJ", new TimeOnly(13, 5)),
    new CarModel(new TimeOnly(18, 0)),
];
foreach (TransportationModel leg in trip)
{
    Console.WriteLine($"4. {leg.ToLabel(),-14} arrives {leg.ArrivesAt:HH:mm}"); // a method and a PROPERTY from the extension block
}

// 5. "with" inside an extension: only flight SK1415 is postponed; the list items themselves don't change.
List<TransportationModel> postponed = trip.Select(leg => leg.PostponeFlight("SK1415", TimeSpan.FromMinutes(45))).ToList();
Console.WriteLine($"5. Postponed: {postponed[0].ToLabel()} now {postponed[0].ArrivesAt:HH:mm} (was {trip[0].ArrivesAt:HH:mm}); train still {postponed[1].ArrivesAt:HH:mm}");

// 6. A static extension member, called on the type.
TransportationModel taxi = TransportationModel.ByCar(new TimeOnly(20, 0));
Console.WriteLine($"6. TransportationModel.ByCar(...): {taxi.ToLabel()} arriving {taxi.ArrivesAt:HH:mm}");
