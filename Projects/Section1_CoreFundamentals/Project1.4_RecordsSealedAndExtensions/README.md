# Project 1.4 — Records, Sealed Hierarchies & Extensions

Three things that go together in modern C#:

- **`with`**: `var p2 = p1 with { Name = "Filip Ekberg" };` copies a record and changes only what you say.
- **A sealed family of records**: an `abstract record TransportationModel` with `sealed` kinds (`PlaneModel`, `ShipModel`,
  `BusModel`, `TrainModel`, `CarModel`), handled with `switch` patterns.
- **.NET 10 extension blocks**: `extension(TransportationModel transport) { ... }` adds methods, **properties** and
  **static** members to a type without changing it.

```bash
cd Records
dotnet run
```

```bash
cd Records.Tests
dotnet test
```

Output:

```
1. p1: Anna Svensson, 34 | p2: Filip Ekberg, 34 | p3: Sam Lee, 41
2. p4 == p1: True, same object: False
3. buyer's orders after adding to the COPY: Tea, Coffee
4. flight SK1415  arrives 09:30
4. train (SJ)     arrives 13:05
4. car            arrives 18:00
5. Postponed: flight SK1415 now 10:15 (was 09:30); train still 13:05
6. TransportationModel.ByCar(...): car arriving 20:00
```

## What's in it

| What | File |
|---|---|
| `abstract record TransportationModel` + five `sealed record` kinds | `Records/Models/` |
| `PersonModel` with a checked `init` (.NET 10 `field`) and an `Orders` list | `Records/Models/PersonModel.cs` |
| extension block: `ToLabel()`, `ArrivesAt` (property), `DelayArrival()`, `PostponeFlight()`, static `ByCar()` | `Records/Extensions/TransportationExtensions.cs` |
| `with` examples | `Records/Program.cs`, `Records.Tests/WithExpressionTest.cs` |

## `with` in one table

| Code | Result |
|---|---|
| `p1 with { Name = "Filip Ekberg" }` | a **new** record; `p1` unchanged |
| `p1 with { Name = "Sam Lee", Age = 41 }` | several properties at once |
| `p1 with { }` | an **equal** copy (`==` is true), but a **different object** |
| `leg with { }` where `leg` is a `TransportationModel` holding a plane | still a `PlaneModel`: records copy their real kind |
| `buyer with { Name = "Kim" }` when `buyer` has a `List` | **shallow**: both share **one** list; adding to the copy's list changes the original's |
| `buyer with { Orders = [.. buyer.Orders] }` | a separate list |
| on a record, `record struct`, plain `struct`, anonymous type | works |
| on a normal `class` | compile error **CS8858** ("not a valid record type") |

## Things the tests showed

- **Lists share and break equality.** `with` copies the *reference* to `Orders`, so both copies change together. And two
  `new PersonModel("Anna", 34)` are **not equal**: each has its own list, and lists compare by reference.
  `PlaneModel`s with the same values *are* equal (only simple values inside).
- **A constructor-only check is skipped by `with`** (`UncheckedPersonVariation`): `valid with { Name = "" }` succeeded.
  (Same trap as 4.2's money record.) The fix: check in the `init` accessor.
- **...but a property initializer then skips the `init` check in the constructor** (`InitializerSkipsInitVariation`):
  with `Name { get; init => check } = Name;`, `new X(" ")` went through, because `= Name` writes the hidden field directly.
  `PersonModel` uses a normal constructor that assigns through the property, so both `new` and `with` are checked.
- **Extensions can be called on `null`** (`nothing.ToLabel()`): unlike an instance method, no `NullReferenceException` at the
  call. Without a check it crashed later inside the switch; now each member starts with `ArgumentNullException.ThrowIfNull`.
- **`sealed` is enforced**: `record FasterPlane(...) : PlaneModel` is compile error **CS0509**.
- **`TimeOnly` wraps around midnight**: a car arriving 23:30 delayed 1 hour arrives 00:30.
- **`PostponeFlight` returns the same object** when nothing changes (`Assert.Same`): no copy is made when there is nothing to change.

## Why `_ => throw` is still needed — and `closed`

Every kind is handled in `ToLabel()`, yet without the last line `_ => throw ...` the compiler warns **CS8509** ("not
exhaustive"). An `abstract` base can get new kinds anywhere (another project), so the compiler can't be sure the list is complete.

A future C# feature, **`closed`** (`public closed record TransportationModel;`), says "these are ALL the kinds": no
`_` arm needed, and adding a sixth kind makes every switch that forgets it a compile error. It **does not compile** with
the .NET 10 SDK (error CS1002, also with `LangVersion preview`), so this project uses `abstract` + `sealed` + `_`.

## Code snippets

**Copy with changes**

```csharp
var p2 = p1 with { Name = "Filip Ekberg" };
var p3 = p1 with { Name = "Sam Lee", Age = 41 };
```

**A sealed family**

```csharp
public abstract record TransportationModel;
public sealed record PlaneModel(string FlightNumber, TimeOnly ArrivalTime) : TransportationModel;
public sealed record CarModel(TimeOnly ExpectedArrivalTime) : TransportationModel;
```

**An extension block (.NET 10)**

```csharp
public static class TransportationExtensions
{
    extension(TransportationModel transport)
    {
        public string ToLabel()                     // method
        {
            ...
        }

        public TimeOnly ArrivesAt                   // property
        {
            get
            {
                ...
            }
        }
    }

    extension(TransportationModel)                  // no name = static members
    {
        public static TransportationModel ByCar(TimeOnly expectedArrival)
        {
            return new CarModel(expectedArrival);
        }
    }
}

plane.ToLabel();  plane.ArrivesAt;  TransportationModel.ByCar(new TimeOnly(20, 0));
```

**`with` inside a switch, and `when`**

```csharp
PlaneModel plane => plane with { ArrivalTime = plane.ArrivalTime.Add(shift) },
PlaneModel plane when plane.FlightNumber == flightNumber => plane.DelayArrival(delay),
_ => transport,
```

**A check that runs on `new` and on `with`**

```csharp
public PersonModel(string name, int age)
{
    Name = name; // through the property, so "init" runs
    Age = age;
}

public string Name { get; init => field = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Name is required.") : value; }
```

## Variations

In `Records/Variations/`, checked by `Records.Tests/VariationsTest.cs`.

**Extensions, three ways**

| Way | Call | Can add |
|---|---|---|
| `extension(TransportationModel transport) { ... }` (original, .NET 10) | `plane.ToLabel()` | methods, **properties**, **static** members |
| `static string ToLabelClassic(this TransportationModel t)` (`ClassicExtensionsVariation`) | `plane.ToLabelClassic()` | methods only |
| plain static helper `Label(TransportationModel t)` | `ClassicExtensionsVariation.Label(plane)` | — (no dot-syntax) |

Both extension styles compile to the same thing: a static method taking the object as its first parameter.

**Sealed vs not** (`UnsealedVehicleVariation` + `ElectricBusVariation`)

| | `sealed record BusModel` (original) | `record UnsealedVehicleVariation` |
|---|---|---|
| `record X : ...` | compile error CS0509 | allowed (`ElectricBusVariation`) |
| `case UnsealedVehicleVariation` matches | exactly that kind | it **and every sub-kind** |
| equality | — | a sub-kind with the same values is **not equal** (records compare the type too) |

**Where `with` works** (`WithTargetsVariation`): a plain `struct` (`PointStructVariation`) and an anonymous type both work;
a normal `class` doesn't compile.

**Where the check goes**

| Check written in | `new X("")` | `x with { Name = "" }` |
|---|---|---|
| property initializer `= IsBlank(Name) ? throw : Name` (`UncheckedPersonVariation`) | throws | **accepted** |
| `init` accessor + `= Name` initializer (`InitializerSkipsInitVariation`) | **accepted** | throws |
| `init` accessor + constructor assigning `Name = name` (`PersonModel`, original) | throws | throws |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `abstract record` base | can't be created on its own; kinds share equality and `with` | an interface can't be used with `with` |
| `sealed` kinds | nobody adds a sub-kind that a switch would silently treat as its parent | unsealed: `case BusModel` would also catch an `ElectricBus` |
| `record` for the kinds | immutable, `with`, value equality, `ToString` for free | a class needs all of that by hand |
| `TimeOnly` | an arrival time without a date | `DateTime` drags a date along |
| extension block | adds behaviour without touching the models (they can stay plain data) | instance methods would mix display (`ToLabel`) into the model |
| `List<string> Orders` in `PersonModel` | shows the shallow-copy and equality traps | an `ImmutableList` would avoid them (see 9.3's immutable builder) |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `with` | changes only what you say; several at once; original unchanged; through the base type keeps the kind | empty `with` equal but a new object; lists shared; `[.. list]` separates | blank name throws on `new` and `with` |
| equality | — | records with a `List` aren't equal; simple-value records are | — |
| extensions | method, property, static member for every kind | delay copies each kind (and wraps midnight); postpone only the matching flight | `null` → `ArgumentNullException` |
| sealed | every kind sealed, base abstract | sub-kind matches the base pattern; equality checks the exact type | — |
| classic extensions | same labels; also callable as a static method | both styles are static methods underneath | `null` → `ArgumentNullException` |
| where `with` works | struct and anonymous type | constructor-only check skipped by `with` | initializer skips the `init` check in the constructor |
