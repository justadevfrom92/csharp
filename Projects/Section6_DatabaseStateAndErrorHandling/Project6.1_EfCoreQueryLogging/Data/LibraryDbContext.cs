using Microsoft.EntityFrameworkCore;

namespace EfCoreQueryLogging.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>().Property(author => author.Name).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<Book>().Property(book => book.Title).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Tag>().HasIndex(tag => tag.Name).IsUnique();

        modelBuilder.Entity<Author>()
            .HasOne(author => author.Profile)
            .WithOne(profile => profile.Author)
            .HasForeignKey<AuthorProfile>(profile => profile.AuthorId);

        modelBuilder.Entity<Book>()
            .HasMany(book => book.Tags)
            .WithMany(tag => tag.Books);

        // Starting data, added by the first migration.
        modelBuilder.Entity<Author>().HasData(new Author { Id = 1, Name = "Mary Shelley" });
        modelBuilder.Entity<AuthorProfile>().HasData(new AuthorProfile { Id = 1, AuthorId = 1, Biography = "English novelist." });
        modelBuilder.Entity<Book>().HasData(new Book { Id = 1, Title = "Frankenstein", PublishedYear = 1818, AuthorId = 1 });
        modelBuilder.Entity<Tag>().HasData(new Tag { Id = 1, Name = "Classic" }, new Tag { Id = 2, Name = "Horror" });
    }
}
