namespace DataflowImport.Models;

// The result of parsing one line: EITHER an order OR an error. Routing in the pipeline looks at which one.
public record ParsedLineModel(int LineNumber, ImportedOrderModel? Order, string? Error)
{
    public bool IsValid => Order != null;
}
