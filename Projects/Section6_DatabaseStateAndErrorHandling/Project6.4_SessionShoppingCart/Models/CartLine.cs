namespace SessionShoppingCart.Models;

public class CartLine
{
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
