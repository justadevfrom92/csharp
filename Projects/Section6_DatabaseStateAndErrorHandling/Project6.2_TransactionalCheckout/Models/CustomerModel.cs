namespace TransactionalCheckout.Models;

public class CustomerModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal WalletBalance { get; set; }
}
