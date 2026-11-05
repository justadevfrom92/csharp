using MvcProductCatalog.Models;

namespace MvcProductCatalog.Services;

public interface IProductCatalogRepository
{
    IReadOnlyList<CatalogProduct> SearchProducts(string? searchText, ProductCategory? categoryFilter);
    CatalogProduct? FindProductById(int productId);
    CatalogProduct CreateProduct(ProductEditorFormViewModel productForm);
    bool TryUpdateProduct(int productId, ProductEditorFormViewModel productForm);
    bool TryDeleteProduct(int productId);
    bool IsProductNameTaken(string productName, int? excludedProductId = null);
}
