using OperatorsAndLambdas.Helpers;
using OperatorsAndLambdas.Models;
using OperatorsAndLambdas.ValueObjects;

// ---------- 1. Operator overloading ----------
var price = new MoneyValueObject(19.99m, "USD");
var shipping = new MoneyValueObject(5m, "USD");

MoneyValueObject total = price * 3 + shipping;    // uses operator * and operator +
Console.WriteLine($"3 x {price} + {shipping} = {total}");
Console.WriteLine($"total > 50 USD? {total > new MoneyValueObject(50m, "USD")}");
Console.WriteLine($"refund: {-price}");
Console.WriteLine($"same as new 19.99 USD? {price == new MoneyValueObject(19.99m, "USD")}");
Console.WriteLine($"as a plain decimal: {(decimal)total}");

try
{
    MoneyValueObject wrong = price + new MoneyValueObject(1m, "EUR");
}
catch (InvalidOperationException exception)
{
    Console.WriteLine($"USD + EUR -> {exception.Message}");
}
Console.WriteLine();

// ---------- 2. Lambda expressions ----------
// Expression lambda (one expression) and statement lambda (a block with return).
Func<int, int, int> add = (a, b) => a + b;
Func<int, string> describe = number =>
{
    if (number % 2 == 0)
    {
        return "even";
    }
    return "odd";
};
Action<string> print = text => Console.WriteLine("  " + text);
Predicate<int> isPositive = number => number > 0;
var multiply = (int a, int b) => a * b;            // "natural type": var works when the parameter types are written
var greet = (string name = "friend") => $"Hi {name}"; // default parameter value (C# 12)
Func<string, int> length = static text => text.Length; // static: can't capture anything by accident
Func<string, string> upper = ToShout;            // a METHOD GROUP is also a Func

print($"add(2, 3) = {add(2, 3)}, multiply(2, 3) = {multiply(2, 3)}");
print($"describe(7) = {describe(7)}, isPositive(-1) = {isPositive(-1)}");
print($"{greet()} / {greet("Ann")} / length = {length("hello")} / {upper("shout")}");

// Closures and composition.
Func<decimal, decimal> addTax = LambdaHelper.MakeMultiplier(1.20m);
Func<decimal, decimal> tenPercentOff = LambdaHelper.MakeMultiplier(0.90m);
Func<decimal, decimal> discountThenTax = LambdaHelper.Compose(tenPercentOff, addTax);
print($"100 -> 10% off, then tax: {discountThenTax(100m):0.00}");
print($"rules in a list: {LambdaHelper.ApplyAll(100m, [tenPercentOff, addTax, value => Math.Round(value, 0)])}");

Func<int> counter = LambdaHelper.MakeCounter();
counter();
counter();
print($"counter called 3 times -> {counter()}");

// Expression trees: the lambda as data.
List<ProductModel> products = [new ProductModel("Keyboard", 25m, 10), new ProductModel("Mouse", 12.5m, 0), new ProductModel("Monitor", 180m, 3)];
print($"condition as text: {LambdaHelper.Describe(product => product.Price > 20 && product.Stock > 0)}");
print($"matches: {string.Join(", ", LambdaHelper.Filter(products, product => product.Price > 20 && product.Stock > 0).Select(product => product.Name))}");
return;

static string ToShout(string text)
{
    return text.ToUpperInvariant();
}
