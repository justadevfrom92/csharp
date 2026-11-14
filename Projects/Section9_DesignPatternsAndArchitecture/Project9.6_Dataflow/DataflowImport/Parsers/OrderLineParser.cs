using System.Globalization;
using DataflowImport.Models;

namespace DataflowImport.Parsers;

// One CSV line: "orderId,customer,quantity,unitPrice", e.g. "A-1,Ann,2,25.00"
public static class OrderLineParser
{
    public static ParsedLineModel Parse(int lineNumber, string line)
    {
        string[] parts = (line ?? "").Split(',');
        if (parts.Length != 4)
        {
            return new ParsedLineModel(lineNumber, null, $"line {lineNumber}: expected 4 values, got {parts.Length}");
        }
        string orderId = parts[0].Trim();
        string customer = parts[1].Trim();
        if (orderId.Length == 0 || customer.Length == 0)
        {
            return new ParsedLineModel(lineNumber, null, $"line {lineNumber}: order id and customer are required");
        }
        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity) || quantity <= 0)
        {
            return new ParsedLineModel(lineNumber, null, $"line {lineNumber}: quantity must be a whole number above 0");
        }
        if (!decimal.TryParse(parts[3], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal unitPrice) || unitPrice <= 0)
        {
            return new ParsedLineModel(lineNumber, null, $"line {lineNumber}: price must be a number above 0");
        }
        return new ParsedLineModel(lineNumber, new ImportedOrderModel(lineNumber, orderId, customer, quantity, unitPrice), null);
    }
}
