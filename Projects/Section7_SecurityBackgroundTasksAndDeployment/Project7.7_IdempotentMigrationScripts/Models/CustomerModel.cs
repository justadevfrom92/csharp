namespace IdempotentMigrationScripts.Models;

public class CustomerModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    // Added in the 2nd migration. Nullable on purpose: old app versions that don't know this
    // column can still insert customers while servers are being upgraded one by one.
    public string? Email { get; set; }
}
