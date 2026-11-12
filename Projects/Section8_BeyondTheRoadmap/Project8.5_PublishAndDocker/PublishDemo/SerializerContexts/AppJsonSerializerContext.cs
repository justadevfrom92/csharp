using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using PublishDemo.Models;

namespace PublishDemo.SerializerContexts;

// SOURCE-GENERATED JSON. Normal System.Text.Json uses reflection at runtime, which Native AOT and trimming
// can't fully support. This tells the compiler which types we turn into JSON, and it writes that code at build time.
[JsonSerializable(typeof(RuntimeInfoModel))]
[JsonSerializable(typeof(GreetingModel))]
[JsonSerializable(typeof(ProblemDetails))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
