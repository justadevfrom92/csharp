using PublishDemo.Endpoints;
using PublishDemo.SerializerContexts;

// CreateSlimBuilder: a smaller starting set of features, made for trimming and Native AOT.
var builder = WebApplication.CreateSlimBuilder(args);

// Use the source-generated JSON code (see SerializerContexts/AppJsonSerializerContext.cs).
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});
builder.Services.AddProblemDetails();

var app = builder.Build();
app.MapDemoEndpoints();
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
