namespace ServiceRegistration.Repositories;

// A GENERIC interface. One "open generic" registration covers IRepository<ProductModel>,
// IRepository<CustomerModel> and any other T:   AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>))
public interface IRepository<T>
{
    void Add(T item);

    IReadOnlyList<T> GetAll();
}
