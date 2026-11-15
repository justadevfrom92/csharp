using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EfCoreQueryLogging.DbContexts;
using EfCoreQueryLogging.Models;

namespace EfCoreQueryLogging.Variations;

// "Every book with its author's name" - loaded six ways. Each request makes its OWN throw-away in-memory
// database with 5 books (the app's real database is not touched), counts the SQL commands, and returns JSON.
// Try /variations/loading/include, /n-plus-one, /explicit, /projection, /split, /filter-in-memory, /filter-in-sql.
[Route("variations/loading")]
public class BookLoadingVariation : Controller
{
    [HttpGet("include")]
    public async Task<IActionResult> WithInclude()
    {
        // ORIGINAL (BooksController): Include = one SQL query with a JOIN. Authors come back WITH their books.
        return await RunAsync("Include", async database =>
        {
            List<BookModel> books = await database.Books.Include(book => book.Author).OrderBy(book => book.Id).ToListAsync();
            return books.Select(book => $"{book.Title} by {book.Author.Name}").ToList();
        });
    }

    [HttpGet("n-plus-one")]
    public async Task<IActionResult> WithNPlusOne()
    {
        // No Include, then load each author separately inside the loop: 1 query for the books + 1 PER BOOK.
        // The classic "N+1 queries" problem: fine with 5 books, terrible with 5,000.
        return await RunAsync("N+1", async database =>
        {
            List<BookModel> books = await database.Books.AsNoTracking().OrderBy(book => book.Id).ToListAsync();
            var lines = new List<string>();
            foreach (BookModel book in books)
            {
                AuthorModel author = await database.Authors.AsNoTracking().SingleAsync(existing => existing.Id == book.AuthorId);
                lines.Add($"{book.Title} by {author.Name}");
            }
            return lines;
        });
    }

    [HttpGet("explicit")]
    public async Task<IActionResult> WithExplicitLoading()
    {
        // EXPLICIT loading: load the books, then decide LATER to load a navigation for one of them.
        // Useful when you only sometimes need the related data.
        return await RunAsync("Explicit loading", async database =>
        {
            List<BookModel> books = await database.Books.OrderBy(book => book.Id).ToListAsync();
            await database.Entry(books[0]).Reference(book => book.Author).LoadAsync();
            return books.Select(book => $"{book.Title} by {(book.Author?.Name ?? "(not loaded)")}").ToList();
        });
    }

    [HttpGet("projection")]
    public async Task<IActionResult> WithProjection()
    {
        // PROJECTION: Select only the two columns needed. One query, no Include, no tracking, least data sent.
        return await RunAsync("Projection (Select)", async database =>
        {
            return await database.Books.OrderBy(book => book.Id).Select(book => book.Title + " by " + book.Author.Name).ToListAsync();
        });
    }

    [HttpGet("split")]
    public async Task<IActionResult> WithSplitQuery()
    {
        // AsSplitQuery: one SQL query PER Include instead of one big JOIN. Avoids huge duplicated result sets
        // when including several collections (books x tags x ...). More round trips.
        return await RunAsync("AsSplitQuery", async database =>
        {
            List<BookModel> books = await database.Books.Include(book => book.Author).Include(book => book.Tags).AsSplitQuery().OrderBy(book => book.Id).ToListAsync();
            return books.Select(book => $"{book.Title} by {book.Author.Name} [{book.Tags.Count} tags]").ToList();
        });
    }

    [HttpGet("filter-in-memory")]
    public async Task<IActionResult> FilterInMemory()
    {
        // ToList() FIRST, then Where: every row is downloaded, then filtered in C#. Same answer, wasteful.
        return await RunAsync("Filter after ToList", async database =>
        {
            List<BookModel> all = await database.Books.AsNoTracking().ToListAsync();
            return all.Where(book => book.PublishedYear > 1900).Select(book => book.Title).ToList();
        });
    }

    [HttpGet("filter-in-sql")]
    public async Task<IActionResult> FilterInSql()
    {
        // Where BEFORE ToList: becomes "WHERE PublishedYear > 1900" in the SQL - only matching rows are sent.
        return await RunAsync("Filter in SQL", async database =>
        {
            IQueryable<string> query = database.Books.Where(book => book.PublishedYear > 1900).Select(book => book.Title);
            var titles = await query.ToListAsync();
            titles.Insert(0, "SQL: " + query.ToQueryString().Replace("\n", " "));
            return titles;
        });
    }

    private static async Task<IActionResult> RunAsync(string style, Func<LibraryDbContext, Task<List<string>>> load)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var counter = new CommandCountingVariation();
        DbContextOptions<LibraryDbContext> options = new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite(connection).AddInterceptors(counter).Options;

        await using (var setup = new LibraryDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync(); // also adds the seed: Frankenstein by Mary Shelley
            string[] names = ["Jules Verne", "H. G. Wells", "Isaac Asimov", "Ursula K. Le Guin"];
            string[] titles = ["Twenty Thousand Leagues", "The Time Machine", "Foundation", "The Dispossessed"];
            int[] years = [1870, 1895, 1951, 1974];
            for (int index = 0; index < names.Length; index++)
            {
                setup.Books.Add(new BookModel { Title = titles[index], PublishedYear = years[index], Author = new AuthorModel { Name = names[index] } });
            }
            await setup.SaveChangesAsync();
        }

        await using var database = new LibraryDbContext(options);
        counter.Commands = 0; // count only the variation's own queries
        List<string> lines = await load(database);
        return new JsonResult(new { style, sqlCommands = counter.Commands, trackedEntities = database.ChangeTracker.Entries().Count(), lines });
    }
}
