using ProductCatalog.Data.Entities;

namespace ProductCatalog.Data;

// Simple in-memory list shared by every request. Data is lost when the app stops (no database yet).
public static class ProductStore
{
    public static readonly string[] Categories = ["Computers", "Furniture", "Lighting"];

    private static readonly List<Product> products =
    [
        new Product { Id = 1, Name = "Keyboard", Category = "Computers", Price = 25m, Description = "Mechanical keyboard" },
        new Product { Id = 2, Name = "Desk", Category = "Furniture", Price = 220m },
    ];

    private static readonly object listLock = new();

    public static List<Product> GetAll()
    {
        lock (listLock)
        {
            return products.ToList();
        }
    }

    public static Product? FindById(int id)
    {
        lock (listLock)
        {
            return products.FirstOrDefault(product => product.Id == id);
        }
    }

    public static void Add(Product product)
    {
        lock (listLock)
        {
            product.Id = products.Count == 0 ? 1 : products.Max(existing => existing.Id) + 1;
            products.Add(product);
        }
    }

    public static bool Update(Product changedProduct)
    {
        lock (listLock)
        {
            Product? existing = products.FirstOrDefault(product => product.Id == changedProduct.Id);
            if (existing == null)
            {
                return false;
            }
            existing.Name = changedProduct.Name;
            existing.Category = changedProduct.Category;
            existing.Price = changedProduct.Price;
            existing.Description = changedProduct.Description;
            return true;
        }
    }
}
