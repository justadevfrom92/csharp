namespace ServiceRegistration.Variations;

// Getting a service OUT of the container - five methods with similar names.
//   GetService<T>()           -> null if T isn't registered
//   GetRequiredService<T>()   -> throws InvalidOperationException if T isn't registered
//   GetServices<T>()          -> ALL registrations of T (empty if none)
//   GetKeyedService<T>(key)   -> the one registered under that key (null if none)
//   ActivatorUtilities.CreateInstance<T>(provider) -> creates a class that is NOT registered, filling its
//                                constructor from the container (handy for one-off objects)
// Normal code doesn't call these at all: it asks for services in its CONSTRUCTOR. They're for factories,
// background loops (IServiceScopeFactory) and tests.
public static class ResolveVariation
{
    public static (object? Missing, int AllNotifiers, string Created) Demo(IServiceProvider provider)
    {
        object? missing = provider.GetService<INeverRegisteredVariation>();
        int allNotifiers = provider.GetServices<Notifiers.INotifier>().Count();
        var created = ActivatorUtilities.CreateInstance<UnregisteredReportVariation>(provider);
        return (missing, allNotifiers, created.Describe());
    }
}
