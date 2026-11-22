namespace MultiTenancy.Models;

// A TENANT = one customer company using the app. Every piece of their data carries their Id.
public record TenantModel(string Id, string Name);
