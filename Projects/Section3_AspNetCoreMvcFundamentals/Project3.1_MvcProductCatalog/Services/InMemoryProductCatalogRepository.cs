using MvcProductCatalog.Models;

namespace MvcProductCatalog.Services;

/// <summary>
/// Thread-safe in-memory store registered as a Singleton. Concurrent requests share one instance,
/// so every read/write goes through a single lock; the data set is tiny, so contention is negligible
/// and correctness is simpler to reason about than with lock-free collections.
/// </summary>
public sealed class InMemoryProductCatalogRepository : IProductCatalogRepository
{
    private readonly Lock _catalogLock = new();
    private readonly Dictionary<int, CatalogProduct> _productsById = new();
    private readonly TimeProvider _timeProvider;
    private int _lastIssuedProductId;

    public InMemoryProductCatalogRepository(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        SeedInitialProducts();
    }

    public IReadOnlyList<CatalogProduct> SearchProducts(string? searchText, ProductCategory? categoryFilter)
    {
        lock (_catalogLock)
        {
            IEnumerable<CatalogProduct> matchingProducts = _productsById.Values;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                matchingProducts = matchingProducts.Where(product =>
                    product.ProductName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || (product.ProductDescription?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            if (categoryFilter is ProductCategory category)
            {
                matchingProducts = matchingProducts.Where(product => product.Category == category);
            }
            return matchingProducts.OrderBy(product => product.ProductName).ToList();
        }
    }

    public CatalogProduct? FindProductById(int productId)
    {
        lock (_catalogLock)
        {
            return _productsById.GetValueOrDefault(productId);
        }
    }

    public CatalogProduct CreateProduct(ProductEditorFormViewModel productForm)
    {
        lock (_catalogLock)
        {
            var newProduct = new CatalogProduct
            {
                ProductId = ++_lastIssuedProductId,
                ProductName = productForm.ProductName.Trim(),
                ProductDescription = productForm.ProductDescription?.Trim(),
                UnitPrice = productForm.UnitPrice ?? 0m,
                Category = productForm.Category ?? ProductCategory.Electronics,
                StockQuantity = productForm.StockQuantity,
                IsAvailableForSale = productForm.IsAvailableForSale,
                CreatedAt = _timeProvider.GetUtcNow()
            };
            _productsById.Add(newProduct.ProductId, newProduct);
            return newProduct;
        }
    }

    public bool TryUpdateProduct(int productId, ProductEditorFormViewModel productForm)
    {
        lock (_catalogLock)
        {
            if (!_productsById.TryGetValue(productId, out CatalogProduct? existingProduct))
            {
                return false;
            }
            existingProduct.ProductName = productForm.ProductName.Trim();
            existingProduct.ProductDescription = productForm.ProductDescription?.Trim();
            existingProduct.UnitPrice = productForm.UnitPrice ?? existingProduct.UnitPrice;
            existingProduct.Category = productForm.Category ?? existingProduct.Category;
            existingProduct.StockQuantity = productForm.StockQuantity;
            existingProduct.IsAvailableForSale = productForm.IsAvailableForSale;
            existingProduct.LastModifiedAt = _timeProvider.GetUtcNow();
            return true;
        }
    }

    public bool TryDeleteProduct(int productId)
    {
        lock (_catalogLock)
        {
            return _productsById.Remove(productId);
        }
    }

    public bool IsProductNameTaken(string productName, int? excludedProductId = null)
    {
        lock (_catalogLock)
        {
            return _productsById.Values.Any(product =>
                product.ProductId != excludedProductId
                && string.Equals(product.ProductName, productName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    private void SeedInitialProducts()
    {
        CreateProduct(new ProductEditorFormViewModel { ProductName = "Wireless Mouse", ProductDescription = "Ergonomic 2.4 GHz mouse.", UnitPrice = 24.99m, Category = ProductCategory.Electronics, StockQuantity = 120 });
        CreateProduct(new ProductEditorFormViewModel { ProductName = "Cast Iron Skillet", ProductDescription = "Pre-seasoned 26 cm pan.", UnitPrice = 39.50m, Category = ProductCategory.Kitchen, StockQuantity = 35 });
        CreateProduct(new ProductEditorFormViewModel { ProductName = "Hardcover Notebook", ProductDescription = "A5, 192 dotted pages.", UnitPrice = 12.00m, Category = ProductCategory.Office, StockQuantity = 400 });
        CreateProduct(new ProductEditorFormViewModel { ProductName = "Trail Headlamp", ProductDescription = "USB-C rechargeable, 400 lumens.", UnitPrice = 29.95m, Category = ProductCategory.Outdoor, StockQuantity = 0, IsAvailableForSale = false });
    }
}
