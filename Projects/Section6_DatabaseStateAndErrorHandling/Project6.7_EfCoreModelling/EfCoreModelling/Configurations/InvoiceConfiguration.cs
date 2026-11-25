using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreModelling.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<InvoiceModel>
{
    public void Configure(EntityTypeBuilder<InvoiceModel> builder)
    {
        builder.Property<int>("Id").ValueGeneratedOnAdd();
        builder.HasKey("Id");
        builder.HasIndex(invoice => invoice.PublicId).IsUnique();

        // The relationship goes through the PRIVATE field "linesCollection" (named as a string, since outside code
        // can't see it). "InvoiceId" is a SHADOW foreign key on the line.
        builder
            .HasMany<InvoiceLineModel>("linesCollection")
            .WithOne()
            .HasForeignKey("InvoiceId");

        // The public Lines property is only a read-only view of the same list: not a second relationship.
        builder.Ignore(invoice => invoice.Lines);

        // AutoInclude: load the lines EVERY time an invoice is loaded, without writing Include().
        // An aggregate is always used whole. Cost: list pages load every line too (see the README).
        builder
            .Navigation("linesCollection")
            .AutoInclude();
    }
}
