using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using PublishDemo.Models;
using PublishDemo.SerializerContexts;

namespace PublishDemo.Variations;

// Turning a model into JSON, three ways. The app (original) uses the SOURCE-GENERATED context, because trimming
// and Native AOT remove the code that reflection-based serialization needs.
public static class JsonSerializationVariation
{
    // REFLECTION (the default you get without thinking about it): looks at the type's properties at RUNTIME.
    // Works in a normal build; in a Native AOT app reflection-based serialization is switched off.
    // These attributes say "this method is NOT trim/AOT-safe". Without them the build WARNS (IL2026, IL3050) - the
    // analyzer that <IsAotCompatible> switches on caught exactly the problem this variation demonstrates.
    [RequiresUnreferencedCode("Reflection-based JSON: types may be trimmed away.")]
    [RequiresDynamicCode("Reflection-based JSON needs runtime code generation.")]
    public static string WithReflection(GreetingModel greeting)
    {
        return JsonSerializer.Serialize(greeting, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    // SOURCE GENERATION (original): the compiler wrote the serialization code at BUILD time for every type listed
    // with [JsonSerializable] in AppJsonSerializerContext. Same JSON, no reflection, AOT-safe, a bit faster.
    public static string WithSourceGeneration(GreetingModel greeting)
    {
        return JsonSerializer.Serialize(greeting, AppJsonSerializerContext.Default.GreetingModel);
    }

    // Options that ONLY know the source-generated types - what an AOT app effectively has. A type that is NOT
    // listed in the context (here a plain anonymous object) can't be serialized: it throws instead of guessing.
    [RequiresUnreferencedCode("Serializes an arbitrary runtime type.")]
    [RequiresDynamicCode("Serializes an arbitrary runtime type.")]
    public static string WithContextOnly(object value)
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = AppJsonSerializerContext.Default };
        return JsonSerializer.Serialize(value, value.GetType(), options);
    }
}
