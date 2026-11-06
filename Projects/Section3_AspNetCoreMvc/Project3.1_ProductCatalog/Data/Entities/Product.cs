namespace ProductCatalog.Data.Entities;

// A product as it is STORED. No validation attributes here: the form (ProductFormViewModel) checks input
// before anything is saved.
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public string? Description { get; set; }
}
