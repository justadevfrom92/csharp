using Iterators.Collections;
using Iterators.Generators;
using Iterators.Readers;

// 1. Lazy: nothing runs until the loop asks.
var log = new List<string>();
IEnumerable<string> orders = OrderNumberGenerator.Generate(3, log);
Console.WriteLine($"1. After Generate(3): {log.Count} lines ran");
foreach (string order in orders)
{
    log.Add($"used {order}");
}
Console.WriteLine($"   After the loop: {string.Join(" | ", log)}");

// 2. An endless sequence is fine with Take.
Console.WriteLine($"2. Forever().Take(5): {string.Join(", ", OrderNumberGenerator.Forever().Take(5))}");

// 3. yield break.
Console.WriteLine($"3. Until the blank line: {string.Join(", ", OrderNumberGenerator.UntilBlankLine(["a", "b", "", "c"]))}");

// 4. Reading a file lazily; break early still closes it.
string path = Path.GetTempFileName();
File.WriteAllLines(path, ["INFO start", "ERROR disk full", "INFO retry", "ERROR timeout"]);
var fileLog = new List<string>();
foreach (string error in LogFileReader.ReadErrors(path, fileLog))
{
    Console.WriteLine($"4. First error: {error}");
    break;
}
Console.WriteLine($"   File: {string.Join(", ", fileLog)}");
File.Delete(path);

// 5. Our own collection with foreach, LINQ and a collection initializer.
var shelf = new ShelfCollection { "Tea", "Coffee", "Cocoa" };
Console.WriteLine($"5. Shelf starting with C: {string.Join(", ", shelf.Where(item => item.StartsWith('C')))}");
