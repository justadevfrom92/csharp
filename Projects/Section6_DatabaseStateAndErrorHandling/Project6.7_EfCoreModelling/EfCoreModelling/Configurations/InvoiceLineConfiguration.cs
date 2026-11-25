using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreModelling.Configurations;

// (The video nests this class inside InvoiceConfiguration; this repo keeps one class per file.)
public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLineModel>
{
    public void Configure(EntityTypeBuilder<InvoiceLineModel> builder)
    {
        builder.Property<int>("Id").ValueGeneratedOnAdd();
        builder.HasKey("Id");
    }
}
