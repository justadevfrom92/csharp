using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreModelling.Configurations;

// IEntityTypeConfiguration<T>: one entity's database setup in its own class, instead of a long OnModelCreating.
public class ProductConfiguration : IEntityTypeConfiguration<ProductModel>
{
    public void Configure(EntityTypeBuilder<ProductModel> builder)
    {
        // SHADOW key: a column "Id" that the C# class doesn't have. The database numbers it (1, 2, 3...).
        builder.Property<int>("Id").ValueGeneratedOnAdd();
        builder.HasKey("Id");
        builder.HasIndex(product => product.PublicId).IsUnique();

        // TABLE-PER-HIERARCHY: Materials and Services share the "Products" table; the "Type" column says which is which.
        builder
            .HasDiscriminator<string>("Type")
            .HasValue<MaterialModel>("Material")
            .HasValue<ServiceModel>("Service");
    }
}
