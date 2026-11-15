namespace ServiceRegistration.Services;

// Gets a new random id when it's CREATED. Comparing ids shows when DI makes a new object:
//   singleton: same id for the whole app;  scoped: same within one request;  transient: new every time.
public class LifetimeProbeService
{
    public Guid Id { get; } = Guid.NewGuid();
}
