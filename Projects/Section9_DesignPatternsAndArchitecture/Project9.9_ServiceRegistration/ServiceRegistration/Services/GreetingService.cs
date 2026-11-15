namespace ServiceRegistration.Services;

// Takes a plain string in its constructor - DI can't guess that, so it's registered with a FACTORY.
public class GreetingService
{
    private readonly string shopName;

    public GreetingService(string shopName)
    {
        this.shopName = shopName;
    }

    public string Greet(string customer)
    {
        return $"Welcome to {shopName}, {customer}!";
    }
}
