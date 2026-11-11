using MinimalApiOpenApi.Models;

namespace MinimalApiOpenApi.Stores;

// In-memory books, shared by all requests (registered as a singleton).
public class BookStore
{
    private readonly List<BookModel> books =
    [
        new BookModel(1, "Frankenstein", "Mary Shelley", 1818),
        new BookModel(2, "Dracula", "Bram Stoker", 1897),
    ];
    private readonly object booksLock = new();

    public List<BookModel> GetAll()
    {
        lock (booksLock)
        {
            return books.ToList();
        }
    }

    public BookModel? FindById(int id)
    {
        lock (booksLock)
        {
            return books.FirstOrDefault(book => book.Id == id);
        }
    }

    public BookModel? Add(SaveBookRequestModel request)
    {
        lock (booksLock)
        {
            if (books.Any(book => book.Title.Equals(request.Title, StringComparison.OrdinalIgnoreCase)))
            {
                return null; // a book with that title already exists
            }
            int newId = books.Count == 0 ? 1 : books.Max(book => book.Id) + 1;
            var book = new BookModel(newId, request.Title, request.Author, request.Year);
            books.Add(book);
            return book;
        }
    }

    public bool Delete(int id)
    {
        lock (booksLock)
        {
            return books.RemoveAll(book => book.Id == id) > 0;
        }
    }
}
