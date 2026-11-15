namespace ServiceRegistration.Services;

public interface IPriceService
{
    decimal GetPrice(int productId);
}
