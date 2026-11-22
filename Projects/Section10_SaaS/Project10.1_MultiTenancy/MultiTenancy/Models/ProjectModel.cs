namespace MultiTenancy.Models;

// A database row that BELONGS to a tenant. TenantId is set by AppDbContext when saving, never by the client.
public class ProjectModel
{
    public int Id { get; set; }

    public string TenantId { get; set; } = "";

    public string Name { get; set; } = "";
}
