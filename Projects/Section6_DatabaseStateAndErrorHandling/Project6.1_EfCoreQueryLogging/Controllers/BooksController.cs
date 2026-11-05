using EfCoreQueryLogging.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfCoreQueryLogging.Controllers;

public class BooksController : Controller
{
    private readonly LibraryDbContext database;

    public BooksController(LibraryDbContext database)
    {
        this.database = database;
    }

    public async Task<IActionResult> Index()
    {
        List<Book> books = await database.Books
            .Include(book => book.Author)
            .ThenInclude(author => author.Profile)
            .Include(book => book.Tags)
            .AsNoTracking() // read-only page: no need for EF to track changes
            .OrderBy(book => book.Title)
            .ToListAsync();

        ViewBag.Tags = await database.Tags.AsNoTracking().OrderBy(tag => tag.Name).ToListAsync();
        return View(books);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, int publishedYear, string authorName, int[] tagIds)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(authorName))
        {
            return RedirectToAction(nameof(Index));
        }

        Author? author = await database.Authors.FirstOrDefaultAsync(existing => existing.Name == authorName.Trim());
        if (author == null)
        {
            author = new Author { Name = authorName.Trim() };
        }

        List<Tag> tags = await database.Tags.Where(tag => tagIds.Contains(tag.Id)).ToListAsync();
        database.Books.Add(new Book { Title = title.Trim(), PublishedYear = publishedYear, Author = author, Tags = tags });
        await database.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
