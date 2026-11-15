namespace ServiceRegistration.Repositories;

// Scoped: one list per HTTP request (it's a demo - a real one would use a DbContext, which is scoped too).
public class InMemoryRepository<T> : IRepository<T>
{
    private readonly List<T> items = new();

    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        items.Add(item);
    }

    public IReadOnlyList<T> GetAll()
    {
        return items.ToList();
    }
}
