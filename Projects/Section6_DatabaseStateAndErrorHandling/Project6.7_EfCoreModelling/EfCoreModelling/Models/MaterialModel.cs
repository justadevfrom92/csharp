namespace EfCoreModelling.Models;

// A physical product, sold by an amount of its Unit (kg, m, pcs).
public sealed class MaterialModel : ProductModel
{
    public MaterialModel(string name, string unit) : base(name)
    {
        Unit = unit;
    }

    public string Unit { get; private set; }
}
