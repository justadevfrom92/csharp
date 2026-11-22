using MultiTenancy.Models;

namespace MultiTenancy.Stores;

// The list of customers. In a real SaaS this is a "tenants" table filled by sign-up.
public class TenantStore
{
    private readonly Dictionary<string, TenantModel> tenants = new(StringComparer.OrdinalIgnoreCase)
    {
        ["acme"] = new TenantModel("acme", "Acme Ltd"),
        ["globex"] = new TenantModel("globex", "Globex Corp"),
    };

    public TenantModel? FindById(string id)
    {
        return tenants.GetValueOrDefault(id);
    }
}
