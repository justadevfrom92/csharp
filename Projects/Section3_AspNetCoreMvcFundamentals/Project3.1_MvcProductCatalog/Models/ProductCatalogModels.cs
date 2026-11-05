using System.ComponentModel.DataAnnotations;

namespace MvcProductCatalog.Models;

public enum ProductCategory
{
    Electronics,
    Kitchen,
    Office,
    Outdoor,
    Books
}

/// <summary>The stored entity. Never bound directly from a form (prevents over-posting of Id/CreatedAt).</summary>
public sealed class CatalogProduct
{
    public int ProductId { get; init; }
    public required string ProductName { get; set; }
    public string? ProductDescription { get; set; }
    public decimal UnitPrice { get; set; }
    public ProductCategory Category { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailableForSale { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastModifiedAt { get; set; }
}

/// <summary>
/// Form-facing view model. Validation rules live here (not on the entity) because they describe
/// what a *user* may submit, which can differ from what the storage layer accepts.
/// </summary>
public sealed class ProductEditorFormViewModel
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Product name must be between 3 and 80 characters.")]
    [Display(Name = "Product name")]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description")]
    public string? ProductDescription { get; set; }

    [Required(ErrorMessage = "Unit price is required.")]
    [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "Unit price must be between 0.01 and 100,000.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Unit price")]
    public decimal? UnitPrice { get; set; }

    [Required(ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public ProductCategory? Category { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Stock must be between 0 and 1,000,000.")]
    [Display(Name = "Stock quantity")]
    public int StockQuantity { get; set; }

    [Display(Name = "Available for sale")]
    public bool IsAvailableForSale { get; set; } = true;

    public static ProductEditorFormViewModel FromProduct(CatalogProduct product) => new()
    {
        ProductName = product.ProductName,
        ProductDescription = product.ProductDescription,
        UnitPrice = product.UnitPrice,
        Category = product.Category,
        StockQuantity = product.StockQuantity,
        IsAvailableForSale = product.IsAvailableForSale
    };
}

public sealed record ProductListPageViewModel(
    IReadOnlyList<CatalogProduct> Products,
    string? SearchText,
    ProductCategory? CategoryFilter,
    decimal TotalInventoryValue);

public sealed record StatusAlertMessage(string AlertKind, string MessageText);
