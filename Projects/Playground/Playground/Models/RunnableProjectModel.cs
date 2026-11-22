namespace Playground.Models;

// One console project the playground can run. "AssemblyName" is the project's .dll name;
// "Args" are its command-line arguments (as in "dotnet run -- --demo").
public record RunnableProjectModel(string Number, string Title, string AssemblyName, string[] Args, string Note = "");
