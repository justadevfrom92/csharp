using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PublishDemo.Models;
using PublishDemo.Variations;

namespace PublishDemo.Tests;

// Reflection vs source-generated JSON (PublishDemo/Variations/JsonSerializationVariation.cs).
public class VariationsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public VariationsTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void Json_PositivePositive_ReflectionAndSourceGeneration_WriteTheSameValue()
    {
        var greeting = new GreetingModel("Hello");

        using JsonDocument reflection = JsonDocument.Parse(JsonSerializationVariation.WithReflection(greeting));
        using JsonDocument generated = JsonDocument.Parse(JsonSerializationVariation.WithSourceGeneration(greeting));

        Assert.Equal("Hello", reflection.RootElement.EnumerateObject().Single().Value.GetString());
        Assert.Equal("Hello", generated.RootElement.EnumerateObject().Single().Value.GetString());
    }

    [Fact]
    public async Task Json_PositiveNegative_PropertyNameCasing_DependsOnWhichOptionsAreUsed()
    {
        var greeting = new GreetingModel("Hello");
        string fromEndpoint = await factory.CreateClient().GetStringAsync("/greet?name=Ann");

        Assert.Equal("{\"message\":\"Hello\"}", JsonSerializationVariation.WithReflection(greeting));       // Web defaults: camelCase
        Assert.Equal("{\"Message\":\"Hello\"}", JsonSerializationVariation.WithSourceGeneration(greeting)); // the context's own options: names as written
        Assert.StartsWith("{\"message\":", fromEndpoint);                                                  // the app: context inside the Web options -> camelCase
    }

    [Fact]
    public void Json_NegativeNegative_TypeNotInTheContext_CannotBeSerializedWithoutReflection()
    {
        Assert.Equal("{\"Message\":\"Hi\"}", JsonSerializationVariation.WithContextOnly(new GreetingModel("Hi")));
        Assert.Throws<NotSupportedException>(() => JsonSerializationVariation.WithContextOnly(new { Unknown = 1 }));
    }
}
