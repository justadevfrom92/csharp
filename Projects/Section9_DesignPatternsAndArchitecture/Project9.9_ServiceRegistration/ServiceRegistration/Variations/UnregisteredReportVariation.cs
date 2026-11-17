using ServiceRegistration.Services;

namespace ServiceRegistration.Variations;

// NOT registered in the container. ActivatorUtilities.CreateInstance still builds it, filling the constructor
// (GreetingService) from the container.
public class UnregisteredReportVariation
{
    private readonly GreetingService greeting;

    public UnregisteredReportVariation(GreetingService greeting)
    {
        this.greeting = greeting;
    }

    public string Describe()
    {
        return greeting.Greet("report reader");
    }
}
