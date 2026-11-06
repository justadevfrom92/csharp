using EfCoreQueryLogging.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreQueryLogging.DbContexts;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<AuthorModel> Authors { get; set; } = null!;
    public DbSet<BookModel> Books { get; set; } = null!;
    public DbSet<TagModel> Tags { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthorModel>().Property(author => author.Name).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<BookModel>().Property(book => book.Title).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<TagModel>().HasIndex(tag => tag.Name).IsUnique();

        modelBuilder.Entity<AuthorModel>()
            .HasOne(author => author.Profile)
            .WithOne(profile => profile.Author)
            .HasForeignKey<AuthorProfileModel>(profile => profile.AuthorId);

        modelBuilder.Entity<BookModel>()
            .HasMany(book => book.Tags)
            .WithMany(tag => tag.Books)
            .UsingEntity(join => join.ToTable("BookTag"));

        // Tables are named after the DbSet properties (Authors, Books, Tags). AuthorProfileModel has no
        // DbSet, so EF would name its table "AuthorProfileModel" - keep the table name "AuthorProfile".
        modelBuilder.Entity<AuthorProfileModel>().ToTable("AuthorProfile");

        // Starting data, added by the first migration.
        modelBuilder.Entity<AuthorModel>().HasData(new AuthorModel { Id = 1, Name = "Mary Shelley" });
        modelBuilder.Entity<AuthorProfileModel>().HasData(new AuthorProfileModel { Id = 1, AuthorId = 1, Biography = "English novelist." });
        modelBuilder.Entity<BookModel>().HasData(new BookModel { Id = 1, Title = "Frankenstein", PublishedYear = 1818, AuthorId = 1 });
        modelBuilder.Entity<TagModel>().HasData(new TagModel { Id = 1, Name = "Classic" }, new TagModel { Id = 2, Name = "Horror" });
    }
}
