namespace EfCoreModelling.Models;

// The BASE of what an invoice can sell: a material or a service. Stored in ONE "Products" table with a
// "Type" column saying which kind each row is (table-per-hierarchy, see ProductConfiguration).
// No "Id" property: the database key is a SHADOW property that only EF knows about. Code uses PublicId instead,
// which is safe to show in URLs (a Guid can't be guessed like 1, 2, 3).
public abstract class ProductModel
{
    protected ProductModel(string name)
    {
        Name = name;
    }

    public Guid PublicId { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; }
}
