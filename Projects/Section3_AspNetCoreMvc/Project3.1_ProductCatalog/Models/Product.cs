using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be 2 to 50 characters.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Pick a category.")]
    public string Category { get; set; } = "";

    [Range(0.01, 100000, ErrorMessage = "Price must be between 0.01 and 100,000.")]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
