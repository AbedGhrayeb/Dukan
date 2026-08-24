using Dukan.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dukan.Web.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.StoreName)
            .HasMaxLength(150)
            .IsRequired()
            .IsUnicode(true);

        builder.Property(c => c.Phone)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.WhatsAppNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(c => c.Phone)
            .HasDatabaseName("IX_Customers_Phone");

        builder.HasIndex(c => c.WhatsAppNumber)
            .HasDatabaseName("IX_Customers_WhatsAppNumber");

        builder.HasMany(c => c.Subscriptions)
            .WithOne(s => s.Customer)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
