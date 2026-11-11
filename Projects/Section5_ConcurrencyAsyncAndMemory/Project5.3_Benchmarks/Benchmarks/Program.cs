using BenchmarkDotNet.Running;

// Lets you choose which benchmark to run from the command line, e.g.
//   dotnet run -c Release -- --filter "*Lookup*"
//   dotnet run -c Release -- --filter "*" --job short
// Benchmarks MUST run in Release: Debug builds aren't optimised, so the numbers would be meaningless.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
