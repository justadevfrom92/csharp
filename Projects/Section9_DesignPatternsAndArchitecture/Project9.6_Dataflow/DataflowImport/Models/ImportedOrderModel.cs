namespace DataflowImport.Models;

public record ImportedOrderModel(int LineNumber, string OrderId, string Customer, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}
