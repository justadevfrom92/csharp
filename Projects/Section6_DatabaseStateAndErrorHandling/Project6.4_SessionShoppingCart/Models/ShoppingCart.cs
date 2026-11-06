namespace SessionShoppingCart.Models;

public class ShoppingCart
{
    public List<CartLine> Lines { get; set; } = new();

    public decimal Total
    {
        get
        {
            return Lines.Sum(line => line.Price * line.Quantity);
        }
    }

    public void AddItem(ShopItem item)
    {
        CartLine? existingLine = Lines.FirstOrDefault(line => line.ItemId == item.Id);
        if (existingLine != null)
        {
            existingLine.Quantity++;
            return;
        }
        Lines.Add(new CartLine { ItemId = item.Id, Name = item.Name, Price = item.Price, Quantity = 1 });
    }

    public void RemoveItem(int itemId)
    {
        Lines.RemoveAll(line => line.ItemId == itemId);
    }
}
