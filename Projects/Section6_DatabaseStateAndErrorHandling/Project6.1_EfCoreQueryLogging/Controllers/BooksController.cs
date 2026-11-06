using EfCoreQueryLogging.Data;
using EfCoreQueryLogging.Data.Entities;
using EfCoreQueryLogging.Models;
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

    // GET /  - the list + an empty "Add book" form.
    public async Task<IActionResult> Index()
    {
        var page = new BookLibraryViewModel();
        await LoadBooksAndTagsAsync(page);
        return View(page);
    }

    // POST /Books/Create - model binding fills Title, PublishedYear, AuthorName and SelectedTagIds.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookLibraryViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await LoadBooksAndTagsAsync(form); // the lists are not posted, so load them again
            return View("Index", form);        // same page, now showing the error messages
        }

        string authorName = form.AuthorName.Trim();
        Author? author = await database.Authors.FirstOrDefaultAsync(existing => existing.Name == authorName);
        if (author == null)
        {
            author = new Author { Name = authorName };
        }

        List<Tag> tags = await database.Tags.Where(tag => form.SelectedTagIds.Contains(tag.Id)).ToListAsync();
        database.Books.Add(new Book { Title = form.Title.Trim(), PublishedYear = form.PublishedYear, Author = author, Tags = tags });
        await database.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadBooksAndTagsAsync(BookLibraryViewModel page)
    {
        page.Books = await database.Books
            .Include(book => book.Author)
            .ThenInclude(author => author.Profile)
            .Include(book => book.Tags)
            .AsNoTracking() // read-only list: no need for EF to track changes
            .OrderBy(book => book.Title)
            .ToListAsync();

        page.AllTags = await database.Tags.AsNoTracking().OrderBy(tag => tag.Name).ToListAsync();
    }
}
